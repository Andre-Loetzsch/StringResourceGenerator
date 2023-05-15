namespace Oleander.StrResGen.SingleFileGenerator.ExternalProcesses
{
    public class StrResGenProcess : ExternalProcess
    {
        public StrResGenProcess(string stringResFile) 
            : base("strresgen", $"generate --file {stringResFile}")
        {
        }

        public StrResGenProcess(string stringResFile, string fileNamespace)
            : base("strresgen", $"generate --file {stringResFile} --namespace {fileNamespace}")
        {
        }
    }
}