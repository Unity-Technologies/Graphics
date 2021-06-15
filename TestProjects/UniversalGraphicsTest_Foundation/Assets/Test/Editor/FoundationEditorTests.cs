using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor.Rendering.Universal.Internal;
using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;

class FoundationEditorTests
{
    const string kProjectName = "Foundation";

    [Test]
    public void AllRenderersPostProcessingDisabled()
    {
        UniversalProjectAssert.AllRenderersPostProcessing(kProjectName, expectDisabled: true);
    }

    [Test]
    public void AllUrpAssetsHaveMixedLightingDisabled()
    {
        UniversalProjectAssert.AllUrpAssetsHaveMixedLighting(kProjectName, expectDisabled: true);
    }

    [Test]
    public void AllRenderersAreNotRenderer2D()
    {
        UniversalProjectAssert.AllRenderersAreNotRenderer2D(kProjectName);
    }

    [Test]
    public void CheckIfScenesDoNoHaveGI()
    {
        var guids = AssetDatabase.FindAssets("t:LightingSettings");
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);

            // We only care what is in assets folder
            if (!path.StartsWith("Assets"))
                continue;

            LightingSettings lightingSettings = AssetDatabase.LoadAssetAtPath<LightingSettings>(path);
            //Assert.IsFalse(lightingSettings.realtimeGI, $"Lighting Setting at path {path} has realtime GI enabled. Foundation project should not have realtime GI");
            //Assert.IsFalse(lightingSettings.bakedGI, $"Lighting Setting at path {path} has realtime GI enabled. Foundation project should not have baked GI");
        }
    }

    [TestCase(ShaderPathID.SpeedTree7)]
    [TestCase(ShaderPathID.SpeedTree7Billboard)]
    [TestCase(ShaderPathID.SpeedTree8)]
    public void AllShadersAreNot(ShaderPathID shaderPathID)
    {
        UniversalProjectAssert.AllShadersAreNot(kProjectName, shaderPathID);
    }
}
