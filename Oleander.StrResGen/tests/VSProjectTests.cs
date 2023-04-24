using System;
using System.Collections.Generic;
using System.IO;
using Xunit;

namespace Oleander.StrResGen.Tests;

// ReSharper disable once InconsistentNaming
public class VSProjectTests
{

    [Fact]
    public void TestTryFindProjectDir()
    {
        Assert.True(VSProject.TryFindProjectFileName(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData"), out var projectFile));
        Assert.True(File.Exists(projectFile));
    }

    [Fact]
    public void TestTryFindNameSpaceFromProjectItem()
    {
        var projectDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MyProject");

        if (Directory.Exists(projectDir)) Directory.Delete(projectDir, true);
        Directory.CreateDirectory(projectDir);

        var projectFileName = Path.Combine(projectDir, "MyProject.csproj");

        File.WriteAllText(projectFileName, string.Empty);

        var projectItemDir = Path.Combine(projectDir, "Resources");
        if (!Directory.Exists(projectItemDir)) Directory.CreateDirectory(projectItemDir);
        var projectItemFileName = Path.Combine(projectItemDir, "SR.strings");

        File.WriteAllText(projectItemFileName, string.Empty);

        Assert.True(VSProject.TryFindNameSpaceFromProjectItem(projectItemFileName, out var nameSpace));
        Assert.Equal("MyProject.Resources", nameSpace);

        Directory.Delete(projectDir, true);
    }

    [Fact]
    public void Test_TryGetMetaData()
    {
        var projectDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MyProject");

        if (Directory.Exists(projectDir)) Directory.Delete(projectDir, true);
        Directory.CreateDirectory(projectDir);
        var projectFileName = Path.Combine(projectDir, "ClassLibrary.csproj");

        File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "ClassLibrary.csproj"), projectFileName, true);

        var vsProject = new VSProject(projectFileName);
        Assert.True(vsProject.TryGetMetaData("EmbeddedResource", "Resources\\StringResources.srt.resx", out var metaData));
        Assert.True(metaData.TryGetValue("AutoGen", out var value));
        Assert.Equal("True", value);
    }

    [Fact]
    public void Test_UpdateOrCreateItemElement()
    {
        var projectDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MyProject");

        if (Directory.Exists(projectDir)) Directory.Delete(projectDir, true);
        Directory.CreateDirectory(projectDir);
        var projectFileName = Path.Combine(projectDir, "ClassLibrary.csproj");

        File.Copy(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "ClassLibrary.csproj"), projectFileName, true);

        var vsProject = new VSProject(projectFileName);

        var metaData = new Dictionary<string, string>
        {
            ["Test"] = "Test value"
        };

        vsProject.UpdateOrCreateItemElement("EmbeddedResource", $"Resources{Path.DirectorySeparatorChar}StringResources.srt.resx", metaData);
        vsProject.SaveChanges();

        Assert.True(vsProject.TryGetMetaData("EmbeddedResource", $"Resources{Path.DirectorySeparatorChar}StringResources.srt.resx", out metaData));
        Assert.True(metaData.TryGetValue("Test", out var value));
        Assert.Equal("Test value", value);
    }


    
}