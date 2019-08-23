using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;

[TestFixture]
class RuntimeTests
{
    GameObject go;
    Camera camera;
    RenderPipelineAsset currentAsset;

    [SetUp]
    public void Setup()
    {
        go = new GameObject();
        camera = go.AddComponent<Camera>();
        currentAsset = GraphicsSettings.renderPipelineAsset;
    }

    [TearDown]
    public void Cleanup()
    {
        GraphicsSettings.renderPipelineAsset = currentAsset;
        Object.DestroyImmediate(go);
    }

    // When LWRP pipeline is active, lightsUseLinearIntensity must match active color space.
    [UnityTest]
    public IEnumerator PipelineHasCorrectColorSpace()
    {
        AssetCheck();
        
        camera.Render();
        yield return null;

        Assert.AreEqual(QualitySettings.activeColorSpace == ColorSpace.Linear, GraphicsSettings.lightsUseLinearIntensity,
            "GraphicsSettings.lightsUseLinearIntensity must match active color space.");
    }

    // When switching to LWRP it sets "UniversalPipeline" as global shader tag.
    // When switching to Built-in it sets "" as global shader tag.
    [UnityTest]
    public IEnumerator PipelineSetsAndRestoreGlobalShaderTagCorrectly()
    {
        AssetCheck();
        
        camera.Render();
        yield return null;

        Assert.AreEqual("UniversalPipeline,LightweightPipeline", Shader.globalRenderPipeline, "Wrong render pipeline shader tag.");

        GraphicsSettings.renderPipelineAsset = null;
        camera.Render();
        yield return null;
        camera.Render();
        yield return null;

        Assert.AreEqual("", Shader.globalRenderPipeline, "Render Pipeline shader tag is not restored.");
    }

    void AssetCheck()
    {
        Assert.IsNotNull(currentAsset, "Render Pipeline Asset is Null");

        Assert.AreEqual(currentAsset.GetType(), typeof(UniversalRenderPipelineAsset),
            "Pipeline Asset is not Universal RP");
    }
}
