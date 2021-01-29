using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Unity.RenderPipelines.Core.Editor.Tests")]

namespace UnityEngine.Rendering
{
    //We need to have only one version number amongst packages (so public)
    /// <summary>
    /// Documentation Info class.
    /// </summary>
    public class DocumentationInfo
    {
        //Update this field when upgrading the target Documentation for the package
        //Should be linked to the package version somehow.
        /// <summary>
        /// Current version of the documentation.
        /// </summary>
        public const string version = "10.4";
    }

    //Need to live in Runtime as Attribute of documentation is on Runtime classes \o/
    /// <summary>
    /// Documentation class.
    /// </summary>
    class Documentation : DocumentationInfo
    {
        //This must be used like
        //[HelpURL(Documentation.baseURL + Documentation.version + Documentation.subURL + "some-page" + Documentation.endURL)]
        //It cannot support String.Format nor string interpolation
        internal const string baseURL = "https://docs.unity3d.com/Packages/com.unity.render-pipelines.core@";
        internal const string subURL = "/manual/";
        internal const string endURL = ".html";

        //Temporary for now, there is several part of the Core documentation that are misplaced in HDRP documentation.
        //use this base url for them:
        internal const string baseURLHDRP = "https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@";
    }
}
