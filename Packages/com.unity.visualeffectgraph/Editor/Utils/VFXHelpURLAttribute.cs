using System;
using System.Diagnostics;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEditor.VFX
{
    /// <summary>
    /// Attribute to define the help url
    /// </summary>
    /// <example>
    /// [VFXHelpURLAttribute("Context-Initialize")]
    /// class VFXBasicInitialize : VFXContext
    /// </example>
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum)]
    internal class VFXHelpURLAttribute : CoreRPHelpURLAttribute
    {
        public VFXHelpURLAttribute(string pageName, string pageHash = "")
            : base(pageName, pageHash, Documentation.packageName)
        {
        }
    }

    internal class Documentation : DocumentationInfo
    {
        /// <summary>
        /// The name of the package
        /// </summary>
        public const string packageName = "com.unity.visualeffectgraph";

        /// <summary>
        /// Generates a Visual Effect Graph help url for the given page name
        /// </summary>
        /// <param name="pageName">The page name</param>
        /// <returns>The full url.</returns>
        public static string GetPageLink(string pageName) => GetPageLink(packageName, pageName);
        
        /// <summary>
        /// Generates a default Visual Effect Graph help url
        /// </summary>
        /// <returns>The full url to the index page.</returns>
        public static string GetDefaultPackageLink() => GetDefaultPackageLink(packageName);
    }
}
