using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine.Assertions;

namespace UnityEngine.Rendering
{
    internal struct InstanceCullerViewStats
    {
        public BatchCullingViewType viewType;
        public int viewInstanceID;
        public int splitIndex;
        public int visibleInstancesOnCPU;
        public int visibleInstancesOnGPU;
        public int visiblePrimitivesOnCPU;
        public int visiblePrimitivesOnGPU;
        public int drawCommands;
    }

    internal enum InstanceOcclusionEventType
    {
        OcclusionTest,
        OccluderUpdate,
    }

    internal struct InstanceOcclusionEventStats
    {
        public int viewInstanceID;
        public InstanceOcclusionEventType eventType;
        public int occluderVersion;
        public int subviewMask;
        public OcclusionTest occlusionTest;
        public int visibleInstances;
        public int culledInstances;
        public int visiblePrimitives;
        public int culledPrimitives;
    }

    internal struct DebugOccluderStats
    {
        public int viewInstanceID;
        public int subviewCount;
        public Vector2Int occluderMipLayoutSize;
    }

    internal class DebugRendererBatcherStats : IDisposable
    {
        public bool enabled;
        public NativeList<InstanceCullerViewStats> instanceCullerStats;
        public NativeList<InstanceOcclusionEventStats> instanceOcclusionEventStats;
        public NativeList<DebugOccluderStats> occluderStats;
        public bool occlusionOverlayEnabled;
        public bool occlusionOverlayCountVisible;
        public bool overrideOcclusionTestToAlwaysPass;

        public DebugRendererBatcherStats()
        {
            instanceCullerStats = new NativeList<InstanceCullerViewStats>(Allocator.Persistent);
            instanceOcclusionEventStats = new NativeList<InstanceOcclusionEventStats>(Allocator.Persistent);
            occluderStats = new NativeList<DebugOccluderStats>(Allocator.Persistent);
        }

        public void FinalizeInstanceCullerViewStats()
        {
            // For each view, update the on GPU instance and primitive counts. The final rendered primitive and
            // instance count can be found at the last pass of all the occlusion passes.
            for (int viewIndex = 0; viewIndex < instanceCullerStats.Length; viewIndex++)
            {
                InstanceCullerViewStats cullerStats = instanceCullerStats[viewIndex];
                InstanceOcclusionEventStats lastOcclusionEventStats = GetLastInstanceOcclusionEventStatsForView(viewIndex);

                if (lastOcclusionEventStats.viewInstanceID == cullerStats.viewInstanceID)
                {
                    // The Min test is because the SelectionOutline view (and probably picking as well) share the same viewInstanceID with
                    // the scene camera for instance, so we pick up the camera's occlusion event. And we can't have more instances on GPU than we had on CPU.
                    cullerStats.visibleInstancesOnGPU = Math.Min(lastOcclusionEventStats.visibleInstances, cullerStats.visibleInstancesOnCPU);
                    cullerStats.visiblePrimitivesOnGPU = Math.Min(lastOcclusionEventStats.visiblePrimitives, cullerStats.visiblePrimitivesOnCPU);
                }
                else
                {
                    // There was no occlusion culling for this view, so reuse the same counts as on the CPU.
                    cullerStats.visibleInstancesOnGPU = cullerStats.visibleInstancesOnCPU;
                    cullerStats.visiblePrimitivesOnGPU = cullerStats.visiblePrimitivesOnCPU;
                }

                instanceCullerStats[viewIndex] = cullerStats;
            }
        }

        private InstanceOcclusionEventStats GetLastInstanceOcclusionEventStatsForView(int viewIndex)
        {
            if (viewIndex < instanceCullerStats.Length)
            {
                int viewInstanceID = instanceCullerStats[viewIndex].viewInstanceID;
                for (int passIndex = instanceOcclusionEventStats.Length - 1; passIndex >= 0; passIndex--)
                {
                    if (instanceOcclusionEventStats[passIndex].viewInstanceID == viewInstanceID)
                        return instanceOcclusionEventStats[passIndex];
                }
            }

            return new InstanceOcclusionEventStats();
        }

        public void Dispose()
        {
            if (instanceCullerStats.IsCreated)
                instanceCullerStats.Dispose();
            if (instanceOcclusionEventStats.IsCreated)
                instanceOcclusionEventStats.Dispose();
            if (occluderStats.IsCreated)
                occluderStats.Dispose();
        }
    }

    internal struct OcclusionCullingDebugOutput
    {
        public RTHandle occluderDepthPyramid;
        public GraphicsBuffer occlusionDebugOverlay;
        public OcclusionCullingDebugShaderVariables cb;
    }
}
