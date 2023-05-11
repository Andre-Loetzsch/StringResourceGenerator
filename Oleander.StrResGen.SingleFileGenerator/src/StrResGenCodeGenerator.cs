using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using EnvDTE;
using Microsoft.VisualStudio.TextTemplating.VSHost;

namespace Oleander.StrResGen.SingleFileGenerator
{
    [Guid("8C3E490C-C096-4336-B687-1162F34386E4")]
    public sealed class StrResGenCodeGenerator : BaseCodeGeneratorWithSite
    {
        public const string Name = nameof(StrResGenCodeGenerator);
        public const string Description = "Generates a minified version of JavaScript, CSS and HTML files files.";

        public override string GetDefaultExtension()
        {
            var item = this.GetService(typeof(ProjectItem)) as ProjectItem;
            return ".cs" + Path.GetExtension(item?.FileNames[1]);
        }

        protected override byte[] GenerateCode(string inputFileName, string inputFileContent)
        {
            Console.WriteLine($"inputFileName: {inputFileName}");
            if (Path.GetExtension(inputFileName) != ".strings") return null;
            var csFile = string.Concat(inputFileName.Substring(0, inputFileName.Length - 8), "1.cs");

            File.WriteAllText(csFile, "Hello World!");

            return Encoding.UTF8.GetBytes("Hello World!");
        }
    }
}