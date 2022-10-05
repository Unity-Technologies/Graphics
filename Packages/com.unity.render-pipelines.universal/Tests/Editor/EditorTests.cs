using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor.Rendering.Universal.Internal;
using UnityEngine.Experimental.Rendering.Universal;
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
            UniversalRendererData data = ScriptableObject.CreateInstance<UniversalRendererData>();
            UniversalRenderPipelineAsset asset = UniversalRenderPipelineAsset.Create(data);
            LogAssert.NoUnexpectedReceived();
            ScriptableObject.DestroyImmediate(asset);
            ScriptableObject.DestroyImmediate(data);
        }
        // Makes sure the render pipeline is restored in case of a NullReference exception.
        finally
        {
            GraphicsSettings.renderPipelineAsset = renderPipelineAsset;
        }
    }

    // When creating a new Universal Renderer asset it should not log any errors or throw exceptions.
    [Test]
    public void CreateUniversalRendererAssetWithoutErrors()
    {
        // Test without any render pipeline assigned to GraphicsSettings.
        var renderPipelineAsset = GraphicsSettings.renderPipelineAsset;
        GraphicsSettings.renderPipelineAsset = null;

        try
        {
            var asset = ScriptableObject.CreateInstance<UniversalRendererData>();
            ResourceReloader.ReloadAllNullIn(asset, UniversalRenderPipelineAsset.packagePath);
            var renderer = asset.InternalCreateRenderer();
            LogAssert.NoUnexpectedReceived();
            renderer.Dispose();
            ScriptableObject.DestroyImmediate(asset);
        }
        // Makes sure the render pipeline is restored in case of a NullReference exception.
        finally
        {
            GraphicsSettings.renderPipelineAsset = renderPipelineAsset;
        }
    }

    // When creating a new renderer 2d asset it should not log any errors or throw exceptions.
    [Test]
    public void CreateRenderer2DAssetWithoutErrors()
    {
        // Test without any render pipeline assigned to GraphicsSettings.
        var renderPipelineAsset = GraphicsSettings.renderPipelineAsset;
        GraphicsSettings.renderPipelineAsset = null;

        try
        {
            var asset = ScriptableObject.CreateInstance<Renderer2DData>();
            ResourceReloader.ReloadAllNullIn(asset, UniversalRenderPipelineAsset.packagePath);
            var renderer = asset.InternalCreateRenderer();
            LogAssert.NoUnexpectedReceived();
            renderer.Dispose();
            ScriptableObject.DestroyImmediate(asset);
        }
        // Makes sure the render pipeline is restored in case of a NullReference exception.
        finally
        {
            GraphicsSettings.renderPipelineAsset = renderPipelineAsset;
        }
    }

    // Validate that resource Guids are valid
    [Test]
    public void ValidateBuiltinResourceFiles()
    {
        string templatePath = AssetDatabase.GUIDToAssetPath(ResourceGuid.rendererTemplate);
        Assert.IsFalse(string.IsNullOrEmpty(templatePath));

        string editorResourcesPath = AssetDatabase.GUIDToAssetPath(UniversalRenderPipelineAsset.editorResourcesGUID);
        Assert.IsFalse(string.IsNullOrEmpty(editorResourcesPath));
    }

    // Validate that ShaderUtils.GetShaderGUID results are valid and that ShaderUtils.GetShaderPath match shader names.
    [TestCase(ShaderPathID.Lit)]
    [TestCase(ShaderPathID.SimpleLit)]
    [TestCase(ShaderPathID.Unlit)]
    [TestCase(ShaderPathID.TerrainLit)]
    [TestCase(ShaderPathID.ParticlesLit)]
    [TestCase(ShaderPathID.ParticlesSimpleLit)]
    [TestCase(ShaderPathID.ParticlesUnlit)]
    [TestCase(ShaderPathID.BakedLit)]
    [TestCase(ShaderPathID.SpeedTree7)]
    [TestCase(ShaderPathID.SpeedTree7Billboard)]
    [TestCase(ShaderPathID.SpeedTree8)]
    public void ValidateShaderResources(ShaderPathID shaderPathID)
    {
        string path = AssetDatabase.GUIDToAssetPath(ShaderUtils.GetShaderGUID(shaderPathID));
        Assert.IsFalse(string.IsNullOrEmpty(path));

        var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
        Assert.AreEqual(shader.name, ShaderUtils.GetShaderPath(shaderPathID));

        var propertyNames = new System.Collections.Generic.HashSet<string>();
        for (int j = 0; j < shader.GetPropertyCount(); ++j)
        {
            string propertyName = shader.GetPropertyName(j);
            Assert.IsFalse(propertyNames.Contains(propertyName), $"{shader.name} has duplicated property {propertyName}!");
            propertyNames.Add(propertyName);
        }
    }

    // When creating URP all required resources should be initialized.
    [Test]
    public void ValidateNewAssetResources()
    {
        UniversalRendererData data = ScriptableObject.CreateInstance<UniversalRendererData>();
        UniversalRenderPipelineAsset asset = UniversalRenderPipelineAsset.Create(data);
        Assert.AreNotEqual(null, asset.defaultMaterial);
        Assert.AreNotEqual(null, asset.defaultParticleMaterial);
        Assert.AreNotEqual(null, asset.defaultLineMaterial);
        Assert.AreNotEqual(null, asset.defaultTerrainMaterial);
        Assert.AreNotEqual(null, asset.defaultShader);

        // URP doesn't override the following materials
        Assert.AreEqual(null, asset.defaultUIMaterial);
        Assert.AreEqual(null, asset.defaultUIOverdrawMaterial);
        Assert.AreEqual(null, asset.defaultUIETC1SupportedMaterial);
        Assert.AreEqual(null, asset.default2DMaterial);

        Assert.AreNotEqual(null, asset.m_EditorResourcesAsset, "Editor Resources should be initialized when creating a new pipeline.");
        Assert.AreNotEqual(null, asset.m_RendererDataList, "A default renderer data should be created when creating a new pipeline.");
        ScriptableObject.DestroyImmediate(asset);
        ScriptableObject.DestroyImmediate(data);
    }

    // When changing URP settings, all settings should be valid.
    [Test]
    public void ValidateAssetSettings()
    {
        // Create a new asset and validate invalid settings
        UniversalRendererData data = ScriptableObject.CreateInstance<UniversalRendererData>();
        UniversalRenderPipelineAsset asset = UniversalRenderPipelineAsset.Create(data);
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
        ScriptableObject.DestroyImmediate(data);
    }

    // When working with SpeedTree v7 assets, UniversalSpeedTree8Upgrader should not throw exception
    [Test]
    public void UniversalSpeedTree8Upgrader_ShouldntThrowExceptionWhenImportingSpeedTree7Assets()
    {
        const string STv7AssetName = "EuropeanBeech_Desktop.spm";
        const string STv7AssetPath = "Assets/CommonAssets/SpeedTree/SpeedTreeV7/EU_Beech/" + STv7AssetName;

        // Ensure this is not thrown:
        // NullReferenceException: Object reference not set to an instance of an object
        //  UnityEditor.Rendering.SpeedTree8MaterialUpgrader.GetWindQuality
        //  UnityEditor.Rendering.SpeedTree8MaterialUpgrader.UpgradeWindQuality
        //  UnityEditor.Rendering.SpeedTree8MaterialUpgrader.SpeedTree8MaterialFinalizer
        //  UnityEditor.Rendering.Universal.UniversalSpeedTree8Upgrader.UniversalSpeedTree8MaterialFinalizer 
        Assert.DoesNotThrow(() => AssetDatabase.ImportAsset(STv7AssetPath));
    }
}
