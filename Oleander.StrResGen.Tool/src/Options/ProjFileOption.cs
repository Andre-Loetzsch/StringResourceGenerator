using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;

namespace Oleander.StrResGen.Tool.Options;

internal class ProjFileOption : Option<FileInfo>
{
    public ProjFileOption() : base(name: "--proj", description: "The project file to which the resources belong")
    {
        this.AddAlias("-p");

        this.AddValidator(result =>
        {
            var fullName = result.GetValueOrDefault<FileInfo>()?.FullName;

            if (fullName == null) return;

            if (!string.Equals(Path.GetExtension(fullName), ".csproj"))
            {
                result.ErrorMessage = $"Invalid project file: '{fullName}'";
            }
        });

        this.AddCompletions(ctx =>
        {
            var wordToComplete = ctx.WordToComplete ?? string.Empty;

            //************************************************************************************

            wordToComplete = "O/.";
            Directory.SetCurrentDirectory("D:\\dev\\git\\oleander\\StringResourceGenerator");

            //************************************************************************************

            File.WriteAllText(@"D:\WordToComplete.log", $"{wordToComplete}{Environment.NewLine}");

            var result = new List<string>();
            var pathList = new List<string>();

            var relativePath = string.Empty;
            var relativeParentPath = string.Empty;

            if (!string.IsNullOrEmpty(wordToComplete))
            {
                var directories = wordToComplete.Split('/', StringSplitOptions.RemoveEmptyEntries).ToList();
                var last = directories.Last();
                var rest = string.Empty;

                if (!this.TryFindPath(directories, out var currentDirectory))
                {
                    rest = last;
                    directories.Remove(last);

                    if (!this.TryFindPath(directories, out currentDirectory))
                    {
                        currentDirectory = Directory.GetCurrentDirectory();
                        directories.Clear();
                    }
                }

                relativePath = string.Join('/', directories);
                if (directories.Count > 1)  directories.RemoveAt(directories.Count - 1);
                relativeParentPath = string.Join('/', directories);

                var currentDirectoryInfo = new DirectoryInfo(currentDirectory);

                if (string.IsNullOrEmpty(rest) && currentDirectoryInfo is { Exists: true, Parent: not null })
                {
                    foreach (var dirInfo in currentDirectoryInfo.Parent.GetDirectories())
                    {
                        if (!string.IsNullOrEmpty(last) && !dirInfo.Name.StartsWith(last)) continue;

                        if (string.IsNullOrEmpty(relativeParentPath))
                        {
                            pathList.Add(dirInfo.Name);
                            continue;
                        }

                        pathList.Add($"{relativeParentPath}\\{dirInfo.Name}".Replace('/', '\\'));
                    }
                }

                if (!string.IsNullOrEmpty(rest) && currentDirectoryInfo.Exists)
                {
                    foreach (var dirInfo in currentDirectoryInfo.GetDirectories())
                    {
                        if (!dirInfo.Name.StartsWith(rest)) continue;

                        if (string.IsNullOrEmpty(relativePath))
                        {
                            pathList.Add(dirInfo.Name);
                            continue;
                        }

                        pathList.Add($"{relativePath}\\{dirInfo.Name}".Replace('/', '\\'));
                    }
                }

                var startChar = wordToComplete.StartsWith('/') ? "/" : string.Empty;

                foreach (var path in pathList)
                {
                    var dirInfo = new DirectoryInfo(path);
                    if (!dirInfo.Exists) continue;

                    result.Add($"{startChar}{path}/".Replace('\\', '/').Replace(":", string.Empty));

                    if (!dirInfo.GetDirectories().Any()) continue;
                    result.AddRange(dirInfo.GetDirectories()
                        .Select(subDirInfo => $"{startChar}{path}/{subDirInfo.Name}/".Replace('\\', '/').Replace(":", string.Empty)));
                }
            }

            File.AppendAllText(@"D:\WordToComplete.log", string.Join(Environment.NewLine, result));

            return result;

        });



    }


    private bool TryFindPath(IList<string> pathItems, out string path)
    {
        path = string.Empty;
        if (pathItems.Count == 0) return false;

        var testPathItems = new List<string>(pathItems);
        var last = testPathItems.Last();
        var relativePathTest = string.Join('/', testPathItems);
        var dirInfoTest = new DirectoryInfo(relativePathTest);

        if (dirInfoTest.Exists && dirInfoTest.Name == last)
        {
            path = dirInfoTest.FullName;
            return true;
        }

        if (testPathItems.Count < 2) return false;

        var driveInfo = DriveInfo.GetDrives().FirstOrDefault(x => x.Name.StartsWith(testPathItems[0]));

        if (driveInfo == null) return false;

        testPathItems[0] = driveInfo.Name;
        relativePathTest = string.Join('/', testPathItems);
        dirInfoTest = new DirectoryInfo(relativePathTest);

        if (dirInfoTest.Exists && dirInfoTest.Name == last)
        {
            path = dirInfoTest.FullName;
            pathItems[0] = testPathItems[0];
            return true;
        }

        return false;
    }

}