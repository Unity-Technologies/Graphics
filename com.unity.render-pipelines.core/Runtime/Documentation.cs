using System.Runtime.CompilerServices;
using System.Diagnostics;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor.PackageManager;
#endif

[assembly: InternalsVisibleTo("Unity.RenderPipelines.Core.Editor.Tests")]

namespace UnityEngine.Rendering
{
    [Conditional("UNITY_EDITOR")]
    internal class CoreRPHelpURLAttribute : HelpURLAttribute
    {
        //This must be used like
        //[CoreRPHelpURLAttribute("some-page")]
        //It cannot support String.Format nor string interpolation.
        public CoreRPHelpURLAttribute(string urlString)
            : base(HelpURL(urlString)) {}

        static string HelpURL(string pageName)
        {
            return Documentation.baseURL + DocumentationInfo.version + Documentation.subURL + pageName + Documentation.endURL;
        }
    }

    //Temporary for now, there is several part of the Core documentation that are misplaced in HDRP documentation.
    [Conditional("UNITY_EDITOR")]
    internal class HDRPHelpURLAttribute : HelpURLAttribute
    {
        //This must be used like
        //[HDRPHelpURLAttribute("some-page")]
        //It cannot support String.Format nor string interpolation.
        public HDRPHelpURLAttribute(string urlString)
            : base(HelpURL(urlString)) {}

        static string HelpURL(string pageName)
        {
            return Documentation.baseURLHDRP + DocumentationInfo.version + Documentation.subURL + pageName + Documentation.endURL;
        }
    }


    //We need to have only one version number amongst packages (so public)
    /// <summary>
    /// Documentation Info class.
    /// </summary>
    public class DocumentationInfo
    {
        private const string fallbackVersion = "12.0";
        /// <summary>
        /// Current version of the documentation.
        /// </summary>
        public static string version
        {
            get
            {
#if UNITY_EDITOR
                UnityEditor.PackageManager.PackageInfo packageInfo = UnityEditor.PackageManager.PackageInfo.FindForAssembly(typeof(DocumentationInfo).Assembly);
                return packageInfo == null ? fallbackVersion : packageInfo.version.Substring(0, 4);
#else
                return fallbackVersion;
#endif
            }
        }
    }

    //Need to live in Runtime as Attribute of documentation is on Runtime classes \o/
    /// <summary>
    /// Documentation class.
    /// </summary>
    class Documentation : DocumentationInfo
    {
        internal const string baseURL = "https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@";
        internal const string subURL = "/manual/";
        internal const string endURL = ".html";

        //Temporary for now, there is several part of the Core documentation that are misplaced in HDRP documentation.
        //use this base url for them:
        internal const string baseURLHDRP = "https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@";
    }
}
