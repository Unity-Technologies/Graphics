using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering
{
    // TODO: Move the shading atlas resources to be render-graph managed.
    public partial class LineRendering
    {
        // Internal graphics resource for the shading atlas, do not try to access this directly.
        private readonly RTHandle[] m_ShadingAtlasRT = { null, null };

        // Table of <Line Renderer Instance ID, Allocation> pairs
        private static readonly Dictionary<int, ShadingAtlasAllocation> s_ShadingAtlasAllocations = new();

        // Counter for verifying atlas validity.
        private int m_ShadingAtlasUpdateCount = 0;

        internal struct ShadingAtlas
        {
            public RTHandle previous;
            public RTHandle current;
            public int      reserved;
            public bool     valid;
        }

        internal struct ShadingAtlasAllocation
        {
            public int updateCount;
            public int currentOffset;
            public int currentSize;
            public int previousOffset;
            public int previousSize;
        }

        void CleanupShadingAtlas()
        {
            RTHandles.Release(m_ShadingAtlasRT[0]);
            RTHandles.Release(m_ShadingAtlasRT[1]);
        }

        // Utility for obtaining the shading atlas allocation for a given line renderer.
        static ShadingAtlasAllocation GetShadingAtlasAllocationForRenderer(RendererData renderer) => s_ShadingAtlasAllocations[renderer.hash];

        // Computes allocations for a list of line renderers in the shading atlas.
        void ComputeShadingAtlasAllocations(RendererData[] renderers, ref ShadingAtlas atlas)
        {
            int offset = atlas.reserved;

            foreach (var renderer in renderers)
            {
                if (!s_ShadingAtlasAllocations.TryGetValue(renderer.hash, out var allocation))
                {
                    allocation = new ShadingAtlasAllocation();
                }

                allocation.previousOffset = allocation.currentOffset;
                allocation.previousSize   = allocation.currentSize;

                // Only reserve space from history if it's really needed, otherwise reset allocation
                if (renderer.shadingFraction < 1)
                {
                    var shadingSample = renderer.mesh.vertexCount;
                    {
                        allocation.currentOffset = offset;
                        allocation.currentSize   = shadingSample;
                        allocation.updateCount   = atlas.valid ? allocation.updateCount + 1 : 0;
                    }

                    offset += shadingSample;
                }
                else
                {
                    allocation.currentOffset = -1;
                    allocation.currentSize   = -1;
                    allocation.updateCount   =  0;
                }

                s_ShadingAtlasAllocations[renderer.hash] = allocation;
            }

            atlas.reserved = offset;
        }

        internal ShadingAtlas GetShadingAtlas(RenderGraph renderGraph, Camera camera)
        {
            // Calculate the number of shading samples required for history next frame.
            int samples = 0;

            if (HasRenderDatas())
            {
                // TODO: Unfortunately this currently will be called twice in one frame.
                var renderDatas = GetValidRenderDatas(renderGraph, camera);

                if (renderDatas.Length > 0)
                {
                    foreach (var data in renderDatas)
                    {
                        // Only make allocations for renderers that need it.
                        if (data.shadingFraction < 1)
                            samples += data.mesh.vertexCount;
                    }
                }
            }

            CoreUtils.Swap(ref m_ShadingAtlasRT[0], ref m_ShadingAtlasRT[1]);

            var historyCurRT = m_ShadingAtlasRT[0];

            if(samples > 0)
            {
                // TODO: Release memory if required memory is considerably less than what we now have reserved?
                if (historyCurRT == null || historyCurRT.rt == null || historyCurRT.rt.width * historyCurRT.rt.height < samples)
                {
                    if (historyCurRT != null)
                        RTHandles.Release(historyCurRT);

                    int w = Math.Max(Mathf.NextPowerOfTwo(Mathf.CeilToInt(Mathf.Sqrt(samples))), 1);
                    int h = Mathf.NextPowerOfTwo(Mathf.CeilToInt(DivRoundUp(samples, w)));

                    historyCurRT = RTHandles.Alloc(w, h, colorFormat: GraphicsFormat.R32G32B32A32_SFloat, enableRandomWrite: true);
                }
            }
            else
            {
                if (historyCurRT != null)
                    RTHandles.Release(historyCurRT);

                historyCurRT = null;
            }

            m_ShadingAtlasRT[0] = historyCurRT;
            var historyPrevRT = m_ShadingAtlasRT[1];

            bool historyExists = historyPrevRT != null && historyPrevRT.rt != null && historyPrevRT.rt.IsCreated();

            if (historyExists)
                ++m_ShadingAtlasUpdateCount;
            else
                m_ShadingAtlasUpdateCount = 0;

            return new ShadingAtlas()
            {
                current  = historyCurRT,
                previous = historyPrevRT,
                reserved     = 0,
                valid    = m_ShadingAtlasUpdateCount > 0
            };
        }
    }
}
