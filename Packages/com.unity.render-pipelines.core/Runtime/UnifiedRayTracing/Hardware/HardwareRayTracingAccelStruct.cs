using System.Collections.Generic;
using System.Diagnostics;

namespace UnityEngine.Rendering.UnifiedRayTracing
{
    internal sealed class HardwareRayTracingAccelStruct : IRayTracingAccelStruct
    {
        public RayTracingAccelerationStructure accelStruct { get; }

        readonly RayTracingAccelerationStructureBuildFlags m_BuildFlags;

        // keep a reference to Meshes because RayTracingAccelerationStructure impl is to automatically
        // remove instances when the mesh is disposed
        readonly Dictionary<int, Mesh> m_Meshes = new();
        readonly ReferenceCounter m_Counter;

        #if UNITY_ASSERTIONS
            readonly HashSet<int> m_InstanceHandles = new();
        #endif

        internal HardwareRayTracingAccelStruct(AccelerationStructureOptions options, ReferenceCounter counter)
        {
            m_BuildFlags = (RayTracingAccelerationStructureBuildFlags)options.buildFlags;

            RayTracingAccelerationStructure.Settings settings = new RayTracingAccelerationStructure.Settings();
            settings.rayTracingModeMask = RayTracingAccelerationStructure.RayTracingModeMask.Everything;
            settings.managementMode = RayTracingAccelerationStructure.ManagementMode.Manual;
            settings.enableCompaction = false;
            settings.layerMask = 255;
            settings.buildFlagsStaticGeometries = m_BuildFlags;

            accelStruct = new RayTracingAccelerationStructure(settings);

            m_Counter = counter;
            m_Counter.Inc();
        }

        public void Dispose()
        {
            m_Counter.Dec();
            accelStruct?.Dispose();
        }

        public int AddInstance(MeshInstanceDesc meshInstance)
        {
            Utils.CheckArgIsNotNull(meshInstance.mesh, "meshInstance.mesh");
            Utils.CheckArg(meshInstance.mesh.HasVertexAttribute(VertexAttribute.Position), "Cant use a mesh buffer that has no positions.");
            Utils.CheckArgRange(meshInstance.subMeshIndex, 0, meshInstance.mesh.subMeshCount, "meshInstance.subMeshIndex");

            var instanceDesc = new RayTracingMeshInstanceConfig(meshInstance.mesh, (uint)meshInstance.subMeshIndex, null);
            instanceDesc.mask = meshInstance.mask;
            instanceDesc.enableTriangleCulling = meshInstance.enableTriangleCulling;
            instanceDesc.frontTriangleCounterClockwise = meshInstance.frontTriangleCounterClockwise;
            instanceDesc.subMeshFlags = meshInstance.opaqueGeometry ? RayTracingSubMeshFlags.Enabled | RayTracingSubMeshFlags.ClosestHitOnly : RayTracingSubMeshFlags.Enabled | RayTracingSubMeshFlags.UniqueAnyHitCalls;
            int instanceHandle = accelStruct.AddInstance(instanceDesc, meshInstance.localToWorldMatrix, null, meshInstance.instanceID);

            // If instanceID is auto assigned, set it in the same way as ComputeRaytracingAccelStruct
            if (meshInstance.instanceID == 0xFFFFFFFF)
                accelStruct.UpdateInstanceID(instanceHandle, (uint)instanceHandle);

            m_Meshes.Add(instanceHandle, meshInstance.mesh);

            #if UNITY_ASSERTIONS
                m_InstanceHandles.Add(instanceHandle);
            #endif

            return instanceHandle;
        }

        public void RemoveInstance(int instanceHandle)
        {
            #if UNITY_ASSERTIONS
                if (!m_InstanceHandles.Remove(instanceHandle))
                    throw new System.ArgumentException($"accel struct does not contain instanceHandle {instanceHandle}", "instanceHandle");
            #endif

            m_Meshes.Remove(instanceHandle);
            accelStruct.RemoveInstance(instanceHandle);
        }

        public void ClearInstances()
        {
            #if UNITY_ASSERTIONS
                m_InstanceHandles.Clear();
            #endif

            m_Meshes.Clear();
            accelStruct.ClearInstances();
        }

        public void UpdateInstanceTransform(int instanceHandle, Matrix4x4 localToWorldMatrix)
        {
            CheckInstanceHandleIsValid(instanceHandle);

            accelStruct.UpdateInstanceTransform(instanceHandle, localToWorldMatrix);
        }

        public void UpdateInstanceID(int instanceHandle, uint instanceID)
        {
            CheckInstanceHandleIsValid(instanceHandle);

            accelStruct.UpdateInstanceID(instanceHandle, instanceID);
        }

        public void UpdateInstanceMask(int instanceHandle, uint mask)
        {
            CheckInstanceHandleIsValid(instanceHandle);

            accelStruct.UpdateInstanceMask(instanceHandle, mask);
        }

        public void Build(CommandBuffer cmd, GraphicsBuffer scratchBuffer)
        {
            Utils.CheckArgIsNotNull(cmd, nameof(cmd));

            var buildSettings = new RayTracingAccelerationStructure.BuildSettings()
            {
                buildFlags = m_BuildFlags,
                relativeOrigin = Vector3.zero
            };
            cmd.BuildRayTracingAccelerationStructure(accelStruct, buildSettings);
        }

        public ulong GetBuildScratchBufferRequiredSizeInBytes()
        {
            // Unity's Hardware impl (RayTracingAccelerationStructure) does not require any scratchbuffers.
            // They are directly handled internally by the GfxDevice.
            return 0;
        }

        [Conditional("UNITY_ASSERTIONS")]
        void CheckInstanceHandleIsValid(int instanceHandle)
        {
#if UNITY_ASSERTIONS
            if (!m_InstanceHandles.Contains(instanceHandle))
                throw new System.ArgumentException($"accel struct does not contain instanceHandle {instanceHandle}", "instanceHandle");
#endif
        }
    }
}

