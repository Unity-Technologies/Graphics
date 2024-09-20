using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering.Universal;

[TestFixture]
class RenderingLayersTests
{
    string[] m_DefinedLayers;
    int[] m_DefinedValues;
    int m_LayersSize;

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        m_DefinedLayers = RenderingLayerMask.GetDefinedRenderingLayerNames();
        m_DefinedValues = RenderingLayerMask.GetDefinedRenderingLayerValues();
        m_LayersSize = RenderingLayerMask.GetRenderingLayerCount();
    }

    [OneTimeTearDown]
    public void OneTimeTeardown()
    {
        var diff = RenderingLayerMask.GetRenderingLayerCount() - m_LayersSize;
        if (diff > 0)
            for (int i = 0; i < diff; i++)
                RenderPipelineEditorUtility.TryRemoveLastRenderingLayerName();
        else
            for (int i = 0; i < -diff; i++)
                RenderPipelineEditorUtility.TryAddRenderingLayerName(string.Empty);

        for (int i = 1; i < RenderingLayerMask.GetRenderingLayerCount(); i++)
            RenderPipelineEditorUtility.TrySetRenderingLayerName(i, string.Empty);

        for (int i = 0; i < m_DefinedValues.Length; i++)
        {
            var value = m_DefinedValues[i];
            if (RenderingLayerMask.defaultRenderingLayerMask == value)
                continue;
            var name = m_DefinedLayers[i];
            var index = Mathf.FloorToInt(Mathf.Log(value, 2));
            RenderPipelineEditorUtility.TrySetRenderingLayerName(index, name);
        }
    }

    [SetUp]
    public void Setup()
    {
        var layerCount = RenderingLayerMask.GetRenderingLayerCount() - 1;
        for (int i = 0; i < layerCount; i++)
            RenderPipelineEditorUtility.TryRemoveLastRenderingLayerName();
    }

    static TestCaseData[] s_MaskSizeTestCases =
    {
        new TestCaseData(7)
            .SetName("Given a Rendering Layers size of 7, the mask size is 8 bits")
            .Returns((int)RenderingLayerUtils.MaskSize.Bits8),
        new TestCaseData(8)
            .SetName("Given a Rendering Layers size of 8, the mask size is 8 bits")
            .Returns((int)RenderingLayerUtils.MaskSize.Bits8),
        new TestCaseData(9)
            .SetName("Given a Rendering Layers size of 9, the mask size is 16 bits")
            .Returns((int)RenderingLayerUtils.MaskSize.Bits16),
        new TestCaseData(16)
            .SetName("Given a Rendering Layers size of 16, the mask size is 16 bits")
            .Returns((int)RenderingLayerUtils.MaskSize.Bits16),
        new TestCaseData(17)
            .SetName("Given a Rendering Layers size of 17, the mask size is 24 bits")
            .Returns((int)RenderingLayerUtils.MaskSize.Bits24),
        new TestCaseData(24)
            .SetName("Given a Rendering Layers size of 24, the mask size is 24 bits")
            .Returns((int)RenderingLayerUtils.MaskSize.Bits24),
        new TestCaseData(25)
            .SetName("Given a Rendering Layers size of 25, the mask size is 32 bits")
            .Returns((int)RenderingLayerUtils.MaskSize.Bits32),
        new TestCaseData(32)
            .SetName("Given a Rendering Layers size of 32, the mask size is 32 bits")
            .Returns((int)RenderingLayerUtils.MaskSize.Bits32)
    };

    [Test, TestCaseSource(nameof(s_MaskSizeTestCases))]
    public int MaskSizeTest(int tagManagerLayerCount)
    {
        var currentLayerCount = RenderingLayerMask.GetRenderingLayerCount();
        var requiredLayers = tagManagerLayerCount - currentLayerCount;
        if (requiredLayers > 0)
            for (int i = 0; i < requiredLayers; i++)
                RenderPipelineEditorUtility.TryAddRenderingLayerName($"Layer {currentLayerCount + i}");

        var urpRenderer = ScriptableObject.CreateInstance<UniversalRendererData>().InternalCreateRenderer() as UniversalRenderer;
        RenderingLayerUtils.RequireRenderingLayers(urpRenderer, new List<ScriptableRendererFeature>(), 0, out var evt, out var maskSize);
        urpRenderer.Dispose();
        return (int)maskSize;
    }
}
