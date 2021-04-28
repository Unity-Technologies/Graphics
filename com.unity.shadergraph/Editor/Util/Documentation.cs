using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Unity.ShaderGraph.Editor.Tests")]

namespace UnityEngine.Rendering.ShaderGraph
{
    //Need to live in Runtime as Attribute of documentation is on Runtime classes \o/
    class Documentation
    {
        //This must be used like
        //[HelpURL(Documentation.baseURL + Documentation.version + Documentation.subURL + "some-page" + Documentation.endURL)]
        //It cannot support String.Format nor string interpolation
        internal const string baseURL = "https://docs.unity3d.com/Packages/com.unity.shadergraph@";
        internal const string subURL = "/manual/";
        internal const string endURL = ".html";

        internal const string version = "7.7";

        internal static string GetPageLink(string pageName)
        {
            return baseURL + version + subURL + pageName + endURL;
        }
    }
}
