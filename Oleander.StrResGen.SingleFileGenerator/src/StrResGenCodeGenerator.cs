using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.VisualStudio.TextTemplating.VSHost;
using Oleander.StrResGen.SingleFileGenerator.ExternalProcesses;

namespace Oleander.StrResGen.SingleFileGenerator
{
    [Guid("8C3E490C-C096-4336-B687-1162F34386E4")]
    public sealed class StrResGenCodeGenerator : BaseCodeGeneratorWithSite
    {
        private string _defaultExtension = ".cs";

        public int WarnLevel { get; set; }
        public int ErrorLevel { get; set; }
        public string Message { get; set; }

        public ExternalProcessResult ExternalProcessResult { get; set; }

        public override string GetDefaultExtension()
        {
            return this._defaultExtension;
        }

        protected override byte[] GenerateCode(string inputFileName, string inputFileContent)
        {
            var result = this.GenerateCSharpCode(inputFileName, this.FileNamespace);
            return result == null ? null : Encoding.UTF8.GetBytes(result);
        }

        public string GenerateCSharpCode(string inputFileName, string fileNamespace)
        {
            if (Path.GetExtension(inputFileName) != ".strings")
            {
                this.CreateError(1, $"File must have '*.strings' extension! ({inputFileName})");
                return null;
            }

            if (string.IsNullOrEmpty(fileNamespace))
            {
                this.CreateWarning(1, "File namespace is null or empty!");
            }

            this.ExternalProcessResult = (string.IsNullOrEmpty(fileNamespace) ? 
                new StrResGenProcess(inputFileName) : 
                new StrResGenProcess(inputFileName, fileNamespace)).Start();

            if (this.ExternalProcessResult.Win32ExitCode == Win32ExitCodes.ERROR_FILE_NOT_FOUND)
            {
                this.CreateWarning(2, "Dotnet tool 'dotnet-oleander-strresgen-tool' not found! Try to install missing dependencies.");

                this.ExternalProcessResult = new DotnetProcess().Start();

                if (this.ExternalProcessResult.ExitCode == 0)
                {
                    this.ExternalProcessResult = (string.IsNullOrEmpty(fileNamespace) ?
                        new StrResGenProcess(inputFileName) :
                        new StrResGenProcess(inputFileName, fileNamespace)).Start();
                }
            }

            if (this.ExternalProcessResult.ExitCode != 0)
            {
                if (this.ExternalProcessResult.Win32ExitCode == Win32ExitCodes.ERROR_SUCCESS)
                {
                    this.CreateError(2, $"Process {this.ExternalProcessResult.ExeFileName} failed!  ERROR{this.ExternalProcessResult.ExitCode} {this.ExternalProcessResult.StandardErrorOutput}");
                }
                else
                {
                    this.CreateError(3, $"Process {this.ExternalProcessResult.ExeFileName} failed!  {this.ExternalProcessResult.Win32ExitCode} {this.ExternalProcessResult.StandardErrorOutput}");
                }

                return null;
            }

            var csFile = string.Concat(inputFileName.Substring(0, inputFileName.Length - 8), ".cs");
            return File.Exists(csFile) ? File.ReadAllText(csFile) : null;
        }


        public void CreateWarning(int level, string message)
        {
            this.WarnLevel = level;
            this.Message = message;
            this.GeneratorErrorCallback(true, level, message, -1, -1);
        }

        public void CreateError(int level, string message)
        {
            this.ErrorLevel = level;
            this.Message = message;

            this._defaultExtension = null;
            this.GeneratorErrorCallback(false, level, message, -1, -1);
        }
    }
}