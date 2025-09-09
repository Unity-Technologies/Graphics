using NUnit.Framework;
using System;
using System.IO;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.PathTracing.Integration;
using UnityEngine.PathTracing.Lightmapping;
using UnityEngine.Rendering;
using UnityEngine.Rendering.UnifiedRayTracing;
using UnityEditor;

namespace UnityEngine.PathTracing.Tests
{
    [TestFixture("Compute")]
    [TestFixture("Hardware")]
    internal class ResourceCacheTests
    {
        RayTracingBackend _backend;
        RayTracingContext _context;
        CommandBuffer m_Cmd;
        UVFallbackBufferBuilder m_UVFBBuilder;
        LightmapIntegrationResourceCache m_ResourceCache;
        ChartRasterizer m_ChartRasterizer;
        ChartRasterizer.Buffers m_ChartRasterizerBuffers;

        public ResourceCacheTests(string backendAsString)
        {
            _backend = Enum.Parse<RayTracingBackend>(backendAsString);
        }

        void CreateRayTracingResources()
        {
            var resources = new RayTracingResources();
            resources.Load();
            _context = new RayTracingContext(_backend, resources);
            m_UVFBBuilder = new UVFallbackBufferBuilder();
            Material uvFbMaterial = new(Shader.Find("Hidden/UVFallbackBufferGeneration"));

            m_UVFBBuilder.Prepare(uvFbMaterial);
            m_ResourceCache = new LightmapIntegrationResourceCache();

            ChartRasterizer.LoadShaders(out var software, out var hardware);
            m_ChartRasterizer = new ChartRasterizer(software, hardware);

            int maxIndexCount = 6; // 2 triangles
            m_ChartRasterizerBuffers = new ChartRasterizer.Buffers()
            {
                vertex = new GraphicsBuffer(GraphicsBuffer.Target.Structured, maxIndexCount, UnsafeUtility.SizeOf<Vector2>()),
                vertexToOriginalVertex = new GraphicsBuffer(GraphicsBuffer.Target.Structured, maxIndexCount, sizeof(uint)),
                vertexToChartID = new GraphicsBuffer(GraphicsBuffer.Target.Structured, maxIndexCount, sizeof(uint)),
            };
        }

        void DisposeRayTracingResources()
        {
            m_ResourceCache?.Dispose();
            m_UVFBBuilder?.Dispose();
            m_ChartRasterizer?.Dispose();
            m_ChartRasterizerBuffers.vertex?.Dispose();
            m_ChartRasterizerBuffers.vertexToOriginalVertex?.Dispose();
            m_ChartRasterizerBuffers.vertexToChartID?.Dispose();
            _context?.Dispose();
        }

        [SetUp]
        public void SetUp()
        {
            if (!SystemInfo.supportsRayTracing && _backend == RayTracingBackend.Hardware)
            {
                Assert.Ignore("Cannot run test on this Graphics API. Hardware RayTracing is not supported");
            }

            if (!SystemInfo.supportsComputeShaders && _backend == RayTracingBackend.Compute)
            {
                Assert.Ignore("Cannot run test on this Graphics API. Compute shaders are not supported");
            }

            if (SystemInfo.graphicsDeviceName.Contains("llvmpipe"))
            {
                Assert.Ignore("Cannot run test on this device (Renderer: llvmpipe (LLVM 10.0.0, 128 bits)). Tests are disabled because they fail on some platforms (that do not support 11 SSBOs). Once we do not run Ubuntu 18.04 try removing this");
            }

            CreateRayTracingResources();
            m_Cmd = new();
        }

        [TearDown]
        public void TearDown()
        {
            DisposeRayTracingResources();
            m_Cmd?.Dispose();
        }

        [Test]
        public void ResourceCache_AddInstance_ResourcesAreAdded()
        {
            Mesh mesh = TestUtils.CreateSingleTriangleMesh();
            BakeInstance instance = new();
            instance.Build(mesh, new Vector4(1, 1, 0, 0), new Vector4(1, 1, 0, 0), new Vector2Int(10, 10), Vector2Int.zero, Matrix4x4.identity, true, LodIdentifier.Invalid, 0);
            BakeInstance[] instances = { instance };
            Assert.IsTrue(m_ResourceCache.AddResources(instances, _context, m_Cmd, m_UVFBBuilder), "Expected that the instance could be added to cache.");
            Assert.AreEqual(1, m_ResourceCache.UVMeshCount(), "Expected that the cache has one uv mesh.");
            Assert.AreEqual(1, m_ResourceCache.UVAccelerationStructureCount(), "Expected that the cache has one uv acceleration structure.");
            Assert.AreEqual(1, m_ResourceCache.UVFallbackBufferCount(), "Expected that the cache has one uv fallback buffer.");
        }

