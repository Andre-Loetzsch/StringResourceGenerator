using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.VisualStudio.TextTemplating.VSHost;
using Oleander.StrResGen.SingleFileGenerator.ExternalProcesses;

namespace Oleander.StrResGen.SingleFileGenerator
{
    [Guid("8C3E490C-C096-4336-B687-1162F34386E4")]
    public sealed class StrResGenCodeGenerator : BaseCodeGeneratorWithSite
    {
        public int WarnLevel { get; private set; }
        public int ErrorLevel { get; private set; }
        public string Message { get; private set; }

        public ExternalProcessResult ExternalProcessResult { get; private set; }

        private readonly List<string> _errors = new List<string>();
        private readonly List<string> _warnings = new List<string>();
        private bool _updateDotnetToolDone;
        private bool _isDotnetToolInstalled;

        public override string GetDefaultExtension()
        {
            return ".cs";
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

            this._isDotnetToolInstalled = this._isDotnetToolInstalled || IsDotnetToolInstalled;

            if (!this._isDotnetToolInstalled)
            {
                this.CreateWarning(2, "Dotnet tool 'dotnet-oleander-strresgen-tool' not found! Try to install missing dependencies.");
                this.ExternalProcessResult = InstallDotnetTool();
                this._isDotnetToolInstalled = this.ExternalProcessResult.ExitCode == 0;
            }
            else
            {
                if (!this._updateDotnetToolDone && ShouldUpdateDotnetTool)
                {
                    this._updateDotnetToolDone = true;
                    var result = UpdateDotnetTool();

                    if (result.ExitCode != 0)
                    {
                        this.CreateWarning(3, $"Dotnet tool 'dotnet-oleander-strresgen-tool' update failed! {result.StandardErrorOutput}");
                    }
                }
            }

            this.ExternalProcessResult = (string.IsNullOrEmpty(fileNamespace) ?
                new StrResGenProcess(inputFileName) :
                new StrResGenProcess(inputFileName, fileNamespace)).Start();

            if (this.ExternalProcessResult.Win32ExitCode == Win32ExitCodes.ERROR_FILE_NOT_FOUND)
            {
                this.CreateWarning(4, "Dotnet tool 'dotnet-oleander-strresgen-tool' not found! Try to install missing dependencies.");
                this.ExternalProcessResult = InstallDotnetTool();

                if (this.ExternalProcessResult.ExitCode == 0)
                {
                    this._isDotnetToolInstalled = true;
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

            if (this._errors.Any())
            {
                return string.Join(Environment.NewLine, this._errors);
            }

            var csFile = string.Concat(inputFileName.Substring(0, inputFileName.Length - 8), ".cs");

            if (!File.Exists(csFile))
            {
                this.CreateError(4, $"File '{csFile}' not found!");
                this._errors.AddRange(this._warnings);
                return string.Join(Environment.NewLine, this._errors);
            }

            var csFileContent = File.ReadAllText(csFile);

            return this._warnings.Any() ?
                string.Concat(string.Join(Environment.NewLine, this._warnings), Environment.NewLine, csFileContent) :
                csFileContent;
        }

        private void CreateWarning(int level, string message)
        {
            this.WarnLevel = level;
            this.Message = message;
            this._warnings.Add($"// {level} : {message}");
            this.GeneratorErrorCallback(true, level, message, -1, -1);
        }

        private void CreateError(int level, string message)
        {
            this.ErrorLevel = level;
            this.Message = message;
            this._errors.Add(message);

            this.GeneratorErrorCallback(false, level, message, -1, -1);
        }

        public static bool ShouldUpdateDotnetTool
        {
            get
            {
                const string dateTimeFormat = "yyyy.MM.dd HH:mm:ss";
                var path = Path.Combine(Path.GetTempPath(), "StrResGen.Update");

                if (!File.Exists(path))
                {
                    File.WriteAllText(path, DateTime.Now.ToString(dateTimeFormat));
                    return false;
                }

                var lastUpdateDateTimeString = File.ReadAllLines(path).FirstOrDefault();

                if (string.IsNullOrEmpty(lastUpdateDateTimeString) ||
                    !DateTime.TryParseExact(lastUpdateDateTimeString, dateTimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var lastUpdateDateTime))
                {
                    File.WriteAllText(path, DateTime.Now.ToString(dateTimeFormat));
                    return false;
                }

                if ((DateTime.Now - lastUpdateDateTime).TotalDays < 10) return false;

                File.WriteAllText(path, DateTime.Now.ToString(dateTimeFormat));
                return true;
            }
        }

        public static bool IsDotnetToolInstalled
        {
            get
            {
                var path = Path.Combine(Path.GetTempPath(), "StrResGen.Update.log");

                try
                {
                    var result = new ListDotnetToolProcess().Start();
                    File.WriteAllText(path, $"{DateTime.Now}{Environment.NewLine}{result}");

                    return result.ExitCode == 0 && result.StandardOutput.Contains("dotnet-oleander-strresgen-tool");
                }
                catch (Exception ex)
                {
                    File.WriteAllText(path, ex.ToString());
                }

                return false;
            }
        }

        public static ExternalProcessResult UpdateDotnetTool()
        {
            var path = Path.Combine(Path.GetTempPath(), "StrResGen.Update.log");

            try
            {
                var result = new UpdateDotnetToolProcess().Start();
                File.WriteAllText(path, $"{DateTime.Now}{Environment.NewLine}{result}");
                return result;
            }
            catch (Exception ex)
            {
                File.WriteAllText(path, ex.ToString());
                return new ExternalProcessResult("dotnet", "tool update") { ExitCode = -1, StandardErrorOutput = ex.Message};
            }
        }

        public static ExternalProcessResult  InstallDotnetTool()
        {
            var path = Path.Combine(Path.GetTempPath(), "StrResGen.Install.log");

            

            try
            {
                var result = new InstallDotnetToolProcess().Start();
                File.WriteAllText(path, $"{DateTime.Now}{Environment.NewLine}{result}");
                return result;
            }
            catch (Exception ex)
            {
                File.WriteAllText(path, ex.ToString());
                return new ExternalProcessResult("dotnet", "tool install") { ExitCode = -1, StandardErrorOutput = ex.Message };
            }
        }
    }
}