using System.Diagnostics;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Rendering.RenderGraphModule
{
    /// <summary>
    /// RayTracingAccelerationStructure resource handle.
    /// </summary>
    [DebuggerDisplay("RayTracingAccelerationStructure ({handle.index})")]
    [MovedFrom(true, "UnityEngine.Experimental.Rendering.RenderGraphModule", "UnityEngine.Rendering.RenderGraphModule")]
    public struct RayTracingAccelerationStructureHandle
    {
        private static RayTracingAccelerationStructureHandle s_NullHandle = new RayTracingAccelerationStructureHandle();

        /// <summary>
        /// Returns a null ray tracing acceleration structure handle
        /// </summary>
        /// <value>A null ray tracing acceleration structure handle.</value>
        public static RayTracingAccelerationStructureHandle nullHandle { get { return s_NullHandle; } }

        internal ResourceHandle handle;

        internal RayTracingAccelerationStructureHandle(int handle) { this.handle = new ResourceHandle(handle, RenderGraphResourceType.AccelerationStructure, false); }

        /// <summary>
        /// Cast to RayTracingAccelerationStructure
        /// </summary>
        /// <param name="handle">Input RayTracingAccelerationStructureHandle</param>
        /// <returns>Resource as a RayTracingAccelerationStructure.</returns>
        public static implicit operator RayTracingAccelerationStructure(RayTracingAccelerationStructureHandle handle) => handle.IsValid() ? RenderGraphResourceRegistry.current.GetRayTracingAccelerationStructure(handle) : null;

        /// <summary>
        /// Return true if the handle is valid.
        /// </summary>
        /// <returns>True if the handle is valid.</returns>
        public bool IsValid() => handle.IsValid();
    }


    /// <summary>
    /// Descriptor used to identify ray tracing acceleration structure resources
    /// </summary>
    [MovedFrom(true, "UnityEngine.Experimental.Rendering.RenderGraphModule", "UnityEngine.Rendering.RenderGraphModule")]
    public struct RayTracingAccelerationStructureDesc
    {
        /// <summary>RayTracingAccelerationStructure name.</summary>
        public string name;
    }

    [DebuggerDisplay("RayTracingAccelerationStructureResource ({desc.name})")]
    class RayTracingAccelerationStructureResource : RenderGraphResource<RayTracingAccelerationStructureDesc, RayTracingAccelerationStructure>
    {
        public override string GetName()
        {
            return desc.name;
        }
    }
}
