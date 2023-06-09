﻿using System;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Parsing;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Oleander.Extensions.Configuration;
using Oleander.Extensions.Logging;
using Oleander.Extensions.Logging.Abstractions;
using Oleander.Extensions.Logging.Providers;
using Oleander.StrResGen.Tool.Commands;
using Oleander.StrResGen.Tool.Options;

namespace Oleander.StrResGen.Tool;

internal class Program
{
    public static async Task<int> Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddJsonFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appConfiguration.json"), true, true);

        var serviceProvider = builder.Services.BuildServiceProvider();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();

        builder.Services.AddSingleton<ResGen>()
            .AddSingleton((IConfigurationRoot)configuration)
            .Configure<ConfiguredTypes>(configuration.GetSection("types"))
            .TryAddSingleton(typeof(IConfiguredTypesOptionsMonitor<>), typeof(ConfiguredTypesOptionsMonitor<>));

        builder.Logging
            .ClearProviders()
            .AddConfiguration(builder.Configuration.GetSection("Logging"))
            .Services.TryAddEnumerable(ServiceDescriptor.Singleton<ILoggerProvider, LoggerSinkProvider>());

        var host = builder.Build();

        host.Services.InitLoggerFactory();

        var logger = host.Services.GetRequiredService<ILoggerFactory>().CreateLogger<Program>();
        var console = new ToolConsole(logger);
        var resGen = host.Services.GetRequiredService<ResGen>();
        var rootCommand = new RootCommand("String resources tool");
        var commandLine = new CommandLineBuilder(rootCommand)
            .UseDefaults() // automatically configures dotnet-suggest
            .Build();

        TabCompletions.Logger = logger;

        rootCommand.AddCommand(new GenerateCommand(logger, resGen));
        rootCommand.AddCommand(new NewCommand(logger, resGen));

        var exitCode = await commandLine.InvokeAsync(args, console);

        console.Flush();

        const string logMsg = "StrResGen '{args}' exit with exit code {exitCode}";

        var arguments = string.Join(" ", args);

        if (exitCode == 0)
        {
            logger.LogInformation(logMsg, arguments, exitCode);
            
            if (!arguments.StartsWith("[suggest:"))
            {
                MSBuildLogFormatter.CreateMSBuildMessage("SRG0", $"StrResGen {exitCode}", "Main");
            }
        }
        else
        {
            logger.LogError(logMsg, arguments, exitCode);
        }

        await host.WaitForLoggingAsync(TimeSpan.FromSeconds(5));
        return exitCode;
    }
}