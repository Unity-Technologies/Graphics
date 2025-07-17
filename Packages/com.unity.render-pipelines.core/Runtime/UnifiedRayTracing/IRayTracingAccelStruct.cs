using System;

namespace UnityEngine.Rendering.UnifiedRayTracing
{
    /// <summary>
    /// Parameters used to configure the creation of instances that are part of a <see cref="IRayTracingAccelStruct"/>.
    /// </summary>
    public struct MeshInstanceDesc
    {
        /// <summary>
        /// The Mesh used to build this instance's geometry.
        /// </summary>
        public Mesh mesh;

        /// <summary>
        /// The index of the sub-mesh (MeshInstanceDesc references a single sub-mesh).
        /// </summary>
        public int subMeshIndex;

        /// <summary>
        /// The transformation matrix of the instance.
        /// </summary>
        public Matrix4x4 localToWorldMatrix;

        /// <summary>
        /// The instance mask.
        /// </summary>
        /// <remarks>
        /// Instances in the acceleration structure contain an 8-bit user defined instance mask.
        /// The TraceRayClosestHit/TraceRayAnyHit HLSL functions have an 8-bit input parameter, InstanceInclusionMask which gets ANDed with the instance mask from
        /// any instance that is a candidate for intersection during acceleration structure traversal on the GPU.
        /// If the result of the AND operation is zero, the GPU ignores the intersection.
        /// </remarks>
        public uint mask;

        /// <summary>
        /// Instance identifier. Can be accessed in the HLSL via the instanceID member of Hit (Hit is returned by the TraceRayClosestHit/TraceRayAnyHit HLSL functions).
        /// </summary>
        public uint instanceID;

        /// <summary>
        /// Whether front/back face culling for this ray tracing instance is enabled. Default value: true.
        /// </summary>
        public bool enableTriangleCulling;

        /// <summary>
        /// Whether to flip the way triangles face in this ray tracing instance. Default value: false.
        /// </summary>
        public bool frontTriangleCounterClockwise;

        /// <summary>
        /// Whether the geometry is considered opaque. Default value: true.
        /// </summary>
        /// <remarks>
        /// When an instance's opaqueGeometry field is set to false, the AnyHitExecute shader function will be invoked during the ray traversal when a hit is found.
        /// This alows the user to programmatically decide whether to reject or accept the candidate hit. This feature can, for example, be used to implement alpha cutout transparency.
        /// For best performance, prefer to set this parameter to false for as many geometries as possibe.
        /// </remarks>
        public bool opaqueGeometry;

        /// <summary>
        /// Creates a MeshInstanceDesc.
        /// </summary>
        /// <param name="mesh">The Mesh used to build this instance's geometry.</param>
        /// <param name="subMeshIndex">The index of the sub-mesh (MeshInstanceDesc references a single sub-mesh).</param>
        public MeshInstanceDesc(Mesh mesh, int subMeshIndex = 0)
        {
            this.mesh = mesh;
            this.subMeshIndex = subMeshIndex;
            localToWorldMatrix = Matrix4x4.identity;
            mask = 0xFFFFFFFF;
            instanceID = 0xFFFFFFFF;
            enableTriangleCulling = true;
            frontTriangleCounterClockwise = false;
            opaqueGeometry = true;
        }
    }

    /// <summary>
    /// A data structure used to represent a collection of instances and geometries that are used for GPU ray tracing.
    /// It can be created by calling <see cref="RayTracingContext.CreateAccelerationStructure"/>.
    /// </summary>
    public interface IRayTracingAccelStruct : IDisposable
    {
        /// <summary>
        /// Adds an instance to the RayTracingAccelerationStructure.
        /// </summary>
        /// <param name="meshInstance">The parameters describing this instance.</param>
        /// <returns>A value representing a handle that you can use to perform later actions (e.g. RemoveInstance...)</returns>
        /// <exception cref="UnifiedRayTracingException">Thrown if the instance cannot be added. Inspect <see cref="UnifiedRayTracingException.errorCode"/> for the reason.</exception>
        int AddInstance(MeshInstanceDesc meshInstance);

        /// <summary>
        /// Removes an instance.
        /// </summary>
        /// <param name="instanceHandle">The handle associated with an instance.</param>
        void RemoveInstance(int instanceHandle);

        /// <summary>
        /// Removes all ray tracing instances from the acceleration structure.
        /// </summary>
        void ClearInstances();

        /// <summary>
        /// Updates the transformation of an instance.
        /// </summary>
        /// <param name="instanceHandle">The handle associated with an instance.</param>
        /// <param name="localToWorldMatrix">The new transformation matrix of the instance.</param>
        void UpdateInstanceTransform(int instanceHandle, Matrix4x4 localToWorldMatrix);

        /// <summary>
        /// Updates the instance ID of an instance.
        /// </summary>
        /// <param name="instanceHandle">The handle associated with an instance.</param>
        /// <param name="instanceID">The new instance ID.</param>
        void UpdateInstanceID(int instanceHandle, uint instanceID);

        /// <summary>
        /// Updates the instance mask of an instance.
        /// </summary>
        /// <remarks>
        /// Ray tracing instances in the acceleration structure contain an 8-bit user defined instance mask.
        /// The TraceRay() HLSL function has an 8-bit input parameter, InstanceInclusionMask which gets ANDed with the instance mask from
        /// any ray tracing instance that is a candidate for intersection during acceleration structure traversal on the GPU.
        /// If the result of the AND operation is zero, the GPU ignores the intersection.
        /// </remarks>
        /// <param name="instanceHandle">The handle associated with an instance.</param>
        /// <param name="mask">	The new mask.</param>
        void UpdateInstanceMask(int instanceHandle, uint mask);

        /// <summary>
        /// Adds a command in cmd to build this acceleration structure on the GPU.
        /// </summary>
        /// <remarks>
        /// Depending on the backend, the GPU build algorithm can require additional GPU storage that is supplied through the scratchBuffer parameter.
        /// Its required size can be queried by calling <see cref="GetBuildScratchBufferRequiredSizeInBytes"/>.
        /// </remarks>
        /// <param name="cmd">CommandBuffer to register the build command to.</param>
        /// <param name="scratchBuffer">Temporary buffer used during the build.</param>
        /// <exception cref="UnifiedRayTracingException">Thrown if the build failed. Inspect <see cref="UnifiedRayTracingException.errorCode"/> for the reason.</exception>
        void Build(CommandBuffer cmd, GraphicsBuffer scratchBuffer);

        /// <summary>
        /// Returns the minimum buffer size that is required by the scratchBuffer parameter of <see cref="Build"/>.
        /// </summary>
        /// <returns>The minimum size in bytes.</returns>
        ulong GetBuildScratchBufferRequiredSizeInBytes();
    }
}

