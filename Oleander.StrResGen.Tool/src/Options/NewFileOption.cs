using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Completions;
using System.IO;
using System.Linq;

namespace Oleander.StrResGen.Tool.Options;

public class NewFileOption : Option<FileInfo[]>
{
    internal NewFileOption() : base("--file", "The '.strings' filename to create")
    {
        this.AddAlias("-f");
        this.IsRequired = true;

        this.AddValidator(result =>
        {
            try
            {
                var fileInfos = result.GetValueOrDefault<FileInfo[]>() ?? Enumerable.Empty<FileInfo>();

                foreach (var fileInfo in fileInfos)
                {
                    var fullName = fileInfo.FullName;

                    if (!string.Equals(Path.GetExtension(fullName).Trim('\"'), ".strings"))
                    {
                        result.ErrorMessage = $"File must have an '.strings' extension: {fullName}";
                    }

                    if (File.Exists(fullName))
                    {
                        result.ErrorMessage = $"File already exists: {fullName}";
                    }
                }

            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
            }
        });

        this.AddCompletions(ctx =>
        {
            var wordToComplete = ctx.WordToComplete;
            var fileItems = new List<CompletionItem>();


            //************************************************************************************

            wordToComplete = "O/.";
            Directory.SetCurrentDirectory("D:\\dev\\git\\oleander\\StringResourceGenerator");

            //************************************************************************************

            // TODO remove File.WriteAllText
            File.WriteAllText(@"D:\WordToComplete.log", $"{wordToComplete}{Environment.NewLine}");


            if (string.IsNullOrEmpty(wordToComplete)) return fileItems;

            var wordToCompleteList = wordToComplete.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            var lastWordToComplete = wordToCompleteList.Last();

            if (wordToCompleteList.Count > 1)
            {
                wordToCompleteList.RemoveAt(wordToCompleteList.Count - 1);
            }

            // Test directory without last word

            var pathTest = wordToCompleteList.Count > 0 ?
                string.Join(Path.DirectorySeparatorChar, wordToCompleteList) :
                Directory.GetCurrentDirectory();

            var dirInfoTest = new DirectoryInfo(pathTest);

            if (!dirInfoTest.Exists) return fileItems;

            fileItems.Add(new(label: Path.Combine(pathTest, dirInfoTest.Name)));

            foreach (var fileInfo in dirInfoTest.GetFiles($"{lastWordToComplete}*.strings"))
            {
                fileItems.Add(new(label: Path.Combine(pathTest, fileInfo.Name)));
            }

            foreach (var dirInfo in dirInfoTest.GetDirectories().Where(x => x.Name.StartsWith(lastWordToComplete)))
            {
                fileItems.Add(new(label: Path.Combine(pathTest, dirInfo.Name)));
            }

            // Test directory with last word

            pathTest = Path.Combine(pathTest, lastWordToComplete);
            dirInfoTest = new DirectoryInfo(pathTest);

            if (!dirInfoTest.Exists) return fileItems;

            fileItems.Add(new(label: Path.Combine(pathTest, dirInfoTest.Name)));


            foreach (var fileInfo in dirInfoTest.GetFiles("*.strings"))
            {
                fileItems.Add(new(label: Path.Combine(pathTest, fileInfo.Name)));
            }

            foreach (var dirInfo in dirInfoTest.GetDirectories())
            {
                fileItems.Add(new(label: Path.Combine(pathTest, dirInfo.Name)));
            }










            // TODO remove File.WriteAllText
            File.AppendAllText(@"D:\WordToComplete.log", string.Join("|", fileItems.Select(x => x.Label)));


            return fileItems;

        });
    }
}