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
        Assert.AreNotEqual(asset.defaultMaterial, null);
        Assert.AreNotEqual(asset.defaultParticleMaterial, null);
        Assert.AreNotEqual(asset.defaultLineMaterial, null);
        Assert.AreNotEqual(asset.defaultTerrainMaterial, null);
        Assert.AreNotEqual(asset.defaultShader, null);

        // LWRP doesn't override the following materials
        Assert.AreEqual(asset.defaultUIMaterial, null);
        Assert.AreEqual(asset.defaultUIOverdrawMaterial, null);
        Assert.AreEqual(asset.defaultUIETC1SupportedMaterial, null);
        Assert.AreEqual(asset.default2DMaterial, null);

        Assert.AreNotEqual(asset.m_EditorResourcesAsset, null, "Editor Resources should be initialized when creating a new pipeline.");
        Assert.AreNotEqual(asset.m_RendererData, null, "A default renderer data should be created when creating a new pipeline.");
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
