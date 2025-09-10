using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine.Assertions;
using UnityEngine.Rendering.RadeonRays;
using UnityEditor;
using System.Data;




#if UNITY_EDITOR
using UnityEditor.Embree;
#endif

namespace UnityEngine.Rendering.UnifiedRayTracing
{
    internal class ComputeRayTracingAccelStruct : IRayTracingAccelStruct
    {
        internal ComputeRayTracingAccelStruct(
            AccelerationStructureOptions options, RayTracingResources resources,
            ReferenceCounter counter, int blasBufferInitialSizeBytes = 64 * 1024 * 1024)
        {
            m_CopyShader = resources.copyBuffer;

            RadeonRaysShaders shaders = new RadeonRaysShaders();
            shaders.bitHistogram = resources.bitHistogram;
            shaders.blockReducePart = resources.blockReducePart;
            shaders.blockScan = resources.blockScan;
            shaders.buildHlbvh = resources.buildHlbvh;
            shaders.restructureBvh = resources.restructureBvh;
            shaders.scatter = resources.scatter;
            m_RadeonRaysAPI = new RadeonRaysAPI(shaders);

            m_BuildFlags = options.buildFlags;

            #if UNITY_EDITOR
                m_UseCpuBuild = options.useCPUBuild;
            #endif

            m_Blases = new Dictionary<(int mesh, int subMeshIndex), MeshBlas>();

            var blasNodeCount = blasBufferInitialSizeBytes / RadeonRaysAPI.BvhInternalNodeSizeInBytes();
            m_BlasBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, blasNodeCount, RadeonRaysAPI.BvhInternalNodeSizeInBytes());
            m_BlasLeavesBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, blasNodeCount, RadeonRaysAPI.BvhLeafNodeSizeInBytes());
            m_BlasPositions = new BLASPositionsPool(resources.copyPositions, resources.copyBuffer);

            m_BlasAllocator = new BlockAllocator();
            m_BlasAllocator.Initialize(blasNodeCount);

            m_BlasLeavesAllocator = new BlockAllocator();
            m_BlasLeavesAllocator.Initialize(blasNodeCount);

