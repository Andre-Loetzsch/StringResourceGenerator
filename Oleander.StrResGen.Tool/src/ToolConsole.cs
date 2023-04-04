using System.CommandLine;
using System.CommandLine.IO;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Oleander.StrResGen.Tool;

public class ToolConsole : IConsole
{
    private readonly ILogger _logger;
    private readonly StringBuilder _sbOut = new();
    private readonly StringBuilder _sbError = new();


    public ToolConsole(ILogger logger)
    {
        this._logger = logger;
        this.Out =   new StreamWriter(this._sbOut);
        this.Error = new StreamWriter(this._sbError);
    }

    public IStandardStreamWriter Out { get; }
    public IStandardStreamWriter Error { get; }

    public bool IsOutputRedirected { get; } = true;

    public bool IsErrorRedirected { get; } = false;
    public bool IsInputRedirected { get; } = false;

    public void Flush()
    {

        if (this._sbError.Length > 0)
        {
            this._logger.LogError(this._sbError.ToString());
            this._sbError.Clear();
        }

        if (this._sbOut.Length > 0)
        {
            this._logger.LogInformation(this._sbOut.ToString());
            this._sbOut.Clear().Clear();
        }
    }
}