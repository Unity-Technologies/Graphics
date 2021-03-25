using NUnit.Framework;
using System.Collections;
using Unity.Testing.XR.Editor;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEditor.XR.Management;
using UnityEngine.TestTools;
using UnityEngine.TestTools.Graphics;

class InjectMockHMDTest
{
    [Test]
    public void ValidateLoaderTest()
    {
        if (RuntimeSettings.reuseTestsForXR)
        {
            InjectMockHMD.SetupLoader();

            var buildTargetSettings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(BuildTargetGroup.Standalone);

// XRTODO: remove pragmas once MockHMD package is published with new dependencies to xr.sdk.management and replace loaders with activeLoaders
#pragma warning disable CS0618
            Assert.That(buildTargetSettings != null, "Unable to read for XR settings for build target!");
            Assert.AreEqual(1, buildTargetSettings.AssignedSettings.loaders.Count, "There should be exactly one XR loader!");
            Assert.That(buildTargetSettings.InitManagerOnStart, "XR loader is not set to init on start!");
            Assert.AreEqual("Mock HMD Loader", buildTargetSettings.AssignedSettings.loaders[0].name, "Invalid XR loader found!");
#pragma warning restore CS0618
        }
    }

    bool IsPackageInstalled(string name, ListRequest req)
    {
        Assume.That(req.Status == StatusCode.Success, "Client.List() failed!");

        foreach (var package in req.Result)
        {
            if (package.name == name)
                return true;
        }

        return false;
    }

    [UnityTest]
    public IEnumerator AddPackageTest()
    {
        var req = Client.List();
        while (!req.IsCompleted)
            yield return null;

        bool packageFound = IsPackageInstalled("com.unity.xr.mock-hmd", req);

        if (RuntimeSettings.reuseTestsForXR)
        {
            Assert.True(packageFound, "MockHMD was not installed!");
        }
        else
        {
            Assert.False(packageFound, "MockHMD should not be installed when running non-XR tests!");
        }
    }
}
