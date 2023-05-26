namespace Oleander.StrResGen.SingleFileGenerator.ExternalProcesses
{
    public class ListDotnetToolProcess : ExternalProcess
    {
        public ListDotnetToolProcess()
            : base("dotnet", "tool list -g")
        {
        }
    }
}