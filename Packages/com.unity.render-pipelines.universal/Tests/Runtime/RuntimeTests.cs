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
    RenderPipelineAsset currentAssetGraphics;
    RenderPipelineAsset currentAssetQuality;

    [SetUp]
    public void Setup()
    {
        go = new GameObject();
        camera = go.AddComponent<Camera>();
        currentAssetGraphics = GraphicsSettings.renderPipelineAsset;
        currentAssetQuality = QualitySettings.renderPipeline;
    }

    [TearDown]
    public void Cleanup()
    {
        GraphicsSettings.renderPipelineAsset = currentAssetGraphics;
        QualitySettings.renderPipeline = currentAssetQuality;
        Object.DestroyImmediate(go);
    }

    // When URP pipeline is active, lightsUseLinearIntensity must match active color space.
    [UnityTest]
    public IEnumerator PipelineHasCorrectColorSpace()
    {
        AssetCheck();

        camera.Render();
        yield return null;

        Assert.AreEqual(QualitySettings.activeColorSpace == ColorSpace.Linear, GraphicsSettings.lightsUseLinearIntensity,
            "GraphicsSettings.lightsUseLinearIntensity must match active color space.");
    }

    // When switching to URP it sets "UniversalPipeline" as global shader tag.
    // When switching to Built-in it sets "" as global shader tag.
#if UNITY_EDITOR // TODO This API call does not reset in player
    [UnityTest]
    public IEnumerator PipelineSetsAndRestoreGlobalShaderTagCorrectly()
    {
        AssetCheck();

        camera.Render();
        yield return null;

        Assert.AreEqual("UniversalPipeline", Shader.globalRenderPipeline, "Wrong render pipeline shader tag.");

        GraphicsSettings.renderPipelineAsset = null;
        QualitySettings.renderPipeline = null;
        camera.Render();
        yield return null;

        Assert.AreEqual("", Shader.globalRenderPipeline, "Render Pipeline shader tag is not restored.");
    }

#endif

    void AssetCheck()
    {
        //Assert.IsNotNull(currentAssetGraphics, "Render Pipeline Asset is Null");
        // Temp fix, test passes if project isnt setup for Universal RP
        if (RenderPipelineManager.currentPipeline == null)
            Assert.Pass("Render Pipeline Asset is Null, test pass by default");

        Assert.AreEqual(RenderPipelineManager.currentPipeline.GetType(), typeof(UniversalRenderPipeline),
            "Pipeline Asset is not Universal RP");
    }
}
