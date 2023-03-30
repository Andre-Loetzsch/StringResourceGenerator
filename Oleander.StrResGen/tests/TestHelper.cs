using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

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
        vsProject.Save();
    }

    public static void AddAccessor(string fileName, string accessor)
    {
        File.WriteAllText(fileName, string.Concat(accessor, Environment.NewLine, File.ReadAllText(fileName)));
    }

    public static void AssertTest(string inputFileName, string? nameSpace, string expectedNameSpace)
    {
        var csFile = Path.Combine(Path.GetDirectoryName(inputFileName) ?? string.Empty, string.Concat(Path.GetFileNameWithoutExtension(inputFileName), ".cs"));
        new ResourceGenerator().Generate(inputFileName, nameSpace);

        Assert.True(File.Exists(csFile));
        var text = File.ReadAllText(csFile);
        Assert.Contains(string.Concat("namespace ", expectedNameSpace), text);
    }
}