using System.CommandLine.IO;
using System.Text;

namespace Oleander.StrResGen.Tool;

public class StreamWriter : IStandardStreamWriter
{
    private readonly StringBuilder _sb;

    public StreamWriter(StringBuilder sb)
    {
        this._sb = sb;
    }

    public void Write(string? value)
    {
        this._sb.Append(value);
    }
}