using NUnit.Framework;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;

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
        UniversalProjectAssert.AllLightingSettingsHaveNoBakedGI(kProjectName, new List<string>()
        {
            "Assets/Scenes/010_AdditionalLightsSortedSettings.lighting",
            "Assets/Scenes/026_Shader_PBRscene_AccurateGBufferSettings.lighting",
            "Assets/Scenes/029_Particles_DeferredSettings.lighting",
            "Assets/Scenes/054_Lighting_AttenuationSettings.lighting",
            "Assets/Scenes/130_ClearCoat_deferred/130_ClearCoat_deferred_LightingSettings.lighting",
            "Assets/Scenes/230_Decal_Projector.lighting",
            "Assets/Scenes/231_Decal_Mesh.lighting",
        });
    }

    [TestCase(ShaderPathID.SpeedTree7)]
    [TestCase(ShaderPathID.SpeedTree7Billboard)]
    [TestCase(ShaderPathID.SpeedTree8)]
    public void AllMaterialShadersAreNotBuiltin(ShaderPathID shaderPathID)
    {
        UniversalProjectAssert.AllMaterialShadersAreNotBuiltin(kProjectName, shaderPathID);
    }
}
