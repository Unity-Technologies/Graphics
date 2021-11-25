using System.Diagnostics;

namespace UnityEngine.Rendering.HighDefinition
{
    [Conditional("UNITY_EDITOR")]
    internal class HDRPHelpURLAttribute : CoreRPHelpURLAttribute
    {
        public HDRPHelpURLAttribute(string pageName)
            : base(pageName, Documentation.packageName)
        {
        }
    }

    internal class Documentation : DocumentationInfo
    {
        /// <summary>
        /// The name of the package
        /// </summary>
        public const string packageName = "com.unity.render-pipelines.high-definition";
        /// <summary>
        /// Generates a help url for the given package and page name
        /// </summary>
        /// <param name="pageName">The page name</param>
        /// <returns>The full url page</returns>
        public static string GetPageLink(string pageName) => GetPageLink(packageName, pageName);
    }
}
