using System.Runtime.CompilerServices;
using System.Diagnostics;
using UnityEngine;


namespace UnityEngine.Rendering.HighDefinition
{
    [Conditional("UNITY_EDITOR")]
    internal class HDRPHelpURLAttribute : HelpURLAttribute
    {
        //This must be used like
        //[HDRPHelpURLAttribute("some-page")]
        //It cannot support String.Format nor string interpolation.
        public HDRPHelpURLAttribute(string pageName)
            : base(HelpURL(pageName)) {}

        static string HelpURL(string pageName)
        {
            return Documentation.baseURL + Documentation.version + Documentation.subURL + pageName + Documentation.endURL;
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
