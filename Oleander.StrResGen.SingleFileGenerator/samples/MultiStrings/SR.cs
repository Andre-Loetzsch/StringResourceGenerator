//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Reflection;
using System.Resources;
using System.Threading;


// -----------------------------------------------------------------------------
//  <autogeneratedinfo>
//      This code was generated by:
//        SR Resource Generator custom tool for VS.NET, by Oleander
// 
//      It contains classes defined from the contents of the resource file:
//        D:\dev\git\oleander\StringResourceGenerator\Oleander.StrResGen.SingleFileGenerator\samples\MultiStrings\SR.srt.resx
// 
//      Generated: Monday, October 14, 2024 2:09 PM
//  </autogeneratedinfo>
// -----------------------------------------------------------------------------
namespace Samples.MultiStrings
{
    
    
    internal partial class SR
    {
        
        public static string Raw
        {
            get
            {
                return SRKeys.GetString(SRKeys.Raw);
            }
        }
        
        public static string MultiLineEx
        {
            get
            {
                return SRKeys.GetString(SRKeys.MultiLineEx);
            }
        }
        
        public static string SimpleArg(object arg1)
        {
            return SRKeys.GetString(SRKeys.SimpleArg, new object[] {
                        arg1});
        }
        
        public static string StringArg(string name)
        {
            return SRKeys.GetString(SRKeys.StringArg, new object[] {
                        name});
        }
        
        public static string NumberArgs(int name2, decimal amount, float chanceOfWinning)
        {
            return SRKeys.GetString(SRKeys.NumberArgs, new object[] {
                        name2,
                        amount,
                        chanceOfWinning});
        }
    }
    
    internal partial class SRKeys
    {
        
        public static System.Resources.ResourceManager resourceManager = new System.Resources.ResourceManager("Samples.MultiStrings.SR.srt", typeof(Samples.MultiStrings.SR).Assembly);
        
        public const string Raw = "Raw";
        
        public const string SimpleArg = "SimpleArg";
        
        public const string StringArg = "StringArg";
        
        public const string NumberArgs = "NumberArgs";
        
        public const string MultiLineEx = "MultiLineEx";
        
        public static string GetString(string key)
        {
            return resourceManager.GetString(key, Resources.CultureInfo);
        }
        
        public static string GetString(string key, object[] args)
        {
            string msg = resourceManager.GetString(key, Resources.CultureInfo);
            msg = string.Format(msg, args);
            return msg;
        }
    }
}
