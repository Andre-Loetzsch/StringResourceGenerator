namespace Oleander.StrResGen.SingleFileGenerator.ExternalProcesses
{
    public class InstallDotnetToolProcess : ExternalProcess
    {
        public InstallDotnetToolProcess()
            : base("dotnet", "tool install dotnet-oleander-strresgen-tool -g --prerelease")
        {
        }
    }
}