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
    [Test]
    public void CheckIfPostProcessingDisabled()
    {
        var guids = AssetDatabase.FindAssets("t:UniversalRendererData");
        Assert.NotZero(guids.Length);
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);

            // We only care what is in assets folder
            if (!path.StartsWith("Assets"))
                continue;

            var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
            Assert.IsNotNull(rendererData, $"Failed to load renderer at path {path}");

            Assert.IsNull(rendererData.postProcessData, $"Universal renderer at path {path} has post processing enabled. Foundation project should not have renderers with post processing enabled.");
        }
    }

    [Test]
    public void CheckIfMixedLightingDisabled()
    {
        var guids = AssetDatabase.FindAssets("t:UniversalRenderPipelineAsset");
        Assert.NotZero(guids.Length);
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);

            // We only care what is in assets folder
            if (!path.StartsWith("Assets"))
                continue;

            var asset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(path);
            Assert.IsNotNull(asset, $"Failed to load URP asset at path {path}");

            Assert.IsFalse(asset.supportsMixedLighting, $"URP asset at path {path} has post mixed lighting enabled. Foundation project should not have URP asset with mixed lighting enabled.");
        }
    }

    [Test]
    public void CheckIfRenderer2DIsNotPresent()
    {
        var guids = AssetDatabase.FindAssets("t:Renderer2DData");
        Assert.AreEqual(guids.Length, 0, "Foundation project should not have renderer 2d.");
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
    public void CheckIfScenesDoNoHaveShader(ShaderPathID shaderPathID)
    {
        string shaderPath = AssetDatabase.GUIDToAssetPath(ShaderUtils.GetShaderGUID(shaderPathID));
        Assert.IsFalse(string.IsNullOrEmpty(shaderPath));

        var shader = AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
        Assert.AreEqual(shader.name, ShaderUtils.GetShaderPath(shaderPathID));

        var guids = AssetDatabase.FindAssets("t:Material");
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);

            // We only care what is in assets folder
            if (!path.StartsWith("Assets"))
                continue;

            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            Assert.AreNotEqual(material.shader, shader, $"Material at path {path} has excluded shader. Foundation project should not have shader {shader.name}");
        }
    }
}
