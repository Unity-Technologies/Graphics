using NUnit.Framework;
using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine.Rendering;

namespace UnityEngine.Rendering.Tests
{
    class GeometryPoolTests
    {
        public static Mesh sCube = null;
        public static Mesh sSphere = null;
        public static Mesh sCapsule = null;
        public static Mesh sCube16bit = null;
        public static Mesh sCapsule16bit = null;
        public static Mesh sMergedCubeSphere = null;

        private static Mesh MergeMeshes(Mesh a, Mesh b)
        {
            Mesh newMesh = new Mesh();
            CombineInstance[] c = new CombineInstance[2];
            var ca = new CombineInstance();
            ca.transform = Matrix4x4.identity;
            ca.subMeshIndex = 0;
            ca.mesh = a;

            var cb = new CombineInstance();
            cb.transform = Matrix4x4.identity;
            cb.subMeshIndex = 0;
            cb.mesh = b;
            c[0] = ca;
            c[1] = cb;

            newMesh.CombineMeshes(c, false);
            newMesh.UploadMeshData(false);
            return newMesh;
        }

        private static Mesh Create16BitIndexMesh(Mesh input)
        {
            Mesh newMesh = new Mesh();
            newMesh.vertices = new Vector3[input.vertexCount];
            System.Array.Copy(input.vertices, newMesh.vertices, input.vertexCount);

            newMesh.uv = new Vector2[input.vertexCount];
            System.Array.Copy(input.uv, newMesh.uv, input.vertexCount);

            newMesh.normals = new Vector3[input.vertexCount];
            System.Array.Copy(input.normals, newMesh.normals, input.vertexCount);

            newMesh.vertexBufferTarget = GraphicsBuffer.Target.Raw;
            newMesh.indexBufferTarget = GraphicsBuffer.Target.Raw;

            newMesh.subMeshCount = input.subMeshCount;

            int indexCounts = 0;
            for (int i = 0; i < input.subMeshCount; ++i)
                indexCounts += (int)input.GetIndexCount(i);

            newMesh.SetIndexBufferParams(indexCounts, IndexFormat.UInt16);

            for (int i = 0; i < input.subMeshCount; ++i)
            {
                newMesh.SetSubMesh(i, input.GetSubMesh(i), MeshUpdateFlags.DontValidateIndices | MeshUpdateFlags.DontNotifyMeshUsers | MeshUpdateFlags.DontRecalculateBounds);
                newMesh.SetIndices(input.GetIndices(i), MeshTopology.Triangles, i);
            }

            newMesh.UploadMeshData(false);
            return newMesh;
        }

        internal struct GeometryPoolTestCpuData
        {
            CommandBuffer m_cmdBuffer;
            AsyncGPUReadbackRequest m_request;
            GeometryPool m_geometryPool;

            public GeometryPool geoPool { get { return m_geometryPool; } }
            public NativeArray<int> gpuIndexData;
            public NativeArray<float> gpuVertexData;
            public NativeArray<int> gpuSubMeshLookupData;
            public NativeArray<GeoPoolSubMeshEntry> gpuSubMeshEntryData;
            public NativeArray<GeoPoolMetadataEntry> gpuMetadatas;

