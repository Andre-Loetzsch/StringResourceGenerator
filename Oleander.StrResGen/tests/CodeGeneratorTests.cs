using System;
using System.IO;
using System.Linq;
using Xunit;

namespace Oleander.StrResGen.Tests;

public class CodeGeneratorTests
{

    [Fact]
    public void Test_inputFileName_is_null()
    {
        Assert.Throws<ArgumentNullException>(() =>
        {
            #pragma warning disable CS8625
            _ = new CodeGenerator().GenerateCSharpResources(null);
            #pragma warning restore CS8625
        });
    }

    [Fact]
    public void Test_inputFileName_does_not_exist()
    {
        var inputFileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SR.strings");

        if (File.Exists(inputFileName)) File.Delete(inputFileName);

        var generated = new CodeGenerator().GenerateCSharpResources(inputFileName, "Oleander.StrResGen.Tests").ToList();

        Assert.True(File.Exists(inputFileName));

        Assert.Equal(3, generated.Count);
        Assert.EndsWith("SR.cs", generated[0]);
        Assert.EndsWith("SR.srt.resx", generated[1]);
        Assert.EndsWith("SR.srt.de.resx", generated[2]);

        File.Delete(inputFileName);
        File.Delete(generated[0]);
        File.Delete(generated[1]);
        File.Delete(generated[2]);
    }


}