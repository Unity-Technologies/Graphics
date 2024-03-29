using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Profiling;
using UnityEngine.Rendering.Universal;
using UnityEditor.Rendering.Universal.Internal;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.Universal;
using UnityEngine.TestTools;

class EditorTests_URPDisabled
{
    private RenderPipelineAsset m_PreviousRenderPipelineAssetGraphicsSettings;
    private RenderPipelineAsset m_PreviousRenderPipelineAssetQualitySettings;

    [SetUp]
    public void Setup()
    {
        m_PreviousRenderPipelineAssetGraphicsSettings = GraphicsSettings.defaultRenderPipeline;
        m_PreviousRenderPipelineAssetQualitySettings = QualitySettings.renderPipeline;

        GraphicsSettings.defaultRenderPipeline = null;
        QualitySettings.renderPipeline = null;
    }

    [TearDown]
    public void TearDown()
    {
        GraphicsSettings.defaultRenderPipeline = m_PreviousRenderPipelineAssetGraphicsSettings;
        QualitySettings.renderPipeline = m_PreviousRenderPipelineAssetQualitySettings;
    }

    // When creating a new render pipeline asset it should not log any errors or throw exceptions.
    [Test]
    public void CreatePipelineAssetWithoutErrors()
    {
        UniversalRendererData data = ScriptableObject.CreateInstance<UniversalRendererData>();
        UniversalRenderPipelineAsset asset = UniversalRenderPipelineAsset.Create(data);
        LogAssert.NoUnexpectedReceived();
        ScriptableObject.DestroyImmediate(asset);
        ScriptableObject.DestroyImmediate(data);
    }

    // When creating a new Universal Renderer asset it should not log any errors or throw exceptions.
    [Test]
    public void CreateUniversalRendererAssetWithoutErrors()
    {
        var asset = ScriptableObject.CreateInstance<UniversalRendererData>();
        ResourceReloader.ReloadAllNullIn(asset, UniversalRenderPipelineAsset.packagePath);
        var renderer = asset.InternalCreateRenderer();
        LogAssert.NoUnexpectedReceived();
        renderer.Dispose();
        ScriptableObject.DestroyImmediate(asset);
    }

    // When creating a new renderer 2d asset it should not log any errors or throw exceptions.
    [Test]
    public void CreateRenderer2DAssetWithoutErrors()
    {
        var asset = ScriptableObject.CreateInstance<Renderer2DData>();
        ResourceReloader.ReloadAllNullIn(asset, UniversalRenderPipelineAsset.packagePath);
        var renderer = asset.InternalCreateRenderer();
        LogAssert.NoUnexpectedReceived();
        renderer.Dispose();
        ScriptableObject.DestroyImmediate(asset);
    }
}
