using System.Collections.Generic;
using UnityEngine.PathTracing.Core;
using UnityEngine.LightTransport;
using UnityEngine.Rendering;
using UnityEngine.Rendering.UnifiedRayTracing;

namespace UnityEngine.PathTracing.Integration
{
    using LightHandle = Handle<World.LightDescriptor>;

    internal class UnityComputeWorld : IWorld
    {
        internal World PathTracingWorld;
        internal GraphicsBuffer ScratchBuffer;
        internal Mesh[] Meshes;
        internal LightHandle[] LightHandles;
        internal RayTracingContext RayTracingContext;
        internal readonly List<Object> TemporaryObjects = new();

        internal const uint RenderingObjectLayer = 1 << 0;

        internal void BuildAccelerationStructure(CommandBuffer cmd)
        {
            PathTracingWorld.GetAccelerationStructure().Build(cmd, ref ScratchBuffer);
        }

        public void Init(RayTracingContext rayTracingContext, WorldResourceSet worldResources)
        {
            PathTracingWorld = new World();
            PathTracingWorld.Init(rayTracingContext, worldResources);
            RayTracingContext = rayTracingContext;
        }

        public void Dispose()
        {
            PathTracingWorld?.Dispose();
            ScratchBuffer?.Dispose();

            foreach (var obj in TemporaryObjects)
                CoreUtils.Destroy(obj);
        }
    }
}
