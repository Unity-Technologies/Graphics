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
    }
}
