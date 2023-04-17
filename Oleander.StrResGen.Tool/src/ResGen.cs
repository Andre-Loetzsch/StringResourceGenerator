using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Oleander.StrResGen.Tool;

public class ResGen
{
    private readonly ILogger<ResGen> _logger;
    private readonly ResourceGenerator _generator = new();

    public ResGen(ILogger<ResGen> logger)
    {
        this._logger = logger;
    }

    public int Generate(string inputFileName, string nameSpace)
    {
        this._logger.LogInformation("Generate inputFileName='{inputFileName}' nameSpace='{nameSpace}'", inputFileName, nameSpace);
        return this._generator.Generate(inputFileName, nameSpace);
    }

    public int Generate(string projectFileName, string inputFileName, string nameSpace)
    {
        this._logger.LogInformation("Generate projectFileName='{projectFileName}', inputFileName='{inputFileName}' nameSpace='{nameSpace}'", projectFileName, inputFileName, nameSpace);
        return this._generator.Generate(projectFileName, inputFileName, nameSpace);
    }

    public int Generate(IEnumerable<string> inputFileNames, string nameSpace)
    {
        var fileNames = inputFileNames.ToList();
        this._logger.LogInformation("Generate inputFileNames='{inputFileName}' nameSpace='{nameSpace}'", string.Join(Environment.NewLine, fileNames), nameSpace);
        return this._generator.Generate(fileNames, nameSpace);
    }

    public int Generate(string projectFileName, IEnumerable<string> inputFileNames, string nameSpace)
    {
        var fileNames = inputFileNames.ToList();
        this._logger.LogInformation("Generate projectFileName='{projectFileName}', inputFileName='{inputFileNames}' nameSpace='{nameSpace}'", projectFileName, string.Join(Environment.NewLine, fileNames), nameSpace);
        return this._generator.Generate(projectFileName, fileNames, nameSpace);
    }
}