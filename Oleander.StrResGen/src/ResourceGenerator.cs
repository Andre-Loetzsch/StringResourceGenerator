using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Oleander.Extensions.Logging.Abstractions;
using System.Runtime.CompilerServices;
using System.Diagnostics;
// ReSharper disable ExplicitCallerInfoArgument

namespace Oleander.StrResGen;

public class ResourceGenerator
{
    private readonly ILogger _logger;

    public ResourceGenerator()
    {
        this._logger = LoggerFactory.CreateLogger<ResourceGenerator>();
    }

    public int Generate(string inputFileName, string? nameSpace)
    {
        var projectItemDir = Path.GetDirectoryName(inputFileName);
        if (projectItemDir == null)
        {
            this.ReportError(1, $"Get directory name failed! Path='{inputFileName}'");
            return this.ErrorCode;
        }

        if (!VSProject.TryFindProjectFileName(projectItemDir, out var projectFileName))
        {
            this.ReportError(232, $"Find project filename failed! Project directory='{projectItemDir}'");
            return this.ErrorCode;
        }

        var projectDir = Path.GetDirectoryName(projectFileName);

        if (projectDir == null)
        {
            this.ReportError(3, $"Get directory name failed! Path='{inputFileName}'");
            return this.ErrorCode;
        }

        if (nameSpace == null) VSProject.TryFindNameSpaceFromProjectItem(inputFileName, out nameSpace);
        return this.Generate(projectDir, projectFileName, projectItemDir, inputFileName, nameSpace);
    }

    public int Generate(string projectFileName, string inputFileName, string? nameSpace)
    {
        var projectItemDir = Path.GetDirectoryName(inputFileName);
        var projectDir = Path.GetDirectoryName(projectFileName);

        if (projectItemDir == null)
        {
            this.ReportError(4, $"Get directory name failed! Path='{inputFileName}'");
            return this.ErrorCode;
        }

        if (projectDir == null)
        {
            this.ReportError(5, $"Get directory name failed! Path='{projectFileName}'");
            return this.ErrorCode;
        }

        if (string.IsNullOrEmpty(nameSpace)) VSProject.TryFindNameSpaceFromProjectItem(projectFileName, inputFileName, out nameSpace);

        return this.Generate(projectDir, projectFileName, projectItemDir, inputFileName, nameSpace);
    }

    public int Generate(IEnumerable<string> inputFileNames, string? nameSpace)
    {
        var projects = new Dictionary<string, List<string>>(StringComparer.CurrentCultureIgnoreCase);

        foreach (var inputFileName in inputFileNames)
        {
            var projectItemDir = Path.GetDirectoryName(inputFileName);

            if (projectItemDir == null)
            {
                this.ReportError(6, $"Get directory name failed! Path='{inputFileName}'");
                return this.ErrorCode;
            }

            if (!VSProject.TryFindProjectFileName(projectItemDir, out var projectFileName))
            {
                this.ReportError(7, $"Find project filename failed! Project directory='{projectItemDir}'");
                return this.ErrorCode;
            }

            var projectDir = Path.GetDirectoryName(projectFileName);

            if (projectDir == null)
            {
                this.ReportError(8, $"Get directory name failed! Path='{inputFileName}'");
                return this.ErrorCode;
            }

            if (!projects.TryGetValue(projectFileName, out var list))
            {
                list = new();
                projects[projectFileName] = list;
            }

            if (list.Contains(inputFileName))
            {
                this.ReportWarning(1, $"The specified file '{inputFileName}' is ignored because it has already been specified.");
                continue;
            }

            list.Add(inputFileName);
        }

        return projects.Select(project =>
            this.Generate(project.Key, project.Value, nameSpace))
            .FirstOrDefault(errorCode => errorCode != 0);
    }

    public int Generate(string projectFileName, IEnumerable<string> inputFileNames, string? nameSpace)
    {
        var projectDir = Path.GetDirectoryName(projectFileName);

        if (projectDir == null)
        {
            this.ReportError(9, $"Get directory name failed! Path='{projectFileName}'");
            return this.ErrorCode;
        }

        var vsProject = new VSProject(projectFileName);

        foreach (var inputFileName in inputFileNames)
        {
            var projectItemDir = Path.GetDirectoryName(inputFileName);

            if (projectItemDir == null)
            {
                this.ReportError(10, $"Get directory name failed! Path='{inputFileName}'");
                return this.ErrorCode;
            }

            if (string.IsNullOrEmpty(nameSpace)) VSProject.TryFindNameSpaceFromProjectItem(projectFileName, inputFileName, out nameSpace);

            var errorCode = this.Generate(projectDir, vsProject, projectItemDir, inputFileName, nameSpace);
            if (errorCode != 0) return errorCode;
        }

        vsProject.SaveChanges();
        return 0;
    }

    #region private members

