using System;
using System.CommandLine;
using System.CommandLine.IO;
using System.Diagnostics;
using System.IO;
using System.IO.Enumeration;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.VisualBasic.FileIO;
using Oleander.Extensions.Configuration;
using Oleander.Extensions.Logging;
using Oleander.Extensions.Logging.Abstractions;
using Oleander.Extensions.Logging.Providers;
using LoggerFactory = Oleander.Extensions.Logging.Abstractions.LoggerFactory;


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
        var resGen = host.Services.GetRequiredService<ResGen>();
        var rootCommand = new RootCommand("String resources tool");
        
        
        var generateCommand = new GenerateCommand(resGen);


        rootCommand.AddCommand(generateCommand);



        //var toolConsole = new ToolConsole(logger);

        //rootCommand.Invoke("run --file \"D:\\dev\\git\\oleander\\StringResourceGenerator\\README.md\" --namespace OLE", new ToolConsole(logger));
        //rootCommand.Invoke("generate --file \"D:\\dev\\git\\oleander\\StringResourceGenerator\\Oleander.StrResGen.Tool\\src\\Oleander.StrResGen.Tool.SR.strings\" --namespace Oleander.Test.Resource --projfile \"D:\\dev\\git\\oleander\\StringResourceGenerator\\Oleander.StrResGen.Tool\\src\\Oleander.StrResGen.Tool.csproj\"");

        //rootCommand.Invoke("new --file \"D:\\dev\\git\\oleander\\StringResourceGenerator\\README.md\" --namespace OLE");




        //rootCommand.Invoke(args, toolConsole);
        rootCommand.Invoke(args);





        //toolConsole.Flush();
        host.WaitForLogging(TimeSpan.FromSeconds(3));

        //host.WaitForShutdown();

        //Thread.Sleep(15000);
    }



}

internal class GenerateCommand : Command
{
    private readonly ResGen _resGen;

    public GenerateCommand(ResGen resGen) : base("generate", "Generate resource file.")
    {
        this._resGen = resGen;

        var fileOption = CreateFileOption();
        var projFileOption = CreateProjFileOption();
        var namespaceOption = CreateNameSpaceOption();

        this.AddOption(fileOption);
        this.AddOption(projFileOption);
        this.AddOption(namespaceOption);

        this.SetHandler((file, projFile, nameSpace) => 
        {
            if (File.Exists(projFile.FullName))
            {
                this.ResGenGenerate(projFile.FullName, file.FullName, nameSpace);
                return;
            }

            this.ResGenGenerate(file.FullName, nameSpace);

        }, fileOption, projFileOption, namespaceOption);
    }


    private static Option<FileInfo> CreateFileOption()
    {
        var option = new Option<FileInfo>(name: "--file", description: "The file to generate.");//.ExistingOnly();
        
        option.AddAlias("--f");
        option.IsRequired = true;
        option.AddValidator(result =>
        {
            var fullName = result.GetValueOrDefault<FileInfo>()?.FullName;

            if (fullName == null)
            {
                result.ErrorMessage = "File name is necessary!";
                return;
            }

            if (!string.Equals(Path.GetExtension(fullName), ".strings"))
            {
                result.ErrorMessage = $"File must have '*.strings' extension ({fullName})";
            }
        });

        return option;
    }

    private static Option<FileInfo> CreateProjFileOption()
    {
        var option = new Option<FileInfo>(name: "--projfile", description: "The project file to update the resource file.").ExistingOnly();

        option.AddAlias("--p");
        
        option.AddValidator(result =>
        {
            var fullName = result.GetValueOrDefault<FileInfo>()?.FullName;

            if (fullName == null) return;

            if (!string.Equals(Path.GetExtension(fullName), ".csproj"))
            {
                result.ErrorMessage = $"Invalid project file: '{fullName}'";
            }
        });

        return option;
    }

    private static Option<string> CreateNameSpaceOption()
    {
        var option = new Option<string>(name: "--namespace", description: "The custom tool namespace");

        option.AddAlias("--n");

        return option;
    }


    private void ResGenGenerate(string inputFileName, string nameSpace)
    {
        try
        {
            this._resGen.Generate(inputFileName, nameSpace);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
           
        }
    }

    private void ResGenGenerate(string projectFileName, string inputFileName, string nameSpace)
    {
        try
        {
            this._resGen.Generate(projectFileName, inputFileName, nameSpace);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

}








public class ToolConsole : IConsole
{
    private readonly ILogger _logger;

    public ToolConsole(ILogger logger)
    {
        this._logger = logger;
        this.Out = new StreamWriter();
        this.Error = new StreamWriter();
    }

    public IStandardStreamWriter Out { get; }
    public IStandardStreamWriter Error { get; }

    public bool IsOutputRedirected { get; } = true;

    public bool IsErrorRedirected { get; } = false;
    public bool IsInputRedirected { get; } = false;

    public void Flush()
    {
        this._logger.LogError(((StreamWriter)this.Error).ToString());
        this._logger.LogInformation(((StreamWriter)this.Out).ToString());
    }
}


public class StreamWriter : IStandardStreamWriter
{
    private readonly StringBuilder _sb = new();

    public void Write(string? value)
    {
        this._sb.Append(value);
    }

    public override string ToString()
    {
        var value = this._sb.ToString();
        this._sb.Clear();
        return value;
    }
}

