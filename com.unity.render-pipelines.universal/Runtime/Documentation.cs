using System.Diagnostics;

namespace UnityEngine.Rendering.Universal
{
    [Conditional("UNITY_EDITOR")]
    internal class URPHelpURLAttribute : CoreRPHelpURLAttribute
    {
        public URPHelpURLAttribute(string pageName, string pageHash = "")
            : base(pageName, Documentation.packageName, pageHash)
        {
        }
    }

    internal class Documentation : DocumentationInfo
    {
        /// <summary>
        /// The name of the package
        /// </summary>
        public const string packageName = "com.unity.render-pipelines.universal";

        //KGARCIA: todo, this is a temporary fix in PR https://github.com/Unity-Technologies/Graphics/pull/6726
        /// <summary>
        /// Generates a help url for the given package and page name
        /// </summary>
        /// <param name="pageName">The page name</param>
        /// <param name="pageHash">The page hash</param>
        /// <returns>The full url page</returns>
        //public static string GetPageLink(string pageName, string pageHash = "") => GetPageLink(packageName, pageName, pageHash);
    }
}
