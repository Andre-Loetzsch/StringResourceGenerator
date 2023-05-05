using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Completions;
using System.IO;
using System.Linq;
using System.Runtime;

namespace Oleander.StrResGen.Tool.Options;

internal class ExistFilesOption : Option<FileInfo[]>
{
    public ExistFilesOption() : base(name: "--file", description: "The '.strings' file to generate resources")
    {
        this.AddAlias("-f");
        this.AddValidator(result =>
        {
            try
            {
                var fileInfos = (result.GetValueOrDefault<FileInfo[]>() ?? Enumerable.Empty<FileInfo>()).ToList();

                foreach (var fileInfo in fileInfos)
                {
                    if (!string.Equals(Path.GetExtension(fileInfo.FullName).Trim('\"'), ".strings"))
                    {
                        result.ErrorMessage = $"File must have an '*.strings' extension: {fileInfo.FullName}";
                        return;
                    }

                    if (fileInfo.Exists) continue;
                    result.ErrorMessage = $"File does not exist: {fileInfo.FullName}";
                    return;
                }
            }
            catch (Exception ex)
            {
                result.ErrorMessage = ex.Message;
            }
        });


        this.AddCompletions(ctx =>
        {
            var wordToComplete = ctx.WordToComplete;
            var fileItems = new List<CompletionItem>();
            var strResFiles = new List<string>();

            //************************************************************************************

            //wordToComplete = "O";
            //////wordToComplete = "Oleander.StrResGen.";
            //Directory.SetCurrentDirectory("D:\\dev\\git\\oleander\\StringResourceGenerator");

            //************************************************************************************

            // TODO remove File.WriteAllText
            File.WriteAllText(@"D:\WordToComplete-f.log", $"{Environment.NewLine}--{DateTime.Now}--{Environment.NewLine}{wordToComplete}{Environment.NewLine}");


            if (string.IsNullOrEmpty(wordToComplete)) return fileItems;

            var toReplace = new Dictionary<string, string>();
            var directorySeparatorChar = wordToComplete.Contains('\\') ? '\\' : '/';
            var wordToCompleteList = wordToComplete.Split(new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            var lastWordToComplete = wordToCompleteList.Last();

            if (wordToCompleteList.Count > 1)
            {
                wordToCompleteList.RemoveAt(wordToCompleteList.Count - 1);
            }
            else
            {
                lastWordToComplete = "";
            }

            var currentDirectory = Directory.GetCurrentDirectory();
            File.AppendAllText(@"D:\WordToComplete-f.log", $"{currentDirectory}-- currentDirectory {Environment.NewLine}");

            var pathTest = currentDirectory;

            if (wordToCompleteList.Count > 0 && wordToComplete.StartsWith(directorySeparatorChar))
            {
                pathTest = string.Join(Path.DirectorySeparatorChar, wordToCompleteList);

                if (!new DirectoryInfo(pathTest).Exists)
                {
                    var driveInfoNames = DriveInfo.GetDrives().Select(x => x.Name.ToLower()).ToList();
                    var driveInfo = driveInfoNames.FirstOrDefault(x => x.StartsWith(wordToCompleteList[0].ToLower()));

                    if (driveInfo == null) return fileItems;

                    wordToCompleteList[0] = driveInfo;
                    var newPathTest = Path.GetFullPath(string.Join(Path.DirectorySeparatorChar, wordToCompleteList));

                    toReplace[newPathTest] = string.Concat(pathTest, directorySeparatorChar);
                    pathTest = newPathTest;
                }
            }

            var dirInfoTest = new DirectoryInfo(pathTest);
            if (!dirInfoTest.Exists) return fileItems;

            var isRelativePath = wordToCompleteList.Count == 0 || pathTest != dirInfoTest.FullName;

            // Test directory without last word
            if (!isRelativePath)
            {
                strResFiles.Add(string.Concat(dirInfoTest.FullName, directorySeparatorChar));
                strResFiles.AddRange(dirInfoTest.GetFiles($"{lastWordToComplete}*.strings",
                    SearchOption.TopDirectoryOnly).Select(x => x.FullName));
            }

            this.AddSingleDirectory(strResFiles, dirInfoTest.GetDirectories("*", SearchOption.TopDirectoryOnly)
                    .Where(x => x.Name.StartsWith(lastWordToComplete) && x.Name != lastWordToComplete), directorySeparatorChar);

            // Test directory with last word
            pathTest = Path.Combine(pathTest, lastWordToComplete);
            dirInfoTest = new DirectoryInfo(pathTest);

            if (dirInfoTest.Exists && dirInfoTest.FullName.EndsWith(lastWordToComplete))
            {
                strResFiles.Add(string.Concat(dirInfoTest.FullName, directorySeparatorChar));
                strResFiles.AddRange(dirInfoTest.GetFiles("*.strings",
                    SearchOption.TopDirectoryOnly).Select(x => x.FullName));

                this.AddSingleDirectory(strResFiles, dirInfoTest.GetDirectories("*", SearchOption.TopDirectoryOnly), directorySeparatorChar);
            }

            var firstChar = wordToComplete.StartsWith(directorySeparatorChar) ? directorySeparatorChar.ToString() : string.Empty;
            var pathToRemove = (isRelativePath ? currentDirectory : string.Empty)
                .Replace('\\', directorySeparatorChar)
                .Replace('/', directorySeparatorChar);

            strResFiles = strResFiles.Distinct().ToList();

            for (var i = 0; i < strResFiles.Count; i++)
            {
                var label = $"{firstChar}{strResFiles[i]}";

                foreach (var (key, value) in toReplace)
                {
                    label = label.Replace(key, value);
                }

                label = label
                    .Replace("\\\\", directorySeparatorChar.ToString())
                    .Replace('\\', directorySeparatorChar)
                    .Replace("//", directorySeparatorChar.ToString())
                    .Replace('/', directorySeparatorChar);

                if (isRelativePath && label.StartsWith(pathToRemove))
                {
                    label = label[pathToRemove.Length..];
                    //File.AppendAllText(@"D:\WordToComplete-f.log", string.Join(Environment.NewLine, $"pathToRemove.Length:{pathToRemove.Length}"));
                }

                var indexOf = label.IndexOf(wordToComplete, StringComparison.InvariantCulture);
                if (indexOf >= 0) label = label[indexOf..];
                if (!label.StartsWith(wordToComplete)) continue;

                fileItems.Add(new(label: label, sortText: $"{i:000}"));
            }

            // TODO remove File.WriteAllText
            File.AppendAllText(@"D:\WordToComplete-f.log", string.Join(Environment.NewLine, fileItems.Select(x => x.Label)));

            return fileItems;

        });



        

    }


    private void AddSingleDirectory(List<string> strResFiles, IEnumerable<DirectoryInfo> dirInfos, char directorySeparatorChar)
    {
        var dirInfoList = dirInfos.ToList();

        foreach (var dirInfo in dirInfoList)
        {
            strResFiles.Add(string.Concat(dirInfo.FullName, directorySeparatorChar));

            try
            {
                strResFiles.AddRange(dirInfo.GetFiles("*.strings", SearchOption.TopDirectoryOnly)
                    .Select(fileInfo => fileInfo.FullName));
            }
            catch
            {
               //
            }

            if (dirInfoList.Count == 1)
            {
                this.AddSingleDirectory(strResFiles, dirInfo.GetDirectories("*", SearchOption.TopDirectoryOnly), directorySeparatorChar);
            }
        }
    }

}