            m_Counter = counter;
            m_Counter.Inc();
        }

        internal GraphicsBuffer topLevelBvhBuffer { get { return m_TopLevelAccelStruct?.topLevelBvh; } }
        internal GraphicsBuffer bottomLevelBvhBuffer { get { return m_TopLevelAccelStruct?.bottomLevelBvhs; } }
        internal GraphicsBuffer instanceInfoBuffer { get { return m_TopLevelAccelStruct?.instanceInfos; } }

        public void Dispose()
        {
            foreach (var blas in m_Blases.Values)
            {
                if (blas.buildInfo.triangleIndices != null)
                    blas.buildInfo.triangleIndices.Dispose();
            }

            m_Counter.Dec();
            m_RadeonRaysAPI.Dispose();
            m_BlasBuffer.Dispose();
            m_BlasLeavesBuffer.Dispose();
            m_BlasPositions.Dispose();
            m_BlasAllocator.Dispose();
            m_BlasLeavesAllocator.Dispose();
            m_TopLevelAccelStruct?.Dispose();
        }

        public int AddInstance(MeshInstanceDesc meshInstance)
        {
            Utils.CheckArgIsNotNull(meshInstance.mesh, "meshInstance.mesh");
            Utils.CheckArg(meshInstance.mesh.HasVertexAttribute(VertexAttribute.Position), "Cant use a mesh buffer that has no positions.");
            Utils.CheckArgRange(meshInstance.subMeshIndex, 0, meshInstance.mesh.subMeshCount, "meshInstance.subMeshIndex");

            var blas = GetOrAllocateMeshBlas(meshInstance.mesh, meshInstance.subMeshIndex);
            blas.IncRef();

            FreeTopLevelAccelStruct();

            int handle = NewHandle();
            m_RadeonInstances.Add(handle, new RadeonRaysInstance
            {
                geomKey = (meshInstance.mesh.GetHashCode(), meshInstance.subMeshIndex),
                blas = blas,
                instanceMask = meshInstance.mask,
                triangleCullingEnabled = meshInstance.enableTriangleCulling,
                invertTriangleCulling = meshInstance.frontTriangleCounterClockwise,
                userInstanceID = meshInstance.instanceID == 0xFFFFFFFF ? (uint)handle : meshInstance.instanceID,
                opaqueGeometry = meshInstance.opaqueGeometry,
                localToWorldTransform = ConvertTranform(meshInstance.localToWorldMatrix)
            });

            return handle;
        }

        public void RemoveInstance(int instanceHandle)
        {
            CheckInstanceHandleIsValid(instanceHandle);

            ReleaseHandle(instanceHandle);

            m_RadeonInstances.Remove(instanceHandle, out RadeonRaysInstance entry);
            var meshBlas = entry.blas;
            meshBlas.DecRef();
            if (meshBlas.IsUnreferenced())
                DeleteMeshBlas(entry.geomKey, meshBlas);

            FreeTopLevelAccelStruct();
        }

        public void ClearInstances()
        {
            m_FreeHandles.Clear();
            m_RadeonInstances.Clear();
            foreach (var blas in m_Blases.Values)
            {
                if (blas.buildInfo.triangleIndices != null)
                    blas.buildInfo.triangleIndices.Dispose();
            }

            m_Blases.Clear();
            m_BlasPositions.Clear();
            var currentCapacity = m_BlasAllocator.capacity;
            m_BlasAllocator.Dispose();
            m_BlasAllocator = new BlockAllocator();
            m_BlasAllocator.Initialize(currentCapacity);
            currentCapacity = m_BlasLeavesAllocator.capacity;
            m_BlasLeavesAllocator.Dispose();
            m_BlasLeavesAllocator = new BlockAllocator();
            m_BlasLeavesAllocator.Initialize(currentCapacity);

            FreeTopLevelAccelStruct();
        }

        public void UpdateInstanceTransform(int instanceHandle, Matrix4x4 localToWorldMatrix)
        {
            CheckInstanceHandleIsValid(instanceHandle);

            m_RadeonInstances[instanceHandle].localToWorldTransform = ConvertTranform(localToWorldMatrix);
            FreeTopLevelAccelStruct();
        }

        public void UpdateInstanceID(int instanceHandle, uint instanceID)
        {
            CheckInstanceHandleIsValid(instanceHandle);

            m_RadeonInstances[instanceHandle].userInstanceID = instanceID;
            FreeTopLevelAccelStruct();
        }

        public void UpdateInstanceMask(int instanceHandle, uint mask)
        {
            CheckInstanceHandleIsValid(instanceHandle);

            m_RadeonInstances[instanceHandle].instanceMask = mask;
            FreeTopLevelAccelStruct();
        }

        public void Build(CommandBuffer cmd, GraphicsBuffer scratchBuffer)
        {
            Utils.CheckArgIsNotNull(cmd, nameof(cmd));

            var requiredScratchSize = GetBuildScratchBufferRequiredSizeInBytes();
            if (requiredScratchSize > 0)
            {
                Utils.CheckArgIsNotNull(scratchBuffer, nameof(scratchBuffer));
                Utils.CheckArg((ulong)(scratchBuffer.count * scratchBuffer.stride) >= requiredScratchSize, "scratchBuffer size is too small");
                Utils.CheckArg(scratchBuffer.stride == 4, "scratchBuffer stride must be 4");
            }

            if (m_TopLevelAccelStruct != null)
                return;

            CreateBvh(cmd, scratchBuffer);
        }

        public ulong GetBuildScratchBufferRequiredSizeInBytes()
        {
            return GetBvhBuildScratchBufferSizeInDwords() * 4;
        }

        private void FreeTopLevelAccelStruct()
        {
            m_TopLevelAccelStruct?.Dispose();
            m_TopLevelAccelStruct = null;
        }

        private MeshBlas GetOrAllocateMeshBlas(Mesh mesh, int subMeshIndex)
        {
            MeshBlas blas;
            if (m_Blases.TryGetValue((mesh.GetHashCode(), subMeshIndex), out blas))
                return blas;

            blas = new MeshBlas();
            AllocateBlas(mesh, subMeshIndex, blas);

            m_Blases[(mesh.GetHashCode(), subMeshIndex)] = blas;

            return blas;
        }

        // throws UnifiedRayTracingException
        void AllocateBlas(Mesh mesh, int submeshIndex, MeshBlas blas)
        {
            blas.blasVertices = BlockAllocator.Allocation.Invalid;
            blas.bvhAlloc = BlockAllocator.Allocation.Invalid;
            blas.bvhLeavesAlloc = BlockAllocator.Allocation.Invalid;

            var bvhNodeSizeInDwords = RadeonRaysAPI.BvhInternalNodeSizeInDwords();

            mesh.indexBufferTarget |= GraphicsBuffer.Target.Raw;
            mesh.vertexBufferTarget |= GraphicsBuffer.Target.Raw;
            SubMeshDescriptor submeshDescriptor = mesh.GetSubMesh(submeshIndex);
            using var vertexBuffer = LoadPositionBuffer(mesh, out int stride, out int offset);

            GraphicsBuffer indexBuffer = null;
            #if UNITY_EDITOR
            if (!m_UseCpuBuild)
            #endif
                indexBuffer = LoadIndexBuffer(mesh);

            var vertexBufferDesc = new VertexBufferChunk();
            vertexBufferDesc.vertices = vertexBuffer;
            vertexBufferDesc.verticesStartOffset = offset;
            vertexBufferDesc.baseVertex = submeshDescriptor.baseVertex + submeshDescriptor.firstVertex;
            vertexBufferDesc.vertexCount = (uint)submeshDescriptor.vertexCount;
            vertexBufferDesc.vertexStride = (uint)stride;
            m_BlasPositions.Add(vertexBufferDesc, out blas.blasVertices);

            var meshBuildInfo = new MeshBuildInfo();
            meshBuildInfo.vertices = m_BlasPositions.VertexBuffer;
            meshBuildInfo.verticesStartOffset = blas.blasVertices.block.offset * BLASPositionsPool.VertexSizeInDwords;
            meshBuildInfo.baseVertex = 0;
            meshBuildInfo.triangleIndices = indexBuffer;
            meshBuildInfo.vertexCount = (uint)blas.blasVertices.block.count;
            meshBuildInfo.triangleCount = (uint)submeshDescriptor.indexCount / 3;
            meshBuildInfo.indicesStartOffset = submeshDescriptor.indexStart;
            meshBuildInfo.baseIndex = -submeshDescriptor.firstVertex;
            meshBuildInfo.indexFormat = mesh.indexFormat == IndexFormat.UInt32 ? RadeonRays.IndexFormat.Int32 : RadeonRays.IndexFormat.Int16;
            meshBuildInfo.vertexStride = 3;
            blas.buildInfo = meshBuildInfo;

            #if UNITY_EDITOR
            if (m_UseCpuBuild)
            {
                blas.indicesForCpuBuild = new List<int>();
                mesh.GetTriangles(blas.indicesForCpuBuild, submeshIndex, false);
                blas.baseIndexForCpuBuild = -submeshDescriptor.firstVertex;
                blas.verticesForCpuBuild = new List<Vector3>();
                mesh.GetVertices(blas.verticesForCpuBuild);
                blas.bvhAlloc = BlockAllocator.Allocation.Invalid;
            }
            else
            #endif
            {
                try
                {
                    var requirements = m_RadeonRaysAPI.GetMeshBuildMemoryRequirements(meshBuildInfo, ConvertFlagsToGpuBuild(m_BuildFlags));
                    var allocationNodeCount = (ulong)(requirements.bvhSizeInDwords / (ulong)bvhNodeSizeInDwords);
                    if (allocationNodeCount > int.MaxValue)
                        throw new UnifiedRayTracingException($"Can't allocate a GraphicsBuffer bigger than {GraphicsHelpers.MaxGraphicsBufferSizeInGigaBytes:F1}GB", UnifiedRayTracingError.GraphicsBufferAllocationFailed);

                    blas.bvhAlloc = AllocateBlasInternalNodes((int)allocationNodeCount);
                    blas.bvhLeavesAlloc = AllocateBlasLeafNodes((int)meshBuildInfo.triangleCount);
                }
                catch (UnifiedRayTracingException)
                {
                    if (blas.blasVertices.valid)
                        m_BlasPositions.Remove(ref blas.blasVertices);

                    if (blas.bvhAlloc.valid)
                        m_BlasAllocator.FreeAllocation(blas.bvhAlloc);

                    if (blas.bvhLeavesAlloc.valid)
                        m_BlasAllocator.FreeAllocation(blas.bvhLeavesAlloc);

                    throw;
                }
            }
        }

        private GraphicsBuffer LoadIndexBuffer(Mesh mesh)
        {
            Debug.Assert((mesh.indexBufferTarget & GraphicsBuffer.Target.Raw) != 0 || (mesh.GetIndices(0) != null && mesh.GetIndices(0).Length != 0),
               "Cant use a mesh buffer that is not raw and has no CPU index information.");

            return mesh.GetIndexBuffer();
        }

        GraphicsBuffer LoadPositionBuffer(Mesh mesh, out int stride, out int offset)
        {
            VertexAttribute attribute = VertexAttribute.Position;
            Debug.Assert(mesh.HasVertexAttribute(attribute), "Cant use a mesh buffer that has no positions.");

            int stream = mesh.GetVertexAttributeStream(attribute);
            stride = mesh.GetVertexBufferStride(stream) / 4;
            offset = mesh.GetVertexAttributeOffset(attribute) / 4;
            return mesh.GetVertexBuffer(stream);
        }

        private void DeleteMeshBlas((int mesh, int subMeshIndex) geomKey, MeshBlas blas)
        {
            m_BlasAllocator.FreeAllocation(blas.bvhAlloc);
            blas.bvhAlloc = BlockAllocator.Allocation.Invalid;
            m_BlasLeavesAllocator.FreeAllocation(blas.bvhLeavesAlloc);
            blas.bvhLeavesAlloc = BlockAllocator.Allocation.Invalid;

            m_BlasPositions.Remove(ref blas.blasVertices);

            if (blas.buildInfo.triangleIndices != null)
                blas.buildInfo.triangleIndices.Dispose();

            m_Blases.Remove(geomKey);
        }

        private ulong GetBvhBuildScratchBufferSizeInDwords()
        {
            #if UNITY_EDITOR
            if (m_UseCpuBuild)
                return 0;
            #endif

            var bvhNodeSizeInDwords = RadeonRaysAPI.BvhInternalNodeSizeInDwords();
            ulong scratchBufferSize = 0;

            foreach (var meshBlas in m_Blases)
            {
                if (meshBlas.Value.bvhBuilt)
                    continue;

                var requirements = m_RadeonRaysAPI.GetMeshBuildMemoryRequirements(meshBlas.Value.buildInfo, ConvertFlagsToGpuBuild(m_BuildFlags));
                Assert.AreEqual(requirements.bvhSizeInDwords / (ulong)bvhNodeSizeInDwords, (ulong)meshBlas.Value.bvhAlloc.block.count);
                scratchBufferSize = math.max(scratchBufferSize, requirements.buildScratchSizeInDwords);
            }

            var topLevelScratchSize = m_RadeonRaysAPI.GetSceneBuildMemoryRequirements((uint)m_RadeonInstances.Count).buildScratchSizeInDwords;
            scratchBufferSize = math.max(scratchBufferSize, topLevelScratchSize);
            scratchBufferSize = math.max(4, scratchBufferSize);

            return scratchBufferSize;
        }

        private void CreateBvh(CommandBuffer cmd, GraphicsBuffer scratchBuffer)
        {
            BuildMissingBottomLevelAccelStructs(cmd, scratchBuffer);
            BuildTopLevelAccelStruct(cmd, scratchBuffer);
        }

        private void BuildMissingBottomLevelAccelStructs(CommandBuffer cmd, GraphicsBuffer scratchBuffer)
        {
            foreach (var meshBlas in m_Blases.Values)
            {
                if (meshBlas.bvhBuilt)
                    continue;

                meshBlas.buildInfo.vertices = m_BlasPositions.VertexBuffer;

                #if UNITY_EDITOR
                if (m_UseCpuBuild)
                {
                    CpuBuildForBottomLevelAccelStruct(cmd, meshBlas);
                }
                else
                #endif
                {
                    var blasDesc = new BottomLevelLevelAccelStruct(){
                        bvh = m_BlasBuffer,
                        bvhOffset = (uint)meshBlas.bvhAlloc.block.offset,
                        bvhLeaves = m_BlasLeavesBuffer,
                        bvhLeavesOffset = (uint)meshBlas.bvhLeavesAlloc.block.offset,
                    };

                    m_RadeonRaysAPI.BuildMeshAccelStruct(
                        cmd,
                        meshBlas.buildInfo, ConvertFlagsToGpuBuild(m_BuildFlags),
                        scratchBuffer, in blasDesc);

                    meshBlas.buildInfo.triangleIndices.Dispose();
                    meshBlas.buildInfo.triangleIndices = null;
                }

                meshBlas.bvhBuilt = true;
            }

        }

        private void BuildTopLevelAccelStruct(CommandBuffer cmd, GraphicsBuffer scratchBuffer)
        {
            var radeonRaysInstances = new RadeonRays.Instance[m_RadeonInstances.Count];
            int i = 0;
            foreach (var instance in m_RadeonInstances.Values)
            {
                radeonRaysInstances[i].meshAccelStructOffset = (uint)instance.blas.bvhAlloc.block.offset;
                radeonRaysInstances[i].localToWorldTransform = instance.localToWorldTransform;
                radeonRaysInstances[i].instanceMask = instance.instanceMask;
                radeonRaysInstances[i].vertexOffset = (uint)instance.blas.blasVertices.block.offset * BLASPositionsPool.VertexSizeInDwords;
                radeonRaysInstances[i].meshAccelStructLeavesOffset = (uint)instance.blas.bvhLeavesAlloc.block.offset;
                radeonRaysInstances[i].triangleCullingEnabled = instance.triangleCullingEnabled;
                radeonRaysInstances[i].invertTriangleCulling = instance.invertTriangleCulling;
                radeonRaysInstances[i].userInstanceID = instance.userInstanceID;
                radeonRaysInstances[i].isOpaque = instance.opaqueGeometry;
                i++;
            }

            m_TopLevelAccelStruct?.Dispose();

            #if UNITY_EDITOR
            if (m_UseCpuBuild)
                m_TopLevelAccelStruct = CpuBuildForTopLevelAccelStruct(cmd, radeonRaysInstances);
            else
            #endif
                m_TopLevelAccelStruct = m_RadeonRaysAPI.BuildSceneAccelStruct(cmd, m_BlasBuffer, radeonRaysInstances, scratchBuffer);
        }

