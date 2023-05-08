using Microsoft.Build.Construction;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Xunit;
// ReSharper disable InconsistentNaming

namespace Oleander.StrResGen.Tests;

internal static class TestHelper
{
    public static string PrepareTest(string testName)
    {
        var projectDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, testName);

        if (Directory.Exists(projectDir)) Directory.Delete(projectDir, true);
        Directory.CreateDirectory(projectDir);

        return projectDir;
    }

    public static string CopyFile(string sourceFileName, string destDirName)
    {
        if (!Directory.Exists(destDirName)) Directory.CreateDirectory(destDirName);

        var destFileName = Path.Combine(destDirName, Path.GetFileName(sourceFileName));
        File.Copy(sourceFileName, destFileName, true);
        return destFileName;
    }

    public static void AddMetaDataToItemElement(string projectFileName, string elementName, string update, string metaDataKey, string metaDataValue)
    {
        var vsProject = new VSProject(projectFileName);
        var metaData = new Dictionary<string, string>
        {
            [metaDataKey] = metaDataValue
        };

        vsProject.UpdateOrCreateItemElement(elementName, update, metaData);
        vsProject.SaveChanges();
    }

    public static void AddAccessor(string fileName, string accessor)
    {
        File.WriteAllText(fileName, string.Concat(accessor, Environment.NewLine, File.ReadAllText(fileName)));
    }

    public static void AssertExpectedNameSpace(string inputFileName, string? nameSpace, string expectedNameSpace)
    {
        var csFile = Path.Combine(Path.GetDirectoryName(inputFileName) ?? string.Empty, string.Concat(Path.GetFileNameWithoutExtension(inputFileName), ".cs"));
        new ResourceGenerator().Generate(inputFileName, nameSpace);

        Assert.True(File.Exists(csFile));
        var text = File.ReadAllText(csFile);
        Assert.Contains(string.Concat("namespace ", expectedNameSpace), text);
    }
    
    public static void AssertExpectedSRClassName(string inputFileName, string? nameSpace, string expectedKeysSRClassName)
    {
        var csFile = Path.Combine(Path.GetDirectoryName(inputFileName) ?? string.Empty, string.Concat(Path.GetFileNameWithoutExtension(inputFileName), ".cs"));
        new ResourceGenerator().Generate(inputFileName, nameSpace);

        Assert.True(File.Exists(csFile));
        var text = File.ReadAllText(csFile);
        Assert.Contains(string.Concat("internal partial class ", expectedKeysSRClassName), text);
    }

    public static bool TryGetProjectItemElement(this ProjectRootElement projectRootElement, string elementName, string updateOrInclude, out ProjectItemElement? itemElement)
    {
        itemElement = null;

        foreach (var projectItemGroupElement in projectRootElement.ItemGroups)
        {
            itemElement = projectItemGroupElement.Items.FirstOrDefault(x => x.ElementName == elementName && (x.Update == updateOrInclude || x.Include == updateOrInclude));

            if (itemElement == null) continue;

           
            return true;
        }

        return false;
    }

    public static IEnumerable<ProjectItemElement> GetProjectItemElements(this ProjectRootElement projectRootElement, string elementName, string updateOrInclude)
    {
        return projectRootElement.ItemGroups.SelectMany(projectItemGroupElement => projectItemGroupElement.Items
            .Where(x => x.ElementName == elementName && (x.Update == updateOrInclude || x.Include == updateOrInclude)));
    }

}