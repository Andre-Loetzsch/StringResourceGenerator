using System.IO;

namespace Oleander.StrResGen.SingleFileGenerator.ExternalProcesses
{
    public class ExternalProcessResult
    {
        public ExternalProcessResult(string exeFileName)
        {
            this.ExeFileName = Path.GetFileName(exeFileName);
        }

        public string ExeFileName { get; }
        public Win32ExitCodes Win32ExitCode { get; internal set; } = Win32ExitCodes.ERROR_SUCCESS;
        public int ExitCode { get; internal set; }
        public string StandardOutput { get; internal set; }
        public string StandardErrorOutput { get; internal set; }
    }
}