            public void Load(GeometryPool geometryPool)
            {
                m_cmdBuffer = new CommandBuffer();
                m_geometryPool = geometryPool;

                var indexData = new NativeArray<int>(geometryPool.indicesCount, Allocator.Persistent);
                m_cmdBuffer.RequestAsyncReadback(geometryPool.globalIndexBuffer, (AsyncGPUReadbackRequest req) =>
                {
                    if (req.done)
                        indexData.CopyFrom(req.GetData<int>());
                });

                var vertData = new NativeArray<float>(geometryPool.verticesCount * (GeometryPool.GetVertexByteSize() / 4), Allocator.Persistent);
                m_cmdBuffer.RequestAsyncReadback(geometryPool.globalVertexBuffer, (AsyncGPUReadbackRequest req) =>
                {
                    if (req.done)
                        vertData.CopyFrom(req.GetData<float>());
                });

                var subMeshLookupData = new NativeArray<int>(geometryPool.subMeshLookupCount / 4, Allocator.Persistent);
                m_cmdBuffer.RequestAsyncReadback(geometryPool.globalSubMeshLookupBuffer, (AsyncGPUReadbackRequest req) =>
                {
                    if (req.done)
                        subMeshLookupData.CopyFrom(req.GetData<int>());
                });

                var subMeshEntryData = new NativeArray<GeoPoolSubMeshEntry>(geometryPool.subMeshEntryCount, Allocator.Persistent);
                m_cmdBuffer.RequestAsyncReadback(geometryPool.globalSubMeshEntryBuffer, (AsyncGPUReadbackRequest req) =>
                {
                    if (req.done)
                        subMeshEntryData.CopyFrom(req.GetData<GeoPoolSubMeshEntry>());
                });

                var metaData = new NativeArray<GeoPoolMetadataEntry>(geometryPool.maxMeshes, Allocator.Persistent);
                m_cmdBuffer.RequestAsyncReadback(geometryPool.globalMetadataBuffer, (AsyncGPUReadbackRequest req) =>
                {
                    if (req.done)
                        metaData.CopyFrom(req.GetData<GeoPoolMetadataEntry>());
                });

                m_cmdBuffer.WaitAllAsyncReadbackRequests();

                Graphics.ExecuteCommandBuffer(m_cmdBuffer);
                gpuIndexData = indexData;
                gpuVertexData = vertData;
                gpuSubMeshLookupData = subMeshLookupData;
                gpuSubMeshEntryData = subMeshEntryData;
                gpuMetadatas = metaData;
            }

            public void Dispose()
            {
                gpuIndexData.Dispose();
                gpuVertexData.Dispose();
                gpuMetadatas.Dispose();
                gpuSubMeshLookupData.Dispose();
                gpuSubMeshEntryData.Dispose();
                m_cmdBuffer.Dispose();
            }
        }

        private static bool EpsilonAreEqual(Vector3 a, Vector3 b)
        {
            var d = a - b;
            var minV = Math.Min(Math.Abs(d.x), Math.Min(Math.Abs(d.y), Math.Abs(d.z)));
            return minV < Single.Epsilon;
        }

        internal static void VerifyMeshInPool(
            in GeometryPoolTestCpuData geopoolCpuData,
            in GeometryPoolHandle handle,
            Mesh mesh)
        {
            var gpuIndexData = geopoolCpuData.gpuIndexData;
            var gpuVertexData = geopoolCpuData.gpuVertexData;

            var idxBufferBlock = geopoolCpuData.geoPool.GetIndexBufferBlock(handle).block;
            var idxVertexBlock = geopoolCpuData.geoPool.GetVertexBufferBlock(handle).block;

            //validate indices
            for (int smId = 0; smId < (int)mesh.subMeshCount; ++smId)
            {
                var indices = mesh.GetIndices(smId);
                Assert.IsTrue(indices.Length <= idxBufferBlock.count);
                if (indices.Length != idxBufferBlock.count)
                    continue;

                for (int i = 0; i < idxBufferBlock.count; ++i)
                {
                    int expected = indices[i];
                    int result = gpuIndexData[idxBufferBlock.offset + i];

                    if (expected != result)
                        Debug.LogError("Expected index " + expected + " but got " + result);
                    Assert.IsTrue(expected == result);
                }
            }

            var srcVertices = mesh.vertices;
            var srcNormals = mesh.normals;

            int maxPoolVertCount = geopoolCpuData.geoPool.verticesCount;

            //validate vertices & normals
            for (int vId = 0; vId < (int)mesh.vertexCount; ++vId)
            {

                int poolVertIndex = idxVertexBlock.offset + vId;

                //sample vertex data
                Vector3 srcVertex = srcVertices[vId];
                int posOffset = (maxPoolVertCount * GeometryPoolConstants.GeoPoolPosByteOffset + poolVertIndex * GeometryPoolConstants.GeoPoolPosByteSize) / 4;

                var poolVertex = new Vector3(
                    gpuVertexData[posOffset + 0],
                    gpuVertexData[posOffset + 1],
                    gpuVertexData[posOffset + 2]);

                Assert.IsTrue(EpsilonAreEqual(srcVertex, poolVertex));


                //sample normal data
                Vector3 srcNormal = srcNormals[vId];
                int normalOffset = (maxPoolVertCount * GeometryPoolConstants.GeoPoolNormalByteOffset + poolVertIndex * GeometryPoolConstants.GeoPoolNormalByteSize) / 4;

                var poolNormal = new Vector3(
                    gpuVertexData[normalOffset + 0],
                    gpuVertexData[normalOffset + 1],
                    gpuVertexData[normalOffset + 2]);

                Assert.IsTrue(EpsilonAreEqual(srcNormal, poolNormal));
            }

            //validate submesh data
            var gpuSubMeshLookup = geopoolCpuData.gpuSubMeshLookupData;
            var gpuSubMeshEntry = geopoolCpuData.gpuSubMeshEntryData;
            var submeshLookupBlock = geopoolCpuData.geoPool.GetSubMeshLookupBlock(handle).block;
            var submeshEntryBlock = geopoolCpuData.geoPool.GetSubMeshEntryBlock(handle).block;
            for (int subMeshIndex = 0; subMeshIndex < mesh.subMeshCount; ++subMeshIndex)
            {
                SubMeshDescriptor descriptor = mesh.GetSubMesh(subMeshIndex);
                for (int index = descriptor.indexStart; index < descriptor.indexCount; ++index)
                {
                    int lookupIndex = (submeshLookupBlock.offset + (index / 3));
                    int lookupBucket = lookupIndex >> 2;
                    int lookupOffset = lookupIndex & 0x3;
                    int lookupShift = lookupOffset * 8;
                    int lookupValue = (gpuSubMeshLookup[lookupBucket] >> lookupShift) & 0xFF;
                    Assert.IsTrue(lookupValue == subMeshIndex);
                }

                var subMeshEntry = gpuSubMeshEntry[submeshEntryBlock.offset + subMeshIndex];
                Assert.IsTrue(subMeshEntry.baseVertex == descriptor.baseVertex);
                Assert.IsTrue(subMeshEntry.indexStart == descriptor.indexStart);
                Assert.IsTrue(subMeshEntry.indexCount == descriptor.indexCount);
            }

            //validate metadata
            GeoPoolMetadataEntry metadataEntry = geopoolCpuData.gpuMetadatas[handle.index];
            Assert.AreEqual(metadataEntry.vertexOffset, idxVertexBlock.offset);
            Assert.AreEqual(metadataEntry.indexOffset, idxBufferBlock.offset);
        }


