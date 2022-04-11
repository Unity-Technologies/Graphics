using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

class LightingEditorTests
{
    const string KProjectName = "Lighting";

    [Test]
    public void AllRenderersPostProcessingDisabled()
    {
        UniversalProjectAssert.AllRenderersPostProcessing(KProjectName, expectDisabled: true);
    }

    [Test]
    public void AllUrpAssetsHaveMixedLightingEnabled()
    {
        UniversalProjectAssert.AllUrpAssetsHaveMixedLighting(KProjectName, expectDisabled: false);
    }
}
