using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Xml;
using System.Xml.XPath;
using Microsoft.CSharp;

namespace Oleander.StrResGen;

public class CodeGenerator
{
    public static ReportErrors ReportErrors { get; set; } = (message, line, lineNo) => Console.WriteLine($"{message} - line: {line} lineNo: {lineNo}");




    public static string? SingleFileGeneratorGenerate(string inputFileName, string? fileNameSpace = null)
    {
        if (string.IsNullOrWhiteSpace(inputFileName)) throw new ArgumentNullException(nameof(inputFileName));

        if (string.IsNullOrEmpty(fileNameSpace))
        {
            fileNameSpace = GetNameSpaceFromProjectItem(inputFileName);
        }
     
        var generatedFiles = GenerateResourceFiles(inputFileName).ToList();

        if (!generatedFiles.Any()) return null;

       





        var cSharpCode = GenerateCSharpCode(generatedFiles.First(), fileNameSpace);

        return string.IsNullOrEmpty(cSharpCode) ? null : cSharpCode;
    }







    private static IEnumerable<string> GenerateResourceFiles(string stringsSource)
    {
        var resourceFiles = new List<string>();
        var locale = GetLocaleFromFileName(stringsSource);
        var template = new XmlDocument();

        using (var stream = typeof(CodeGenerator).Assembly.GetManifestResourceStream(typeof(CodeGenerator).Namespace + ".Template.xml"))
        {
            if (stream == null)
            {
                ReportError("Resource Template.xml not found!", string.Empty, 0);
                return Enumerable.Empty<string>();
            }

            template.Load(stream);
        }

        var ext = Path.GetExtension(stringsSource).ToLower();

        if (ext.StartsWith(".strings"))
        {
            if (IsCultureSpecified(stringsSource)) ParseHeader("#! generate_class = false", template);
        }
        else
        {
            Console.WriteLine($"File must have *.strings extension ({stringsSource})", "0", 0);
            return Enumerable.Empty<string>();
        }

        if (!ParseHeader($"#! stringSource={stringsSource}", template)) return Enumerable.Empty<string>();

        XmlDocument? doc = null;
        XmlElement? prevElement = null;

        using (TextReader tr = File.OpenText(stringsSource))
        {
            var lineNo = 0;

            while (tr.ReadLine() is { } line)
            {
                lineNo++;

                if (line.Trim().Length == 0)
                {
                    prevElement = null;
                    continue;
                }

                if (line[0] == ';')
                {
                    prevElement = null;
                    continue;
                }

                if (ParseHeader(line, template)) continue;

                // Only process the [strings] section of the file.
                if (line[0] == '[')
                {
                    if (doc != null)
                    {
                        resourceFiles.Add(SaveResX(stringsSource, locale, doc));
                        doc = null;
                    }

                    if (line.Trim().ToLower().Substring(0, 8) == "[strings")
                    {
                        var strLocale = line.Trim().Substring(8, line.Trim().Length - 9);
                        if (strLocale != "") locale = strLocale;
                        doc = new XmlDocument();

                        if (template.DocumentElement == null)
                        {
                            ReportError("template.DocumentElement == null", line, lineNo);
                        }
                        else
                        {
                            doc.AppendChild(doc.ImportNode(template.DocumentElement, true));
                        }

                    }
                    continue;
                }

                // Not writing to a resource document.
                if (doc == null) continue;


                // Could use regex here, but this is probably faster.
                var split = line.IndexOf('=');
                if (split == -1)
                {
                    ReportError("Invalid resource, missing = sign", line, lineNo);
                    continue;
                }

                var res = line.Substring(0, split).Trim();
                var text = line.Substring(split + 1).Trim();
                string? comment = null;

                if (res.Length == 0)
                {
                    if (prevElement != null)
                    {
                        prevElement.InnerText = prevElement.InnerText + "\n" + text;
                    }
                    else
                    {
                        ReportError("Invalid resource, missing resource name", line, lineNo);
                    }
                    continue;
                }

                var resArgSplit = res.IndexOf('(');
                if (resArgSplit > -1)
                {
                    if (resArgSplit == 0)
                    {
                        ReportError("Invalid resource, no resource name", line, lineNo);
                        continue;
                    }

                    var resArgSplit2 = res.IndexOf(')', resArgSplit);
                    if (resArgSplit2 == -1)
                    {
                        ReportError("Invalid resource, missing end bracket on arguments", line, lineNo);
                        continue;
                    }
                    comment = res.Substring(resArgSplit + 1, resArgSplit2 - resArgSplit - 1);
                    res = res.Substring(0, resArgSplit);
                }

                var el = doc.CreateElement("data");
                el.SetAttribute("name", res);

                var valueEl = doc.CreateElement("value");
                valueEl.AppendChild(doc.CreateTextNode(text));
                el.AppendChild(valueEl);

                if (comment != null)
                {
                    var commentEl = doc.CreateElement("comment");
                    commentEl.AppendChild(doc.CreateTextNode(comment));
                    el.AppendChild(commentEl);
                }

                // Allow appending new lines
                prevElement = valueEl;

                if (doc.DocumentElement == null)
                {
                    ReportError("doc.DocumentElement == null", line, lineNo);
                }
                else
                {
                    doc.DocumentElement.AppendChild(el);
                }
            }
        }

        if (doc != null)
        {
            resourceFiles.Add(SaveResX(stringsSource, locale, doc));
        }

        if (resourceFiles.Count == 0)
        {
            ReportError("No [strings] section has been found.", string.Empty, 0);
        }

        return resourceFiles;
    }