        [SetUp]
        public void SetupGeometryPoolTests()
        {
            sCube = GameObject.CreatePrimitive(PrimitiveType.Cube).GetComponent<MeshFilter>().sharedMesh;
            sSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere).GetComponent<MeshFilter>().sharedMesh;
            sCapsule = GameObject.CreatePrimitive(PrimitiveType.Capsule).GetComponent<MeshFilter>().sharedMesh;
            sCube16bit = Create16BitIndexMesh(sCube);
            sCapsule16bit = Create16BitIndexMesh(sCapsule);

            var newCube = GameObject.CreatePrimitive(PrimitiveType.Cube).GetComponent<MeshFilter>();
            sMergedCubeSphere = MergeMeshes(sCube, sSphere);
        }

        [TearDown]
        public void TearDownGeometryPoolTests()
        {
            sCube = null;
            sSphere = null;
            sCapsule = null;
            sCube16bit = null;
            sCapsule16bit = null;
            sMergedCubeSphere = null;
        }

        [Test]
        public void TestGeometryPoolAddRemove()
        {
            var geometryPool = new GeometryPool(GeometryPoolDesc.NewDefault());
            bool status;
            status = geometryPool.Register(sCube, out var handle0);
            Assert.IsTrue(status);
            Assert.AreEqual(handle0.index, geometryPool.GetHandle(sCube).index);

            status = geometryPool.Register(sSphere, out var handle1);
            Assert.IsTrue(status);
            Assert.AreEqual(handle1.index, geometryPool.GetHandle(sSphere).index);

            geometryPool.Unregister(sSphere);
            Assert.IsTrue(!geometryPool.GetHandle(sSphere).valid);

            geometryPool.Unregister(sCube);
            Assert.IsTrue(!geometryPool.GetHandle(sCube).valid);

            geometryPool.Dispose();
        }

