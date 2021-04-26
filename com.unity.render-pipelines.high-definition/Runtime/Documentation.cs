using System;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using UnityEngine;


namespace UnityEngine.Rendering.HighDefinition
{
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum, AllowMultiple = false)]
    internal class HDRPHelpURLAttribute : HelpURLAttribute
    {
        //This must be used like
        //[HDRPHelpURLAttribute("some-page")]
        //It cannot support String.Format nor string interpolation.
        public HDRPHelpURLAttribute(string pageName)
            : base(Documentation.GetPageLink(pageName))
        {
        }
    }


    //Need to live in Runtime as Attribute of documentation is on Runtime classes \o/
    class Documentation : DocumentationInfo
    {
        internal const string baseURL = "https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@";
        internal const string subURL = "/manual/";
        internal const string endURL = ".html";

        internal static string GetPageLink(string pageName)
        {
            return baseURL + version + subURL + pageName + endURL;
        }
    }
}
