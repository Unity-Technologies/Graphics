using System;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Diagnostics.CodeAnalysis;
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
            : base(DocumentationInfo.GetPageLink(packageName, pageName, ""))
        {
        }

        /// <summary>
        /// The constructor of the attribute
        /// </summary>
        /// <param name="pageName"></param>
        /// <param name="packageName"></param>
        /// <param name="pageHash"></param>
        public CoreRPHelpURLAttribute(string pageName, string pageHash, string packageName = "com.unity.render-pipelines.core")
            : base(DocumentationInfo.GetPageLink(packageName, pageName, pageHash))
        {
        }
    }

    //We need to have only one version number amongst packages (so public)
    /// <summary>
    /// Documentation Info class.
    /// </summary>
    public class DocumentationInfo
    {
        const string fallbackVersion = "13.1";
        const string url = "https://docs.unity3d.com/Packages/{0}@{1}/manual/{2}.html{3}";

        /// <summary>
        /// Current version of the documentation.
        /// </summary>
        public static string version
        {
            get
            {
#if UNITY_EDITOR
                if (DocumentationUtils.TryGetPackageInfoForType(typeof(DocumentationInfo), out _, out var version))
                    return version;
#endif
                return fallbackVersion;
            }
        }

        /// <summary>
        /// Generates a help url for the given package and page name
        /// </summary>
        /// <param name="packageName">The package name</param>
        /// <param name="pageName">The page name</param>
        /// <returns>The full url page</returns>
        public static string GetPageLink(string packageName, string pageName) => string.Format(url, packageName, version, pageName, "");

        /// <summary>
        /// Generates a help url for the given package and page name
        /// </summary>
        /// <param name="packageName">The package name</param>
        /// <param name="pageName">The page name</param>
        /// <param name="pageHash">The page hash</param>
        /// <returns>The full url page</returns>
        public static string GetPageLink(string packageName, string pageName, string pageHash) => string.Format(url, packageName, version, pageName, pageHash);
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

        /// <summary>
        /// Obtains the help URL from a type.
        /// </summary>
        /// <param name="type">The type decorated with the HelpURL attribute.</param>
        /// <param name="url">The full URL from the HelpURL attribute. If the attribute is not present, this value is null.</param>
        /// <returns>Returns true if the attribute is present, and false otherwise.</returns>
        public static bool TryGetHelpURL(Type type, out string url)
        {
            var attribute = type.GetCustomAttribute<HelpURLAttribute>(false);
            url = attribute?.URL;
            return attribute != null;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Obtain package informations from a specific type
        /// </summary>
        /// <param name="type">The type used to retrieve package information</param>
        /// <param name="packageName">The name of the package containing the given type</param>
        /// <param name="version">The version number of the package containing the given type. Only Major.Minor will be returned as fix is not used for documentation</param>
        /// <returns></returns>
        public static bool TryGetPackageInfoForType([DisallowNull] Type type, [NotNullWhen(true)] out string packageName, [NotNullWhen(true)] out string version)
        {
            var packageInfo = PackageInfo.FindForAssembly(type.Assembly);
            if (packageInfo == null)
            {
                packageName = null;
                version = null;
                return false;
            }

            packageName = packageInfo.name;
            version = packageInfo.version.Substring(0, packageInfo.version.LastIndexOf('.'));
            return true;
        }
#endif
    }
}
