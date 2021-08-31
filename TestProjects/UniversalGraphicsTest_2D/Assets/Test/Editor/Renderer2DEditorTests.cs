using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

class Renderer2DEditorTests
{
    const string KProjectName = "2D";

    [Test]
    public void AllRenderersPostProcessingDisabled()
    {
        UniversalProjectAssert.AllRenderersPostProcessing(KProjectName, expectDisabled: true,
            new List<string>() { "Assets/CommonAssets/ForwardRenderer.asset" }); // Used by 003_PixelPerfect_PostProcessing
    }

    [Test]
    public void AllUrpAssetsHaveMixedLightingEnabled()
    {
        UniversalProjectAssert.AllUrpAssetsHaveMixedLighting(KProjectName, expectDisabled: false);
    }

    [Test]
    public void AllRenderersAreNotUniversalRenderer()
    {
        UniversalProjectAssert.AllRenderersAreNotUniversalRenderer(KProjectName,
            new List<string>() { "Assets/CommonAssets/ForwardRenderer.asset" }); // Used by 069_2D_Forward_Shader_Compatibility_Forward
    }
}