    private int Generate(string projectDir, string projectFileName, string projectItemDir, string inputFileName, string? nameSpace)
    {
        var vsProject = new VSProject(projectFileName);
        var errorCode = this.Generate(projectDir, vsProject, projectItemDir, inputFileName, nameSpace);

        vsProject.SaveChanges();

        return errorCode;
    }

    private int Generate(string projectDir, VSProject vsProject, string projectItemDir, string inputFileName, string? nameSpace)
    {
#if NET

        var relativeDir = Path.GetRelativePath(projectDir, projectItemDir);
#else

        var relativeDir = projectItemDir.Length > projectDir.Length ?
            Path.GetFullPath(projectItemDir).Substring(Path.GetFullPath(projectDir).Length).Trim('\\') : string.Empty;
#endif

        if (relativeDir == ".") relativeDir = string.Empty;
        var elementNameStrings = Path.Combine(relativeDir, Path.GetFileName(inputFileName));

        string? customToolNamespace = null;

        if (vsProject.TryGetMetaData("None", elementNameStrings, out var metaData) &&
            metaData.TryGetValue("CustomToolNamespace", out customToolNamespace) &&
            !string.IsNullOrEmpty(customToolNamespace))
        {
            nameSpace = customToolNamespace;
            this._logger.LogInformation("Use CustomToolNamespace: '{nameSpace}' from Element: '{elementNameStrings}'.", nameSpace, elementNameStrings);
        }

        var itemGroup = vsProject.FindOrCreateProjectItemGroupElement("None", elementNameStrings);
        var noneMetaData = new Dictionary<string, string>();

        var embeddedResourceMetaData = new Dictionary<string, string>
        {
            ["AutoGen"] = "True",
            ["DependentUpon"] = Path.GetFileName(inputFileName),
            ["DesignTime"] = "True"
        };

        var compileMetaData = new Dictionary<string, string>
        {
            ["AutoGen"] = "True",
            ["DependentUpon"] = Path.GetFileName(inputFileName),
            ["DesignTime"] = "True"
        };

        var generator = new CodeGenerator();
        var options = generator.CreateGenerationOptions(nameSpace);

        options.SRClassName = string.Concat(Path.GetFileNameWithoutExtension(inputFileName).Replace('.', '_').Replace('-', '_'));

        if (!vsProject.IsDotnetCoreProject)
        {
            options.KeysSRClassName = string.Concat("Keys_", Path.GetFileNameWithoutExtension(inputFileName).Replace('.', '_').Replace('-', '_'));
        }

        var generated = generator.GenerateCSharpResources(inputFileName, options).ToList();

        if (generator.ErrorCode != 0)
        {
            this.ReportWarning(2, "No files generated");
            return generator.ErrorCode;
        }

        foreach (var file in generated)
        {
            this.ReportMessage(0, $"File '{file}' generated");
        }

        var csFile = generated.FirstOrDefault(x => x.ToLower().EndsWith(".cs"));

        if (csFile != null) csFile = Path.GetFileName(csFile);
        if (csFile != null) noneMetaData["LastGenOutput"] = csFile;
        if (!string.IsNullOrEmpty(customToolNamespace)) noneMetaData["CustomToolNamespace"] = customToolNamespace ?? string.Empty;

        foreach (var path in generated)
        {
            var fileExtension = Path.GetExtension(path);
            var elementName = Path.Combine(relativeDir, Path.GetFileName(path));
            this._logger.LogInformation("Process generated file: '{path}'", path);

            switch (fileExtension.ToLower())
            {
                case ".cs":
                    vsProject.UpdateOrCreateItemElement(itemGroup, "Compile", elementName, compileMetaData);
                    break;
                case ".strings":
                    vsProject.UpdateOrCreateItemElement(itemGroup, "None", elementName, noneMetaData);
                    break;
                case ".resx":
                    vsProject.UpdateOrCreateItemElement(itemGroup, "EmbeddedResource", elementName, embeddedResourceMetaData);
                    break;
            }
        }

        return generator.ErrorCode;
    }

    public int ErrorCode { get; private set; }

    #region MSBuild message

    private void ReportMessage(int code, string text, [CallerLineNumber] int line = 0, [CallerMemberName] string subCategory = "")
    {
        MSBuildLogFormatter.CreateMSBuildMessage($"RG{code}", text, subCategory, line);
    }

    private void ReportWarning(int code, string text, [CallerLineNumber] int line = 0, [CallerMemberName] string subCategory = "")
    {
        this._logger.CreateMSBuildWarning($"RG{code}", text, subCategory, line);
    }

    private void ReportError(int code, string text, [CallerLineNumber] int line = 0, [CallerMemberName] string subCategory = "")
    {
        this._logger.CreateMSBuildError($"RG{code}", text, subCategory, line);
        this.ErrorCode = code;
    }

    #endregion

    #endregion
}