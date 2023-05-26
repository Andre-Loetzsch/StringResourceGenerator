namespace Oleander.StrResGen.SingleFileGenerator.ExternalProcesses
{
    public class UpdateDotnetToolProcess : ExternalProcess
    {
        public UpdateDotnetToolProcess()
            : base("dotnet", "tool update dotnet-oleander-strresgen-tool -g --prerelease")
        {
        }
    }
}