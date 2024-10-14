using System.Runtime.InteropServices;
using System.Text;
using Microsoft.VisualStudio.TextTemplating.VSHost;

namespace Oleander.StrResGen.SingleFileGenerator
{
    [Guid("8C3E490C-C096-4336-B687-1162F34386E4")]
    public sealed class StrResGenCodeGenerator : BaseCodeGeneratorWithSite
    {
        private readonly ResourceGenerator _generator = new ResourceGenerator();

        public int WarnLevel { get; private set; }
        public int ErrorLevel { get; private set; }
        public string Message { get; private set; }

        private readonly List<string> _errors = new List<string>();
        private readonly List<string> _warnings = new List<string>();
    

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
            if (string.IsNullOrEmpty(fileNamespace))
            {
                this.CreateWarning(1, "File namespace is null or empty!");
            }

            var result = this._generator.Generate(inputFileName, fileNamespace);

            if (result != 0)
            {
                this.CreateError(result, $"StrResGen exit with exit code {result}");
                return null;
            }

            var csFile = string.Concat(inputFileName.Substring(0, inputFileName.Length - 8), ".cs");

            if (!File.Exists(csFile))
            {
                this.CreateError(2, $"File '{csFile}' not found!");
                this._errors.AddRange(this._warnings);
                return string.Join(Environment.NewLine, this._errors);
            }

            var csFileContent = File.ReadAllText(csFile);

            return this._warnings.Any() ? string.Concat(string.Join(Environment.NewLine, this._warnings), Environment.NewLine, csFileContent) : csFileContent;

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
    }
}