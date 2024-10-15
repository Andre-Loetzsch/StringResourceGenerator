using System;
using Oleander.StrResGen.SingleFileGenerator;

namespace Tests
{
    internal class Program
    {
        static void Main(string[] args)
        {

            if (args.Length == 0)
            {
                Console.WriteLine("Run with arg1 = inputFile, arg2 = nameSpace");
                Console.ReadLine();
                return;
            }

            var generator = new StrResGenCodeGenerator();

            var inputFileName = args[0];
            var nameSpace = args.Length > 1 ? args[1] : null;


            var result = generator.GenerateCSharpCode(inputFileName, nameSpace);

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

            Console.WriteLine(result);
            Console.ReadLine();
        }
    }
}