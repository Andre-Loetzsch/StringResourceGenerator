using System.CommandLine;

namespace Oleander.StrResGen.Tool.Options;

internal class NameSpaceOption : Option<string>
{
    public NameSpaceOption() : base("--namespace", "The namespace of the resource")
    {
        this.AddAlias("-n");
    }
}