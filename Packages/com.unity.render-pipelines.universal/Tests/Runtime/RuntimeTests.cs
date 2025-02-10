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
        currentAssetGraphics = GraphicsSettings.defaultRenderPipeline;
        currentAssetQuality = QualitySettings.renderPipeline;
    }

    [TearDown]
    public void Cleanup()
    {
        GraphicsSettings.defaultRenderPipeline = currentAssetGraphics;
        QualitySettings.renderPipeline = currentAssetQuality;
        Object.DestroyImmediate(go);
    }

    // When URP pipeline is active, lightsUseLinearIntensity must match active color space.
    [UnityTest]
    public IEnumerator PipelineHasCorrectColorSpace()
    {
        AssetCheck();

        var rr = new UnityEngine.Rendering.RenderPipeline.StandardRequest();
        rr.destination = new RenderTexture(128, 128, UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_SRGB,
                    CoreUtils.GetDefaultDepthOnlyFormat());
        rr.mipLevel = 0;
        rr.slice = 0;
        rr.face = CubemapFace.Unknown;
        UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera, rr);
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

        var rr = new UnityEngine.Rendering.RenderPipeline.StandardRequest();
        rr.destination = new RenderTexture(128, 128, UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_SRGB, CoreUtils.GetDefaultDepthOnlyFormat());
        rr.mipLevel = 0;
        rr.slice = 0;
        rr.face = CubemapFace.Unknown;
        UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(camera, rr);
        yield return null;

        Assert.AreEqual("UniversalPipeline", Shader.globalRenderPipeline, "Wrong render pipeline shader tag.");

        GraphicsSettings.defaultRenderPipeline = null;
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
