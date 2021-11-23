using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

class LightingEditorTests
{
    const string kProjectName = "Lighting";

    [Test]
    public void AllRenderersPostProcessingDisabled()
    {
        UniversalProjectAssert.AllRenderersPostProcessing(kProjectName, expectDisabled: true);
    }

    [Test]
    public void AllUrpAssetsHaveMixedLightingEnabled()
    {
        UniversalProjectAssert.AllUrpAssetsHaveMixedLighting(kProjectName, expectDisabled: false);
    }

    [Test]
    public void EnsureOnlySingleQualityOption()
    {
        Assert.IsTrue(QualitySettings.names?.Length == 1, $"{kProjectName} project MUST have ONLY single quality setting to ensure test consistency!!!");
    }
}
