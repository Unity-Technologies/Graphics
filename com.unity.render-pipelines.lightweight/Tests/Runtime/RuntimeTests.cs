using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.LWRP;
using UnityEngine.Rendering;

class RuntimeTests
{
    // When LWRP pipeline is active, lightsUseLinearIntensity must match active color space.
    [UnityTest]
    public IEnumerator PipelineHasCorrectColorSpace()
    {
        GameObject go = new GameObject();
        Camera camera = go.AddComponent<Camera>();
        RenderPipelineAsset prevAsset = GraphicsSettings.renderPipelineAsset;

        LightweightRenderPipelineAsset asset = ScriptableObject.CreateInstance<LightweightRenderPipelineAsset>();
        GraphicsSettings.renderPipelineAsset = asset;
        camera.Render();
        yield return null;
        
        Assert.AreEqual(QualitySettings.activeColorSpace == ColorSpace.Linear, GraphicsSettings.lightsUseLinearIntensity,
            "GraphicsSettings.lightsUseLinearIntensity must match active color space.");

        GraphicsSettings.renderPipelineAsset = prevAsset;
        ScriptableObject.DestroyImmediate(asset);
        GameObject.DestroyImmediate(go);
    }

    // When switching to LWRP it sets "LightweightPipeline" as global shader tag.
    // When switching to Built-in it sets "" as global shader tag.
    [UnityTest]
    public IEnumerator PipelineSetsAndRestoreGlobalShaderTagCorrectly()
    {
        GameObject go = new GameObject();
        Camera camera = go.AddComponent<Camera>();
        RenderPipelineAsset prevAsset = GraphicsSettings.renderPipelineAsset;
        LightweightRenderPipelineAsset asset = ScriptableObject.CreateInstance<LightweightRenderPipelineAsset>();
        GraphicsSettings.renderPipelineAsset = asset;
        camera.Render();
        yield return null;

        Assert.AreEqual("LightweightPipeline", Shader.globalRenderPipeline, "Wrong render pipeline shader tag.");

        GraphicsSettings.renderPipelineAsset = null;
        camera.Render();
        yield return null;

        Assert.AreEqual("", Shader.globalRenderPipeline, "Render Pipeline shader tag is not restored.");
        GraphicsSettings.renderPipelineAsset = prevAsset;

        ScriptableObject.DestroyImmediate(asset);
        GameObject.DestroyImmediate(go);
    }
}
