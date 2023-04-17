﻿using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
// ReSharper disable ExplicitCallerInfoArgument
// ReSharper disable InconsistentNaming

namespace Oleander.StrResGen;

public static class MSBuildLogFormatter
{
    private const string messageFormat = "{origin}({line},{column}) : {subCategory} {category} {code} : {text}";

    public static string CreateMSBuildWarningFormat(int code, string text, string subCategory, [CallerLineNumber] int line = 0)
    {
        return $"{FileName}({line},0) : {subCategory} warning SRG{code} : {text}";
    }

    public static string CreateMSBuildErrorFormat(int code, string text, string subCategory, [CallerLineNumber] int line = 0)
    {
        return $"{FileName}({line},0) : {subCategory} error SRG{code} : {text}";
    }


    public static string CreateMSBuildWarning(int code, string text, int line, string subCategory)
    {
        return CreateMSBuildWarning(code, text, subCategory, line);
    }

    public static string CreateMSBuildWarning(int code, string text, string subCategory, [CallerLineNumber] int line = 0)
    {
        var message = CreateMSBuildWarningFormat(code, text, subCategory, line);

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(message);
        Console.ResetColor();

        return message;
    }

    public static string CreateMSBuildError(int code, string text, int line, string subCategory)
    {
        return CreateMSBuildError(code, text, subCategory, line);
    }

    public static string CreateMSBuildError(int code, string text, string subCategory, [CallerLineNumber] int line = 0)
    {
        var message = CreateMSBuildErrorFormat(code, text, subCategory, line);

        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine(message);
        Console.ResetColor();

        return message;
    }

    public static string CreateMSBuildWarning(this ILogger logger, int code, string text, int line, string subCategory)
    {
        return CreateMSBuildWarning(logger, code, text, subCategory, line);
    }

    public static string CreateMSBuildWarning(this ILogger logger, int code, string text, string subCategory, [CallerLineNumber] int line = 0)
    {
        logger.LogWarning(messageFormat, FileName, line, 0, subCategory, "warning", $"SRG{code}", text);
        return CreateMSBuildWarning(code, text, subCategory, line);
    }

    public static string CreateMSBuildError(this ILogger logger, int code, string text, int line, string subCategory)
    {
        return CreateMSBuildError(logger, code, text, subCategory, line);
    }

    public static string CreateMSBuildError(this ILogger logger, int code, string text, string subCategory, [CallerLineNumber] int line = 0)
    {
        logger.LogError(messageFormat, FileName, line, 0, subCategory, "error", $"SRG{code}", text);
        return CreateMSBuildError(code, text, subCategory, line);
    }

    private static string FileName => Path.GetFileName(Assembly.GetExecutingAssembly().Location);
}