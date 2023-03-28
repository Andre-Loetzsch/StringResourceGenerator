using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace Oleander.StrResGen.Tests;

// ReSharper disable once InconsistentNaming
public class VSProjectTests
{

    [Fact]
    public void TestTryFindProjectDir()
    {
        Assert.True(VSProject.TryFindProjectFileName(AppDomain.CurrentDomain.BaseDirectory, out var projectFile));
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
    public void Test()
    {
        var projectDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MyProject");

        if (Directory.Exists(projectDir)) Directory.Delete(projectDir, true);
        Directory.CreateDirectory(projectDir);

        var projectFileName = Path.Combine(projectDir, "MyProject.csproj");
        var projectFileContent = $"<Project Sdk=\"Microsoft.NET.Sdk\">{Environment.NewLine}</Project>";

        File.WriteAllText(projectFileName, projectFileContent);

        var projectItemDir = Path.Combine(projectDir, "Resources");
        if (!Directory.Exists(projectItemDir)) Directory.CreateDirectory(projectItemDir);

        File.WriteAllText(Path.Combine(projectItemDir, "SR.strings"), string.Empty);
        File.WriteAllText(Path.Combine(projectItemDir, "SR.cs"), string.Empty);
        File.WriteAllText(Path.Combine(projectItemDir, "SR.srt.resx"), string.Empty);
        File.WriteAllText(Path.Combine(projectItemDir, "SR.srt.de.resx"), string.Empty);

        File.WriteAllText(Path.Combine(projectItemDir, "SR2.strings"), string.Empty);
        File.WriteAllText(Path.Combine(projectItemDir, "SR2.cs"), string.Empty);
        File.WriteAllText(Path.Combine(projectItemDir, "SR2.srt.resx"), string.Empty);
        File.WriteAllText(Path.Combine(projectItemDir, "SR2.srt.de.resx"), string.Empty);

        var vsProject = new VSProject(projectFileName);
        var metaData = new Dictionary<string, string>
        {
            ["AutoGen"] = "True",
            ["DependentUpon"] = "SR.strings",
            ["DesignTime"] = "True"
        };

        var itemGroup = vsProject.FindOrCreateProjectItemGroupElement("Non", "Resources\\SR.strings");

        vsProject.UpdateOrCreateItemElement(itemGroup, "Non", "Resources\\SR.strings");
        vsProject.UpdateOrCreateItemElement(itemGroup, "Compile", "Resources\\SR.cs", metaData);
        vsProject.UpdateOrCreateItemElement(itemGroup, "EmbeddedResource", "Resources\\SR.srt.resx", metaData);
        vsProject.UpdateOrCreateItemElement(itemGroup, "EmbeddedResource", "Resources\\SR.srt.de.resx", metaData);


        metaData["DependentUpon"] = "SR2.strings";

        itemGroup = vsProject.FindOrCreateProjectItemGroupElement("Non", "Resources\\SR2.strings");

        vsProject.UpdateOrCreateItemElement(itemGroup, "Non", "Resources\\SR2.strings");
        vsProject.UpdateOrCreateItemElement(itemGroup, "Compile", "Resources\\SR2.cs", metaData);
        vsProject.UpdateOrCreateItemElement(itemGroup, "EmbeddedResource", "Resources\\SR2.srt.resx", metaData);
        vsProject.UpdateOrCreateItemElement(itemGroup, "EmbeddedResource", "Resources\\SR2.srt.de.resx", metaData);



        vsProject.Save();

        Directory.Delete(projectDir, true);
    }




    [Fact]
    public void Test2()
    {
        var projectDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "MyProject");

        if (Directory.Exists(projectDir)) Directory.Delete(projectDir, true);
        Directory.CreateDirectory(projectDir);

        var projectFileName = Path.Combine(projectDir, "MyProject.csproj");
        var projectFileContent = $"<Project Sdk=\"Microsoft.NET.Sdk\">{Environment.NewLine}</Project>";

        File.WriteAllText(projectFileName, projectFileContent);

        var projectItemDir = Path.Combine(projectDir, "Resources");
        if (!Directory.Exists(projectItemDir)) Directory.CreateDirectory(projectItemDir);

        var inputFileName = Path.Combine(projectItemDir, "SR.strings");
        var relativeDir = Path.GetRelativePath(projectDir, projectItemDir);
        var generated = new CodeGenerator().GenerateCSharpResources(inputFileName).ToList();
        var vsProject = new VSProject(projectFileName);
        var elementNameStrings = Path.Combine(relativeDir, Path.GetFileName(inputFileName));


        //var itemGroup = vsProject.FindOrCreateProjectItemGroupElement("Non", "Resources\\SR.strings");
        var itemGroup = vsProject.FindOrCreateProjectItemGroupElement("Non", elementNameStrings);

        var metaData = new Dictionary<string, string>
        {
            ["AutoGen"] = "True",
            ["DependentUpon"] = Path.GetFileName(inputFileName),
            ["DesignTime"] = "True"
        };

        foreach (var path in generated)
        {
            var fileExtension = Path.GetExtension(path);

            switch (fileExtension.ToLower())
            {
                case ".cs":
                    vsProject.UpdateOrCreateItemElement(itemGroup, "Compile", Path.Combine(relativeDir, Path.GetFileName(path)), metaData);
                    break;
                case ".strings":
                    vsProject.UpdateOrCreateItemElement(itemGroup, "Non", Path.Combine(relativeDir, Path.GetFileName(path)));
                    break;
                case ".resx":
                    vsProject.UpdateOrCreateItemElement(itemGroup, "EmbeddedResource", Path.Combine(relativeDir, Path.GetFileName(path)), metaData);
                    break;
            }
        }


        vsProject.Save();

        Directory.Delete(projectDir, true);


        return;


        File.WriteAllText(Path.Combine(projectItemDir, "SR.strings"), string.Empty);
        File.WriteAllText(Path.Combine(projectItemDir, "SR.cs"), string.Empty);
        File.WriteAllText(Path.Combine(projectItemDir, "SR.srt.resx"), string.Empty);
        File.WriteAllText(Path.Combine(projectItemDir, "SR.srt.de.resx"), string.Empty);

        File.WriteAllText(Path.Combine(projectItemDir, "SR2.strings"), string.Empty);
        File.WriteAllText(Path.Combine(projectItemDir, "SR2.cs"), string.Empty);
        File.WriteAllText(Path.Combine(projectItemDir, "SR2.srt.resx"), string.Empty);
        File.WriteAllText(Path.Combine(projectItemDir, "SR2.srt.de.resx"), string.Empty);

        

       

        vsProject.UpdateOrCreateItemElement(itemGroup, "Non", "Resources\\SR.strings");
        vsProject.UpdateOrCreateItemElement(itemGroup, "Compile", "Resources\\SR.cs", metaData);
        vsProject.UpdateOrCreateItemElement(itemGroup, "EmbeddedResource", "Resources\\SR.srt.resx", metaData);
        vsProject.UpdateOrCreateItemElement(itemGroup, "EmbeddedResource", "Resources\\SR.srt.de.resx", metaData);


        metaData["DependentUpon"] = "SR2.strings";

        itemGroup = vsProject.FindOrCreateProjectItemGroupElement("Non", "Resources\\SR2.strings");

        vsProject.UpdateOrCreateItemElement(itemGroup, "Non", "Resources\\SR2.strings");
        vsProject.UpdateOrCreateItemElement(itemGroup, "Compile", "Resources\\SR2.cs", metaData);
        vsProject.UpdateOrCreateItemElement(itemGroup, "EmbeddedResource", "Resources\\SR2.srt.resx", metaData);
        vsProject.UpdateOrCreateItemElement(itemGroup, "EmbeddedResource", "Resources\\SR2.srt.de.resx", metaData);



        vsProject.Save();

        Directory.Delete(projectDir, true);
    }

}