        [Test]
        public void TestGeometryPoolRefCount()
        {
            var geometryPool = new GeometryPool(GeometryPoolDesc.NewDefault());

            bool status;

            status = geometryPool.Register(sCube, out var handle0);
            Assert.IsTrue(status);
            status = geometryPool.Register(sCube, out var handle1);
            Assert.IsTrue(status);
            status = geometryPool.Register(sSphere, out var handle2);
            Assert.IsTrue(status);

            Assert.AreEqual(handle0.index, handle1.index);
            Assert.AreNotEqual(handle0.index, handle2.index);

            geometryPool.Unregister(sCube);

            Assert.IsTrue(geometryPool.GetHandle(sCube).valid);

            geometryPool.Unregister(sCube);

            Assert.IsTrue(!geometryPool.GetHandle(sCube).valid);

            status = geometryPool.Register(sCube, out var _);
            Assert.IsTrue(status);
            Assert.IsTrue(geometryPool.GetHandle(sCube).valid);

            geometryPool.Dispose();
        }

        [Test]
        public void TestGeometryPoolFailedAllocByIndex()
        {
            int cubeIndices = 0;
            for (int i = 0; i < (int)sCube.subMeshCount; ++i)
                cubeIndices += (int)sCube.GetIndexCount(i);

            int sphereIndices = 0;
            for (int i = 0; i < (int)sSphere.subMeshCount; ++i)
                sphereIndices += (int)sSphere.GetIndexCount(i);

            int capsuleIndices = 0;
            for (int i = 0; i < (int)sCapsule.subMeshCount; ++i)
                capsuleIndices += (int)sCapsule.GetIndexCount(i);

            var gpdesc = GeometryPoolDesc.NewDefault();
            gpdesc.indexPoolByteSize = (cubeIndices + capsuleIndices) * GeometryPool.GetIndexByteSize();

            var geometryPool = new GeometryPool(gpdesc);

            bool status;
            status = geometryPool.Register(sCube, out var _);
            Assert.IsTrue(status);

            status = geometryPool.Register(sCapsule, out var _);
            Assert.IsTrue(status);

            status = geometryPool.Register(sSphere, out var _);
            Assert.IsTrue(!status);

            geometryPool.Unregister(sCapsule);

            status = geometryPool.Register(sSphere, out var _);
            Assert.IsTrue(status);

            geometryPool.Dispose();
        }

        [Test]
        public void TestGeometryPoolFailedAllocByMaxVertex()
        {
            int cubeVertices = sCube.vertexCount;
            int sphereVertices = sSphere.vertexCount;
            int capsuleVertices = sCapsule.vertexCount;

            var gpdesc = GeometryPoolDesc.NewDefault();
            gpdesc.vertexPoolByteSize = (cubeVertices + capsuleVertices) * GeometryPool.GetVertexByteSize();

            var geometryPool = new GeometryPool(gpdesc);

            bool status;
            status = geometryPool.Register(sCube, out var _);
            Assert.IsTrue(status);

            status = geometryPool.Register(sCapsule, out var _);
            Assert.IsTrue(status);

            status = geometryPool.Register(sSphere, out var _);
            Assert.IsTrue(!status);

            geometryPool.Unregister(sCapsule);

            status = geometryPool.Register(sSphere, out var _);
            Assert.IsTrue(status);

            geometryPool.Dispose();
        }

        [Test]
        public void TestGeometryPoolFailedAllocByMaxMeshes()
        {
            var gpdesc = GeometryPoolDesc.NewDefault();
            gpdesc.maxMeshes = 2;

            var geometryPool = new GeometryPool(gpdesc);

            bool status;
            status = geometryPool.Register(sCube, out var _);
            Assert.IsTrue(status);

            status = geometryPool.Register(sCapsule, out var _);
            Assert.IsTrue(status);

            status = geometryPool.Register(sSphere, out var _);
            Assert.IsTrue(!status);

            geometryPool.Unregister(sCapsule);

            status = geometryPool.Register(sSphere, out var _);
            Assert.IsTrue(status);

            geometryPool.Dispose();
        }

        [Test]
        public void TestGpuUploadToGeometryPool()
        {
            var geometryPool = new GeometryPool(GeometryPoolDesc.NewDefault());

            bool status;

            status = geometryPool.Register(sCube, out var cubeHandle);
            Assert.IsTrue(status);

            status = geometryPool.Register(sSphere, out var sphereHandle);
            Assert.IsTrue(status);

            geometryPool.SendGpuCommands();

            GeometryPoolTestCpuData geopoolCpuData = new GeometryPoolTestCpuData();
            geopoolCpuData.Load(geometryPool);

            VerifyMeshInPool(geopoolCpuData, cubeHandle, sCube);
            VerifyMeshInPool(geopoolCpuData, sphereHandle, sSphere);

            geopoolCpuData.Dispose();
            geometryPool.Dispose();
        }