        [Test]
        public void ResourceCache_AddTwoInstancesSameMesh_OnlyOneSetOfResourcesAreAdded()
        {
            Mesh mesh = TestUtils.CreateSingleTriangleMesh();
            BakeInstance instance1 = new();
            instance1.Build(mesh, new Vector4(1, 1, 0, 0), new Vector4(1, 1, 0, 0), new Vector2Int(10, 10), Vector2Int.zero, Matrix4x4.identity, true, LodIdentifier.Invalid, 0);
            BakeInstance instance2 = new();
            instance2.Build(mesh, new Vector4(1, 1, 0, 0), new Vector4(1, 1, 0, 0), new Vector2Int(10, 10), Vector2Int.zero, Matrix4x4.identity, true, LodIdentifier.Invalid, 0);
            BakeInstance[] instances = { instance1, instance2 };
            Assert.IsTrue(m_ResourceCache.AddResources(instances, _context, m_Cmd, m_UVFBBuilder), "Expected that the instance could be added to cache.");
            Assert.AreEqual(1, m_ResourceCache.UVMeshCount(), "Expected that the cache has one uv mesh.");
            Assert.AreEqual(1, m_ResourceCache.UVAccelerationStructureCount(), "Expected that the cache has one uv acceleration structure.");
            Assert.AreEqual(1, m_ResourceCache.UVFallbackBufferCount(), "Expected that the cache has one uv fallback buffer.");
        }

        [Test]
        public void ResourceCache_AddTwoInstancesDifferentMesh_TwoSetsOfResourcesAreAdded()
        {
            Mesh mesh1 = TestUtils.CreateSingleTriangleMesh();
            Mesh mesh2 = TestUtils.CreateQuadMesh();
            BakeInstance instance1 = new();
            instance1.Build(mesh1, new Vector4(1, 1, 0, 0), new Vector4(1, 1, 0, 0), new Vector2Int(10, 10), Vector2Int.zero, Matrix4x4.identity, true, LodIdentifier.Invalid, 0);
            BakeInstance instance2 = new();
            instance2.Build(mesh2, new Vector4(1, 1, 0, 0), new Vector4(1, 1, 0, 0), new Vector2Int(10, 10), Vector2Int.zero, Matrix4x4.identity, true, LodIdentifier.Invalid, 0);
            BakeInstance[] instances = { instance1, instance2 };
            Assert.IsTrue(m_ResourceCache.AddResources(instances, _context, m_Cmd, m_UVFBBuilder), "Expected that the instance could be added to cache.");
            Assert.AreEqual(2, m_ResourceCache.UVMeshCount(), "Expected that the cache has 2 uv meshes.");
            Assert.AreEqual(2, m_ResourceCache.UVAccelerationStructureCount(), "Expected that the cache has two uv acceleration structures.");
            Assert.AreEqual(2, m_ResourceCache.UVFallbackBufferCount(), "Expected that the cache has two uv fallback buffers.");
        }

        [Test]
        public void ResourceCache_AddTwoInstanceSameMeshDifferentResolution_TwoFallbackBuffersAreAdded()
        {
            Mesh mesh1 = TestUtils.CreateSingleTriangleMesh();
            Mesh mesh2 = mesh1;
            BakeInstance instance1 = new();
            instance1.Build(mesh1, new Vector4(1, 1, 0, 0), new Vector4(1, 1, 0, 0), new Vector2Int(5, 5), Vector2Int.zero, Matrix4x4.identity, true, LodIdentifier.Invalid, 0);
            BakeInstance instance2 = new();
            instance2.Build(mesh2, new Vector4(1, 1, 0, 0), new Vector4(1, 1, 0, 0), new Vector2Int(10, 10), Vector2Int.zero, Matrix4x4.identity, true, LodIdentifier.Invalid, 0);
            BakeInstance[] instances = { instance1, instance2 };
            Assert.IsTrue(m_ResourceCache.AddResources(instances, _context, m_Cmd, m_UVFBBuilder), "Expected that the instance could be added to cache.");
            Assert.AreEqual(1, m_ResourceCache.UVMeshCount(), "Expected that the cache has one uv mesh.");
            Assert.AreEqual(1, m_ResourceCache.UVAccelerationStructureCount(), "Expected that the cache has one uv acceleration structure.");
            Assert.AreEqual(2, m_ResourceCache.UVFallbackBufferCount(), "Expected that the cache has two uv fallback buffers due to the different resolution.");
        }
    }
}
