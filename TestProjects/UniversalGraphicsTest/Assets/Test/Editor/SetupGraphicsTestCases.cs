using UnityEditor;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine.Rendering;
using UnityEngine.TestTools;
using NUnit.Framework;


public class SetupGraphicsTestCases : IPrebuildSetup
{
    public void Setup()
    {
        // Work around case #1033694, unable to use PrebuildSetup types directly from assemblies that don't have special names.
        // Once that's fixed, this line can be deleted and the SetupGraphicsTestCases class in Unity.TestFramework.Graphics.Editor
        // can be used directly instead.
        UnityEditor.TestTools.Graphics.SetupGraphicsTestCases.Setup(UniversalGraphicsTests.universalPackagePath);

        // Configure project for XR tests
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