        [Test]
        public void TestGpuUploadAddRemoveToGeometryPool()
        {
            var geometryPool = new GeometryPool(GeometryPoolDesc.NewDefault());

            bool status;

            status = geometryPool.Register(sSphere, out var sphereHandle);
            Assert.IsTrue(status);

            status = geometryPool.Register(sCube, out var cubeHandle);
            Assert.IsTrue(status);

            geometryPool.SendGpuCommands();

            geometryPool.Unregister(sSphere);

            status = geometryPool.Register(sCapsule, out var capsuleHandle);
            Assert.IsTrue(status);

            geometryPool.SendGpuCommands();

            GeometryPoolTestCpuData geopoolCpuData = new GeometryPoolTestCpuData();
            geopoolCpuData.Load(geometryPool);
            VerifyMeshInPool(geopoolCpuData, cubeHandle, sCube);
            VerifyMeshInPool(geopoolCpuData, capsuleHandle, sCapsule);

            geopoolCpuData.Dispose();
            geometryPool.Dispose();
        }

        [Test]
        public void TestGpuUploadIndexBuffer16bitGeometryPool()
        {
            var geometryPool = new GeometryPool(GeometryPoolDesc.NewDefault());

            bool status;

            status = geometryPool.Register(sCube16bit, out var cubeHandle);
            Assert.IsTrue(status);

            status = geometryPool.Register(sSphere, out var sphereHandle);
            Assert.IsTrue(status);

            geometryPool.SendGpuCommands();

            GeometryPoolTestCpuData geopoolCpuData = new GeometryPoolTestCpuData();
            geopoolCpuData.Load(geometryPool);

            VerifyMeshInPool(geopoolCpuData, cubeHandle, sCube16bit);
            VerifyMeshInPool(geopoolCpuData, sphereHandle, sSphere);

            geopoolCpuData.Dispose();
            geometryPool.Dispose();
        }

        [Test]
        public void TestGpuUploadAddRemoveIndexBuffer16bitGeometryPool()
        {
            var geometryPool = new GeometryPool(GeometryPoolDesc.NewDefault());

            bool status;

            status = geometryPool.Register(sSphere, out var sphereHandle);
            Assert.IsTrue(status);

            status = geometryPool.Register(sCube16bit, out var cubeHandle);
            Assert.IsTrue(status);

            geometryPool.SendGpuCommands();

            geometryPool.Unregister(sSphere);

            status = geometryPool.Register(sCapsule16bit, out var capsuleHandle);
            Assert.IsTrue(status);

            geometryPool.SendGpuCommands();

            GeometryPoolTestCpuData geopoolCpuData = new GeometryPoolTestCpuData();
            geopoolCpuData.Load(geometryPool);
            VerifyMeshInPool(geopoolCpuData, cubeHandle, sCube16bit);
            VerifyMeshInPool(geopoolCpuData, capsuleHandle, sCapsule16bit);

            geopoolCpuData.Dispose();
            geometryPool.Dispose();
        }

        [Test]
        public void TestGpuUploadAddRemoveMergedMeshes()
        {
            var geometryPool = new GeometryPool(GeometryPoolDesc.NewDefault());

            bool status;

            status = geometryPool.Register(sSphere, out var sphereHandle);
            Assert.IsTrue(status);


            status = geometryPool.Register(sMergedCubeSphere, out var mergedCubeSphereHandle);
            Assert.IsTrue(status);


            status = geometryPool.Register(sCube16bit, out var cubeHandle);
            Assert.IsTrue(status);

            geometryPool.SendGpuCommands();

            geometryPool.Unregister(sSphere);

            status = geometryPool.Register(sCapsule16bit, out var capsuleHandle);
            Assert.IsTrue(status);

            geometryPool.SendGpuCommands();

            GeometryPoolTestCpuData geopoolCpuData = new GeometryPoolTestCpuData();
            geopoolCpuData.Load(geometryPool);
            VerifyMeshInPool(geopoolCpuData, cubeHandle, sCube16bit);
            VerifyMeshInPool(geopoolCpuData, capsuleHandle, sCapsule16bit);
            VerifyMeshInPool(geopoolCpuData, mergedCubeSphereHandle, sMergedCubeSphere);


            geopoolCpuData.Dispose();
            geometryPool.Dispose();
        }

    }
}
