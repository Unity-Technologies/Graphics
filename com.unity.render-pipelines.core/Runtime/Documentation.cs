using System;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Linq;
#if UNITY_EDITOR
using PackageInfo = UnityEditor.PackageManager.PackageInfo;
#endif

[assembly: InternalsVisibleTo("Unity.RenderPipelines.Core.Editor.Tests")]

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Attribute to define the help url
    /// </summary>
    /// <example>
    /// [CoreRPHelpURLAttribute("Volume")]
    /// public class Volume : MonoBehaviour
    /// </example>
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum, AllowMultiple = false)]
    public class CoreRPHelpURLAttribute : HelpURLAttribute
    {
        /// <summary>
        /// The constructor of the attribute
        /// </summary>
        /// <param name="pageName"></param>
        /// <param name="packageName"></param>
        public CoreRPHelpURLAttribute(string pageName, string packageName = "com.unity.render-pipelines.core")
            : base(DocumentationInfo.GetPageLink(packageName, pageName))
        {
        }
    }

    //We need to have only one version number amongst packages (so public)
    /// <summary>
    /// Documentation Info class.
    /// </summary>
    public class DocumentationInfo
    {
        const string fallbackVersion = "12.1";
        const string url = "https://docs.unity3d.com/Packages/{0}@{1}/manual/{2}.html";

        /// <summary>
        /// Current version of the documentation.
        /// </summary>
        public static string version
        {
            get
            {
#if UNITY_EDITOR
                var packageInfo = PackageInfo.FindForAssembly(typeof(DocumentationInfo).Assembly);
                return packageInfo == null ? fallbackVersion : packageInfo.version.Substring(0, 4);
#else
                return fallbackVersion;
#endif
            }
        }

        /// <summary>
        /// Generates a help url for the given package and page name
        /// </summary>
        /// <param name="packageName">The package name</param>
        /// <param name="pageName">The page name</param>
        /// <returns>The full url page</returns>
        public static string GetPageLink(string packageName, string pageName) => string.Format(url, packageName, version, pageName);
    }

    /// <summary>
    /// Set of utils for documentation
    /// </summary>
    public static class DocumentationUtils
    {
        /// <summary>
        /// Obtains the help url from an enum
        /// </summary>
        /// <typeparam name="TEnum">The enum with a <see cref="HelpURLAttribute"/></typeparam>
        /// <param name="mask">[Optional] The current value of the enum</param>
        /// <returns>The full url</returns>
        public static string GetHelpURL<TEnum>(TEnum mask = default)
            where TEnum : struct, IConvertible
        {
            var helpURLAttribute = (HelpURLAttribute)mask
                .GetType()
                .GetCustomAttributes(typeof(HelpURLAttribute), false)
                .FirstOrDefault();

            return helpURLAttribute == null ? string.Empty : $"{helpURLAttribute.URL}#{mask}";
        }
    }
}
