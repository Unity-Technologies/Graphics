using System;
using System.Diagnostics;
using UnityEngine.Rendering;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Attribute to define the help URL for Universal Render Pipeline classes.
    /// </summary>
    /// <remarks>
    /// The page name is treated as a path relative to the Unity Manual root
    /// (<c>https://docs.unity3d.com/&lt;version&gt;/Documentation/Manual/</c>). Most URP
    /// pages live under the <c>urp/</c> subfolder, so callers typically pass a path of
    /// the form <c>"urp/&lt;page&gt;"</c>, but pages elsewhere in the Manual (for
    /// example shared content) can also be linked. The URL is built by
    /// <see cref="DocumentationInfo.GetManualLink(string,string)"/> in the core package.
    /// </remarks>
    /// <example>
    /// <code>
    /// [URPHelpURL("urp/Volumes")]
    /// public class VolumeProfile : ScriptableObject { /* ... */ }
    /// </code>
    /// </example>
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum, AllowMultiple = false)]
    internal class URPHelpURLAttribute : HelpURLAttribute
    {
        /// <summary>
        /// Creates a help URL attribute for a page in the Unity Manual.
        /// </summary>
        /// <param name="pageName">The page path relative to the Manual root, without the <c>.html</c> extension (for example <c>"urp/Volumes"</c>).</param>
        /// <param name="pageHash">Optional section anchor on the page, with or without the leading <c>#</c>.</param>
        public URPHelpURLAttribute(string pageName, string pageHash = "")
            : base(DocumentationInfo.GetManualLink(pageName, pageHash))
        {
        }
    }
}
