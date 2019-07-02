using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.LWRP;
using UnityEngine.Rendering;

[TestFixture]
class RuntimeTests
{
    GameObject go;
    Camera camera;
    RenderPipelineAsset prevAsset;
    LightweightRenderPipelineAsset asset;

    [SetUp]
    public void Setup()
    {
        go = new GameObject();
        camera = go.AddComponent<Camera>();
        prevAsset = GraphicsSettings.renderPipelineAsset;
        asset = ScriptableObject.CreateInstance<LightweightRenderPipelineAsset>();
    }

    [TearDown]
    public void Cleanup()
    {
        GraphicsSettings.renderPipelineAsset = prevAsset;
        Object.DestroyImmediate(asset);
        Object.DestroyImmediate(go);
    }

    // When LWRP pipeline is active, lightsUseLinearIntensity must match active color space.
    [UnityTest]
    public IEnumerator PipelineHasCorrectColorSpace()
    {
        GraphicsSettings.renderPipelineAsset = asset;
        camera.Render();
        yield return null;
        
        Assert.AreEqual(QualitySettings.activeColorSpace == ColorSpace.Linear, GraphicsSettings.lightsUseLinearIntensity,
            "GraphicsSettings.lightsUseLinearIntensity must match active color space.");
    }

    // When switching to LWRP it sets "LightweightPipeline" as global shader tag.
    // When switching to Built-in it sets "" as global shader tag.
    [UnityTest]
    public IEnumerator PipelineSetsAndRestoreGlobalShaderTagCorrectly()
    {
        GraphicsSettings.renderPipelineAsset = asset;
        camera.Render();
        yield return null;

        Assert.AreEqual("LightweightPipeline", Shader.globalRenderPipeline, "Wrong render pipeline shader tag.");

        GraphicsSettings.renderPipelineAsset = null;
        camera.Render();
        yield return null;

        Assert.AreEqual("", Shader.globalRenderPipeline, "Render Pipeline shader tag is not restored.");
    }
}
