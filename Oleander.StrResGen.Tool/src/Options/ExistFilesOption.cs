using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Completions;
using System.IO;
using System.Linq;

namespace Oleander.StrResGen.Tool.Options;

internal class ExistFilesOption : Option<FileInfo[]>
{
    public ExistFilesOption() : base(name: "--file", description: "The '.strings' file to generate resources")
    {
        this.AddAlias("-f");
        this.AddValidator(result =>
        {
            try
            {
                var fileInfos = (result.GetValueOrDefault<FileInfo[]>() ?? Enumerable.Empty<FileInfo>()).ToList();

                foreach (var fileInfo in fileInfos)
                {
                    if (!string.Equals(Path.GetExtension(fileInfo.FullName).Trim('\"'), ".strings"))
                    {
                        result.ErrorMessage = $"File must have an '*.strings' extension: {fileInfo.FullName}";
                        return;
                    }

                    if (fileInfo.Exists) continue;
                    result.ErrorMessage = $"File does not exist: {fileInfo.FullName}";
                    return;
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




            //wordToComplete = "Oleander.StrResGen.Tool/";
            //Directory.SetCurrentDirectory("D:\\dev\\git\\oleander\\StringResourceGenerator");




            wordToComplete = wordToComplete?.Replace('/', Path.DirectorySeparatorChar);
            
            File.WriteAllText(@"D:\WordToComplete.log", $"{wordToComplete}{Environment.NewLine}" );


            var currentDirectory = Directory.GetCurrentDirectory();
            var fileItems = new List<CompletionItem>();







            if (!string.IsNullOrEmpty(wordToComplete) && wordToComplete.EndsWith(Path.DirectorySeparatorChar) && Directory.Exists(wordToComplete))
            {
                currentDirectory = Path.GetFullPath(wordToComplete);

                foreach (var subDirectory in new DirectoryInfo(currentDirectory).GetDirectories())
                {
                    //fileItems.Add(new(label: $"{subDirectory.Name}/" , sortText: subDirectory.Name));
                    fileItems.Add(new(label: subDirectory.FullName.Replace('\\', '/') , sortText: subDirectory.Name));
                }
            }
            else
            {

                if (!string.IsNullOrEmpty(wordToComplete) && wordToComplete.Contains(Path.DirectorySeparatorChar))
                {
                    var directoryList = wordToComplete.Split(new[] { Path.DirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries).ToList();


                    for (var i = 0; i < directoryList.Count; i++)
                    {

                        if (new DirectoryInfo(currentDirectory).GetDirectories().All(x => x.Name != directoryList[i])) break;

                        currentDirectory = Path.Combine(currentDirectory, directoryList[i]);

                        directoryList[i] = string.Empty;
                    }

                    wordToComplete = string.Join(Path.DirectorySeparatorChar, directoryList.Where(x => !string.IsNullOrEmpty(x)));
                }

              

                foreach (var subDirectory in new DirectoryInfo(currentDirectory).GetDirectories().Where(x => x.Name.StartsWith(wordToComplete ?? string.Empty)))
                {
                    //fileItems.Add(new(label: $"{subDirectory.Name}/", sortText: subDirectory.Name));
                    fileItems.Add(new(label: subDirectory.FullName.Replace('\\', '/'), sortText: subDirectory.Name));
                }
            }


            File.AppendAllText(@"D:\WordToComplete.log", string.Join("|", fileItems.Select(x => x.Label)));


            return fileItems;

        });
    }



    public class DateCommand : Command
    {
        private Argument<string> subjectArgument =
            new("subject", "The subject of the appointment.");
        private Option<DateTime> dateOption =
            new("--date", "The day of week to schedule. Should be within one week.");

        public DateCommand() : base("schedule", "Makes an appointment for sometime in the next week.")
        {
            this.AddArgument(subjectArgument);
            this.AddOption(dateOption);

            dateOption.AddCompletions((ctx) =>
            {
                var today = System.DateTime.Today;
                var dates = new List<CompletionItem>();

                foreach (var i in Enumerable.Range(1, 7))
                {
                    var date = today.AddDays(i);
                    dates.Add(new CompletionItem(
                        label: date.ToShortDateString(),
                        sortText: $"{i:2}"));
                }

                return dates;
            });

            this.SetHandler((subject, date) =>
                {
                    Console.WriteLine($"Scheduled \"{subject}\" for {date}");
                },
                subjectArgument, dateOption);
        }
    }
}