﻿using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Text;
using System.Xml;
using System.Xml.XPath;
using Microsoft.CSharp;
// ReSharper disable BitwiseOperatorOnEnumWithoutFlags

namespace Oleander.StrResGen;

public class CodeGenerator
{
    public ReportErrors ReportErrors { get; set; } = (message, line, lineNo) =>
    {
        Console.WriteLine($"{message} - line: {line} lineNo: {lineNo}");
    };

    public IEnumerable<string> GenerateCSharpResources(string inputFileName, string? nameSpace = null)
    {
        if (string.IsNullOrWhiteSpace(inputFileName)) throw new ArgumentNullException(nameof(inputFileName));

        var options = !string.IsNullOrEmpty(nameSpace) || VSProject.TryFindNameSpaceFromProjectItem(inputFileName, out nameSpace) ? 
            new GenerationOptions { SRNamespace = nameSpace } : 
            new GenerationOptions();

        return this.GenerateCSharpResources(inputFileName, options);
    }

    public IEnumerable<string> GenerateCSharpResources(string inputFileName, GenerationOptions options)
    {
        if (string.IsNullOrWhiteSpace(inputFileName)) throw new ArgumentNullException(nameof(inputFileName));

        this._errorStringBuilder.Clear();
        
        var fileExtension = Path.GetExtension(inputFileName);
        var outputFileName = string.Concat(inputFileName[..^fileExtension.Length], ".cs");

        if (!fileExtension.Equals(".strings", StringComparison.InvariantCultureIgnoreCase))
        {
            this.ReportError($"File must have '*.strings' extension ({inputFileName})", string.Empty, 0);
            File.WriteAllText(outputFileName, this._errorStringBuilder.ToString());
            return new[] { outputFileName };
        }

        var generatedFiles = new List<string>();

        if (!File.Exists(inputFileName))
        {
            var content = $"[strings]{Environment.NewLine}Test(string s)=Test: {0}{Environment.NewLine}{Environment.NewLine}[strings.de]{Environment.NewLine}Test(string s)=German Test: {0}";
            File.WriteAllText(inputFileName, content);
            generatedFiles.Add(inputFileName);
        }

        generatedFiles.AddRange(this.GenerateResourceFiles(inputFileName));

        if (!generatedFiles.Any())
        {
            if (this._errorStringBuilder.Length < 1)
            {
                this.ReportError("No files were generated!", string.Empty, 0);
            }

            File.WriteAllText(outputFileName, this._errorStringBuilder.ToString());
            return new[] { outputFileName };
        }

        var cSharpCode = this.GenerateCSharpCode(generatedFiles.First(x => x.ToLower().EndsWith(".resx")), options);

        if (string.IsNullOrEmpty(cSharpCode) && this._errorStringBuilder.Length < 1)
        {
            this.ReportError("No c# code was generated!", string.Empty, 0);
        }

        if (this._errorStringBuilder.Length > 0)
        {
            cSharpCode = string.Concat(this._errorStringBuilder.ToString(), Environment.NewLine, cSharpCode);
        }

        File.WriteAllText(outputFileName, cSharpCode);
        generatedFiles.Insert(0, outputFileName);
        return generatedFiles;
    }

    #region private members

    private IEnumerable<string> GenerateResourceFiles(string stringsSource)
    {
        var resourceFiles = new List<string>();
        var locale = GetLocaleFromFileName(stringsSource);
        var template = new XmlDocument();

        using (var stream = typeof(CodeGenerator).Assembly.GetManifestResourceStream(typeof(CodeGenerator).Namespace + ".Template.xml"))
        {
            if (stream == null)
            {
                this.ReportError("Resource Template.xml not found!", string.Empty, 0);
                return Enumerable.Empty<string>();
            }

            template.Load(stream);
        }

        if (IsCultureSpecified(stringsSource)) ParseHeader("#! generate_class = false", template);

        if (!ParseHeader($"#! stringSource={stringsSource}", template))
        {
            this.ReportError($"Header could not be parsed! ({stringsSource})", string.Empty, 0);
            return Enumerable.Empty<string>();
        }

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
                        resourceFiles.Add(this.SaveResX(stringsSource, locale, doc));
                        doc = null;
                    }

