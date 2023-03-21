using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Xunit;

namespace Oleander.StrResGen.Tests
{
    public class CodeGeneratorTests
    {
        [Fact]
        public void Test1()
        {
            var projectDir = GetProjectDir(AppDomain.CurrentDomain.BaseDirectory);
            Assert.NotNull(projectDir);

            var inputFileName = Path.Combine(projectDir, "SR.strings");
            var generated = CodeGenerator.GenerateCSharpResources(inputFileName).ToList();

            Assert.Equal(3, generated.Count);
        }


        [Fact]
        public void Test2()
        {
            var projectDir = GetProjectDir(AppDomain.CurrentDomain.BaseDirectory);
            Assert.NotNull(projectDir);

            this.AddToProject(GetProjectFile(AppDomain.CurrentDomain.BaseDirectory), "SR.strings", "SR.srt.resx");
            this.AddToProject(GetProjectFile(AppDomain.CurrentDomain.BaseDirectory), "SR.strings", "SR.srt.de.resx");
        }

        private static string? GetProjectDir(string baseDirectory)
        {
            var projectFileName = string.Empty;
            var dirInfo = new DirectoryInfo(baseDirectory);
            var parentDir = dirInfo;

            while (parentDir != null)
            {
                var fileInfo = parentDir.GetFiles("*.csproj").FirstOrDefault();
                if (fileInfo != null)
                {
                    return parentDir.FullName;
                }

                parentDir = parentDir.Parent;
            }

            return null;
        }


        private static string? GetProjectFile(string baseDirectory)
        {
            var projectFileName = string.Empty;
            var dirInfo = new DirectoryInfo(baseDirectory);
            var parentDir = dirInfo;

            while (parentDir != null)
            {
                var fileInfo = parentDir.GetFiles("*.csproj").FirstOrDefault();
                if (fileInfo != null)
                {
                    return fileInfo.FullName;
                }

                parentDir = parentDir.Parent;
            }

            return null;
        }






        private void AddToProject(string projectFileName, string stringResFile, string resFile)
        {

            //var coll = new ProjectCollection();
            //coll.LoadProject()

            //var fileStream = File.Open(projectFileName, FileMode.Open);
            //var project = ProjectRootElement.Create(new XmlTextReader(fileStream));



            var project = ProjectRootElement.Open(
                projectFileName,
                ProjectCollection.GlobalProjectCollection,
                preserveFormatting: true);




            //if (project.ItemGroups.Any(itemGroupElement => itemGroupElement.Items.Any(x => x.ElementName == "EmbeddedResource" && x.Update == resFile)))
            //{
            //    return;
            //}




            //ProjectItemGroupElement group = project.AddItemGroup();

            //// MUST set the Update attribute before adding it to the group
            //ProjectItemElement compile = project.CreateItemElement("Compile");
            //compile.Update = childName;
            //group.AppendChild(compile);

            //// MUST be in the group before we can add metadata
            //compile.AddMetadata("AutoGen", "True");
            //compile.AddMetadata("DependentUpon", parentName);








            var item = project.ItemGroups.FirstOrDefault(itemGroupElement => itemGroupElement.Items.Any(x => x.ElementName == "EmbeddedResource" && x.Update == resFile));
          


            foreach (var itemGroupElement in project.ItemGroups)
            {
                if (itemGroupElement.Items.Any(x => x.ElementName == "None" && x.Update == stringResFile))
                {


                    ProjectItemElement compile = project.CreateItemElement("EmbeddedResource");
                    compile.Update = resFile;

                    itemGroupElement.AppendChild(compile);

                    // MUST be in the group before we can add metadata
                    compile.AddMetadata("AutoGen", "True");
                    compile.AddMetadata("DependentUpon", stringResFile);
                    compile.AddMetadata("DesignTime", "True");






                    //var metaData = new Dictionary<string, string>
                    //{
                    //    ["DependentUpon"] = stringResFile,
                    //    ["DesignTime"] = "True",
                    //    ["AutoGen"] = "True"
                    //};

                    //var projectItemElement = itemGroupElement.AddItem("EmbeddedResource", "Update", metaData);


                    //projectItemElement.Update = resFile;

                   


                }
            }

            //fileStream.Flush(true);
            //fileStream.Close();

         
            //project.Save(projectFileName);
            project.Save();


        }
    }
}