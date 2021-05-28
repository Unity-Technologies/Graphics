using System;
using System.Diagnostics;


namespace UnityEngine.Rendering.Universal
{
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum, AllowMultiple = false)]
    internal class URPHelpURLAttribute : HelpURLAttribute
    {
        //This must be used like
        //[URPHelpURLAttribute("some-page")]
        public URPHelpURLAttribute(string pageName)
            : base(Documentation.GetPageLink(pageName))
        {
        }
    }


    //Need to live in Runtime as Attribute of documentation is on Runtime classes \o/
    class Documentation : DocumentationInfo
    {
        internal static string GetPageLink(string pageName)
        {
            return $"https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@latest/index.html?subfolder=/manual/{pageName}.html";
        }
    }
}
