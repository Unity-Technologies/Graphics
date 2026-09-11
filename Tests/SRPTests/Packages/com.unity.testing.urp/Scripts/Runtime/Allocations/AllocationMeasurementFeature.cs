using System;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal.Tests.Allocations
{
    /// <summary>
    /// Carries a measurement into a real URP frame.
    ///
    /// Custom passes are recorded with the renderer's own <c>ContextContainer</c> and render graph, so
    /// a pass invoked from here sees the camera data, resource handles, light data and culling results
    /// that URP built for the frame. That is the point: a test does not have to construct any of it,
    /// and cannot get it subtly wrong.
    /// </summary>
    sealed class AllocationMeasurementFeature : ScriptableRendererFeature
    {
        MeasurementPass m_Pass;

        /// <summary>Invoked once per frame, inside the frame, with the renderer's live state.</summary>
        internal Action<RenderGraph, ContextContainer> measuredWork;

        /// <summary>Where in the frame the measurement runs.</summary>
        internal RenderPassEvent injectionPoint = RenderPassEvent.BeforeRenderingPostProcessing;

        public override void Create()
        {
            m_Pass = new MeasurementPass(this);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (measuredWork == null)
                return;

            m_Pass ??= new MeasurementPass(this);
            m_Pass.renderPassEvent = injectionPoint;
            renderer.EnqueuePass(m_Pass);
        }

        sealed class MeasurementPass : ScriptableRenderPass
        {
            readonly AllocationMeasurementFeature m_Feature;

            internal MeasurementPass(AllocationMeasurementFeature feature)
            {
                m_Feature = feature;
                renderPassEvent = feature.injectionPoint;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                m_Feature.measuredWork?.Invoke(renderGraph, frameData);
            }
        }
    }
}
