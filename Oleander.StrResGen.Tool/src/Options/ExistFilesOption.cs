using System;
using System.CommandLine;
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


        this.AddCompletions(ctx => TabCompletions.FileCompletions(ctx.WordToComplete, "*.strings"));
    }
}