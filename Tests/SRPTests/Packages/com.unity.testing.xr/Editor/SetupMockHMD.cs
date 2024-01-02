using NUnit.Framework;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.XR.Management;
using UnityEngine.Rendering;
using UnityEngine.XR.Management;

namespace Unity.Testing.XR.Editor
{
    public class SetupMockHMD
    {
        static readonly string pathToSettings = "Packages/com.unity.testing.xr/XR/XRGeneralSettings.asset";
        
        static public void SetupLoader()
        {
            if (XRGraphicsAutomatedTests.enabled)
            {
                var xrGeneralSettings = AssetDatabase.LoadAssetAtPath(pathToSettings, typeof(XRGeneralSettingsPerBuildTarget));
                Assert.NotNull(xrGeneralSettings, "Unable to load " + pathToSettings);

                EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, xrGeneralSettings, true);
                var settings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);
                settings.InitManagerOnStart = true;
                UnityEditor.XR.Management.Metadata.XRPackageMetadataStore.AssignLoader(settings.AssignedSettings, "Unity.XR.MockHMD.MockHMDLoader", BuildTargetGroup.Standalone);
 
                EditorUtility.SetDirty(xrGeneralSettings);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }
    }
}
