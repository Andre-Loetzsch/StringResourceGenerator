﻿using System.CommandLine;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Oleander.StrResGen.Tool.Options;

namespace Oleander.StrResGen.Tool.Commands;

internal class NewCommand : CommandBase
{
    public NewCommand(ILogger logger, ResGen resGen) : base(logger, resGen, "new", "Generate a new string resource files")
    {
        var fileOption = new NewFileOption();
        var projFileOption = new ProjFileOption().ExistingOnly();
        var namespaceOption = new NameSpaceOption();

        this.AddOption(fileOption);
        this.AddOption(projFileOption);
        this.AddOption(namespaceOption);

        this.SetHandler((files, projFile, nameSpace) => File.Exists(projFile?.FullName) ?
            Task.FromResult(this.ResGenGenerate(projFile, files, nameSpace)) :
            Task.FromResult(this.ResGenGenerate(files, nameSpace)), fileOption, projFileOption, namespaceOption);
    }
}