#if UNITY_EDITOR
        void CpuBuildForBottomLevelAccelStruct(CommandBuffer cmd, MeshBlas blas)
        {
            var vertices = blas.verticesForCpuBuild;
            var indices = blas.indicesForCpuBuild;

            var prims = new GpuBvhPrimitiveDescriptor[blas.buildInfo.triangleCount];
            for (int i = 0; i < blas.buildInfo.triangleCount; ++i)
            {
                var triangleIndices = GetFaceIndices(indices, i);
                var triangle = GetTriangle(vertices, triangleIndices);

                AABB aabb = new AABB();
                aabb.Encapsulate(triangle.v0);
                aabb.Encapsulate(triangle.v1);
                aabb.Encapsulate(triangle.v2);

                prims[i].primID = (uint)i;
                prims[i].lowerBound = aabb.Min;
                prims[i].upperBound = aabb.Max;
            }

            blas.indicesForCpuBuild = null;
            blas.verticesForCpuBuild = null;

            var options = ConvertFlagsToCpuBuild(m_BuildFlags, false);
            var bvhBlob = GpuBvh.Build(options, prims);
            var internalNodeCount = bvhBlob[0];
            var leafNodeCount = bvhBlob[1];
            var bvhSizeInDwords = RadeonRaysAPI.BvhInternalNodeSizeInDwords() * ((int)internalNodeCount + 1);
            var bvhLeavesSizeInDwords = bvhBlob.Length - bvhSizeInDwords;

            blas.bvhAlloc = BlockAllocator.Allocation.Invalid;
            try
            {
                blas.bvhAlloc = AllocateBlasInternalNodes((int)internalNodeCount + 1);
                blas.bvhLeavesAlloc = AllocateBlasLeafNodes((int)leafNodeCount);
            }
            catch (UnifiedRayTracingException)
            {
                if (blas.bvhAlloc.valid)
                    m_BlasAllocator.FreeAllocation(blas.bvhAlloc);

                throw;
            }
            // Fill triangle indices in leaf nodes.
            int leafOffset = bvhSizeInDwords;
            for (int i = 0; i < leafNodeCount; ++i)
            {
                var triangleIndices = GetFaceIndices(indices,(int) bvhBlob[leafOffset+3]);

                bvhBlob[leafOffset] = (uint)(triangleIndices.x + blas.baseIndexForCpuBuild);
                bvhBlob[leafOffset+1] = (uint)(triangleIndices.y + blas.baseIndexForCpuBuild);
                bvhBlob[leafOffset+2] = (uint)(triangleIndices.z + blas.baseIndexForCpuBuild);

                leafOffset += 4;
            }

            var bvhStartInDwords = blas.bvhAlloc.block.offset * RadeonRaysAPI.BvhInternalNodeSizeInDwords();
            cmd.SetBufferData(m_BlasBuffer, bvhBlob, 0, bvhStartInDwords, bvhSizeInDwords);

            var bvhLeavesStartInDwords = blas.bvhLeavesAlloc.block.offset * RadeonRaysAPI.BvhLeafNodeSizeInDwords();
            cmd.SetBufferData(m_BlasLeavesBuffer, bvhBlob, bvhSizeInDwords, bvhLeavesStartInDwords, bvhLeavesSizeInDwords);

            // read mesh aabb from bvh header.
            blas.aabbForCpuBuild = new AABB();
            blas.aabbForCpuBuild.Min.x = math.asfloat(bvhBlob[4]);
            blas.aabbForCpuBuild.Min.y = math.asfloat(bvhBlob[5]);
            blas.aabbForCpuBuild.Min.z = math.asfloat(bvhBlob[6]);
            blas.aabbForCpuBuild.Max.x = math.asfloat(bvhBlob[7]);
            blas.aabbForCpuBuild.Max.y = math.asfloat(bvhBlob[8]);
            blas.aabbForCpuBuild.Max.z = math.asfloat(bvhBlob[9]);
        }

        TopLevelAccelStruct CpuBuildForTopLevelAccelStruct(CommandBuffer cmd, RadeonRays.Instance[] radeonRaysInstances)
        {
            var prims = new GpuBvhPrimitiveDescriptor[m_RadeonInstances.Count];
            int i = 0;
            foreach (var instance in m_RadeonInstances.Values)
            {
                var blas = instance.blas;
                AABB aabb = blas.aabbForCpuBuild;

                var m = ConvertTranform(instance.localToWorldTransform);
                var bounds = GeometryUtility.CalculateBounds(new Vector3[]
                {
                    new Vector3(aabb.Min.x, aabb.Min.y, aabb.Min.z),
                    new Vector3(aabb.Min.x, aabb.Min.y, aabb.Max.z),
                    new Vector3(aabb.Min.x, aabb.Max.y, aabb.Min.z),
                    new Vector3(aabb.Min.x, aabb.Max.y, aabb.Max.z),
                    new Vector3(aabb.Max.x, aabb.Min.y, aabb.Min.z),
                    new Vector3(aabb.Max.x, aabb.Min.y, aabb.Max.z),
                    new Vector3(aabb.Max.x, aabb.Max.y, aabb.Min.z),
                    new Vector3(aabb.Max.x, aabb.Max.y, aabb.Max.z)
                  }, m);

                prims[i].primID = (uint)i;
                prims[i].lowerBound = bounds.min;
                prims[i].upperBound = bounds.max;
                i++;
            }

            if (m_RadeonInstances.Count != 0)
            {
                var options = ConvertFlagsToCpuBuild(m_BuildFlags, true);

                var bvhBlob = GpuBvh.Build(options, prims);
                var bvhSizeInDwords = bvhBlob.Length;
                var result = m_RadeonRaysAPI.CreateSceneAccelStructBuffers(m_BlasBuffer, (uint)bvhSizeInDwords, radeonRaysInstances);
                cmd.SetBufferData(result.topLevelBvh, bvhBlob);

                return result;
            }
            else
            {
                return m_RadeonRaysAPI.CreateSceneAccelStructBuffers(m_BlasBuffer, 0, radeonRaysInstances);
            }
        }

        GpuBvhBuildOptions ConvertFlagsToCpuBuild(BuildFlags flags, bool isTopLevel)
        {
            GpuBvhBuildQuality quality = GpuBvhBuildQuality.Medium;

            if ((flags & BuildFlags.PreferFastBuild) != 0 && (flags & BuildFlags.PreferFastTrace) == 0)
                quality = GpuBvhBuildQuality.Low;
            else if ((flags & BuildFlags.PreferFastTrace) != 0 && (flags & BuildFlags.PreferFastBuild) == 0)
                quality = GpuBvhBuildQuality.High;

            return new GpuBvhBuildOptions
            {
                quality = quality,
                minLeafSize = (flags & BuildFlags.MinimizeMemory) != 0 && !isTopLevel ? 4u : 1u,
                maxLeafSize = isTopLevel ? 1u : 4u,
                allowPrimitiveSplits = !isTopLevel && (flags & BuildFlags.MinimizeMemory) == 0,
                isTopLevel = isTopLevel
            };
        }
