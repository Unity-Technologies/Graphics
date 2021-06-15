using NUnit.Framework;
using UnityEngine.Rendering.Universal;

class FoundationEditorTests
{
    const string kProjectName = "Terrain";

    [Test]
    public void AllRenderersPostProcessingDisabled()
    {
        UniversalProjectAssert.AllRenderersPostProcessing(kProjectName, expectDisabled:true);
    }

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

    // TODO:
    // Assets/Scenes/058_SpeedTree_V8Settings.lighting
    //[Test]
    public void CheckIfScenesDoNoHaveGI()
    {
        UniversalProjectAssert.AllLightingSettingsHaveNoGI(kProjectName);
    }

    // TODO:
    // Imported models have automated lit generated, which is not used
    //[TestCase(ShaderPathID.Lit)]
    [TestCase(ShaderPathID.SimpleLit)]
    [TestCase(ShaderPathID.Unlit)]
    [TestCase(ShaderPathID.ParticlesLit)]
    [TestCase(ShaderPathID.ParticlesSimpleLit)]
    [TestCase(ShaderPathID.ParticlesUnlit)]
    public void AllShadersAreNot(ShaderPathID shaderPathID)
    {
        UniversalProjectAssert.AllShadersAreNot(kProjectName, shaderPathID);
    }
}