    private static string GetLocaleFromFileName(string inputFileName)
    {
        var resFilename = Path.GetFileNameWithoutExtension(inputFileName);
        if (string.IsNullOrEmpty(resFilename)) return string.Empty;

        var localeIndex = resFilename.LastIndexOf(".", StringComparison.OrdinalIgnoreCase);

        if (localeIndex != -1 && IsCultureSpecified(inputFileName))
        {
            return resFilename.Substring(localeIndex);
        }

        return string.Empty;
    }

    private static bool ParseHeader(string line, XmlDocument template)
    {
        if (line[0] != '#') return false;
        if (line.Length == 1) return true;
        if (line[1] != '!') return true;

        // Options
        var pos = line.IndexOf('=');

        if (pos <= -1) return true;

        var opt = line.Substring(2, pos - 2).Trim();
        var arg = line.Substring(pos + 1).Trim();

        // Add to the headers
        var resHeaderEl = template.CreateElement("resheader");
        resHeaderEl.SetAttribute("name", opt);

        var resValueEl = template.CreateElement("value");
        resValueEl.AppendChild(template.CreateTextNode(arg));
        resHeaderEl.AppendChild(resValueEl);

        if (template.DocumentElement == null) return false;
        template.DocumentElement.AppendChild(resHeaderEl);
        return true;
    }

