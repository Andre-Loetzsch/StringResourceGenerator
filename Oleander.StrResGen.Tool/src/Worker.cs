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

    public void Generate(string inputFileName, string nameSpace)
    {
        this._logger.LogInformation("Generate inputFileName='{inputFileName}' nameSpace='{nameSpace}'", inputFileName, nameSpace);

        this._generator.Generate(inputFileName, nameSpace);
    }

    public void Generate(string projectFileName, string inputFileName, string nameSpace)
    {
        this._logger.LogInformation("Generate projectFileName='{projectFileName}', inputFileName='{inputFileName}' nameSpace='{nameSpace}'", projectFileName, inputFileName, nameSpace);
        this._generator.Generate(projectFileName, inputFileName, nameSpace);
    }
}