using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.TestTools.Graphics;
using UnityEngine.TestTools.Graphics.Contexts;

class GpuResidentDrawerContextTests
{
    static readonly GpuResidentDrawerContext[] k_Contexts =
    {
        GpuResidentDrawerContext.GRDDisabled,
        GpuResidentDrawerContext.GRDEnabled,
    };

    [Test]
    public void ActivateAppliesModeToEveryQualityLevelAsset([ValueSource(nameof(k_Contexts))] GpuResidentDrawerContext context)
    {
        var globalContext = GlobalContextManager.Get<GpuResidentDrawerGlobalContext>();
        var previous = globalContext.Current;
        var expected = context == GpuResidentDrawerContext.GRDEnabled
            ? GPUResidentDrawerMode.InstancedDrawing
            : GPUResidentDrawerMode.Disabled;

        try
        {
            globalContext.Activate(context);

            var mismatches = new List<string>();
            if (GraphicsSettings.defaultRenderPipeline is IGPUResidentRenderPipeline defaultAsset
                && defaultAsset.gpuResidentDrawerMode != expected)
                mismatches.Add($"default: {defaultAsset.gpuResidentDrawerMode}");
            for (int i = 0; i < QualitySettings.count; i++)
            {
                if (QualitySettings.GetRenderPipelineAssetAt(i) is IGPUResidentRenderPipeline asset
                    && asset.gpuResidentDrawerMode != expected)
                    mismatches.Add($"{QualitySettings.names[i]}: {asset.gpuResidentDrawerMode}");
            }

            Assert.That(mismatches, Is.Empty, $"Render pipeline assets whose mode is not {expected}");
        }
        finally
        {
            globalContext.Activate(previous);
        }
    }

    [Test]
    public void RestoreDrawerModesReturnsEachAssetToItsOriginalMode()
    {
        var globalContext = GlobalContextManager.Get<GpuResidentDrawerGlobalContext>();
        var original = globalContext.CaptureDrawerModes();
        var currentAsset = GraphicsSettings.currentRenderPipeline as IGPUResidentRenderPipeline;
        var otherAsset = original.Keys.First(asset => !ReferenceEquals(asset, currentAsset));

        try
        {
            otherAsset.gpuResidentDrawerMode = GPUResidentDrawerMode.InstancedDrawing;
            var mixed = globalContext.CaptureDrawerModes();

            globalContext.Activate(GpuResidentDrawerContext.GRDDisabled);
            globalContext.RestoreDrawerModes(mixed);

            var mismatches = mixed
                .Where(pair => pair.Key.gpuResidentDrawerMode != pair.Value)
                .Select(pair => $"{pair.Key}: {pair.Key.gpuResidentDrawerMode}, expected {pair.Value}")
                .ToList();
            Assert.That(mismatches, Is.Empty, "Render pipeline assets not returned to their captured mode");
            Assert.That(otherAsset.gpuResidentDrawerMode, Is.EqualTo(GPUResidentDrawerMode.InstancedDrawing));
        }
        finally
        {
            globalContext.RestoreDrawerModes(original);
        }
    }
}
