using System.CommandLine;
using System.IO;
using System.Linq;

namespace Oleander.StrResGen.Tool.Options;

internal class ExistFilesOption : Option<FileInfo[]>
{
    public ExistFilesOption() : base(name: "--file", description: "The '*.strings' file to generate resources")
    {
        this.AddAlias("-f");
        this.IsRequired = true;

        this.AddValidator(result =>
        {
            var fileInfos = result.GetValueOrDefault<FileInfo[]>() ?? Enumerable.Empty<FileInfo>();

            foreach (var fileInfo in fileInfos)
            {
                var fullName = fileInfo.FullName;

                if (!string.Equals(Path.GetExtension(fullName).Trim('\"'), ".strings"))
                {
                    result.ErrorMessage = MSBuildLogFormatter.CreateMSBuildError(1, $"File must have an '*.strings' extension! ({fullName})", "Oleander.StrResGen.Tool");
                }
            }
        });
    }
}