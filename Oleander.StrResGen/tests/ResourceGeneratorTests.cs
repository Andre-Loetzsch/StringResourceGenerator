using System;
using System.IO;
using Xunit;

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
        TestHelper.AssertTest(strResFile, null, "ClassLibrary.Resources");
    }

    [Fact]
    public void TestGeneratedNamespace_NameSpaceIsNotNull()
    {
        var projectDir = TestHelper.PrepareTest("MyProject2");
        var projectFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "ClassLibrary.csproj");
        var strResFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "SR.strings");

        TestHelper.CopyFile(projectFileName, projectDir);
        strResFile = TestHelper.CopyFile(strResFile, Path.Combine(projectDir, "Resources"));
        TestHelper.AssertTest(strResFile, "ClassLibrary2.Resources", "ClassLibrary2.Resources");
    }

    [Fact]
    public void TestGenerated_CustomToolNamespace()
    {
        var projectDir = TestHelper.PrepareTest("MyProject3");
        var projectFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "ClassLibrary.csproj");
        var strResFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "SR.strings");

        projectFileName = TestHelper.CopyFile(projectFileName, projectDir);
        strResFile = TestHelper.CopyFile(strResFile, Path.Combine(projectDir, "Resources"));

        TestHelper.AddMetaDataToItemElement(projectFileName, "None", "Resources\\SR.strings", "CustomToolNamespace", "MyProject.TestGenerated_CustomToolNamespace.Resources");
        TestHelper.AssertTest(strResFile, null, "MyProject.TestGenerated_CustomToolNamespace.Resources");
    }

    [Fact]
    public void TestGenerated_accessor_namespace()
    {
        var projectDir = TestHelper.PrepareTest("MyProject4");
        var projectFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "ClassLibrary.csproj");
        var strResFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "SR.strings");

        projectFileName = TestHelper.CopyFile(projectFileName, projectDir);
        strResFile = TestHelper.CopyFile(strResFile, Path.Combine(projectDir, "Resources"));

        TestHelper.AddMetaDataToItemElement(projectFileName, "None", "Resources\\SR.strings", "CustomToolNamespace", "MyProject.TestGenerated_CustomToolNamespace.Resources");
        TestHelper.AddAccessor(strResFile, "#! accessor_namespace=MyProject.accessor_namespace.Resources");
        TestHelper.AssertTest(strResFile, null, "MyProject.accessor_namespace.Resources");
    }
}