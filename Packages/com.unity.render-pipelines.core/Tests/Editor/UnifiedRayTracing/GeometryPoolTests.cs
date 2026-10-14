using NUnit.Framework;
using Unity.Mathematics;

namespace UnityEngine.Rendering.UnifiedRayTracing.Tests
{
    internal class GeometryPoolTests
    {
        RayTracingResources m_Resources;

        [SetUp]
        public void SetUp()
        {
            if (!SystemInfo.supportsComputeShaders)
                Assert.Ignore("Cannot run test on this Graphics API. Compute shaders are not supported");

            m_Resources = new RayTracingResources();
            m_Resources.Load();
        }

        static Mesh CreateTriangleStripMesh(int vertexCount, float zOffset)
        {
            var vertices = new Vector3[vertexCount];
            for (int i = 0; i < vertexCount; ++i)
                vertices[i] = new Vector3(i, i % 2, zOffset);

            var indices = new int[(vertexCount - 2) * 3];
            for (int i = 0; i < vertexCount - 2; ++i)
            {
                indices[i * 3] = i;
                indices[i * 3 + 1] = i + 1;
                indices[i * 3 + 2] = i + 2;
            }

            var mesh = new Mesh { indexFormat = IndexFormat.UInt32 };
            mesh.vertices = vertices;
            mesh.SetIndices(indices, MeshTopology.Triangles, 0);
            mesh.UploadMeshData(false);
            return mesh;
        }

        static void AssertPoolContainsMesh(GeometryPool pool, GeometryPoolHandle handle, Mesh mesh)
        {
            var meshChunk = pool.GetEntryGeomAllocation(handle).meshChunks[0];
            int vertexStrideInDwords = GeometryPool.GetVertexByteSize() / 4;

            var vertexData = new uint[pool.globalVertexBuffer.count];
            pool.globalVertexBuffer.GetData(vertexData);
            var expectedVertices = mesh.vertices;
            for (int i = 0; i < expectedVertices.Length; ++i)
            {
                int dwordOffset = (meshChunk.vertexAlloc.block.offset + i) * vertexStrideInDwords;
                var actual = math.asfloat(new uint3(vertexData[dwordOffset], vertexData[dwordOffset + 1], vertexData[dwordOffset + 2]));
                Assert.AreEqual((float3)expectedVertices[i], actual, $"Vertex {i} of mesh {mesh.name}");
            }

            var indexData = new uint[pool.globalIndexBuffer.count];
            pool.globalIndexBuffer.GetData(indexData);
            var expectedIndices = mesh.GetIndices(0);
            for (int i = 0; i < expectedIndices.Length; ++i)
                Assert.AreEqual((uint)expectedIndices[i], indexData[meshChunk.indexAlloc.block.offset + i], $"Index {i} of mesh {mesh.name}");

            var meshChunkTable = new GeoPoolMeshChunk[pool.globalMeshChunkTableEntryBuffer.count];
            pool.globalMeshChunkTableEntryBuffer.GetData(meshChunkTable);
            var tableEntry = meshChunkTable[pool.GetEntryGeomAllocation(handle).meshChunkTableAlloc.block.offset];
            Assert.AreEqual(meshChunk.vertexAlloc.block.offset, tableEntry.vertexOffset);
            Assert.AreEqual(expectedVertices.Length, tableEntry.vertexCount);
            Assert.AreEqual(meshChunk.indexAlloc.block.offset, tableEntry.indexOffset);
            Assert.AreEqual(expectedIndices.Length, tableEntry.indexCount);
        }

        [Test]
        public void Register_MeshExceedsInitialCapacity_GrowsAndPreservesExistingData()
        {
            const int firstMeshVertexCount = 4;
            var desc = new GeometryPoolDesc
            {
                vertexPoolByteSize = firstMeshVertexCount * GeometryPool.GetVertexByteSize(),
                indexPoolByteSize = (firstMeshVertexCount - 2) * 3 * GeometryPool.GetIndexByteSize(),
                meshChunkTablesByteSize = GeometryPool.GetMeshChunkTableEntryByteSize()
            };

            var firstMesh = CreateTriangleStripMesh(firstMeshVertexCount, 1.0f);
            firstMesh.name = "first";
            var secondMesh = CreateTriangleStripMesh(1000, 2.0f);
            secondMesh.name = "second";
            var pool = new GeometryPool(desc, m_Resources.geometryPoolKernels, m_Resources.copyBuffer);
            try
            {
                Assert.IsTrue(pool.Register(firstMesh, out var firstHandle));
                pool.SendGpuCommands();
                int initialVertexCapacity = pool.verticesCount;
                int initialIndexCapacity = pool.indicesCount;
                int initialMeshChunkTableCapacity = pool.meshChunkTablesEntryCount;

                Assert.IsTrue(pool.Register(secondMesh, out var secondHandle));
                pool.SendGpuCommands();

                Assert.Greater(pool.verticesCount, initialVertexCapacity);
                Assert.Greater(pool.indicesCount, initialIndexCapacity);
                Assert.Greater(pool.meshChunkTablesEntryCount, initialMeshChunkTableCapacity);
                AssertPoolContainsMesh(pool, firstHandle, firstMesh);
                AssertPoolContainsMesh(pool, secondHandle, secondMesh);
            }
            finally
            {
                pool.Dispose();
                UnityEngine.Object.DestroyImmediate(firstMesh);
                UnityEngine.Object.DestroyImmediate(secondMesh);
            }
        }
    }
}