    private static string SaveResX(string source, string locale, XmlDocument doc)
    {
        var resFilename = source;

        if (IsCultureSpecified(resFilename))
        {
            resFilename = Path.GetFileNameWithoutExtension(resFilename);
        }

        resFilename = Path.GetFileNameWithoutExtension(resFilename);
        resFilename += ".srt" + locale;

        var directory = Path.GetDirectoryName(source) ?? string.Empty;
        var fullFilename = Path.Combine(directory, $"{resFilename}.resx");

        if (File.Exists(fullFilename))
        {
            var attributes = File.GetAttributes(fullFilename);

            if ((attributes & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
            {
                attributes = RemoveAttribute(attributes, FileAttributes.ReadOnly);
                File.SetAttributes(fullFilename, attributes);
                Console.WriteLine($"{fullFilename} ReadOnly attribute removed.");
            }
        }

        try
        {
            doc.Save(fullFilename);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Save '{fullFilename}' failed! {ex.Message}");
        }

        return fullFilename;
    }

    private static FileAttributes RemoveAttribute(FileAttributes attributes, FileAttributes attributesToRemove)
    {
        return attributes & ~attributesToRemove;
    }

    private static string GetNameSpaceFromProjectItem(string inputFileName)
    {
        var directory = Path.GetDirectoryName(inputFileName);
        if (directory == null) return "Oleander.StrResGen";

        var projectFileName = string.Empty;
        var dirInfo = new DirectoryInfo(directory);
        var parentDir = dirInfo;

        while (parentDir != null)
        {
            var fileInfo = parentDir.GetFiles("*.csproj").FirstOrDefault();
            if (fileInfo != null)
            {
                projectFileName = fileInfo.FullName;
                break;
            }

            parentDir = parentDir.Parent;
        }


        var fileNameSpace = (Path.GetDirectoryName(inputFileName) ?? string.Empty)
            .Replace(Path.GetDirectoryName(projectFileName) ?? string.Empty, string.Empty)
            .Replace("\\", ".");

        fileNameSpace = string.Concat(Path.GetFileNameWithoutExtension(projectFileName), fileNameSpace);
        return fileNameSpace;
    }

    private static string? GenerateCSharpCode(string inputFileName, string? fileNameSpace = null)
    {
        GenerationOptions? options;
        Dictionary<string, string> commandLines;

        using (Stream inputStream = File.OpenRead(inputFileName))
        {
            var doc = new XPathDocument(inputStream);
            options = GetGenerationOptions(inputFileName, doc);

            if (options == null) return null;

            if (!string.IsNullOrEmpty(fileNameSpace))
            {
                options.SRNamespace = fileNameSpace;
            }

            commandLines = ParsLines(doc);
        }

        var codeProvider = new CSharpCodeProvider();
        var ccu = Process(codeProvider, options, commandLines, inputFileName);
        var sw = new StringWriter();
        var opts = new CodeGeneratorOptions { BracingStyle = "C" };
        codeProvider.GenerateCodeFromCompileUnit(ccu, sw, opts);
        sw.Close();

        return sw.GetStringBuilder().ToString();
    }

    private string GenerateCSharpCode(string inputFileName, GenerationOptions options)
    {
        Dictionary<string, string> commandLines;

        using (Stream inputStream = File.OpenRead(inputFileName))
        {
            commandLines = ParsLines(new XPathDocument(inputStream));
        }

        var codeProvider = new CSharpCodeProvider();
        var ccu = Process(new CSharpCodeProvider(), options, commandLines, inputFileName);
        var sw = new StringWriter();
        var opts = new CodeGeneratorOptions { BracingStyle = "C" };
        codeProvider.GenerateCodeFromCompileUnit(ccu, sw, opts);
        sw.Close();

        return sw.GetStringBuilder().ToString();
    }

    private static void ReportError(string message, string line, int lineNo)
    {
        ReportErrors.Invoke(message, line, lineNo);
    }

    private static GenerationOptions? GetGenerationOptions(string inputFileName, IXPathNavigable doc)
    {
        var options = new GenerationOptions();
        var nav = doc.CreateNavigator();

        if (nav == null)
        {
            ReportError("nav is null!", "var nav = doc.CreateNavigator();", 0);
            return null;
        }

        var headerIt = nav.Select("//resheader");

        while (headerIt.MoveNext())
        {
            if (headerIt.Current == null) continue;
            var opt = headerIt.Current.GetAttribute("name", string.Empty);
            var valueIt = headerIt.Current.Select("value");

            if (valueIt.MoveNext())
            {
                if (valueIt.Current == null) continue;
                var arg = valueIt.Current.Value;

                switch (opt.ToLower())
                {
                    case "accessor_class_accessibility":
                        if (arg.ToLower() == "public")
                        {
                            options.PublicSRClass = true;
                        }
                        break;
                    case "accessor_keys_class_accessibility":
                        if (arg.ToLower() == "public")
                        {
                            options.PublicKeysSRClass = true;
                        }
                        break;
                    case "accessor_class_name":
                        if (!string.IsNullOrEmpty(arg))
                        {
                            options.SRClassName = arg;
                        }
                        break;

                    case "culture_info":
                        options.CultureInfoFragment = arg;
                        break;

                    case "generate_methods_only":
                        options.GenerateMethodsOnly = bool.Parse(arg);
                        break;
                    case "accessor_namespace":
                        options.SRNamespace = arg;
                        break;
                    case "accessor_keys_class_name":
                        options.KeysSRClassName = arg;
                        break;
                }
            }
        }

        if (string.IsNullOrEmpty(options.SRClassName))
        {
            options.SRClassName = Path.GetFileNameWithoutExtension(inputFileName);
            //--remove srt---
            options.SRClassName = options.SRClassName.Replace(".srt", "");
            options.SRClassName = options.SRClassName.Replace('.', '_').Replace("-", "_");
        }
        // remove .resx (resource.locale.resx, messages.de-DE.resx)
        options.ResourceName = Path.GetFileNameWithoutExtension(inputFileName);
        options.ResourceName = Path.GetFileNameWithoutExtension(options.ResourceName);
        // remove .locale
        if (IsCultureSpecified(inputFileName))
        {
            options.ResourceName = Path.GetFileNameWithoutExtension(options.ResourceName);
        }

        return options;
    }

    private static bool IsCultureSpecified(string stringsSource)
    {
        var localeIndex = Path.GetFileNameWithoutExtension(stringsSource).LastIndexOf('.');

        if (localeIndex == -1) return false;

        var culture = Path.GetFileNameWithoutExtension(stringsSource).Substring(localeIndex + 1);
        return CultureInfo.GetCultures(CultureTypes.AllCultures).Any(existingCulture => existingCulture.ToString() == culture);
    }

    private static Dictionary<string, string> ParsLines(IXPathNavigable doc)
    {
        var dict = new Dictionary<string, string>();
        var nav = doc.CreateNavigator();

        if (nav == null)
        {
            ReportError("nav is null!", "var nav = doc.CreateNavigator();", 0);
            return dict;
        }

        var it = nav.Select("//data");

        while (it.MoveNext())
        {
            if (it.Current == null || it.Current.GetAttribute("type", string.Empty).Length != 0)
            {
                continue; // Only interested in string resources
            }

            var key = it.Current.GetAttribute("name", string.Empty);
            var commentValue = string.Empty;

            // Get the value and comment elements
            var valueIt = it.Current.SelectChildren(XPathNodeType.Element);

            while (valueIt.MoveNext())
            {
                if (valueIt.Current == null) continue;
                if (valueIt.Current.Name == "comment")
                {
                    commentValue = valueIt.Current.Value;
                }
            }

            dict[key] = commentValue;
        }

        return dict;
    }

    private static CodeCompileUnit Process(CodeDomProvider codeProvider, GenerationOptions options, IDictionary<string, string> commandLines, string inputFileName)
    {
        var compileUnit = new CodeCompileUnit();

        if (options == null) return compileUnit;

        //Just for VB.NET
        compileUnit.UserData.Add("AllowLateBound", false);
        compileUnit.UserData.Add("RequireVariableDeclaration", true);

        //Dummy namespace, so the Import statements would appear above the main namespace declaration
        var dummyNamespace = new CodeNamespace("");
        compileUnit.Namespaces.Add(dummyNamespace);

        //Namespace Import
        dummyNamespace.Imports.Add(new CodeNamespaceImport("System"));
        dummyNamespace.Imports.Add(new CodeNamespaceImport("System.Resources"));
        dummyNamespace.Imports.Add(new CodeNamespaceImport("System.Reflection"));
        dummyNamespace.Imports.Add(new CodeNamespaceImport("System.Threading"));

        //Namespace
        var nSpace = new CodeNamespace(options.SRNamespace);
        compileUnit.Namespaces.Add(nSpace);

        //Namespace comments
        nSpace.Comments.Add(new CodeCommentStatement("-----------------------------------------------------------------------------"));
        nSpace.Comments.Add(new CodeCommentStatement(" <autogeneratedinfo>"));
        nSpace.Comments.Add(new CodeCommentStatement("     This code was generated by:"));
        nSpace.Comments.Add(new CodeCommentStatement("       SR Resource Generator custom tool for VS.NET, by Tentakel"));
        nSpace.Comments.Add(new CodeCommentStatement(""));
        nSpace.Comments.Add(new CodeCommentStatement("     It contains classes defined from the contents of the resource file:"));
        nSpace.Comments.Add(new CodeCommentStatement("       " + inputFileName));
        nSpace.Comments.Add(new CodeCommentStatement(""));
        nSpace.Comments.Add(new CodeCommentStatement("     Generated: " + DateTime.Now.ToString("f", new CultureInfo("en-US"))));
        nSpace.Comments.Add(new CodeCommentStatement(" </autogeneratedinfo>"));
        nSpace.Comments.Add(new CodeCommentStatement("-----------------------------------------------------------------------------"));

        // Define SR class
        var cSR = new CodeTypeDeclaration(options.SRClassName)
        {
            IsPartial = true,
            TypeAttributes = options.PublicSRClass ? TypeAttributes.Public : TypeAttributes.NotPublic
        };

        // CodeDom Sucks. It doesn't allow support for specifying assembly visibility.
        // So we have to do a search and replace on the resulting text after we have generated
        // the file.

        var keysClassName = options.KeysSRClassName ?? $"{options.ResourceName.Replace('.', '_').Replace('-', '_')}Keys";

        if (options.CultureInfoFragment == null)
        {
            // Create Static Culture property
            var cmStaticCulture = new CodeMemberProperty
            {
                Name = "Culture",
                Attributes = MemberAttributes.Public | MemberAttributes.Static,
                Type = new CodeTypeReference(typeof(CultureInfo))
            };

            cmStaticCulture.GetStatements.Add(
                new CodeMethodReturnStatement(
                    new CodePropertyReferenceExpression(
                        new CodeTypeReferenceExpression(keysClassName), "Culture")));

            cmStaticCulture.SetStatements.Add(
                new CodeAssignStatement(
                    new CodePropertyReferenceExpression(
                        new CodeTypeReferenceExpression(keysClassName), "Culture"),
                    new CodeArgumentReferenceExpression("value")));

            cSR.Members.Add(cmStaticCulture);
        }

        // Define Keys class
        // Define it for name of resource always,
        // so SR class can have several Keys classes
        var cKeys = new CodeTypeDeclaration(keysClassName)
        {
            TypeAttributes = options.PublicKeysSRClass ? TypeAttributes.Public : TypeAttributes.NotPublic
        };

        var fResourceManager = new CodeMemberField(typeof(ResourceManager), "resourceManager")
        {
            Attributes = MemberAttributes.Static | MemberAttributes.Public,
            InitExpression = new CodeObjectCreateExpression(typeof(ResourceManager),
                new CodePrimitiveExpression($"{options.SRNamespace}.{options.ResourceName}.srt"),
                new CodePropertyReferenceExpression(
                    new CodeTypeOfExpression($"{nSpace.Name}.{options.SRClassName}"), "Assembly"))
        };

        cKeys.Members.Add(fResourceManager);

        CodeExpression cultureCodeExp;

        if (options.CultureInfoFragment == null) // Add a property that is settable
        {
            // Culture field and property
            var fCulture = new CodeMemberField(typeof(CultureInfo), "_culture")
            {
                Attributes = MemberAttributes.Static,
                InitExpression = new CodePrimitiveExpression()
            };

            cKeys.Members.Add(fCulture);

            var pCulture = new CodeMemberProperty
            {
                Attributes = MemberAttributes.Static | MemberAttributes.Public,
                Type = new CodeTypeReference(typeof(CultureInfo)),
                Name = "Culture"
            };

            pCulture.GetStatements.Add(
                new CodeMethodReturnStatement(
                    new CodeFieldReferenceExpression(null, "_culture")));

            pCulture.SetStatements.Add(
                new CodeAssignStatement(
                    new CodeFieldReferenceExpression(null, "_culture"),
                    new CodeArgumentReferenceExpression("value")));

            cKeys.Members.Add(pCulture);

            cultureCodeExp = new CodeFieldReferenceExpression(null, "_culture");
        }
        else
        {
            cultureCodeExp = new CodeSnippetExpression(options.CultureInfoFragment);
        }

        // Get String methods

        // No parameters, just return the result of the resourceManager.GetString method.
        // 	return resourceManager.GetString( key, _culture );
        var mGetStringNoParams = new CodeMemberMethod
        {
            Name = "GetString",
            Attributes = MemberAttributes.Public | MemberAttributes.Static,
            ReturnType = new CodeTypeReference(typeof(string))
        };

        mGetStringNoParams.Parameters.Add(
            new CodeParameterDeclarationExpression(typeof(string), "key"));

        mGetStringNoParams.Statements.Add(new CodeMethodReturnStatement(
            new CodeMethodInvokeExpression(
                new CodeFieldReferenceExpression(null, "resourceManager"), "GetString",
                new CodeArgumentReferenceExpression("key"), cultureCodeExp)));

        cKeys.Members.Add(mGetStringNoParams);

        // With parameters, format the results using String.Format
        // return msg;
        var mGetStringWithParams = new CodeMemberMethod
        {
            Name = "GetString",
            Attributes = MemberAttributes.Public | MemberAttributes.Static,
            ReturnType = new CodeTypeReference(typeof(string))
        };

        mGetStringWithParams.Parameters.Add(
            new CodeParameterDeclarationExpression(typeof(string), "key"));

        mGetStringWithParams.Parameters.Add(
            new CodeParameterDeclarationExpression(typeof(object[]), "args"));

        // string msg = resourceManager.GetString( key, _culture );
        mGetStringWithParams.Statements.Add(
            new CodeVariableDeclarationStatement(typeof(string), "msg",
                new CodeMethodInvokeExpression(
                    new CodeFieldReferenceExpression(null, "resourceManager"), "GetString",
                    new CodeArgumentReferenceExpression("key"), cultureCodeExp)));

        // msg = string.Format( msg, args );
        mGetStringWithParams.Statements.Add(
            new CodeAssignStatement(new CodeVariableReferenceExpression("msg"),
                new CodeMethodInvokeExpression(
                    new CodeTypeReferenceExpression(typeof(string)), "Format",
                    new CodeVariableReferenceExpression("msg"),
                    new CodeArgumentReferenceExpression("args"))));

        // return msg
        mGetStringWithParams.Statements.Add(
            new CodeMethodReturnStatement(
                new CodeVariableReferenceExpression("msg")));

        cKeys.Members.Add(mGetStringWithParams);

        // Create a class definition for each string entry  
        foreach (var resource in commandLines)
        {
            // Create a safe identifier
            var safeKey = XmlConvert.EncodeName(resource.Key);

            if (safeKey == null)
            {
                ReportError("safeKey is null!", resource.Key, 0);
                continue;
            }

            // Deal with an initial numeral.
            if (safeKey[0] >= '0' && safeKey[0] <= '9') safeKey = "_" + safeKey;

            // Make sure we don't conflict with a language identifier.
            safeKey = codeProvider.CreateValidIdentifier(safeKey);

            // The VS Generator always lower cases the first letter, which is not
            // wanted in this case.
            safeKey = char.ToUpper(safeKey[0], CultureInfo.InvariantCulture) + safeKey.Substring(1);

            // Get parameter names from comma sep names in comment
            var parameterNames = Array.Empty<string>();
            
            if (resource.Value != null)
            {
                parameterNames = resource.Value.Split(',');
            }

            if (parameterNames.Length > 0 || options.GenerateMethodsOnly)
            {
                // Create as a method that takes the right number of parameters.
                var mGetString = new CodeMemberMethod
                {
                    Name = safeKey,
                    Attributes = MemberAttributes.Static | MemberAttributes.Public,
                    ReturnType = new CodeTypeReference(typeof(string))
                };

                //Create the argument lists
                var args = new CodeExpression[parameterNames.Length];

                for (var i = 0; i < parameterNames.Length; i++)
                {
                    var parameterName = parameterNames[i];
                    parameterName = parameterName.Trim();
                    if (parameterName.IndexOf(' ') > -1)
                    {
                        // parameter name includes type.
                        var typeName = MapType(parameterName.Substring(0, parameterName.IndexOf(' ')).Trim());
                        parameterName = parameterName.Substring(parameterName.IndexOf(' ') + 1).Trim();

                        mGetString.Parameters.Add(new CodeParameterDeclarationExpression(typeName, parameterName));
                    }
                    else
                    {
                        mGetString.Parameters.Add(new CodeParameterDeclarationExpression(typeof(object), parameterName));
                    }
                    args[i] = new CodeArgumentReferenceExpression(parameterName);
                }

                mGetString.Statements.Add(
                    new CodeMethodReturnStatement(
                        new CodeMethodInvokeExpression(
                            new CodeTypeReferenceExpression(keysClassName), "GetString",
                            new CodeFieldReferenceExpression(
                                new CodeTypeReferenceExpression(keysClassName), safeKey),
                            new CodeArrayCreateExpression(typeof(object), args))));

                cSR.Members.Add(mGetString);
            }
            else
            {
                // Create as a property
                var pGetString = new CodeMemberProperty
                {
                    Name = safeKey,
                    // ReSharper disable once BitwiseOperatorOnEnumWithoutFlags
                    Attributes = MemberAttributes.Static | MemberAttributes.Public,
                    Type = new CodeTypeReference(typeof(string))
                };

                pGetString.GetStatements.Add(
                    new CodeMethodReturnStatement(
                        new CodeMethodInvokeExpression(
                            new CodeTypeReferenceExpression(keysClassName), "GetString",
                            new CodeFieldReferenceExpression(
                                new CodeTypeReferenceExpression(keysClassName), safeKey))));

                cSR.Members.Add(pGetString);
            }

            // Add a const field to the Keys class that contains the actual reference
            var fKey = new CodeMemberField(typeof(string), safeKey)
            {
                InitExpression = new CodePrimitiveExpression(resource.Key),
                // ReSharper disable once BitwiseOperatorOnEnumWithoutFlags
                Attributes = MemberAttributes.Const | MemberAttributes.Public
            };

            cKeys.Members.Add(fKey);
        }

        nSpace.Types.Add(cSR);
        nSpace.Types.Add(cKeys);

        return compileUnit;
    }

    private static string MapType(string typeName)
    {
        return typeName switch
        {
            "long" => "System.Int64",
            "int" => "System.Int32",
            "short" => "System.Int16",
            "byte" => "System.Byte",
            "ulong" => "System.UInt64",
            "uint" => "System.UInt32",
            "ushort" => "System.UInt16",
            "sbyte" => "System.SByte",
            "string" => "System.String",
            "decimal" => "System.Decimal",
            "float" => "System.Single",
            "double" => "System.Double",
            "bool" => "System.Boolean",
            _ => typeName
        };
    }

}