using System;
using System.CommandLine;
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


namespace Oleander.StrResGen.Tool;

internal class Program
{
    public static void Main(string[] args)
    {

        var builder = Host.CreateApplicationBuilder();
        builder.Configuration.AddJsonFile("appConfiguration.json", false, true);

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

        logger.LogInformation("CmdLine: {args}", string.Join(" ", args));

        var console = new ToolConsole(logger);
        var resGen = host.Services.GetRequiredService<ResGen>();
        var rootCommand = new RootCommand("String resources tool");

        rootCommand.AddCommand(new GenerateCommand(resGen));
        rootCommand.AddCommand(new NewCommand(resGen));
       
        rootCommand.Invoke(args, console);
        host.WaitForLogging(TimeSpan.FromSeconds(3));

        console.Flush();

        host.WaitForLogging(TimeSpan.FromSeconds(3));
        //host.WaitForShutdown();
    }
}