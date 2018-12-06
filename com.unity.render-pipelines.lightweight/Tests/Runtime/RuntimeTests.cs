using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.LWRP;
using UnityEngine.Rendering;

class RuntimeTests
{
    [UnityTest]
    public IEnumerator PipelineHasCorrectRenderingSettings()
    {
        RenderPipelineAsset prevAsset = GraphicsSettings.renderPipelineAsset;

        LightweightRenderPipelineAsset asset = ScriptableObject.CreateInstance<LightweightRenderPipelineAsset>();
        GraphicsSettings.renderPipelineAsset = asset;
        yield return null;

        //Assert.AreEqual(Shader.globalRenderPipeline, "LightweightPipeline", "Wrong render pipeline shader tag.");
        //Assert.AreEqual(GraphicsSettings.lightsUseLinearIntensity, true, "LWRP must use linear light intensities.");

        GraphicsSettings.renderPipelineAsset = prevAsset;
        yield return null;

        ScriptableObject.DestroyImmediate(asset);
    }

    [UnityTest]
    public IEnumerator PipelineRestoreCorrectSettingsWhenSwitchingToBuiltinPipeline()
    {
        RenderPipelineAsset prevAsset = GraphicsSettings.renderPipelineAsset;
        LightweightRenderPipelineAsset asset = ScriptableObject.CreateInstance<LightweightRenderPipelineAsset>();
        GraphicsSettings.renderPipelineAsset = asset;
        yield return null;

        GraphicsSettings.renderPipelineAsset = null;
        yield return null;
        
        //Assert.AreEqual(Shader.globalRenderPipeline, "", "Render Pipeline shader tag is not restored.");
        GraphicsSettings.renderPipelineAsset = prevAsset;
        ScriptableObject.DestroyImmediate(asset);
    }
}
