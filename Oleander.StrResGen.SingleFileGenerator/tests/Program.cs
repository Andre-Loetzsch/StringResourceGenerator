using System;
using System.Threading;
using Oleander.StrResGen.SingleFileGenerator.ExternalProcesses;

namespace Oleander.StrResGen.SingleFileGenerator.Tests
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var generator = new StrResGenCodeGenerator();
            var isDotnetToolInstalled = StrResGenCodeGenerator.IsDotnetToolInstalled;
            Console.WriteLine($"IsDotnetToolInstalled: {isDotnetToolInstalled}");

            if (isDotnetToolInstalled)
            {
                var shouldUpdateDotnetTool = StrResGenCodeGenerator.ShouldUpdateDotnetTool;
                Console.WriteLine($"ShouldUpdateDotnetTool: {shouldUpdateDotnetTool}");
                Console.WriteLine($"UpdateDotnetTool: {StrResGenCodeGenerator.UpdateDotnetTool()}");
            }
            else
            {
                Console.WriteLine($"InstallDotnetTool: {StrResGenCodeGenerator.InstallDotnetTool()}");
            }

            
            var fileContent = generator.GenerateCSharpCode("D:\\dev\\git\\oleander\\StringResourceGenerator\\Oleander.StrResGen.SingleFileGenerator\\tests\\SR.strings", "");
            var result = generator.ExternalProcessResult ?? new ExternalProcessResult(string.Empty, string.Empty);

            Console.WriteLine(result.ExitCode);

            if (generator.WarnLevel != 0)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(generator.Message);
                Console.ResetColor();
            }

            if (generator.ErrorLevel != 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(generator.Message);
                Console.ResetColor();
            }

            if (result.ExitCode == 0)
            {
                Console.WriteLine(result.StandardOutput);
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;

                if (result.Win32ExitCode != Win32ExitCodes.ERROR_SUCCESS)
                {
                    Console.WriteLine(result.Win32ExitCode);
                }

                Console.WriteLine(result.StandardErrorOutput);
                Console.ResetColor();
            }

            Console.WriteLine("Done!");
            Thread.Sleep(5000);
        }
    }
}