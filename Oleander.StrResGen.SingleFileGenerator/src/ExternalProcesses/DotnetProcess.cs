namespace Oleander.StrResGen.SingleFileGenerator.ExternalProcesses
{
    public class DotnetProcess : ExternalProcess
    {
        public DotnetProcess()
            : base("dotnet", "tool install dotnet-oleander-strresgen-tool -g --prerelease")
        {
        }
    }
}