                    if (line.Trim().ToLower()[..8] == "[strings")
                    {
                        var strLocale = line.Trim().Substring(8, line.Trim().Length - 9);
                        if (strLocale != "") locale = strLocale;
                        doc = new XmlDocument();

                        if (template.DocumentElement == null)
                        {
                            this.ReportError("template.DocumentElement == null", line, lineNo);
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
                    this.ReportError("Invalid resource, missing = sign", line, lineNo);
                    continue;
                }

                var res = line[..split].Trim();
                var text = line[(split + 1)..].Trim();
                string? comment = null;

                if (res.Length == 0)
                {
                    if (prevElement != null)
                    {
                        prevElement.InnerText = prevElement.InnerText + "\n" + text;
                    }
                    else
                    {
                        this.ReportError("Invalid resource, missing resource name", line, lineNo);
                    }
                    continue;
                }

                var resArgSplit = res.IndexOf('(');
                if (resArgSplit > -1)
                {
                    if (resArgSplit == 0)
                    {
                        this.ReportError("Invalid resource, no resource name", line, lineNo);
                        continue;
                    }

                    var resArgSplit2 = res.IndexOf(')', resArgSplit);
                    if (resArgSplit2 == -1)
                    {
                        this.ReportError("Invalid resource, missing end bracket on arguments", line, lineNo);
                        continue;
                    }
                    comment = res.Substring(resArgSplit + 1, resArgSplit2 - resArgSplit - 1);
                    res = res[..resArgSplit];
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
                    this.ReportError("doc.DocumentElement == null", line, lineNo);
                }
                else
                {
                    doc.DocumentElement.AppendChild(el);
                }
            }
        }

        if (doc != null)
        {
            resourceFiles.Add(this.SaveResX(stringsSource, locale, doc));
        }

        if (resourceFiles.Count == 0)
        {
            this.ReportError("No [strings] section has been found.", string.Empty, 0);
        }

        return resourceFiles;
    }
    
    private string GenerateCSharpCode(string resxFileName, GenerationOptions options)
    {
        Dictionary<string, string> commandLines;

        using (Stream inputStream = File.OpenRead(resxFileName))
        {
            var doc = new XPathDocument(inputStream);
            options = this.ReadGenerationOptions(resxFileName, doc, options);
            commandLines = this.ParsLines(doc);
        }

        var codeProvider = new CSharpCodeProvider();
        var ccu = this.Process(new CSharpCodeProvider(), options, commandLines, resxFileName);
        var sw = new StringWriter();
        var opts = new CodeGeneratorOptions { BracingStyle = "C" };
        codeProvider.GenerateCodeFromCompileUnit(ccu, sw, opts);
        sw.Close();

        return sw.GetStringBuilder().ToString();
    }

    private GenerationOptions ReadGenerationOptions(string inputFileName, IXPathNavigable doc, GenerationOptions options)
    {
        var nav = doc.CreateNavigator();

        if (nav == null)
        {
            this.ReportError("nav is null!", "var nav = doc.CreateNavigator();", 0);
            return options;
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

    private Dictionary<string, string> ParsLines(IXPathNavigable doc)
    {
        var dict = new Dictionary<string, string>();
        var nav = doc.CreateNavigator();

        if (nav == null)
        {
            this.ReportError("nav is null!", "var nav = doc.CreateNavigator();", 0);
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

    private CodeCompileUnit Process(CodeDomProvider codeProvider, GenerationOptions options, IDictionary<string, string> commandLines, string inputFileName)
    {
        var compileUnit = new CodeCompileUnit();

        //Just for VB.NET
        compileUnit.UserData.Add("AllowLateBound", false);
        compileUnit.UserData.Add("RequireVariableDeclaration", true);

        //Dummy namespace, so the Import statements would appear above the main namespace declaration
        var dummyNamespace = new CodeNamespace("");
        compileUnit.Namespaces.Add(dummyNamespace);

        //Namespace Import
        dummyNamespace.Imports.Add(new("System"));
        dummyNamespace.Imports.Add(new("System.Resources"));
        dummyNamespace.Imports.Add(new("System.Reflection"));
        dummyNamespace.Imports.Add(new("System.Threading"));

        //Namespace
        var nSpace = new CodeNamespace(options.SRNamespace);
        compileUnit.Namespaces.Add(nSpace);

        //Namespace comments
        nSpace.Comments.Add(new("-----------------------------------------------------------------------------"));
        nSpace.Comments.Add(new(" <autogeneratedinfo>"));
        nSpace.Comments.Add(new("     This code was generated by:"));
        nSpace.Comments.Add(new("       SR Resource Generator custom tool for VS.NET, by Oleander"));
        nSpace.Comments.Add(new(""));
        nSpace.Comments.Add(new("     It contains classes defined from the contents of the resource file:"));
        nSpace.Comments.Add(new("       " + inputFileName));
        nSpace.Comments.Add(new(""));
        nSpace.Comments.Add(new("     Generated: " + DateTime.Now.ToString("f", new CultureInfo("en-US"))));
        nSpace.Comments.Add(new(" </autogeneratedinfo>"));
        nSpace.Comments.Add(new("-----------------------------------------------------------------------------"));

        // Define SR class
        // ReSharper disable once InconsistentNaming
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
                Type = new(typeof(CultureInfo))
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
            var fCulture = new CodeMemberField(typeof(CultureInfo), "culture")
            {
                Attributes = MemberAttributes.Static,
                InitExpression = new CodePrimitiveExpression()
            };

            cKeys.Members.Add(fCulture);

            var pCulture = new CodeMemberProperty
            {
                Attributes = MemberAttributes.Static | MemberAttributes.Public,
                Type = new(typeof(CultureInfo)),
                Name = "Culture"
            };

            pCulture.GetStatements.Add(
                new CodeMethodReturnStatement(
                    new CodeFieldReferenceExpression { FieldName = "culture" }));

            pCulture.SetStatements.Add(
                new CodeAssignStatement(
                    new CodeFieldReferenceExpression { FieldName = "culture" },
                    new CodeArgumentReferenceExpression("value")));

            cKeys.Members.Add(pCulture);

            cultureCodeExp = new CodeFieldReferenceExpression { FieldName = "culture" };
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
            ReturnType = new(typeof(string))
        };

        mGetStringNoParams.Parameters.Add(
            new(typeof(string), "key"));

        mGetStringNoParams.Statements.Add(new CodeMethodReturnStatement(
            new CodeMethodInvokeExpression(
                new CodeFieldReferenceExpression { FieldName = "resourceManager" }, "GetString",
                new CodeArgumentReferenceExpression("key"), cultureCodeExp)));

        cKeys.Members.Add(mGetStringNoParams);

        // With parameters, format the results using String.Format
        // return msg;
        var mGetStringWithParams = new CodeMemberMethod
        {
            Name = "GetString",
            Attributes = MemberAttributes.Public | MemberAttributes.Static,
            ReturnType = new(typeof(string))
        };

        mGetStringWithParams.Parameters.Add(
            new(typeof(string), "key"));

        mGetStringWithParams.Parameters.Add(
            new(typeof(object[]), "args"));

        // string msg = resourceManager.GetString( key, _culture );
        mGetStringWithParams.Statements.Add(
            new CodeVariableDeclarationStatement(typeof(string), "msg",
                new CodeMethodInvokeExpression(
                    new CodeFieldReferenceExpression { FieldName = "resourceManager" }, "GetString",
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

            if (string.IsNullOrEmpty(safeKey))
            {
                this.ReportError("safeKey is empty!", resource.Key, 0);
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
            var parameterNames = resource.Value.Split(',', StringSplitOptions.RemoveEmptyEntries);

            if (parameterNames.Length > 0 || options.GenerateMethodsOnly)
            {
                // Create as a method that takes the right number of parameters.
                var mGetString = new CodeMemberMethod
                {
                    Name = safeKey,
                    Attributes = MemberAttributes.Static | MemberAttributes.Public,
                    ReturnType = new(typeof(string))
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

                        mGetString.Parameters.Add(new(typeName, parameterName));
                    }
                    else
                    {
                        mGetString.Parameters.Add(new(typeof(object), parameterName));
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
                    Attributes = MemberAttributes.Static | MemberAttributes.Public,
                    Type = new(typeof(string))
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
                Attributes = MemberAttributes.Const | MemberAttributes.Public
            };

            cKeys.Members.Add(fKey);
        }

        nSpace.Types.Add(cSR);
        nSpace.Types.Add(cKeys);

        return compileUnit;
    }

    private string SaveResX(string source, string locale, XmlDocument doc)
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
            this.ReportError(ex.Message, string.Empty, 0);
        }

        return fullFilename;
    }

    #region ReportError

    private readonly StringBuilder _errorStringBuilder = new();

    private void ReportError(string message, string line, int lineNo)
    {
        this.ReportErrors.Invoke(message, line, lineNo);
        this._errorStringBuilder.AppendLine($"// {DateTime.Now:yyyy.MM.dd HH:mm:ss}  {message} - line: {line} lineNo: {lineNo}");
    }

    #endregion

    #region static members

    private static FileAttributes RemoveAttribute(FileAttributes attributes, FileAttributes attributesToRemove)
    {
        return attributes & ~attributesToRemove;
    }
    
    private static string GetLocaleFromFileName(string inputFileName)
    {
        var resFilename = Path.GetFileNameWithoutExtension(inputFileName);
        if (string.IsNullOrEmpty(resFilename)) return string.Empty;

        var localeIndex = resFilename.LastIndexOf(".", StringComparison.OrdinalIgnoreCase);

        if (localeIndex != -1 && IsCultureSpecified(inputFileName))
        {
            return resFilename[localeIndex..];
        }

        return string.Empty;
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

    private static bool ParseHeader(string line, XmlDocument template)
    {
        if (line[0] != '#') return false;
        if (line.Length == 1) return true;
        if (line[1] != '!') return true;

        // Options
        var pos = line.IndexOf('=');

        if (pos <= -1) return true;

        var opt = line.Substring(2, pos - 2).Trim();
        var arg = line[(pos + 1)..].Trim();

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

    private static bool IsCultureSpecified(string stringsSource)
    {
        var localeIndex = Path.GetFileNameWithoutExtension(stringsSource).LastIndexOf('.');

        if (localeIndex == -1) return false;

        var culture = Path.GetFileNameWithoutExtension(stringsSource).Substring(localeIndex + 1);
        return CultureInfo.GetCultures(CultureTypes.AllCultures).Any(existingCulture => existingCulture.ToString() == culture);
    }

    #endregion

    #endregion
}