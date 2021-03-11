using NUnit.Framework;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.XR.Management;
using UnityEngine.Rendering;
using UnityEngine.XR.Management;

namespace Unity.Testing.XR.Editor
{
    public class InjectMockHMD
    {
        static readonly string packageToInject = "com.unity.xr.mock-hmd";
        static readonly string pathToSettings = "Packages/com.unity.testing.xr/XR/XRGeneralSettings.asset";

        [InitializeOnLoadMethod]
        static public void Initialize()
        {
            if (XRGraphicsAutomatedTests.enabled)
            {
                var req = Client.Add(packageToInject);

                while (!req.IsCompleted)
                    System.Threading.Thread.Yield();
            }
        }

        static public void SetupLoader()
        {
            if (XRGraphicsAutomatedTests.enabled)
            {
                var xrGeneralSettings = AssetDatabase.LoadAssetAtPath(pathToSettings, typeof(XRGeneralSettingsPerBuildTarget));
                Assert.NotNull(xrGeneralSettings, "Unable to load " + pathToSettings);

                EditorBuildSettings.AddConfigObject(XRGeneralSettings.k_SettingsKey, xrGeneralSettings, true);
            }
        }
    }
}
