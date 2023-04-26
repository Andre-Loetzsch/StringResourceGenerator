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

        this.AddCompletions("Cat", "Dog", "Velociraptor");
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

            dateOption.AddCompletions((ctx) => {
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