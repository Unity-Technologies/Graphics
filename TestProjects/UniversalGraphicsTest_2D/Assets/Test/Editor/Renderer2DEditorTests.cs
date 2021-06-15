using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

class Renderer2DEditorTests
{
    const string KProjectName = "2D";

    // TODO
    // Assets/CommonAssets/ForwardRenderer.asset 003_PixelPerfect_PostProcessing
    //[Test]
    public void AllRenderersPostProcessingDisabled()
    {
        UniversalProjectAssert.AllRenderersPostProcessing(KProjectName, expectDisabled:true);
    }

    [Test]
    public void AllUrpAssetsHaveMixedLightingEnabled()
    {
        UniversalProjectAssert.AllUrpAssetsHaveMixedLighting(KProjectName, expectDisabled: false);
    }

    // TODO
    // There is a lot of universal renderers
    //[Test]
    public void AllRenderersAreNotUniversalRenderer()
    {
        UniversalProjectAssert.AllRenderersAreNotUniversalRenderer(KProjectName);
    }

}
