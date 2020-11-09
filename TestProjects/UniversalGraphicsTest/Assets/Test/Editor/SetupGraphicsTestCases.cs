using NUnit.Framework;
using System.Threading;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEditor.PackageManager.Requests;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

public class SetupGraphicsTestCases : IPrebuildSetup
{
    public void Setup()
    {
        // Work around case #1033694, unable to use PrebuildSetup types directly from assemblies that don't have special names.
        // Once that's fixed, this line can be deleted and the SetupGraphicsTestCases class in Unity.TestFramework.Graphics.Editor
        // can be used directly instead.
        UnityEditor.TestTools.Graphics.SetupGraphicsTestCases.Setup(UniversalGraphicsTests.universalPackagePath);

        // Configure project for XR tests
        // TODO: move to a common package for all test projects
        if (XRGraphicsAutomatedTests.enabled)
        {
            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            var buildTargetSettings = XRGeneralSettingsPerBuildTarget.XRGeneralSettingsForBuildTarget(buildTargetGroup);
            Assert.That(buildTargetSettings != null, "Unable to read for XR settings for build target!");
            buildTargetSettings.AssignedSettings.loaders.Clear();

            var wasAssigned = XRPackageMetadataStore.AssignLoader(buildTargetSettings.AssignedSettings, "Unity.XR.MockHMD.MockHMDLoader", BuildTargetGroup.Standalone);
            Assert.That(wasAssigned, "Unable to load MockHMD plugin!");

            buildTargetSettings.InitManagerOnStart = true;
        }
    }
}

// TODO: move to a common package for all test projects
[InitializeOnLoad]
class InstallMockHMD
{
    static InstallMockHMD()
    {
        if (XRGraphicsAutomatedTests.enabled)
        {
            AddRequest req = Client.Add("com.unity.xr.mock-hmd");

            while (!req.IsCompleted)
                Thread.Sleep(10);
        }
    }
}
