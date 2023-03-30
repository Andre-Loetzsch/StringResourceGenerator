using System.Collections.Generic;
using System.IO;
using System;
using System.Linq;
using Microsoft.Extensions.Logging;
using Oleander.Extensions.Logging.Abstractions;

namespace Oleander.StrResGen;

public class ResourceGenerator
{
    private readonly ILogger _logger;

    public ResourceGenerator()
    {
        this._logger = LoggerFactory.CreateLogger<ResourceGenerator>();
    }

    public void Generate(string inputFileName, string? nameSpace)
    {
        this._logger.LogInformation("Update project file.");

        var projectItemDir = Path.GetDirectoryName(inputFileName);
        if (projectItemDir == null)
        {
            this._logger.LogError("Get directory name failed! Path='{inputFileName}'", inputFileName);
            throw new ArgumentException($"Get directory name failed! Path={inputFileName}", nameof(inputFileName));
        }

        if (!VSProject.TryFindProjectFileName(projectItemDir, out var projectFileName))
        {
            this._logger.LogError("Find project filename failed! Project directory='{projectItemDir}'", projectItemDir);
            throw new FileNotFoundException($"Find project filename failed! Project directory='{projectItemDir}'");
        }

        var projectDir = Path.GetDirectoryName(projectFileName);

        if (projectDir == null)
        {
            this._logger.LogError("Get directory name failed! Path='{inputFileName}'", projectDir);
            throw new ArgumentException($"Get directory name failed! Path={projectDir}", nameof(inputFileName));
        }

        if (nameSpace == null) VSProject.TryFindNameSpaceFromProjectItem(inputFileName, out nameSpace);
        this.Generate(projectDir, projectFileName, projectItemDir, inputFileName, nameSpace);
    }

    public void Generate(string projectFileName, string inputFileName, string? nameSpace)
    {
        var projectItemDir = Path.GetDirectoryName(inputFileName);
        var projectDir = Path.GetDirectoryName(projectFileName);

        if (projectItemDir == null)
        {
            this._logger.LogError("Get directory name failed! Path='{inputFileName}'", inputFileName);
            throw new ArgumentException($"Get directory name failed! Path={inputFileName}", nameof(inputFileName));
        }

        if (projectDir == null)
        {
            this._logger.LogError("Get directory name failed! Path='{projectFileName}'", projectFileName);
            throw new ArgumentException($"Get directory name failed! Path={inputFileName}", nameof(inputFileName));
        }

        if (string.IsNullOrEmpty(nameSpace)) VSProject.TryFindNameSpaceFromProjectItem(inputFileName, out nameSpace);

        this.Generate(projectDir, projectFileName, projectItemDir, inputFileName, nameSpace);
    }

    private void Generate(string projectDir, string projectFileName, string projectItemDir, string inputFileName, string? nameSpace)
    {
        var vsProject = new VSProject(projectFileName);
        var relativeDir = Path.GetRelativePath(projectDir, projectItemDir);
        var elementNameStrings = Path.Combine(relativeDir, Path.GetFileName(inputFileName));
        string? customToolNamespace = null;

        if (vsProject.TryGetMetaData("None", elementNameStrings, out var metaData) &&
            metaData.TryGetValue("CustomToolNamespace", out customToolNamespace) &&
            !string.IsNullOrEmpty(customToolNamespace))
        {
            nameSpace = customToolNamespace;
            this._logger.LogInformation("Use 'CustomToolNamespace' from Element '{elementNameStrings}'.", elementNameStrings);
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

        var generated = new CodeGenerator().GenerateCSharpResources(inputFileName, nameSpace).ToList();
        var csFile = generated.FirstOrDefault(x => x.ToLower().EndsWith(".cs"));
        
        if (csFile != null) csFile = Path.GetFileName(csFile);
        if (csFile != null) noneMetaData["LastGenOutput"] = csFile;
        if (!string.IsNullOrEmpty(customToolNamespace)) noneMetaData["CustomToolNamespace"] = customToolNamespace;

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

        vsProject.Save();
    }
}