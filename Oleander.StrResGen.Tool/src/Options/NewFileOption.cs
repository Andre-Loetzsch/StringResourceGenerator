using System;
using System.CommandLine;
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

        this.AddCompletions(ctx => TabCompletions.FileCompletions(ctx.WordToComplete));
    }
}