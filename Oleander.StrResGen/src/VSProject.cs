using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.Extensions.Logging;
#if !NET48
using Oleander.Extensions.Logging.Abstractions;
#endif

namespace Oleander.StrResGen;

// ReSharper disable once InconsistentNaming
internal class VSProject
{
    private readonly ProjectRootElement _projectRootElement;
    private readonly ILogger _logger;
    private bool _hasChanges;

    public VSProject(string projectFileName)
    {

#if NET48
        this._logger = new TraceLogger();
#else
        this._logger = LoggerFactory.CreateLogger<VSProject>();
#endif

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

        this.IsDotnetCoreProject = !string.IsNullOrEmpty(this._projectRootElement.Sdk) &&
                                   string.IsNullOrEmpty(this._projectRootElement.ToolsVersion);

        this._logger.LogInformation("Project has been opened.");
    }


    public bool IsDotnetCoreProject { get; set; }

    public bool TryGetMetaData(string elementName, string updateOrInclude, out Dictionary<string, string> metaData)
    {
        metaData = new Dictionary<string, string>();

        foreach (var projectItemGroupElement in this._projectRootElement.ItemGroups)
        {
            var element = projectItemGroupElement.Items.FirstOrDefault(x => x.ElementName == elementName && (x.Update == updateOrInclude || x.Include == updateOrInclude));

            if (element == null) continue;

            metaData = element.Metadata.ToDictionary(x => x.Name, metadataElement => metadataElement.Value);
            return true;
        }

        return false;
    }

    public ProjectItemGroupElement? FindProjectItemGroupElement(string elementName, string updateOrInclude)
    {
        return this._projectRootElement.ItemGroups
            .FirstOrDefault(x => x.Items.Any(x1 => x1.ElementName == elementName && (x1.Update == updateOrInclude || x1.Include == updateOrInclude)));
    }

    public ProjectItemGroupElement FindOrCreateProjectItemGroupElement(string elementName, string updateOrInclude)
    {
        return this._projectRootElement.ItemGroups
                   .FirstOrDefault(x => x.Items.Any(x1 => x1.ElementName == elementName && (x1.Update == updateOrInclude || x1.Include == updateOrInclude))) ??
               this._projectRootElement.AddItemGroup();
    }

    public void UpdateOrCreateItemElement(string elementName, string updateOrInclude, Dictionary<string, string>? metaData = null)
    {
        this.UpdateOrCreateItemElement(
            this.FindOrCreateProjectItemGroupElement(elementName, updateOrInclude), elementName, updateOrInclude, metaData);
    }

    public void UpdateOrCreateItemElement(ProjectItemGroupElement defaultProjectItemGroupElement, string elementName, string updateOrInclude, Dictionary<string, string>? metaData = null)
    {
        var element = defaultProjectItemGroupElement.Items.FirstOrDefault(x => x.ElementName == elementName && (x.Update == updateOrInclude || x.Include == updateOrInclude)) ??
                      this.FindProjectItemGroupElement(elementName, updateOrInclude)?.Items.FirstOrDefault(x => x.ElementName == elementName && (x.Update == updateOrInclude || x.Include == updateOrInclude));

        if (element != null) this.IsDotnetCoreProject = !string.IsNullOrEmpty(element.Update);
        this._logger.LogInformation("{action} element: <{elementName} {attribute}=\"{updateOrInclude}\">.",
            element == null ? "Create" : "Update", elementName, this.IsDotnetCoreProject ? "Update" : "Include", updateOrInclude);

        if (element == null)
        {
            element = this._projectRootElement.CreateItemElement(elementName);
            if (this.IsDotnetCoreProject)
            {
                element.Update = updateOrInclude;
            }
            else
            {
                element.Include = updateOrInclude;
            }

            // MUST be in the group before we can add metadata
            defaultProjectItemGroupElement.AppendChild(element);
            this._hasChanges = true;
        }

        if (metaData == null) return;

        foreach (var item in metaData)
        {
            var metaDataElement = element.Metadata.FirstOrDefault(x => x.Name == item.Key);

            if (metaDataElement == null)
            {
                element.AddMetadata(item.Key, item.Value);
                this._hasChanges = true;
                continue;
            }

            if (metaDataElement.Value == item.Value) continue;

            metaDataElement.Value = item.Value;
            this._hasChanges = true;
        }
    }

    public void SaveChanges()
    {
        if (this._hasChanges)
        {
            this._projectRootElement.Save();
            this._logger.LogInformation("Project saved.");
            this._hasChanges = false;

            return;
        }

        this._logger.LogInformation("Nothing to save. Project has no changes.");
    }

    #region static members

    public static bool TryFindProjectFileName(string startDirectory, out string projectFileName)
    {
        projectFileName = string.Empty;
        var dirInfo = new DirectoryInfo(startDirectory);
        var parentDir = dirInfo;

        while (parentDir != null)
        {
            //var fileInfo = parentDir.GetFiles("*.csproj").MinBy(x => x.FullName);
            var fileInfo = parentDir.GetFiles("*.csproj").OrderBy(x => x.Length).FirstOrDefault();

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
        return TryFindProjectFileName(itemDir, out var projectFileName) &&
               TryFindNameSpaceFromProjectItem(projectFileName, itemFileName, out itemNamespace);
    }

    public static bool TryFindNameSpaceFromProjectItem(string projectFileName, string itemFileName, out string itemNamespace)
    {
        itemNamespace = string.Empty;
        var itemDir = Path.GetDirectoryName(itemFileName);
        if (itemDir == null) return false;

        var projectDir = Path.GetDirectoryName(projectFileName);
        if (projectDir == null) return false;

        var projectName = Path.GetFileName(projectFileName);
        if (projectName.Length < 8) return false;

        //projectName = projectName[..^7];
        projectName = projectName.Substring(0, projectName.Length - 7);



        if (itemDir.Length < projectDir.Length) return false;

        //itemNamespace = string.Concat(projectName, itemDir[projectDir.Length..]).Replace(Path.DirectorySeparatorChar, '.');
        itemNamespace = string.Concat(projectName, itemDir.Substring(projectDir.Length)).Replace(Path.DirectorySeparatorChar, '.');

        return true;
    }

    #endregion
}