using System.Runtime.CompilerServices;
using System.Diagnostics;
using UnityEngine;


namespace UnityEngine.Rendering.HighDefinition
{
    [Conditional("UNITY_EDITOR")]
    internal class HDRPHelpURLAttribute : HelpURLAttribute
    {
        public HDRPHelpURLAttribute(string urlString)
            : base(HelpURL(urlString)) {}

        static string HelpURL(string pageName)
        {
            return Documentation.baseURL + DocumentationInfo.version + Documentation.subURL + pageName + Documentation.endURL;
        }
    }

    //Need to live in Runtime as Attribute of documentation is on Runtime classes \o/
    class Documentation : DocumentationInfo
    {
        //This must be used like
        //[HelpURL(Documentation.baseURL + Documentation.version + Documentation.subURL + "some-page" + Documentation.endURL)]
        //It cannot support String.Format nor string interpolation
        internal const string baseURL = "https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@";
        internal const string subURL = "/manual/";
        internal const string endURL = ".html";

        internal static string GetPageLink(string pageName)
        {
            return baseURL + version + subURL + pageName + endURL;
        }
    }
}
