using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using System;
using System.IO;
using System.Linq;
using Xunit;
using static System.Net.WebRequestMethods;

namespace Oleander.StrResGen.Tests;

public class ResourceGeneratorTests
{
    [Fact]
    public void TestGeneratedNamespace_NameSpaceIsNull()
    {
        var projectDir = TestHelper.PrepareTest("MyProject1");
        var projectFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "ClassLibrary.csproj");
        var strResFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "SR.strings");

        TestHelper.CopyFile(projectFileName, projectDir);
        strResFile = TestHelper.CopyFile(strResFile, Path.Combine(projectDir, "Resources"));
        TestHelper.AssertExpectedNameSpace(strResFile, null, "ClassLibrary.Resources");
    }

    [Fact]
    public void TestGeneratedNamespace_NameSpaceIsNotNull()
    {
        var projectDir = TestHelper.PrepareTest("MyProject2");
        var projectFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "ClassLibrary.csproj");
        var strResFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "SR.strings");

        TestHelper.CopyFile(projectFileName, projectDir);
        strResFile = TestHelper.CopyFile(strResFile, Path.Combine(projectDir, "Resources"));
        TestHelper.AssertExpectedNameSpace(strResFile, "ClassLibrary2.Resources", "ClassLibrary2.Resources");
    }

    [Fact]
    public void TestGenerated_CustomToolNamespace()
    {
        var projectDir = TestHelper.PrepareTest("MyProject3");
        var projectFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "ClassLibrary.csproj");
        var strResFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "SR.strings");

        projectFileName = TestHelper.CopyFile(projectFileName, projectDir);
        strResFile = TestHelper.CopyFile(strResFile, Path.Combine(projectDir, "Resources"));

        TestHelper.AddMetaDataToItemElement(projectFileName, "None", $"Resources{Path.DirectorySeparatorChar}SR.strings", "CustomToolNamespace", "MyProject.TestGenerated_CustomToolNamespace.Resources");
        TestHelper.AssertExpectedNameSpace(strResFile, null, "MyProject.TestGenerated_CustomToolNamespace.Resources");
    }

    [Fact]
    public void TestGenerated_accessor_namespace()
    {
        var projectDir = TestHelper.PrepareTest("MyProject4");
        var projectFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "ClassLibrary.csproj");
        var strResFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "SR.strings");

        projectFileName = TestHelper.CopyFile(projectFileName, projectDir);
        strResFile = TestHelper.CopyFile(strResFile, Path.Combine(projectDir, "Resources"));

        TestHelper.AddMetaDataToItemElement(projectFileName, "None", $"Resources{Path.DirectorySeparatorChar}SR.strings", "CustomToolNamespace", "MyProject.TestGenerated_CustomToolNamespace.Resources");
        TestHelper.AddAccessor(strResFile, "#! accessor_namespace=MyProject.accessor_namespace.Resources");
        TestHelper.AssertExpectedNameSpace(strResFile, null, "MyProject.accessor_namespace.Resources");
    }

    [Fact]
    public void Test_LegacyProject()
    {
        var projectDir = TestHelper.PrepareTest("MyLegacyProject");
        var projectFileName = TestHelper.CopyFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "LegacyProject", "LegacyProject.csproj"), projectDir);
        var strResFile = TestHelper.CopyFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "LegacyProject", "Basic_SR.strings"), projectDir);
        var projectRootElement = ProjectRootElement.Open(
            projectFileName,
            ProjectCollection.GlobalProjectCollection,
            preserveFormatting: true);

        Assert.False(projectRootElement.TryGetProjectItemElement("Compile", "Basic_SR.cs", out _));
        Assert.Single(projectRootElement.GetProjectItemElements("EmbeddedResource", "Basic_SR.srt.de.resx"));

        new ResourceGenerator().Generate(strResFile, null);

        projectRootElement = ProjectRootElement.Open(
            projectFileName,
            ProjectCollection.GlobalProjectCollection,
            preserveFormatting: true);

        Assert.True(projectRootElement.TryGetProjectItemElement("Compile", "Basic_SR.cs", out var itemElement));
        Assert.True(string.IsNullOrEmpty(itemElement?.Update));
        Assert.False(string.IsNullOrEmpty(itemElement?.Include));
        Assert.Equal("Basic_SR.cs", itemElement!.Include);

        Assert.Single(projectRootElement.GetProjectItemElements("EmbeddedResource", "Basic_SR.srt.de.resx"));
        TestHelper.AssertExpectedSRClassName(strResFile, null, "Basic_SR");
        TestHelper.AssertExpectedSRClassName(strResFile, null, "Keys_Basic_SR");
    }

    [Fact]
    public void TestDotnetCoreProject()
    {
        var projectDir = TestHelper.PrepareTest("MyDotnetCoreProject");
        var projectFileName = TestHelper.CopyFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "DotnetCore.csproj"), projectDir);
        var strResFile = TestHelper.CopyFile(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "Basic_SR.strings"), projectDir);
        var projectRootElement = ProjectRootElement.Open(
            projectFileName,
            ProjectCollection.GlobalProjectCollection,
            preserveFormatting: true);

        Assert.False(projectRootElement.TryGetProjectItemElement("Compile", "Basic_SR.cs", out _));

        new ResourceGenerator().Generate(strResFile, null);

        projectRootElement = ProjectRootElement.Open(
            projectFileName,
            ProjectCollection.GlobalProjectCollection,
            preserveFormatting: true);

        Assert.True(projectRootElement.TryGetProjectItemElement("Compile", "Basic_SR.cs", out var itemElement));
        Assert.True(string.IsNullOrEmpty(itemElement?.Include));
        Assert.False(string.IsNullOrEmpty(itemElement?.Update));
        Assert.Equal("Basic_SR.cs", itemElement!.Update);

        TestHelper.AssertExpectedSRClassName(strResFile, null, "Basic_SR");
        TestHelper.AssertExpectedSRClassName(strResFile, null, "Basic_SRKeys");
    }
}