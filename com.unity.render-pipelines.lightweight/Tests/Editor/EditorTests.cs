using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.LWRP;

class EditorTests
{
    // When creating LWRP all required resources should be initialized.
    [Test]
    public void ValidateNewAssetResources()
    {
        LightweightRenderPipelineAsset asset = LightweightRenderPipelineAsset.Create();
        Assert.AreNotEqual(null, asset.defaultMaterial);
        Assert.AreNotEqual(null, asset.defaultParticleMaterial);
        Assert.AreNotEqual(null, asset.defaultLineMaterial);
        Assert.AreNotEqual(null, asset.defaultTerrainMaterial);
        Assert.AreNotEqual(null, asset.defaultShader);

        // LWRP doesn't override the following materials
        Assert.AreEqual(null, asset.defaultUIMaterial);
        Assert.AreEqual(null, asset.defaultUIOverdrawMaterial);
        Assert.AreEqual(null, asset.defaultUIETC1SupportedMaterial);
        Assert.AreEqual(null, asset.default2DMaterial);

        Assert.AreNotEqual(null, asset.m_EditorResourcesAsset, "Editor Resources should be initialized when creating a new pipeline.");
        Assert.AreNotEqual(null, asset.m_RendererData, "A default renderer data should be created when creating a new pipeline.");
        ScriptableObject.DestroyImmediate(asset);
    }

    // When changing LWRP settings, all settings should be valid.
    [Test]
    public void ValidateAssetSettings()
    {
        // Create a new asset and validate invalid settings
        LightweightRenderPipelineAsset asset = LightweightRenderPipelineAsset.Create();
        if (asset != null)
        {
            asset.shadowDistance = -1.0f;
            Assert.GreaterOrEqual(asset.shadowDistance, 0.0f);

            asset.renderScale = -1.0f;
            Assert.GreaterOrEqual(asset.renderScale, LightweightRenderPipeline.minRenderScale);

            asset.renderScale = 32.0f;
            Assert.LessOrEqual(asset.renderScale, LightweightRenderPipeline.maxRenderScale);

            asset.shadowNormalBias = -1.0f;
            Assert.GreaterOrEqual(asset.shadowNormalBias, 0.0f);

            asset.shadowNormalBias = 32.0f;
            Assert.LessOrEqual(asset.shadowNormalBias, LightweightRenderPipeline.maxShadowBias);

            asset.shadowDepthBias = -1.0f;
            Assert.GreaterOrEqual(asset.shadowDepthBias, 0.0f);

            asset.shadowDepthBias = 32.0f;
            Assert.LessOrEqual(asset.shadowDepthBias, LightweightRenderPipeline.maxShadowBias);

            asset.maxAdditionalLightsCount = -1;
            Assert.GreaterOrEqual(asset.maxAdditionalLightsCount, 0);

            asset.maxAdditionalLightsCount = 32;
            Assert.LessOrEqual(asset.maxAdditionalLightsCount, LightweightRenderPipeline.maxPerObjectLights);
        }
        ScriptableObject.DestroyImmediate(asset);
    }
}
