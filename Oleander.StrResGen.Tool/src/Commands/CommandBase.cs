using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using Oleander.Extensions.Logging.Abstractions;

namespace Oleander.StrResGen.Tool.Commands;

internal abstract class CommandBase : Command
{
    private readonly ILogger _logger;
    private readonly ResGen _resGen;

    protected CommandBase(ILogger logger, ResGen resGen, string name, string description) : base(name, description)
    {
        this._logger = logger;
        this._resGen = resGen;
    }

    protected int ResGenGenerate(IEnumerable<FileInfo> inputFileNames, string nameSpace)
    {
        try
        {
            return this._resGen.Generate(inputFileNames.Select(x => x.FullName), nameSpace);
        }
        catch (Exception ex)
        {
            MSBuildLogFormatter.CreateMSBuildError(1, ex.Message, "Oleander.StrResGen.Tool");
            this._logger.LogError("{exception}", ex.GetAllMessages());
            return 1;
        }
    }

    protected int ResGenGenerate(FileInfo projectFileInfo, IEnumerable<FileInfo> inputFileNames, string nameSpace)
    {
        try
        {
            return this._resGen.Generate(projectFileInfo.FullName, inputFileNames.Select(x => x.FullName), nameSpace);
        }
        catch (Exception ex)
        {
            MSBuildLogFormatter.CreateMSBuildError(1, ex.Message, "Oleander.StrResGen.Tool");
            this._logger.LogError("{exception}", ex.GetAllMessages());
            return 1;
        }
    }
}