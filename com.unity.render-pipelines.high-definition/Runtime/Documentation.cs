using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Unity.RenderPipelines.HighDefinition.Editor.Tests")]

namespace UnityEngine.Rendering.HighDefinition
{
    //Need to live in Runtime as Attribute of documentation is on Runtime classes \o/
    class Documentation : DocumentationInfo
    {
        //This must be used like
        //[HelpURL(Documentation.baseURL + Documentation.releaseVersion + Documentation.subURL + "some-page" + Documentation.endURL)]
        //It cannot support String.Format nor string interpolation
        internal const string baseURL = "https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@";
        internal const string subURL = "/manual/";
        internal const string endURL = ".html";


        internal const string releaseVersion = "7.5";

        internal static string GetPageLink(string pageName)
        {
            return baseURL + releaseVersion + subURL + pageName + endURL;
        }
    }
}