#endif

        RadeonRays.BuildFlags ConvertFlagsToGpuBuild(BuildFlags flags)
        {
            if ((flags & BuildFlags.PreferFastBuild) != 0 && (flags & BuildFlags.PreferFastTrace) == 0)
                return RadeonRays.BuildFlags.PreferFastBuild;
            else
                return RadeonRays.BuildFlags.None;
        }

        public void Bind(CommandBuffer cmd, string name, IRayTracingShader shader)
        {
            shader.SetBufferParam(cmd, Shader.PropertyToID(name + "bvh"), topLevelBvhBuffer);
            shader.SetBufferParam(cmd, Shader.PropertyToID(name + "bottomBvhs"), bottomLevelBvhBuffer);
            shader.SetBufferParam(cmd, Shader.PropertyToID(name + "bottomBvhLeaves"), m_BlasLeavesBuffer);
            shader.SetBufferParam(cmd, Shader.PropertyToID(name + "instanceInfos"), instanceInfoBuffer);
            shader.SetBufferParam(cmd, Shader.PropertyToID(name + "vertexBuffer"), m_BlasPositions.VertexBuffer);
        }

        public void Bind(CommandBuffer cmd, string name, ComputeShader shader, int kernelIndex)
        {
            cmd.SetComputeBufferParam(shader, kernelIndex, Shader.PropertyToID(name + "bvh"), topLevelBvhBuffer);
            cmd.SetComputeBufferParam(shader, kernelIndex, Shader.PropertyToID(name + "bottomBvhs"), bottomLevelBvhBuffer);
            cmd.SetComputeBufferParam(shader, kernelIndex, Shader.PropertyToID(name + "bottomBvhLeaves"), m_BlasLeavesBuffer);
            cmd.SetComputeBufferParam(shader, kernelIndex, Shader.PropertyToID(name + "instanceInfos"), instanceInfoBuffer);
            cmd.SetComputeBufferParam(shader, kernelIndex, Shader.PropertyToID(name + "vertexBuffer"), m_BlasPositions.VertexBuffer);
        }

        static private RadeonRays.Transform ConvertTranform(Matrix4x4 input)
        {
            return new RadeonRays.Transform()
            {
                row0 = input.GetRow(0),
                row1 = input.GetRow(1),
                row2 = input.GetRow(2)
            };
        }

        static private Matrix4x4 ConvertTranform(RadeonRays.Transform input)
        {
            var m = new Matrix4x4();
            m.SetRow(0, input.row0);
            m.SetRow(1, input.row1);
            m.SetRow(2, input.row2);
            m.SetRow(3, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
            return m;
        }

        static int3 GetFaceIndices(List<int> indices, int triangleIdx)
        {
            return new int3(
                indices[3 * triangleIdx],
                indices[3 * triangleIdx + 1],
                indices[3 * triangleIdx + 2]);
        }

        struct Triangle
        {
            public float3 v0;
            public float3 v1;
            public float3 v2;
        };

        static Triangle GetTriangle(List<Vector3> vertices, int3 idx)
        {
            Triangle tri;
            tri.v0 = vertices[idx.x];
            tri.v1 = vertices[idx.y];
            tri.v2 = vertices[idx.z];
            return tri;
        }

        BlockAllocator.Allocation AllocateBlasInternalNodes(int allocationNodeCount)
        {
            var allocation = m_BlasAllocator.Allocate(allocationNodeCount);
            if (!allocation.valid)
            {
                int oldCapacity = m_BlasAllocator.capacity;

                if (!m_BlasAllocator.GetExpectedGrowthToFitAllocation(allocationNodeCount, (int)(GraphicsHelpers.MaxGraphicsBufferSizeInBytes / RadeonRaysAPI.BvhInternalNodeSizeInBytes()), out int newCapacity))
                    throw new UnifiedRayTracingException($"Can't allocate a GraphicsBuffer bigger than {GraphicsHelpers.MaxGraphicsBufferSizeInGigaBytes:F1}GB", UnifiedRayTracingError.GraphicsBufferAllocationFailed);

                if (!GraphicsHelpers.ReallocateBuffer(m_CopyShader, oldCapacity, newCapacity, RadeonRaysAPI.BvhInternalNodeSizeInBytes(), ref m_BlasBuffer))
                    throw new UnifiedRayTracingException($"Failed to allocate buffer of size: {newCapacity * RadeonRaysAPI.BvhInternalNodeSizeInBytes()} bytes", UnifiedRayTracingError.GraphicsBufferAllocationFailed);

                allocation = m_BlasAllocator.GrowAndAllocate(allocationNodeCount, (int)(GraphicsHelpers.MaxGraphicsBufferSizeInBytes / RadeonRaysAPI.BvhInternalNodeSizeInBytes()), out  oldCapacity, out newCapacity);
                Debug.Assert(allocation.valid);
            }

            return allocation;
        }

        BlockAllocator.Allocation AllocateBlasLeafNodes(int allocationNodeCount)
        {
            var allocation = m_BlasLeavesAllocator.Allocate(allocationNodeCount);
            if (!allocation.valid)
            {
                int oldCapacity = m_BlasLeavesAllocator.capacity;

                if (!m_BlasLeavesAllocator.GetExpectedGrowthToFitAllocation(allocationNodeCount, (int)(GraphicsHelpers.MaxGraphicsBufferSizeInBytes / RadeonRaysAPI.BvhLeafNodeSizeInBytes()), out int newCapacity))
                    throw new UnifiedRayTracingException($"Can't allocate a GraphicsBuffer bigger than {GraphicsHelpers.MaxGraphicsBufferSizeInGigaBytes:F1}GB", UnifiedRayTracingError.GraphicsBufferAllocationFailed);

                if (!GraphicsHelpers.ReallocateBuffer(m_CopyShader, oldCapacity, newCapacity, RadeonRaysAPI.BvhLeafNodeSizeInBytes(), ref m_BlasLeavesBuffer))
                    throw new UnifiedRayTracingException($"Failed to allocate buffer of size: {newCapacity* RadeonRaysAPI.BvhLeafNodeSizeInBytes()} bytes", UnifiedRayTracingError.GraphicsBufferAllocationFailed);

                allocation = m_BlasLeavesAllocator.GrowAndAllocate(allocationNodeCount, (int)(GraphicsHelpers.MaxGraphicsBufferSizeInBytes / RadeonRaysAPI.BvhLeafNodeSizeInBytes()), out oldCapacity, out newCapacity);
                Debug.Assert(allocation.valid);
            }

            return allocation;
        }

        readonly uint m_HandleObfuscation = (uint)Random.Range(int.MinValue, int.MaxValue);

        int NewHandle()
        {
            if (m_FreeHandles.Count != 0)
                return (int)(m_FreeHandles.Dequeue() ^ m_HandleObfuscation);
            else
                return (int)((uint)m_RadeonInstances.Count ^ m_HandleObfuscation);
        }

        void ReleaseHandle(int handle)
        {
            m_FreeHandles.Enqueue((uint)handle ^ m_HandleObfuscation);
        }

        [System.Diagnostics.Conditional("UNITY_ASSERTIONS")]
        void CheckInstanceHandleIsValid(int instanceHandle)
        {
            if (!m_RadeonInstances.ContainsKey(instanceHandle))
                throw new System.ArgumentException($"accel struct does not contain instanceHandle {instanceHandle}", "instanceHandle");
        }


        readonly RadeonRaysAPI m_RadeonRaysAPI;
        readonly BuildFlags m_BuildFlags;
        #if UNITY_EDITOR
            readonly bool m_UseCpuBuild;
        #endif
        readonly ReferenceCounter m_Counter;

        readonly Dictionary<(int mesh, int subMeshIndex), MeshBlas> m_Blases;
        internal BlockAllocator m_BlasAllocator;
        GraphicsBuffer m_BlasBuffer;
        internal BlockAllocator m_BlasLeavesAllocator;
        GraphicsBuffer m_BlasLeavesBuffer;
        readonly BLASPositionsPool m_BlasPositions;

        TopLevelAccelStruct? m_TopLevelAccelStruct = null;
        readonly ComputeShader m_CopyShader;

        readonly Dictionary<int, RadeonRaysInstance> m_RadeonInstances = new ();
        readonly Queue<uint> m_FreeHandles = new();

        sealed class RadeonRaysInstance
        {
            public (int mesh, int subMeshIndex) geomKey;
            public MeshBlas blas;
            public uint instanceMask;
            public bool triangleCullingEnabled;
            public bool invertTriangleCulling;
            public uint userInstanceID;
            public bool opaqueGeometry;
            public RadeonRays.Transform localToWorldTransform;
        }

        sealed class MeshBlas
        {
            public MeshBuildInfo buildInfo;
            public BlockAllocator.Allocation bvhAlloc;
            public BlockAllocator.Allocation bvhLeavesAlloc;
            public BlockAllocator.Allocation blasVertices;
            #if UNITY_EDITOR
                public AABB aabbForCpuBuild;
                public List<int> indicesForCpuBuild;
                public int baseIndexForCpuBuild;
                public List<Vector3> verticesForCpuBuild;
            #endif
            public bool bvhBuilt = false;

            private uint refCount = 0;
            public void IncRef() { refCount++; }
            public void DecRef() { refCount--; }
            public bool IsUnreferenced() { return refCount == 0; }
        }
    }

}
