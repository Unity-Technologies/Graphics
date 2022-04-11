using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

public static class LightmappingExt
{
    public static bool BakeAllReflectionProbesSnapshots()
    {
        return Lightmapping.BakeAllReflectionProbesSnapshots();
    }

    public static bool BakeReflectionProbeSnapshot(ReflectionProbe reflectionProbe)
    {
        return Lightmapping.BakeReflectionProbeSnapshot(reflectionProbe);
    }
}

public static class UniversalProjectAssert
{
    public static void AllUrpAssetsHaveMixedLighting(string projectName, bool expectDisabled)
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

            if (expectDisabled)
                Assert.IsFalse(asset.supportsMixedLighting, $"URP asset ({path}) has mixed lighting enabled. {projectName} project should not have URP asset with mixed lighting enabled.");
            else
                Assert.IsTrue(asset.supportsMixedLighting, $"URP asset ({path}) has mixed lighting disabled. {projectName} project must have URP asset with mixed lighting enabled.");
        }
    }

    public static void AllRenderersPostProcessing(string projectName, bool expectDisabled, List<string> excludePaths = null)
    {
        var guids = AssetDatabase.FindAssets("t:UniversalRendererData");
        Assert.NotZero(guids.Length);
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);

            // We only care what is in assets folder
            if (!path.StartsWith("Assets"))
                continue;

            if (excludePaths != null && excludePaths.Contains(path))
                continue;

            var rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(path);
            Assert.IsNotNull(rendererData, $"Failed to load renderer at path {path}");

            if (expectDisabled)
                Assert.IsNull(rendererData.postProcessData, $"Universal Renderer at path {path} has post processing enabled. {projectName} project should not have renderers with post processing enabled.");
            else
                Assert.IsNotNull(rendererData.postProcessData, $"Universal Renderer at path {path} has post processing disabled. {projectName} project must have post processing enabled.");
        }
    }

    public static void AllRenderersAreNotUniversalRenderer(string projectName, List<string> excludePaths = null)
    {
        var guids = AssetDatabase.FindAssets("t:UniversalRendererData");
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);

            // We only care what is in assets folder
            if (!path.StartsWith("Assets"))
                continue;

            if (excludePaths != null && excludePaths.Contains(path))
                continue;

            Assert.AreEqual(guids.Length, 0, $"{projectName} project should not have Universal Renderer ({path}).");
        }
    }

    public static void AllRenderersAreNotRenderer2D(string projectName)
    {
        var guids = AssetDatabase.FindAssets("t:Renderer2DData");
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);

            // We only care what is in assets folder
            if (!path.StartsWith("Assets"))
                continue;

            Assert.AreEqual(guids.Length, 0, $"{projectName} project should not have Renderer 2d ({path}).");
        }
    }

    public static void AllLightingSettingsHaveNoGI(string projectName, List<string> excludePaths = null)
    {
        var guids = AssetDatabase.FindAssets("t:LightingSettings");
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);

            // We only care what is in assets folder
            if (!path.StartsWith("Assets"))
                continue;

            if (excludePaths != null && excludePaths.Contains(path))
                continue;

            LightingSettings lightingSettings = AssetDatabase.LoadAssetAtPath<LightingSettings>(path);
            Assert.IsFalse(lightingSettings.realtimeGI, $"Lighting Setting ({path}) has realtime GI enabled. {projectName} project should not have realtime GI");
            Assert.IsFalse(lightingSettings.bakedGI, $"Lighting Setting ({path}) has baked GI enabled. {projectName} project should not have baked GI");
        }
    }

    public static void AllLightingSettingsHaveNoBakedGI(string projectName, List<string> excludePaths = null)
    {
        var guids = AssetDatabase.FindAssets("t:LightingSettings");
        foreach (var guid in guids)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);

            // We only care what is in assets folder
            if (!path.StartsWith("Assets"))
                continue;

            if (excludePaths != null && excludePaths.Contains(path))
                continue;

            LightingSettings lightingSettings = AssetDatabase.LoadAssetAtPath<LightingSettings>(path);
            Assert.IsFalse(lightingSettings.bakedGI, $"Lighting Setting ({path}) has baked GI enabled. {projectName} project should not have baked GI");
        }
    }

    public static void AllMaterialShadersAreNotBuiltin(string projectName, ShaderPathID shaderPathID)
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
            Assert.AreNotEqual(material.shader, shader, $"Material ({path}) has excluded shader. {projectName} project should not have shader {shader.name}");
        }
    }
}
