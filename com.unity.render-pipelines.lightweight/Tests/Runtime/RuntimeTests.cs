using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.Experimental.Rendering.LightweightPipeline;
using UnityEngine.Rendering;

class RuntimeTests
{
    [UnityTest]
    public IEnumerator PipelineHasCorrectRenderingSettings()
    {
        yield return null;

        LightweightRenderPipelineAsset asset = GraphicsSettings.renderPipelineAsset as LightweightRenderPipelineAsset;
        Assert.AreNotEqual(asset, null, "LWRP asset is not assigned in the GraphicsSettings.");
        Assert.AreEqual(Shader.globalRenderPipeline, "LightweightPipeline", "Wrong render pipeline shader tag.");
        Assert.AreEqual(GraphicsSettings.lightsUseLinearIntensity, true, "LWRP must use linear light intensities.");
    }

    [UnityTest]
    public IEnumerator PipelineRestoreCorrectSettingsWhenSwitchingToBuiltinPipeline()
    {
        yield return null;

        LightweightRenderPipelineAsset asset = GraphicsSettings.renderPipelineAsset as LightweightRenderPipelineAsset;
        Assert.AreNotEqual(asset, null, "LWRP asset is not assigned in the GraphicsSettings.");
        GraphicsSettings.renderPipelineAsset = null;

        yield return null;

        Assert.AreEqual(Shader.globalRenderPipeline, "", "Render Pipeline shader tag is not restored.");
        GraphicsSettings.renderPipelineAsset = asset;
    }
}
