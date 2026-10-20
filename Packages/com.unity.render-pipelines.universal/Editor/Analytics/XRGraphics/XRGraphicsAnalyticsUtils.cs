using System.Collections.Generic;
#if XR_MANAGEMENT_4_0_1_OR_NEWER
using UnityEditor.XR.Management;
using UnityEngine.XR.Management;
#endif

namespace UnityEditor.Rendering.Universal.XRGraphics
{
    /// <summary>
    /// Shared definitions for the XR graphics analytics category: event name prefix, category name and vendor key.
    /// </summary>
    static class XRGraphicsAnalyticsUtils
    {
        internal const string k_Category = "xr_graphics";
        internal const string k_EventPrefix = "xrgraphics_";

        internal const string k_VendorKey = "unity.universal";

        /// <summary>
        /// Returns the type names of the active XR loaders for the build target group (for example OpenXRLoader).
        /// An empty list means XR is not enabled for that target.
        /// </summary>
        internal static List<string> GetActiveXRLoaders(BuildTargetGroup buildTargetGroup)
        {
            var loaders = new List<string>();
#if XR_MANAGEMENT_4_0_1_OR_NEWER
            XRGeneralSettings generalSettings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(buildTargetGroup);
            if (generalSettings != null && generalSettings.Manager != null)
            {
                foreach (var loader in generalSettings.Manager.activeLoaders)
                {
                    if (loader != null)
                        loaders.Add(loader.GetType().Name);
                }
            }
#endif
            return loaders;
        }
    }
}
