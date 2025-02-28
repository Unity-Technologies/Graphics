using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools.Graphics;

namespace Unity.Rendering.Universal.Tests
{
    [Flags]
    public enum GpuResidentDrawerContext
    {
        None = 0,
        GpuResidentDrawerDisabled = 1,
        GpuResidentDrawerInstancedDrawing = 2,
    }

    public class GpuResidentDrawerGlobalContext : IGlobalContextProvider
    {
        public int Context =>
            IsGpuResidentDrawerActive()
                ? (int)GpuResidentDrawerContext.GpuResidentDrawerInstancedDrawing
                : (int)GpuResidentDrawerContext.GpuResidentDrawerDisabled;

        static bool IsGpuResidentDrawerActive()
        {
            var renderPipelineAsset = QualitySettings.renderPipeline;
            if (renderPipelineAsset is IGPUResidentRenderPipeline mbAsset)
                return mbAsset.gpuResidentDrawerMode != GPUResidentDrawerMode.Disabled;

            return false;
        }

        public void ActivateContext(GpuResidentDrawerContext context)
        {
            if (context == GpuResidentDrawerContext.None)
                return;

            var renderPipelineAsset = QualitySettings.renderPipeline;
            if (renderPipelineAsset is IGPUResidentRenderPipeline mbAsset)
            {
                mbAsset.gpuResidentDrawerMode =
                    context == GpuResidentDrawerContext.GpuResidentDrawerDisabled
                        ? GPUResidentDrawerMode.Disabled
                        : GPUResidentDrawerMode.InstancedDrawing;
            }
        }
    }
}
