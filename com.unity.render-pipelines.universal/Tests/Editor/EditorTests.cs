using NUnit.Framework;
using UnityEditor;
using UnityEditor.Rendering.Universal;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;

class EditorTests
{
    // When creating a new render pipeline asset it should not log any errors or throw exceptions.
    [Test]
    public void CreatePipelineAssetWithoutErrors()
    {
        // Test without any render pipeline assigned to GraphicsSettings.
        var renderPipelineAsset = GraphicsSettings.renderPipelineAsset;
        GraphicsSettings.renderPipelineAsset = null;

        try
        {
            UniversalRenderPipelineAsset asset = UniversalRenderPipelineAsset.Create();
            LogAssert.NoUnexpectedReceived();
            ScriptableObject.DestroyImmediate(asset);
        }
        // Makes sure the render pipeline is restored in case of a NullReference exception.
        finally
        {
            GraphicsSettings.renderPipelineAsset = renderPipelineAsset;
        }
    }

    // When creating a new forward renderer asset it should not log any errors or throw exceptions.
    [Test]
    public void CreateForwardRendererAssetWithoutErrors()
    {
        // Test without any render pipeline assigned to GraphicsSettings.
        var renderPipelineAsset = GraphicsSettings.renderPipelineAsset;
        GraphicsSettings.renderPipelineAsset = null;

        try
        {
            var asset = ScriptableObject.CreateInstance<ForwardRendererData>();
            ResourceReloader.ReloadAllNullIn(asset, UniversalRenderPipelineAsset.packagePath);
            LogAssert.NoUnexpectedReceived();
            ScriptableObject.DestroyImmediate(asset);
        }
        // Makes sure the render pipeline is restored in case of a NullReference exception.
        finally
        {
            GraphicsSettings.renderPipelineAsset = renderPipelineAsset;
        }
    }

    // Validate that resources Guids are valid
    [Test]
    public void ValidateBuiltinResourceFiles()
    {
        string templatePath = AssetDatabase.GUIDToAssetPath(ResourceGuid.rendererTemplate);
        Assert.IsFalse(string.IsNullOrEmpty(templatePath));
    }

    // When creating LWRP all required resources should be initialized.
    [Test]
    public void ValidateNewAssetResources()
    {
        UniversalRenderPipelineAsset asset = UniversalRenderPipelineAsset.Create();
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
        UniversalRenderPipelineAsset asset = UniversalRenderPipelineAsset.Create();
        if (asset != null)
        {
            asset.shadowDistance = -1.0f;
            Assert.GreaterOrEqual(asset.shadowDistance, 0.0f);

            asset.renderScale = -1.0f;
            Assert.GreaterOrEqual(asset.renderScale, UniversalRenderPipeline.minRenderScale);

            asset.renderScale = 32.0f;
            Assert.LessOrEqual(asset.renderScale, UniversalRenderPipeline.maxRenderScale);

            asset.shadowNormalBias = -1.0f;
            Assert.GreaterOrEqual(asset.shadowNormalBias, 0.0f);

            asset.shadowNormalBias = 32.0f;
            Assert.LessOrEqual(asset.shadowNormalBias, UniversalRenderPipeline.maxShadowBias);

            asset.shadowDepthBias = -1.0f;
            Assert.GreaterOrEqual(asset.shadowDepthBias, 0.0f);

            asset.shadowDepthBias = 32.0f;
            Assert.LessOrEqual(asset.shadowDepthBias, UniversalRenderPipeline.maxShadowBias);

            asset.maxAdditionalLightsCount = -1;
            Assert.GreaterOrEqual(asset.maxAdditionalLightsCount, 0);

            asset.maxAdditionalLightsCount = 32;
            Assert.LessOrEqual(asset.maxAdditionalLightsCount, UniversalRenderPipeline.maxPerObjectLights);
        }
        ScriptableObject.DestroyImmediate(asset);
    }
}
