﻿using System.CommandLine;
using System.IO;

namespace Oleander.StrResGen.Tool.Options;

internal class ProjFileOption : Option<FileInfo>
{
    public ProjFileOption() : base(name: "--proj", description: "The project file to which the resources belong")
    {
        this.AddAlias("-p");

        this.AddValidator(result =>
        {
            var fullName = result.GetValueOrDefault<FileInfo>()?.FullName;

            if (fullName == null) return;

            if (!string.Equals(Path.GetExtension(fullName), ".csproj"))
            {
                result.ErrorMessage = $"Invalid project file: '{fullName}'";
            }
        });

        this.AddCompletions(ctx => TabCompletions.FileCompletions(ctx.WordToComplete, "*.csproj"));
    }
}