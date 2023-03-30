using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.Extensions.Logging;
using Oleander.Extensions.Logging.Abstractions;

namespace Oleander.StrResGen;

// ReSharper disable once InconsistentNaming
public class VSProject
{
    private readonly ProjectRootElement _projectRootElement;
    private readonly ILogger _logger;

    public VSProject(string projectFileName)
    {
        this._logger = LoggerFactory.CreateLogger<VSProject>();
        this._logger.LogInformation("Try to open project file: '{projectFileName}'", projectFileName);

        if (!File.Exists(projectFileName))
        {
            this._logger.LogError("Project file '{projectFileName}' not found!", projectFileName);
            throw new FileNotFoundException("Project file not found!", projectFileName);
        }

        this._projectRootElement = ProjectRootElement.Open(
            projectFileName,
            ProjectCollection.GlobalProjectCollection,
            preserveFormatting: true);

        this._logger.LogInformation("Project has been opened.");
    }

    public bool TryGetMetaData(string elementName, string update, out Dictionary<string, string> metaData)
    {
        metaData = new Dictionary<string, string>();

        foreach (var projectItemGroupElement in this._projectRootElement.ItemGroups)
        {
            var element = projectItemGroupElement.Items.FirstOrDefault(x => x.ElementName == elementName && x.Update == update);

            if (element == null) continue;

            metaData = element.Metadata.ToDictionary(x => x.Name, metadataElement => metadataElement.Value);
            return true;
        }

        return false;
    }

    public ProjectItemGroupElement FindOrCreateProjectItemGroupElement(string elementName, string update)
    {
        return this._projectRootElement.ItemGroups
                   .FirstOrDefault(x => x.Items.Any(x1 => x1.ElementName == elementName && x1.Update == update)) ??
               this._projectRootElement.AddItemGroup();
    }


    public void UpdateOrCreateItemElement(string elementName, string update, Dictionary<string, string>? metaData = null)
    {
        this.UpdateOrCreateItemElement(
            this.FindOrCreateProjectItemGroupElement(elementName, update), elementName, update, metaData);
    }

    public void UpdateOrCreateItemElement(ProjectItemGroupElement projectItemGroupElement, string elementName, string update, Dictionary<string, string>? metaData = null)
    {
        var element = projectItemGroupElement.Items.FirstOrDefault(x => x.ElementName == elementName && x.Update == update);
        this._logger.LogInformation("{action} element: <{elementName} Update=\"{update}\">.", element == null ? "Create" : "Update", elementName, update);

        if (element == null)
        {
            element = this._projectRootElement.CreateItemElement(elementName);
            element.Update = update;

            // MUST be in the group before we can add metadata
            projectItemGroupElement.AppendChild(element);
        }

        if (metaData == null) return;

        foreach (var (key, value) in metaData)
        {
            var metaDataElement = element.Metadata.FirstOrDefault(x => x.Name == key);

            if (metaDataElement == null)
            {
                element.AddMetadata(key, value);
                continue;
            }

            metaDataElement.Value = value;
        }
    }

    public void Save()
    {
        this._projectRootElement.Save();
        this._logger.LogInformation("Project saved.");
    }

    #region static members

    public static bool TryFindProjectFileName(string startDirectory, out string projectFileName)
    {
        projectFileName = string.Empty;
        var dirInfo = new DirectoryInfo(startDirectory);
        var parentDir = dirInfo;

        while (parentDir != null)
        {
            var fileInfo = parentDir.GetFiles("*.csproj").FirstOrDefault();
            if (fileInfo != null)
            {
                projectFileName = fileInfo.FullName;
                return true;
            }

            parentDir = parentDir.Parent;
        }

        return false;
    }

    public static bool TryFindNameSpaceFromProjectItem(string itemFileName, out string itemNamespace)
    {
        itemNamespace = string.Empty;
        var itemDir = Path.GetDirectoryName(itemFileName);
        if (itemDir == null) return false;
        if (!TryFindProjectFileName(itemDir, out var projectFileName)) return false;

        var projectDir = Path.GetDirectoryName(projectFileName);
        if (projectDir == null) return false;

        var projectName = Path.GetFileName(projectFileName);
        if (projectName.Length < 8) return false;

        projectName = projectName[..^7];

        if (itemDir.Length < projectDir.Length) return false;

        itemNamespace = string.Concat(projectName, itemDir[projectDir.Length..]).Replace("\\", ".");
        return true;
    }

    #endregion
}