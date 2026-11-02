using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Diagnostics;
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
        /// <param name="pageName">The name of the documentation page.</param>
        /// <param name="packageName">The package name, defaulting to "com.unity.render-pipelines.core".</param>
        public CoreRPHelpURLAttribute(string pageName, string packageName = "com.unity.render-pipelines.core")
            : base(DocumentationInfo.GetPageLink(packageName, pageName, ""))
        {
        }

        /// <summary>
        /// The constructor of the attribute
        /// </summary>
        /// <param name="pageName">The name of the documentation page.</param>
        /// <param name="pageHash">The hash specifying a section within the page.</param>
        /// <param name="packageName">The package name, defaulting to "com.unity.render-pipelines.core".</param>
        public CoreRPHelpURLAttribute(string pageName, string pageHash, string packageName = "com.unity.render-pipelines.core")
            : base(DocumentationInfo.GetPageLink(packageName, pageName, pageHash))
        {
        }
    }

    /// <summary>
    /// Use this attribute to define documentation url for the current Render Pipeline.
    /// </summary>
    /// <remarks>
    /// The URL is resolved at access time based on the active <c>RenderPipelineAsset</c>
    /// type. If that type carries a <see cref="DocumentationInfo.SourceAttribute"/> the URL is
    /// built for the declared <see cref="DocumentationInfo.Location"/>; otherwise it falls back
    /// to the package documentation site for the package that owns the pipeline asset.
    /// </remarks>
    /// <example>
    /// [CurrentPipelineHelpURLAttribute("Volume")]
    /// public class Volume : MonoBehaviour
    /// </example>
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum, AllowMultiple = false)]
    public class CurrentPipelineHelpURLAttribute : HelpURLAttribute
    {
        private string pageName { get; }

        private string pageHash { get; }
        /// <summary>
        /// The constructor of the attribute
        /// </summary>
        /// <param name="pageName">The name of the documentation page.</param>
        /// <param name="pageHash">The name of the section on the documentation page.</param>
        public CurrentPipelineHelpURLAttribute(string pageName, string pageHash = "")
            : base(null)
        {
            this.pageName = pageName;
            this.pageHash = pageHash;
        }

        /// <summary>
        /// Returns the URL to the given page in the current Render Pipeline documentation.
        /// </summary>
        public override string URL
        {
            get
            {
#if UNITY_EDITOR
                if (!GraphicsSettings.isScriptableRenderPipelineEnabled)
                    return string.Empty;

                return DocumentationInfo.BuildPipelineHelpUrl(GraphicsSettings.currentRenderPipelineAssetType, pageName, pageHash);
#else
                return string.Empty;
#endif
            }
        }
    }

    /// <summary>
    /// Use this attribute to define a documentation URL that is only active when a specific Render Pipeline is in use.
    /// </summary>
    /// <example>
    /// <code>
    /// [PipelineHelpURL("HDRenderPipelineAsset", "hdrp-page-name")]
    /// [PipelineHelpURL("UniversalRenderPipelineAsset", "urp-page-name")]
    /// public class MyHDRPComponent : MonoBehaviour { /* ... */ }
    /// </code>
    /// </example>
    /// <remarks>
    /// The URL will only be generated if the active Scriptable Render Pipeline Asset's type name exactly matches the <c>pipelineName</c> provided.
    /// </remarks>
    /// <seealso cref="HelpURLAttribute"/>
    [Conditional("UNITY_EDITOR")]
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum, AllowMultiple = true)]
    public class PipelineHelpURLAttribute : HelpURLAttribute
    {
        private string pipelineName { get; }

        private string pageName { get; }

        private string pageHash { get; }

        /// <summary>
        /// Initializes the attribute to link to a specific documentation page for a named Render Pipeline.
        /// </summary>
        /// <param name="pipelineName">The exact Type name of the Render Pipeline Asset (e.g., "UniversalRenderPipelineAsset", "HDRenderPipelineAsset") for which this URL is valid.</param>
        /// <param name="pageName">The name of the documentation page.</param>
        /// <param name="pageHash">Optional. The specific section anchor (#) on the documentation page.</param>
        public PipelineHelpURLAttribute(string pipelineName, string pageName, string pageHash = "")
            : base(null)
        {
            this.pipelineName = pipelineName;
            this.pageName = pageName;
            this.pageHash = pageHash;
        }

        /// <summary>
        /// Returns the URL to the specified page within the documentation for the designated Render Pipeline,
        /// but only if that pipeline is currently active.
        /// </summary>
        /// <remarks>
        /// Checks if a Scriptable Render Pipeline is enabled and if its asset type name matches the <c>pipelineName</c>
        /// provided in the constructor. If conditions are met and package info is found, constructs the URL.
        /// Otherwise, returns an empty string.
        /// </remarks>
        public override string URL
        {
            get
            {
#if UNITY_EDITOR
                if (string.IsNullOrEmpty(pipelineName) || !GraphicsSettings.isScriptableRenderPipelineEnabled)
                    return string.Empty;

                var pipelineType = GraphicsSettings.currentRenderPipelineAssetType;
                if (pipelineType.Name != pipelineName)
                    return string.Empty;

                return DocumentationInfo.BuildPipelineHelpUrl(pipelineType, pageName, pageHash);
#else
                return string.Empty;
#endif
            }
        }
    }

    //We need to have only one version number amongst packages (so public)
    /// <summary>
    /// Documentation Info class.
    /// </summary>
    public class DocumentationInfo
    {
        /// <summary>
        /// Identifies where a Render Pipeline's user-facing documentation lives, so that
        /// <see cref="CurrentPipelineHelpURLAttribute"/> and <see cref="PipelineHelpURLAttribute"/>
        /// build URLs against the right base location.
        /// </summary>
        public enum Location
        {
            /// <summary>
            /// Pages live on the package documentation site
            /// (<c>https://docs.unity3d.com/Packages/&lt;package&gt;@&lt;version&gt;/manual/&lt;page&gt;.html</c>).
            /// This is the default when no <see cref="SourceAttribute"/> is present.
            /// </summary>
            Package,

            /// <summary>
            /// Pages live in the Unity Manual
            /// (<c>https://docs.unity3d.com/&lt;version&gt;/Documentation/Manual/&lt;page&gt;.html</c>).
            /// </summary>
            Manual,
        }

        /// <summary>
        /// Apply this attribute to a <c>RenderPipelineAsset</c> type to declare where its
        /// user-facing documentation is hosted. Consumed by <see cref="CurrentPipelineHelpURLAttribute"/>
        /// and <see cref="PipelineHelpURLAttribute"/> to choose between Unity Manual URLs and
        /// the package documentation site.
        /// </summary>
        /// <remarks>
        /// Editor-only: the attribute is stripped from non-Editor builds via
        /// <see cref="ConditionalAttribute"/>, since the help URL machinery only runs in the Editor.
        /// </remarks>
        /// <example>
        /// <code>
        /// [DocumentationInfo.Source(DocumentationInfo.Location.Manual)]
        /// public class UniversalRenderPipelineAsset : RenderPipelineAsset { /* ... */ }
        /// </code>
        /// </example>
        [Conditional("UNITY_EDITOR")]
        [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
        public sealed class SourceAttribute : Attribute
        {
            /// <summary>The documentation location for the decorated pipeline asset type.</summary>
            public Location Location { get; }

            /// <summary>Creates the attribute with the given documentation location.</summary>
            /// <param name="location">Where the pipeline's documentation pages are hosted.</param>
            public SourceAttribute(Location location)
            {
                Location = location;
            }
        }

        const string fallbackVersion = "13.1";
        const string packageDocumentationUrl = "https://docs.unity3d.com/Packages/{0}@{1}/manual/";
        const string url = packageDocumentationUrl + "{2}.html{3}";

        const string k_ManualUrlFormat = "https://docs.unity3d.com/{0}/Documentation/Manual/{1}.html{2}";
        const string k_ManualFallbackUrlFormat = "https://docs.unity3d.com/Manual/{0}.html{1}";

#if UNITY_EDITOR
        // Resolves a help URL for a page documented under a specific pipeline asset type.
        // The location (Unity Manual vs package site) is taken from a DocumentationInfo.SourceAttribute
        // on the pipeline asset type; without one, falls back to the package site for the
        // package that owns the type.
        internal static string BuildPipelineHelpUrl(Type pipelineAssetType, string pageName, string pageHash)
        {
            if (pipelineAssetType == null)
                return string.Empty;

            var source = pipelineAssetType.GetCustomAttribute<SourceAttribute>(inherit: false);
            if (source != null && source.Location == Location.Manual)
                return GetManualLink(pageName, pageHash);

            if (DocumentationUtils.TryGetPackageInfoForType(pipelineAssetType, out var package, out var version))
                return GetPackageLink(package, version, pageName, pageHash);

            return string.Empty;
        }
#endif

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
        /// Generates a help URL for the given package and page name.
        /// </summary>
        /// <param name="packageName">The package name.</param>
        /// <param name="packageVersion">The package version.</param>
        /// <param name="pageName">The page name without the extension.</param>
        /// <returns>The full URL of the page.</returns>
        public static string GetPackageLink(string packageName, string packageVersion, string pageName) => string.Format(url, packageName, packageVersion, pageName, "");

        /// <summary>
        /// Generates a help URL for the given package, page name and section name.
        /// </summary>
        /// <param name="packageName">The package name.</param>
        /// <param name="packageVersion">The package version.</param>
        /// <param name="pageName">The page name without the extension.</param>
        /// <param name="pageHash">The section name on the documentation page.</param>
        /// <returns>The full URL of the page.</returns>
        public static string GetPackageLink(string packageName, string packageVersion, string pageName, string pageHash)
        {
            if (!string.IsNullOrEmpty(pageHash) && !pageHash.StartsWith("#"))
                pageHash = $"#{pageHash}";
            return string.Format(url, packageName, packageVersion, pageName, pageHash);
        }

        /// <summary>
        /// Generates a help url for the given package and page name
        /// </summary>
        /// <param name="packageName">The package name</param>
        /// <param name="pageName">The page name without the extension.</param>
        /// <returns>The full URL of the page.</returns>
        public static string GetPageLink(string packageName, string pageName) => string.Format(url, packageName, version, pageName, "");

        /// <summary>
        /// Generates a help url for the given package and page name
        /// </summary>
        /// <param name="packageName">The package name</param>
        /// <param name="pageName">The page name without the extension.</param>
        /// <param name="pageHash">The page hash</param>
        /// <returns>The full URL of the page.</returns>
        public static string GetPageLink(string packageName, string pageName, string pageHash)
        {
            if (!string.IsNullOrEmpty(pageHash) && !pageHash.StartsWith("#"))
                pageHash = $"#{pageHash}";
            return string.Format(url, packageName, version, pageName, pageHash);
        }

        /// <summary>
        /// Generates a help url to the index page for the provided package name and package version.
        /// </summary>
        /// <param name="packageName">The name of the package.</param>
        /// <param name="packageVersion">The version of the package.</param>
        /// <returns>The full URL to the default package documentation page.</returns>
        public static string GetDefaultPackageLink(string packageName, string packageVersion) => string.Format(packageDocumentationUrl, packageName, packageVersion);

        /// <summary>
        /// Generates a help url to the index page for the provided package name and package version.
        /// </summary>
        /// <param name="packageName">The name of the package.</param>
        /// <returns>The full URL to the default package documentation page.</returns>
        public static string GetDefaultPackageLink(string packageName) => string.Format(packageDocumentationUrl, packageName, version);

        /// <summary>
        /// Generates a Unity Manual help URL for the given page.
        /// </summary>
        /// <param name="pageName">The page path relative to the Manual root, without the <c>.html</c> extension (for example <c>"urp/Volumes"</c>).</param>
        /// <param name="pageHash">Optional section anchor on the page, with or without the leading <c>#</c>.</param>
        /// <returns>
        /// A versioned Manual URL such as
        /// <c>https://docs.unity3d.com/6000.6/Documentation/Manual/urp/Volumes.html</c> when
        /// the running Unity version can be determined, otherwise the unversioned
        /// fallback <c>https://docs.unity3d.com/Manual/urp/Volumes.html</c>.
        /// </returns>
        /// <example>
        /// <code>
        /// // Versioned: https://docs.unity3d.com/6000.6/Documentation/Manual/urp/Volumes.html
        /// // Fallback:  https://docs.unity3d.com/Manual/urp/Volumes.html
        /// var url = DocumentationInfo.GetManualLink("urp/Volumes");
        ///
        /// // With a section anchor.
        /// var sectionUrl = DocumentationInfo.GetManualLink("urp/features/rendering-debugger-reference", "lighting");
        /// </code>
        /// </example>
        public static string GetManualLink(string pageName, string pageHash = "")
        {
            if (!string.IsNullOrEmpty(pageHash) && !pageHash.StartsWith("#"))
                pageHash = "#" + pageHash;

#if UNITY_EDITOR
            if (UnityEditor.AssetDatabase.IsAssetImportWorkerProcess())
                return string.Format(k_ManualFallbackUrlFormat, pageName, pageHash);
            var unityVersion = UnityEditorInternal.InternalEditorUtility.GetUnityVersion();
            return string.Format(k_ManualUrlFormat, $"{unityVersion.Major}.{unityVersion.Minor}", pageName, pageHash);
#else
            return string.Format(k_ManualFallbackUrlFormat, pageName, pageHash);
#endif
        }
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
            var helpURLAttributes = mask
                .GetType()
                .GetCustomAttributes(typeof(HelpURLAttribute), false);

            return helpURLAttributes.Length == 0 ? string.Empty : $"{((HelpURLAttribute)helpURLAttributes[0]).URL}#{mask}";
        }

        /// <summary>
        /// Obtains the help URL from a type.
        /// </summary>
        /// <param name="type">The type decorated with the HelpURL attribute.</param>
        /// <param name="url">The full URL from the HelpURL attribute. If the attribute is not present, this value is null.</param>
        /// <returns>Returns true if the attribute is present, and false otherwise.</returns>
        public static bool TryGetHelpURL(Type type, out string url)
        {
            var helpURLAttributes = type.GetCustomAttributes(typeof(HelpURLAttribute), true);
            for (int i = 0; i < helpURLAttributes.Length; i++)
            {
                var attr = (HelpURLAttribute)helpURLAttributes[i];
                var attributeUrl = attr.URL;
                if (string.IsNullOrEmpty(attributeUrl))
                    continue;

                url = attributeUrl;
                return true;
            }
            url = null;
            return false;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Obtains package information for a specified type.
        /// </summary>
        /// <param name="type">The type used to retrieve package information.</param>
        /// <param name="packageName">The name of the package containing the given type.</param>
        /// <param name="version">The version number of the package containing the given type. Only Major.Minor will be returned as fix is not used for documentation.</param>
        /// <returns>Returns true if the package information is found; otherwise, false.</returns>
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

        /// <summary>
        /// Obtains a help URL to the index page for the package documentation of a specified type.
        /// </summary>
        /// <param name="type"> The type used to retrieve package information. </param>
        /// <param name="url"> The generated help URL to the package's index documentation page. </param>
        /// <returns> Returns true if a valid help URL is retrieved; otherwise, false. </returns>
        public static bool TryGetDefaultHelpURL([DisallowNull] Type type, [NotNullWhen(true)] out string url)
        {
            url = string.Empty;
            if (TryGetPackageInfoForType(type, out var packageName, out var version))
                url = DocumentationInfo.GetDefaultPackageLink(packageName, version);
            return !string.IsNullOrEmpty(url);
        }
#endif
    }
}
