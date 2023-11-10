using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine.Assertions;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Context struct for passing into GPUResidentRenderPipeline.PostCullBeginCameraRendering
    /// </summary>
    public struct RenderRequestBatcherContext
    {
        /// <summary>
        /// CommandBuffer that will be used for resulting commands
        /// </summary>
        public CommandBuffer commandBuffer;

        /// <summary>
        /// Ambient probe to be set
        /// </summary>
        public SphericalHarmonicsL2 ambientProbe;
    }

    /// <summary>
    /// Enum for split view types
    /// </summary>
    internal enum SplitViewType
    {
        Camera,
        Shadow,
    }

    /// <summary>
    /// Identifies a split within a view
    /// </summary>
    internal struct SplitID : IEquatable<SplitID>
    {
        public SplitViewType viewType;
        public int viewID;
        public int splitIndex;

        public bool isCamera { get { return viewType == SplitViewType.Camera; } }

        public static SplitID FromCamera(Camera camera)
        {
            return new SplitID { viewType = SplitViewType.Camera, viewID = camera.GetInstanceID() };
        }
        public static SplitID FromCamera(int cameraInstanceID)
        {
            return new SplitID { viewType = SplitViewType.Camera, viewID = cameraInstanceID };
        }

        public static SplitID FromShadow(int lightInstanceID, int splitIndex = 0)
        {
            return new SplitID { viewType = SplitViewType.Shadow, viewID = lightInstanceID, splitIndex = splitIndex };
        }

        public bool Equals(SplitID other)
        {
            return viewType == other.viewType && viewID == other.viewID && splitIndex == other.splitIndex;
        }

        public override int GetHashCode()
        {
            int hash = 13;
            hash = (hash * 23) + (int)viewType;
            hash = (hash * 23) + (int)viewID;
            hash = (hash * 23) + (int)splitIndex;
            return hash;
        }
    }

    internal struct InstanceCullerViewStats
    {
        public SplitID splitID;
        public int visibleInstances;
        public int drawCommands;
    }

    internal class DebugRendererBatcherStats : IDisposable
    {
        public bool enabled;
        public NativeList<InstanceCullerViewStats> instanceCullerStats;

        public DebugRendererBatcherStats()
        {
            instanceCullerStats = new NativeList<InstanceCullerViewStats>(Allocator.Persistent);
        }

        public void Dispose()
        {
            if (instanceCullerStats.IsCreated)
                instanceCullerStats.Dispose();
        }
    }
}
