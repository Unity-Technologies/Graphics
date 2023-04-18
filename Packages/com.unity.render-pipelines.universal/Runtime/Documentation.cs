using System.Diagnostics;

namespace UnityEngine.Rendering.Universal
{
    [Conditional("UNITY_EDITOR")]
    internal class URPHelpURLAttribute : CoreRPHelpURLAttribute
    {
        public URPHelpURLAttribute(string pageName, string pageHash = "")
            : base(pageName, pageHash, Documentation.packageName)
        {
        }
    }

    internal class Documentation : DocumentationInfo
    {
        /// <summary>
        /// The name of the package
        /// </summary>
        public const string packageName = "com.unity.render-pipelines.universal";

        /// <summary>
        /// Generates a Universal Render Pipeline help url for the given page name
        /// </summary>
        /// <param name="pageName">The page name</param>
        /// <returns>The full url</returns>
        public static string GetPageLink(string pageName) => GetPageLink(packageName, pageName);
    }
}
