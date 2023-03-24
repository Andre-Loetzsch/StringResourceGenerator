using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;

namespace Oleander.StrResGen;

// ReSharper disable once InconsistentNaming
public class VSProject
{

    private readonly ProjectRootElement _projectRootElement;

    public VSProject(string projectFileName)
    {
        this._projectRootElement = ProjectRootElement.Open(
            projectFileName,
            ProjectCollection.GlobalProjectCollection,
            preserveFormatting: true);
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


    public bool CreateOrUpdateItemElement(string elementName, string update, Dictionary<string, string> metaData)
    {
        foreach (var projectItemGroupElement in this._projectRootElement.ItemGroups)
        {
            var element = projectItemGroupElement.Items.FirstOrDefault(x => x.ElementName == elementName && x.Update == update);

            if (element == null) continue;

            foreach (var (key, value) in metaData)
            {
                var metaDataElement = element.Metadata.FirstOrDefault(x => x.Name == key);

                if (metaDataElement == null)
                {
                    element.AddMetadata(key, key);
                }
                else
                {
                    metaDataElement.Value = value;
                }
            }
           
            return false;
        }

        var projectItemElement = this._projectRootElement.CreateItemElement(elementName);
        projectItemElement.Update = update;

        var lastItemGroups = this._projectRootElement.ItemGroups.LastOrDefault() ?? this._projectRootElement.AddItemGroup();

        // MUST be in the group before we can add metadata
        lastItemGroups.AppendChild(projectItemElement);

        foreach (var (key, value) in metaData)
        {
            var metaDataElement = projectItemElement.Metadata.FirstOrDefault(x => x.Name == key);

            if (metaDataElement == null)
            {
                projectItemElement.AddMetadata(key, key);
            }
            else
            {
                metaDataElement.Value = value;
            }
        }

        return true;
    }

    public void Save()
    {
        this._projectRootElement.Save();
    }

}