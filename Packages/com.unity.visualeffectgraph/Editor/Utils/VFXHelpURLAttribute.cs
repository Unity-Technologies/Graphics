using System;
using System.Diagnostics;

using UnityEngine;

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
    class VFXHelpURLAttribute : HelpURLAttribute
    {
        const string fallbackVersion = "13.1";
        const string url = "https://docs.unity3d.com/Packages/{0}@{1}/manual/{2}.html{3}";

        /// <summary>
        /// The constructor of the attribute
        /// </summary>
        /// <param name="pageName"></param>
        /// <param name="packageName"></param>
        public VFXHelpURLAttribute(string pageName, string packageName = "com.unity.visualeffectgraph")
            : base(GetPageLink(packageName, pageName))
        {
        }

        public static string version
        {
            get
            {
#if UNITY_EDITOR
                if (TryGetPackageInfoForType(typeof(VFXHelpURLAttribute), out _, out var version))
                    return version;
#endif
                return fallbackVersion;
            }
        }

        public static string GetPageLink(string packageName, string pageName) => string.Format(url, packageName, version, pageName, "");

#if UNITY_EDITOR
        /// <summary>
        /// Obtain package information from a specific type
        /// </summary>
        /// <param name="type">The type used to retrieve package information</param>
        /// <param name="packageName">The name of the package containing the given type</param>
        /// <param name="version">The version number of the package containing the given type. Only Major.Minor will be returned as fix is not used for documentation</param>
        /// <returns></returns>
        public static bool TryGetPackageInfoForType(Type type, out string packageName, out string version)
        {
            var packageInfo = PackageManager.PackageInfo.FindForAssembly(type.Assembly);
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
