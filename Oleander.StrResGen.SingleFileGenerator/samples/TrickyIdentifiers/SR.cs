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
//        D:\dev\git\oleander\StringResourceGenerator\Oleander.StrResGen.SingleFileGenerator\samples\TrickyIdentifiers\SR.srt.resx
// 
//      Generated: Monday, October 14, 2024 2:09 PM
//  </autogeneratedinfo>
// -----------------------------------------------------------------------------
namespace Samples.TrickyIdentifiers
{
    
    
    public partial class SR
    {
        
        public static System.Globalization.CultureInfo Culture
        {
            get
            {
                return SRKeys.Culture;
            }
            set
            {
                SRKeys.Culture = value;
            }
        }
        
        public static string A
        {
            get
            {
                return SRKeys.GetString(SRKeys.A);
            }
        }
        
        public static string _x0031_
        {
            get
            {
                return SRKeys.GetString(SRKeys._x0031_);
            }
        }
        
        public static string _x0024__x0025__x005E__x0040__x0021__x0026__x002A_
        {
            get
            {
                return SRKeys.GetString(SRKeys._x0024__x0025__x005E__x0040__x0021__x0026__x002A_);
            }
        }
        
        public static string _x20A7__x0020_Sign
        {
            get
            {
                return SRKeys.GetString(SRKeys._x20A7__x0020_Sign);
            }
        }
        
        public static string Pts
        {
            get
            {
                return SRKeys.GetString(SRKeys.Pts);
            }
        }
        
        public static string _x25A0_
        {
            get
            {
                return SRKeys.GetString(SRKeys._x25A0_);
            }
        }
        
        public static string _void
        {
            get
            {
                return SRKeys.GetString(SRKeys._void);
            }
        }
        
        public static string Public
        {
            get
            {
                return SRKeys.GetString(SRKeys.Public);
            }
        }
    }
    
    internal partial class SRKeys
    {
        
        public static System.Resources.ResourceManager resourceManager = new System.Resources.ResourceManager("Samples.TrickyIdentifiers.SR.srt", typeof(Samples.TrickyIdentifiers.SR).Assembly);
        
        static System.Globalization.CultureInfo culture = null;
        
        public const string A = "a";
        
        public const string _x0031_ = "1";
        
        public const string _x0024__x0025__x005E__x0040__x0021__x0026__x002A_ = "$%^@!&*";
        
        public const string _x20A7__x0020_Sign = "₧ Sign";
        
        public const string Pts = "Pts";
        
        public const string _x25A0_ = "■";
        
        public const string _void = "void";
        
        public const string Public = "Public";
        
        public static System.Globalization.CultureInfo Culture
        {
            get
            {
                return culture;
            }
            set
            {
                culture = value;
            }
        }
        
        public static string GetString(string key)
        {
            return resourceManager.GetString(key, culture);
        }
        
        public static string GetString(string key, object[] args)
        {
            string msg = resourceManager.GetString(key, culture);
            msg = string.Format(msg, args);
            return msg;
        }
    }
}
