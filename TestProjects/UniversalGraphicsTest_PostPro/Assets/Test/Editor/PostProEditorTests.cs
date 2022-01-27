using NUnit.Framework;
using UnityEngine.Rendering.Universal;

class FoundationEditorTests
{
    const string kProjectName = "PostPro";

    [Test]
    public void AllRenderersPostProcessingEnabled()
    {
        UniversalProjectAssert.AllRenderersPostProcessing(kProjectName, expectDisabled: false);
    }

    /// <summary>
    /// The URP post processing tests expect all pipeline assets to explicitly disable mixed lighting support
    /// since it significantly increases project build times.
    /// </summary>
    [Test]
    public void CheckIfMixedLightingDisabled()
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
        UniversalProjectAssert.AllLightingSettingsHaveNoGI(kProjectName);
    }

    [TestCase(ShaderPathID.TerrainLit)]
    public void CheckIfScenesDoNoHaveShader(ShaderPathID shaderPathID)
    {
        UniversalProjectAssert.AllMaterialShadersAreNotBuiltin(kProjectName, shaderPathID);
    }

    [Test]
    public void EnsureSingleQualityOption()
    {
        Assert.IsTrue(UnityEngine.QualitySettings.names?.Length == 1, $"{kProjectName} project MUST have ONLY single quality setting to ensure test consistency!!!");
    }
}
