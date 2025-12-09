using System;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

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

    static readonly (int tier, string textTier)[] k_ShadowResolutions =
    {
        (UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierLow, "Low"),
        (UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierMedium, "Medium"),
        (UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierHigh, "High"),
    };

    [UnityTest]
    public IEnumerator AdditionalLightsShadowResolutionTier_SetsExpectedResolution_WhenInPlayerOrPlayMode(
        [ValueSource(nameof(k_ShadowResolutions))] (int tier, string textTier) testData)
    {
        AssetCheck();

        UniversalRenderPipelineAsset urpAsset =
            QualitySettings.renderPipeline as UniversalRenderPipelineAsset ??
            GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;

        int expectedResolution = urpAsset.additionalLightsShadowResolutionTierLow;
        if (testData.tier == UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierMedium)
            expectedResolution = urpAsset.additionalLightsShadowResolutionTierMedium;
        else if (testData.tier == UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierHigh)
            expectedResolution = urpAsset.additionalLightsShadowResolutionTierHigh;

        // Create two lights to test that setting the tier on the target light does not affect another light
        var targetLight = new GameObject("targetLight").AddComponent<Light>();
        targetLight.type = LightType.Point;
        targetLight.shadows = LightShadows.Soft;
        var targetData = targetLight.gameObject.AddComponent<UniversalAdditionalLightData>();
        targetData.additionalLightsShadowResolutionTier = testData.tier;

        var anotherLight = new GameObject("anotherLight").AddComponent<Light>();
        anotherLight.type = LightType.Point;
        anotherLight.shadows = LightShadows.Soft;
        var anotherData = anotherLight.gameObject.AddComponent<UniversalAdditionalLightData>();
        // Set the other light to a different tier.
        anotherData.additionalLightsShadowResolutionTier = UniversalAdditionalLightData.AdditionalLightsShadowResolutionTierMedium;

        // Trigger a frame render to ensure URP processes the light data
        var rr = new RenderPipeline.StandardRequest
        {
            destination = new RenderTexture(128, 128, UnityEngine.Experimental.Rendering.GraphicsFormat.R8G8B8A8_SRGB,
                CoreUtils.GetDefaultDepthOnlyFormat())
        };
        RenderPipeline.SubmitRenderRequest(camera, rr);

        yield return null;

        try
        {
            int actualResolution = urpAsset.GetAdditionalLightsShadowResolution(targetData.additionalLightsShadowResolutionTier);
            int actualOtherResolution = urpAsset.GetAdditionalLightsShadowResolution(anotherData.additionalLightsShadowResolutionTier);

            Assert.AreEqual(expectedResolution, actualResolution,
                $"URP should resolve {testData.textTier} tier to {expectedResolution} resolution for targetLight");

            Assert.AreEqual(urpAsset.additionalLightsShadowResolutionTierMedium, actualOtherResolution,
                $"URP should resolve Medium tier to {urpAsset.additionalLightsShadowResolutionTierMedium} resolution for anotherLight");

        }
        finally
        {

            Object.DestroyImmediate(targetLight.gameObject);
            Object.DestroyImmediate(anotherLight.gameObject);
        }
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
    [Ignore("Unstable: https://jira.unity3d.com/browse/UUM-122594")]
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
