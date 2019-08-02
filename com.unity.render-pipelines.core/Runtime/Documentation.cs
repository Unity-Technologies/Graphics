using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Unity.RenderPipelines.Core.Editor.Tests")]

namespace UnityEngine.Rendering
{
    //We need to have only one version number amongst packages (so public)
    public class DocumentationInfo
    {
        //Update this field when upgrading the target Documentation for the package
        //Should be linked to the package version somehow.
        public const string version = "6.9";
    }

    //Need to live in Runtime as Attribute of documentation is on Runtime classes \o/
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

    //Add inheritance only to access Documentation.baseURL in EnvironmentLibrary Editor class
    [HelpURL(Documentation.baseURLHDRP + Documentation.version + Documentation.subURL + "Environment-Library" + Documentation.endURL)]
    public class BaseEnvironmentLibrary : ScriptableObject { }
}
