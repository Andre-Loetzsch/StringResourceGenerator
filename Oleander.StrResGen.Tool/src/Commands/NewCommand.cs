using System;
using System.CommandLine;
using System.IO;

namespace Oleander.StrResGen.Tool.Commands;

internal class NewCommand : Command
{
    private readonly ResGen _resGen;

    public NewCommand(ResGen resGen) : base("new", "Generate a new string resource files.")
    {
        this._resGen = resGen;

        var fileOption = CreateFileOption();
        var projFileOption = CreateProjFileOption();
        var namespaceOption = CreateNameSpaceOption();

        this.AddOption(fileOption);
        this.AddOption(projFileOption);
        this.AddOption(namespaceOption);

        this.SetHandler((file, projFile, nameSpace) =>
        {
            if (File.Exists(projFile.FullName))
            {
                this.ResGenGenerate(projFile.FullName, file.FullName, nameSpace);
                return;
            }

            this.ResGenGenerate(file.FullName, nameSpace);

        }, fileOption, projFileOption, namespaceOption);
    }


    private static Option<FileInfo> CreateFileOption()
    {
        var option = new Option<FileInfo>(name: "--file", description: "The '*.strings' filename to create.");

        option.AddAlias("--f");
        option.IsRequired = true;
        option.AddValidator(result =>
        {
            var fullName = result.GetValueOrDefault<FileInfo>()?.FullName;

            if (fullName == null)
            {
                result.ErrorMessage = "File name is necessary!";
                return;
            }

            if (!string.Equals(Path.GetExtension(fullName), ".strings"))
            {
                result.ErrorMessage = $"File must have an '*.strings' extension! ({fullName})";
            }

            if (File.Exists(fullName))
            {
                result.ErrorMessage = $"File already exists! ({fullName})";
            }
        });

        return option;
    }

    private static Option<FileInfo> CreateProjFileOption()
    {
        var option = new Option<FileInfo>(name: "--projfile", description: "The project file to which the resources belong.").ExistingOnly();

        option.AddAlias("--p");

        option.AddValidator(result =>
        {
            var fullName = result.GetValueOrDefault<FileInfo>()?.FullName;

            if (fullName == null) return;

            if (!string.Equals(Path.GetExtension(fullName), ".csproj"))
            {
                result.ErrorMessage = $"Invalid project file: '{fullName}'";
            }
        });

        return option;
    }

    private static Option<string> CreateNameSpaceOption()
    {
        var option = new Option<string>(name: "--namespace", description: "The namespace of the resource.");

        option.AddAlias("--n");

        return option;
    }


    private void ResGenGenerate(string inputFileName, string nameSpace)
    {
        try
        {
            this._resGen.Generate(inputFileName, nameSpace);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);

        }
    }

    private void ResGenGenerate(string projectFileName, string inputFileName, string nameSpace)
    {
        try
        {
            this._resGen.Generate(projectFileName, inputFileName, nameSpace);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }

}