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
        public int visibleInstances;
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
