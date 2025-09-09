using NUnit.Framework;
using System;
using UnityEngine.Rendering.UnifiedRayTracing;
using UnityEngine.PathTracing.Integration;
using System.Collections.Generic;
using UnityEngine.Rendering;
using System.IO;
using System.Linq;
using UnityEditor;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.PathTracing.Lightmapping;
using UnityEngine.Rendering.Sampling;

namespace UnityEngine.PathTracing.Tests
{
    internal static class TestUtils
    {
        public static Mesh CreateSingleTriangleMesh()
        {
            Mesh mesh = new Mesh();

            Vector3[] vertices = {
                new(-0.5f, -0.5f, 0),
                new(1.0f, -0.5f, 0),
                new(-0.5f, 1.0f, 0)
            };
            mesh.vertices = vertices;

            Vector3[] normals = {
                -Vector3.forward,
                -Vector3.forward,
                -Vector3.forward
            };
            mesh.normals = normals;

            Vector2[] uv = {
                new(0, 1),
                new(1, 1),
                new(0, 0)
            };
            mesh.uv = uv;

            int[] tris = {
                0, 2, 1
            };
            mesh.triangles = tris;

            return mesh;
        }

        public static Mesh CreateQuadMesh()
        {
            Mesh mesh = new Mesh();

            Vector3[] vertices = {
                new(-0.5f, -0.5f, 0.0f),
                new(0.5f, -0.5f, 0.0f),
                new(-0.5f, 0.5f, 0.0f),
                new(0.5f, 0.5f, 0.0f)
            };
            mesh.vertices = vertices;

            Vector3[] normals = {
                -Vector3.forward,
                -Vector3.forward,
                -Vector3.forward,
                -Vector3.forward
            };
            mesh.normals = normals;

            Vector2[] uv = {
                new(0, 0),
                new(1, 0),
                new(0, 1),
                new(1, 1)
            };
            mesh.uv = uv;

            int[] tris = {
                0, 2, 1,
                2, 3, 1
            };
            mesh.triangles = tris;

            return mesh;
        }

        public static void ScaleUVs(Vector2 scale, ref Mesh mesh)
        {
            int dimension = mesh.GetVertexAttributeDimension(VertexAttribute.TexCoord0);
            Assert.AreEqual(2, dimension, $"Expected that uv0 channel has dimension 2.");
            Vector2[] uvs = mesh.uv;
            for (int i = 0; i < uvs.Length; ++i)
            {
                uvs[i] *= scale;
            }
            mesh.uv = uvs;
        }

        public static void TranslateUVs(Vector2 offset, ref Mesh mesh)
        {
            int dimension = mesh.GetVertexAttributeDimension(VertexAttribute.TexCoord0);
            Assert.AreEqual(2, dimension, $"Expected that uv0 channel has dimension 2.");
            Vector2[] uvs = mesh.uv;
            for (int i = 0; i < uvs.Length; ++i)
            {
                uvs[i] += offset;
            }
            mesh.uv = uvs;
        }

        public static void AssertThatUVsAreNormalized(Vector3[] uvs, float tolerance = 0.000001f)
        {
            for (int i = 0; i < uvs.Length; ++i)
            {
                Assert.IsTrue(uvs[i].x <= 1.0f + tolerance, $"Expected that the output uvs are normalized uv.x {uvs[i].x}.");
                Assert.IsTrue(uvs[i].x >= 0.0f - tolerance, $"Expected that the output uvs are normalized uv.x {uvs[i].x}.");
                Assert.IsTrue(uvs[i].y <= 1.0f + tolerance, $"Expected that the output uvs are normalized uv.y {uvs[i].y}.");
                Assert.IsTrue(uvs[i].y >= 0.0f - tolerance, $"Expected that the output uvs are normalized uv.y {uvs[i].y}.");
                Assert.IsTrue(uvs[i].z <= 1.0f + tolerance, $"Expected that the output uvs are normalized uv.z {uvs[i].z}.");
                Assert.IsTrue(uvs[i].z >= 0.0f - tolerance, $"Expected that the output uvs are normalized uv.z {uvs[i].z}.");
            }
        }

        public static Color[] GetRenderTextureData(RenderTexture rt)
        {
            var texture2D = new Texture2D(rt.width, rt.height, TextureFormat.RGBAFloat, false);
            RenderTexture.active = rt;
            texture2D.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
            texture2D.Apply();
            RenderTexture.active = null;
            Color[] pixels = texture2D.GetPixels();
            return pixels;
        }
    }

    internal class UVMeshTests
    {
        [Test]
        public void Build_SingleTriangleUVsAreNormalized_PositionsAreSetToUV0()
        {
            Mesh inputMesh = TestUtils.CreateSingleTriangleMesh();
            UVMesh uvMesh = new UVMesh();
            Assert.IsTrue(uvMesh.Build(inputMesh), "Building the uv mesh failed.");
            Vector3[] outputPositions = uvMesh.Mesh.vertices;
            Vector2[] inputUVs = inputMesh.uv;
            Assert.AreEqual(inputMesh.vertices.Length, outputPositions.Length, $"Expected that the uv mesh has the same number of vertices as the input mesh.");
            for (int i = 0; i < outputPositions.Length; ++i)
            {
                Vector3 inputUV = new Vector3(inputUVs[i].x, inputUVs[i].y, 0.0f);
                Assert.AreEqual(inputUV, outputPositions[i], $"Expected that the input mesh uvs are in the output uv mesh positions.");
            }
        }

        // Array of UV transformations. Each element is a tuple of (scale, translate).
        public static readonly Vector4[] UVSTs =
        {
            new(1.0f, 1.0f, 0.0f, 1.5f),
            new(1.0f, 1.0f, 1.5f, 0.0f),
            new(1.0f, 1.0f, 0.0f, -1.5f),
            new(1.0f, 1.0f, -1.5f, 0.0f),
            new(1.0f, 1.5f, 0.0f, 0.0f),
            new(1.0f, 1.0f, 0.0f, 0.0f),
        };

        [Test]
        public void Build_SingleTriangleWithNonNormalizedUVs_UVsAreNormalized([ValueSource(nameof(UVSTs))] Vector4 uvSTs)
        {
            Mesh inputMesh = TestUtils.CreateSingleTriangleMesh();
            TestUtils.ScaleUVs(new Vector2(uvSTs.x, uvSTs.y), ref inputMesh);
            TestUtils.TranslateUVs(new Vector2(uvSTs.z, uvSTs.w), ref inputMesh);
            UVMesh uvMesh = new UVMesh();
            Assert.IsTrue(uvMesh.Build(inputMesh), "Building the uv mesh failed.");
            Vector3[] outputUVs = uvMesh.Mesh.vertices;
            TestUtils.AssertThatUVsAreNormalized(outputUVs);
        }
    }

    internal class UVFallbackBufferResources : IDisposable
    {
        private readonly RayTracingBackend _backend;
        internal RayTracingContext _context;
        public CommandBuffer _cmd;
        internal UVFallbackBufferBuilder _fallbackBufferBuilder;
        internal ChartRasterizer _conservativeRasterizer;

        internal static RayTracingBackend BackendFromString(string backendAsString)
        {
            return Enum.Parse<RayTracingBackend>(backendAsString);
        }

        public void Dispose()
        {
            _conservativeRasterizer?.Dispose();
            _fallbackBufferBuilder?.Dispose();
            _context?.Dispose();
            _cmd.Dispose();
        }

        internal UVFallbackBufferResources(RayTracingBackend backend)
        {
            _backend = backend;
            var resources = new RayTracingResources();
            resources.Load();
            _context = new RayTracingContext(_backend, resources);
            _fallbackBufferBuilder = new UVFallbackBufferBuilder();
            Material uvFbMaterial = new(Shader.Find("Hidden/UVFallbackBufferGeneration"));
            _fallbackBufferBuilder.Prepare(uvFbMaterial);
            _cmd = new CommandBuffer();
            ChartRasterizer.LoadShaders(out var software, out var hardware);
            _conservativeRasterizer = new ChartRasterizer(software, hardware);
        }

        internal static void BuildUVFallbackBuffer(
            UVFallbackBufferResources resources, Mesh mesh, BuildFlags buildFlags, int width, int height,
            out UVMesh uvMesh, out UVAccelerationStructure uvAS, out UVFallbackBuffer uvFB)
        {
            uvMesh = new UVMesh();
            Assert.IsTrue(uvMesh.Build(mesh), "Building the uv mesh failed.");
            uvAS = new UVAccelerationStructure();
            uvAS.Build(resources._cmd, resources._context, uvMesh, buildFlags);
            uvFB = new UVFallbackBuffer();

            var indexCount = mesh.triangles.Length;
            using var vertex = new GraphicsBuffer(GraphicsBuffer.Target.Structured, indexCount, UnsafeUtility.SizeOf<Vector2>());
            using var vertexToOriginalVertex = new GraphicsBuffer(GraphicsBuffer.Target.Structured, indexCount, sizeof(uint));
            using var vertexToChartID = new GraphicsBuffer(GraphicsBuffer.Target.Structured, indexCount, sizeof(uint));
            var rasterizerBuffers = new ChartRasterizer.Buffers
            {
                vertex = vertex,
                vertexToOriginalVertex = vertexToOriginalVertex,
                vertexToChartID = vertexToChartID
            };

            Assert.IsTrue(uvFB.Build(resources._cmd, resources._fallbackBufferBuilder, width, height, uvMesh), "Building the uv fallback buffer failed.");
            Graphics.ExecuteCommandBuffer(resources._cmd);
            resources._cmd.Clear();
        }

        internal static void GetUVFallbackBuffer(UVFallbackBufferResources resources, Mesh mesh, BuildFlags buildFlags, int width, int height, out Color[] fallbackData)
        {
            BuildUVFallbackBuffer(resources, mesh, buildFlags, width, height, out UVMesh uvMesh, out UVAccelerationStructure uvAS, out UVFallbackBuffer uvFB);
            fallbackData = TestUtils.GetRenderTextureData(uvFB.UVFallbackRT);
            uvMesh.Dispose();
            uvAS.Dispose();
            uvFB.Dispose();
        }

        internal static void FallbackBufferStats(Color[] fallbackData, out int numOccupiedPixels, out int numInvalidFallbackPixels)
        {
            numOccupiedPixels = 0;
            numInvalidFallbackPixels = 0;
            for (int i = 0; i < fallbackData.Length; ++i)
            {
                if (fallbackData[i].r < 0.0f)
                {
                    ++numInvalidFallbackPixels;
                }
                else
                {
                    ++numOccupiedPixels;
                    Assert.IsTrue(fallbackData[i].g >= 0.0f, "Expect that fallback v is not negative when u is not negative.");
                }
            }
        }
    }

    [Category("RequiresGPU")]
    [Explicit("UVFallbackBufferTests requires a GPU to run as it uses conservative raster")]
    [TestFixture("Compute")]
    [TestFixture("Hardware")]
    internal class UVFallbackBufferTests
    {
        private UVFallbackBufferResources _resources;
        private readonly RayTracingBackend _backend;

        public UVFallbackBufferTests(string backendAsString)
        {
            _backend = UVFallbackBufferResources.BackendFromString(backendAsString);
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

            _resources = new UVFallbackBufferResources(_backend);
        }

        [TearDown]
        public void TearDown()
        {
            _resources?.Dispose();
        }

        private static void OccupancyBufferStats(Color[] occupancyData, out int numOccupiedPixels)
        {
            numOccupiedPixels = 0;
            for (int i = 0; i < occupancyData.Length; ++i)
                if (Mathf.Approximately(occupancyData[i].r, 1.0f))
                    ++numOccupiedPixels;
        }

        private static BuildFlags[] _bvhBuildFlags = { BuildFlags.PreferFastBuild, BuildFlags.MinimizeMemory, BuildFlags.None };

        [Test]
        public void Build_SingleTriangle3x3_GetCorrectFallbackBuffer([ValueSource(nameof(_bvhBuildFlags))] BuildFlags buildFlags)
        {
            int width = 3;
            int height = 3;
            Mesh mesh = TestUtils.CreateSingleTriangleMesh();
            UVFallbackBufferResources.GetUVFallbackBuffer(_resources, mesh, buildFlags, width, height, out Color[] fallbackData);
            UVFallbackBufferResources.FallbackBufferStats(fallbackData, out int numOccupiedPixels, out int numInvalidFallbackPixels);

            var support = SystemInfo.supportsConservativeRaster ? "supports" : "does not support";
            var useConservativeRaster = $"Platform {support} conservative raster.";
            Assert.AreEqual(6, numOccupiedPixels, $"Unexpected number of occupied pixels in the uv fallback buffer. {useConservativeRaster}");
            Assert.AreEqual(3, numInvalidFallbackPixels, "Unexpected number of invalid pixels in the uv fallback buffer.");
            Assert.AreEqual(width * height, numOccupiedPixels + numInvalidFallbackPixels, "The sum of occupied and invalid pixels in the uv fallback buffer should match the number of pixels in the buffer.");
        }

        [Test]
        public void Build_SingleTriangle4x3_GetCorrectFallbackBuffer([ValueSource(nameof(_bvhBuildFlags))] BuildFlags buildFlags)
        {
            int width = 4;
            int height = 3;
            Mesh mesh = TestUtils.CreateSingleTriangleMesh();
            mesh.uv = new Vector2[]
            {
                new(0, 1),
                new((float)width/(float)height, 1),
                new(0, 0)
            };
            UVFallbackBufferResources.GetUVFallbackBuffer(_resources, mesh, buildFlags, width, height, out Color[] fallbackData);
            UVFallbackBufferResources.FallbackBufferStats(fallbackData, out int numOccupiedPixels, out int numInvalidFallbackPixels);

            var support = SystemInfo.supportsConservativeRaster ? "supports" : "does not support";
            var useConservativeRaster = $"Platform {support} conservative raster.";
            Assert.AreEqual(9, numOccupiedPixels, $"Unexpected number of occupied pixels in the uv fallback buffer. {useConservativeRaster}");
            Assert.AreEqual(3, numInvalidFallbackPixels, "Unexpected number of invalid pixels in the uv fallback buffer.");
            Assert.AreEqual(width * height, numOccupiedPixels + numInvalidFallbackPixels, "The sum of occupied and invalid pixels in the uv fallback buffer should match the number of pixels in the buffer.");
        }

        private static int[] _multipassTexelSampleCounts = { 1, 16, 32, 127, 128 };
        [Test]
        public void Build_SingleTriangle4x3UsingTexelMultipass_GetCorrectFallbackBuffer([ValueSource(nameof(_multipassTexelSampleCounts))] int samplesPerTexel)
        {
            int width = 4;
            int height = 3;
            Mesh mesh = TestUtils.CreateSingleTriangleMesh();
            mesh.uv = new Vector2[]
            {
                new(0, 1),
                new((float)width/(float)height, 1),
                new(0, 0)
            };
            UVFallbackBufferResources.GetUVFallbackBuffer(_resources, mesh, BuildFlags.None, width, height, out Color[] fallbackData);
            UVFallbackBufferResources.FallbackBufferStats(fallbackData, out int numOccupiedPixels, out int numInvalidFallbackPixels);

            var support = SystemInfo.supportsConservativeRaster ? "supports" : "does not support";
            var useConservativeRaster = $"Platform {support} conservative raster.";
            Assert.AreEqual(9, numOccupiedPixels, $"Unexpected number of occupied pixels in the uv fallback buffer. {useConservativeRaster}");
            Assert.AreEqual(3, numInvalidFallbackPixels, "Unexpected number of invalid pixels in the uv fallback buffer.");
            Assert.AreEqual(width * height, numOccupiedPixels + numInvalidFallbackPixels, "The sum of occupied and invalid pixels in the uv fallback buffer should match the number of pixels in the buffer.");
        }

        private static int[] _multipassMaxSampleCounts = { 1, 10, 100, 1000, 10000 };
        [Test]
        public void Build_SingleTriangle40x30UsingMaxSamples_GetCorrectFallbackBuffer([ValueSource(nameof(_multipassMaxSampleCounts))] int samplesPerPass)
        {
            int width = 40;
            int height = 30;
            Mesh mesh = TestUtils.CreateSingleTriangleMesh();
            mesh.uv = new Vector2[]
            {
                new(0, 1),
                new((float)width/(float)height, 1),
                new(0, 0)
            };
            UVFallbackBufferResources.GetUVFallbackBuffer(_resources, mesh, BuildFlags.None, width, height, out Color[] fallbackData);
            UVFallbackBufferResources.FallbackBufferStats(fallbackData, out int numOccupiedPixels, out int numInvalidFallbackPixels);

            Assert.That(numOccupiedPixels, Is.InRange(628, 632), $"Unexpected number of occupied pixels in the uv fallback buffer.");
            Assert.That(numInvalidFallbackPixels, Is.InRange(568, 572), "Unexpected number of invalid pixels in the uv fallback buffer.");
            Assert.AreEqual(width * height, numOccupiedPixels + numInvalidFallbackPixels, "The sum of occupied and invalid pixels in the uv fallback buffer should match the number of pixels in the buffer.");
        }

        [Test]
        public void Build_Quad2x2_AllFallbackTexelsHit([ValueSource(nameof(_bvhBuildFlags))] BuildFlags buildFlags)
        {

            int width = 2;
            int height = 2;
            Mesh mesh = TestUtils.CreateQuadMesh();
            UVFallbackBufferResources.GetUVFallbackBuffer(_resources, mesh, buildFlags, width, height, out Color[] fallbackData);
            UVFallbackBufferResources.FallbackBufferStats(fallbackData, out int numOccupiedPixels, out int numInvalidFallbackPixels);

            Assert.AreEqual(4, numOccupiedPixels, $"Unexpected number of occupied pixels in the uv fallback buffer.");
            Assert.AreEqual(0, numInvalidFallbackPixels, "Unexpected number of invalid pixels in the uv fallback buffer.");
        }
        [Test]

        public void Build_QuadWithNonSquareUVs_AllCoveredTexelsAreOccupied([ValueSource(nameof(_bvhBuildFlags))] BuildFlags buildFlags)
        {
            int width = 2;
            int height = 2;
            Mesh mesh = TestUtils.CreateQuadMesh();
            mesh.uv = new[]
            {
                new Vector2(0, 0),
                new Vector2(0.501f, 0), // Cover the 2 rightmost texels by only a tiny amount
                new Vector2(0, 1),
                new Vector2(0.501f, 1)
            };

            UVFallbackBufferResources.GetUVFallbackBuffer(_resources, mesh, buildFlags, width, height, out Color[] fallbackData);

            Assert.Greater(fallbackData[0 + 0 * width].r, 0.0f, "Pixel at (0, 0) should be a hit");
            Assert.Greater(fallbackData[1 + 0 * width].r, 0.0f, "Pixel at (1, 0) should be a hit");
            Assert.Greater(fallbackData[0 + 1 * width].r, 0.0f, "Pixel at (0, 1) should be a hit");
            Assert.Greater(fallbackData[1 + 1 * width].r, 0.0f, "Pixel at (1, 1) should be a hit");
        }

        private static (int, int)[] _squareBufferResolutions = { (1, 1), (4, 4) };
        [Test]
        public void Build_QuadSquareUVsToSquareUVFallbackBuffer_AllTexelsHit([ValueSource(nameof(_bvhBuildFlags))] BuildFlags buildFlags, [ValueSource(nameof(_squareBufferResolutions))] (int width, int height) resolution)
        {
            Mesh mesh = TestUtils.CreateQuadMesh();
            mesh.uv = new Vector2[]
            {
                new(0, 0),
                new(0.55f, 0),
                new(0, 0.55f),
                new(0.55f, 0.55f)
            };

            UVFallbackBufferResources.GetUVFallbackBuffer(_resources, mesh, buildFlags, resolution.width, resolution.height, out Color[] fallbackData);

            int i = 0;
            foreach (var color in fallbackData)
            {
                Assert.Greater(color.r, 0.0f, $"Texel {i} should get a hit as the UVFallbackBuffer should be fully occupied.");
                i++;
            }
        }

        private static (int, int)[] _nonSquareBufferResolutions = { (1, 2), (2, 1), (4, 8), (8, 4) };
        [Test]
        public void Build_QuadSquareUVsToNonSquareUVFallbackBuffer_TexelsInSquareHit([ValueSource(nameof(_bvhBuildFlags))] BuildFlags buildFlags, [ValueSource(nameof(_nonSquareBufferResolutions))] (int width, int height) resolution)
        {
            Mesh mesh = TestUtils.CreateQuadMesh();
            mesh.uv = new[]
            {
                new Vector2(0, 0),
                new Vector2(0.55f, 0),
                new Vector2(0, 0.55f),
                new Vector2(0.55f, 0.55f)
            };

            UVFallbackBufferResources.GetUVFallbackBuffer(_resources, mesh, buildFlags, resolution.width, resolution.height, out Color[] fallbackData);

            int uvBoundsResolution = Mathf.Min(resolution.width, resolution.height);
            for (int y = 0; y < resolution.height; ++y)
            {
                for (int x = 0; x < resolution.width; ++x)
                {
                    var color = fallbackData[x + y * resolution.width];
                    if (x < uvBoundsResolution && y < uvBoundsResolution)
                        Assert.Greater(color.r, 0.0f, $"Texel [{x}, {y}] should get a hit as the location falls within the UV bounds.");
                    else
                        Assert.AreEqual(-1.0f, color.r, 0.0001f, $"Texel [{x}, {y}] should get a miss as the location falls outside the UV bounds.");
                }
            }
        }
    }

    [Category("RequiresGPU")]
    [Explicit("UVSamplingTests requires a GPU to run as it uses conservative raster")]
    [TestFixture("Compute")]
    [TestFixture("Hardware")]
    internal class UVSamplingTests
    {
        private UVFallbackBufferResources _resources;
        private SamplingResources _samplingResources;
        private readonly RayTracingBackend _backend;
        private IRayTracingShader _gBufferShader;

        public UVSamplingTests(string backendAsString)
        {
            _backend = UVFallbackBufferResources.BackendFromString(backendAsString);
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

            _resources = new UVFallbackBufferResources(_backend);
            _gBufferShader = _resources._context.LoadRayTracingShader("Packages/com.unity.render-pipelines.core/Runtime/PathTracing/Shaders/LightmapGBufferIntegration.urtshader");
            _samplingResources = new SamplingResources();
            _samplingResources.Load((uint)SamplingResources.ResourceType.All);
        }

        [TearDown]
        public void TearDown()
        {
            _resources?.Dispose();
            _samplingResources?.Dispose();
        }

        private static BuildFlags[] _bvhBuildFlags = { BuildFlags.PreferFastBuild, BuildFlags.MinimizeMemory, BuildFlags.None };

        public struct HitEntry
        {
            public uint instanceID;
            public uint primitiveIndex;
            public Unity.Mathematics.float2 barycentrics;
        };

        private static void GetHitEntries(ref HitEntry[] hitEntries, AsyncGPUReadbackRequest request)
        {
            Debug.Assert(!request.hasError);
            if (!request.hasError)
            {
                var src = request.GetData<HitEntry>();
                hitEntries = new HitEntry[src.Length];
                for (int i = 0; i < src.Length; ++i)
                {
                    hitEntries[i] = src[i];
                }
                return;
            }
            hitEntries = null;
        }

        public static HitEntry[] ReadBackHitEntries(CommandBuffer cmd, GraphicsBuffer gBuffer)
        {
            HitEntry[] hitEntries = null;
            Debug.Assert(gBuffer.stride == 16);
            using GraphicsBuffer stagingBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured | GraphicsBuffer.Target.CopyDestination, gBuffer.count, gBuffer.stride);
            cmd.CopyBuffer(gBuffer, stagingBuffer);
            cmd.RequestAsyncReadback(stagingBuffer, (AsyncGPUReadbackRequest request) => { GetHitEntries(ref hitEntries, request); });
            cmd.WaitAllAsyncReadbackRequests();
            Graphics.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            return hitEntries;
        }

        static private HitEntry[] GetSampleHitEntries(CommandBuffer cmd, IRayTracingShader gbufferShader, UVMesh uvMesh, UVAccelerationStructure uvAS, UVFallbackBuffer uvFB, SamplingResources samplingResources, Vector2Int instanceTexelOffset, int sampleCount, bool stochasticAntialiasing = true, uint superSamplingWidth = 1)
        {
            uint chunkSize = (uint)uvFB.Width * (uint)uvFB.Height;
            uint expandedSampleWidth = math.ceilpow2((uint)sampleCount);
            uint expandedSize = chunkSize * expandedSampleWidth;
            Debug.Assert(expandedSize <= 524288, "The expanded size quite large, consider splitting into multiple dispatches.");

            // Set up compacted GBuffer - here we don't do compaction but we simply inject all the texels - an identity compaction mapping if you will
            using GraphicsBuffer compactedTexelIndices = new GraphicsBuffer(GraphicsBuffer.Target.Structured | GraphicsBuffer.Target.CopySource, (int)(chunkSize), sizeof(uint));
            using GraphicsBuffer compactedGBufferLength = new GraphicsBuffer(GraphicsBuffer.Target.Structured | GraphicsBuffer.Target.CopySource, 1, sizeof(uint));
            compactedGBufferLength.SetData(new uint[] { chunkSize });
            uint[] texelIndices = new uint[chunkSize];
            for (uint i = 0; i < chunkSize; ++i)
                texelIndices[i] = i;
            compactedTexelIndices.SetData(texelIndices);

            // Set up scratch buffer
            GraphicsBuffer traceScratchBuffer = null;
            var requiredSizeInBytes = gbufferShader.GetTraceScratchBufferRequiredSizeInBytes(expandedSize, 1, 1);
            if (requiredSizeInBytes > 0)
                traceScratchBuffer = new GraphicsBuffer(RayTracingHelper.ScratchBufferTarget, (int)(requiredSizeInBytes / 4), 4);

            // Run the GBuffer shader
            using GraphicsBuffer gBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured | GraphicsBuffer.Target.CopySource, (int)expandedSize, UnsafeUtility.SizeOf<HitEntry>());
            ExpansionHelpers.GenerateGBuffer(
                cmd,
                gbufferShader,
                gBuffer,
                traceScratchBuffer,
                samplingResources,
                uvAS,
                uvFB,
                compactedGBufferLength,
                compactedTexelIndices,
                instanceTexelOffset,
                0,
                chunkSize,
                expandedSampleWidth,
                (uint)sampleCount,
                0,
                stochasticAntialiasing ? AntiAliasingType.Stochastic : AntiAliasingType.SuperSampling,
                superSamplingWidth);

            List<HitEntry> hitEntries = new();
            HitEntry[] hits = ReadBackHitEntries(cmd, gBuffer);

            for (uint i = 0; i < hits.Length; ++i)
            {
                var hit = hits[i];
                if (expandedSampleWidth > sampleCount)
                {
                    // The expanded sample width is larger than the sample count, so we need to filter out the unused samples
                    // The local sample index is the texel index modulo the expanded sample width
                    uint localSampleIndex = i % expandedSampleWidth;
                    // We only want to keep the samples that are less than the original sample count
                    if (localSampleIndex < (uint)sampleCount)
                        continue;
                }
                hitEntries.Add(hit);
            }

            traceScratchBuffer?.Release();
            return hitEntries.ToArray();
        }

        static private HitEntry[] FilterHitEntries(uint instanceID, uint primitiveIndex, HitEntry[] allEntries)
        {
            List<HitEntry> hitEntries = new();
            foreach (var entry in allEntries)
            {
                if (entry.instanceID == instanceID && entry.primitiveIndex == primitiveIndex)
                    hitEntries.Add(entry);
            }
            return hitEntries.ToArray();
        }

        [Test]
        public void Sample_SingleQuadIn3x3_BothPrimitivesAreHit([ValueSource(nameof(_bvhBuildFlags))] BuildFlags buildFlags)
        {
            Vector2Int instanceTexelOffset = new Vector2Int(0, 0);
            int width = 3;
            int height = 3;

            Mesh mesh = TestUtils.CreateQuadMesh();
            UVMesh uvMesh = null;
            UVAccelerationStructure uvAS = null;
            UVFallbackBuffer uvFB = null;
            try
            {
                UVFallbackBufferResources.BuildUVFallbackBuffer(_resources, mesh, buildFlags, width, height, out uvMesh, out uvAS, out uvFB);
                {
                    Color[] fallbackData = TestUtils.GetRenderTextureData(uvFB.UVFallbackRT);
                    UVFallbackBufferResources.FallbackBufferStats(fallbackData, out int numOccupiedPixels, out int numInvalidFallbackPixels);
                    Assert.AreEqual(width * height, numOccupiedPixels, $"Unexpected number of occupied pixels in the uv fallback buffer.");
                    Assert.AreEqual(0, numInvalidFallbackPixels, "Unexpected number of invalid pixels in the uv fallback buffer.");
                }

                HitEntry[] hits = GetSampleHitEntries(_resources._cmd, _gBufferShader, uvMesh, uvAS, uvFB, _samplingResources, instanceTexelOffset, 1);
                Assert.IsTrue(FilterHitEntries(0, 0, hits).Length >= 3, "Expected that triangle 0 intersects at least 3 texels.");
                Assert.IsTrue(FilterHitEntries(0, 1, hits).Length >= 3, "Expected that triangle 1 intersects at least 3 texels.");
            }
            finally
            {
                uvMesh?.Dispose();
                uvAS?.Dispose();
                uvFB?.Dispose();
            }
        }

        private static float Discrepancy(float2[] unitSamples, uint iterations, out float2 minDiscrepancyBox, out float2 maxDiscrepancyBox)
        {
            float minDiscrepancy = float.MaxValue;
            float maxDiscrepancy = float.MinValue;
            minDiscrepancyBox = 0.0f;
            maxDiscrepancyBox = 0.0f;
            System.Random rand = new(1345236);
            int numPoints = unitSamples.Length;
            for (int i = 0; i < iterations; ++i)
            {
                // make a new random box
                float2 randomBox = new float2((float)rand.NextDouble(), (float)rand.NextDouble());
                float boxVolume = randomBox.x * randomBox.y;
                // find points in box
                int pointsInBox = 0;
                for (int p = 0; p < unitSamples.Length; ++p)
                {
                    if (unitSamples[p].x <= randomBox.x && unitSamples[p].y <= randomBox.y)
                        pointsInBox++;
                }
                float discrepancyValue = Math.Abs(((float)pointsInBox / (float)numPoints) - boxVolume);
                if (discrepancyValue > maxDiscrepancy)
                {
                    maxDiscrepancy = discrepancyValue;
                    maxDiscrepancyBox = randomBox;
                }
                if (discrepancyValue < minDiscrepancy)
                {
                    minDiscrepancy = discrepancyValue;
                    minDiscrepancyBox = randomBox;
                }
            }
            return maxDiscrepancy;
        }

        private static readonly (int width, int height, int sampleCount, float expectedDiscrepancy)[] SamplingVariations =
{
            (1, 1, 128, 0.026f),
            (4, 4, 16, 0.020f),
            (16, 16, 1, 0.034f)
        };

        float2[] ComputeHitUvs(Mesh mesh, HitEntry[] hits)
        {
            var indices = mesh.triangles;
            var uvs = new List<Vector2>();
            mesh.GetUVs(0, uvs);

            var res = new float2[hits.Length];
            for (int i = 0; i < hits.Length; ++i)
            {
                float2 p0 = uvs[indices[3 * hits[i].primitiveIndex]];
                float2 p1 = uvs[indices[3 * hits[i].primitiveIndex + 1]];
                float2 p2 = uvs[indices[3 * hits[i].primitiveIndex + 2]];
                res[i] = p0 * (1.0f - hits[i].barycentrics.x - hits[i].barycentrics.y) + p1 * hits[i].barycentrics.x + p2 * hits[i].barycentrics.y;
            }

            return res;
        }

        Bounds ComputeUvBounds(float2[] uvs)
        {
            Bounds res = new Bounds(new float3(uvs[0], 0.0f), float3.zero);
            foreach (var uv in uvs)
                res.Encapsulate(new float3(uv, 0.0f));

            return res;
        }

        [Test]
        public void Sample_SingleQuadIn_SamplesAreDistributed([ValueSource(nameof(SamplingVariations))] (int width, int height, int uvSampleCount, float expectedDiscrepancy) testData, [ValueSource(nameof(_bvhBuildFlags))] BuildFlags buildFlags)
        {
            (int width, int height, int uvSampleCount, float expectedDiscrepancy) = testData;

            Vector2Int instanceTexelOffset = new Vector2Int(0, 0);
            Mesh mesh = TestUtils.CreateQuadMesh();

            UVFallbackBufferResources.BuildUVFallbackBuffer(_resources, mesh, buildFlags, width, height, out UVMesh uvMesh, out UVAccelerationStructure uvAS, out UVFallbackBuffer uvFB);
            {
                Color[] fallbackData = TestUtils.GetRenderTextureData(uvFB.UVFallbackRT);
                UVFallbackBufferResources.FallbackBufferStats(fallbackData, out int numOccupiedPixels, out int numInvalidFallbackPixels);
                Assert.AreEqual(width * height, numOccupiedPixels, $"Unexpected number of occupied pixels in the uv fallback buffer.");
                Assert.AreEqual(0, numInvalidFallbackPixels, "Unexpected number of invalid pixels in the uv fallback buffer.");
            }

            HitEntry[] hits = GetSampleHitEntries(_resources._cmd, _gBufferShader, uvMesh, uvAS, uvFB, _samplingResources, instanceTexelOffset, uvSampleCount);
            Assert.IsTrue(hits.Length == uvSampleCount * width * height, "Expected that every sample produces a hit.");

            var hitUvs = ComputeHitUvs(mesh, hits);
            Bounds pointBounds = ComputeUvBounds(hitUvs);

            Assert.IsTrue(pointBounds.size.x <= 1.0f, $"Expected samples fall in [0;1] - x size {pointBounds.size.x} too large.");
            Assert.IsTrue(pointBounds.size.y <= 1.0f, $"Expected samples fall in [0;1] - y size {pointBounds.size.y} too large.");
            Assert.IsTrue(pointBounds.min.x >= 0.0f, $"Expected samples fall in [0;1] - min x {pointBounds.min.x} is negative.");
            Assert.IsTrue(pointBounds.min.y >= 0.0f, $"Expected samples fall in [0;1] - min y {pointBounds.min.y} is negative.");
            Assert.IsTrue(pointBounds.max.x <= 1.0f, $"Expected samples fall in [0;1] - max x {pointBounds.max.x} is greater than 1.");
            Assert.IsTrue(pointBounds.max.y <= 1.0f, $"Expected samples fall in [0;1] - max y {pointBounds.max.y} is greater than 1.");
            // Empirical bound for this configuration of points in the Sobol sequence, could change for another quasi random sequence
            float minSize = 0.98f;
            Assert.IsTrue(pointBounds.size.x >= minSize, $"Expected that samples fall in [0;{minSize}] - bounds width is {pointBounds.size.x}, while {minSize} was expected.");
            Assert.IsTrue(pointBounds.size.y >= minSize, $"Expected that samples fall in [0;{minSize}] - bounds height is {pointBounds.size.y}, while {minSize} was expected.");


            float discrepancy = Discrepancy(hitUvs, 6000, out float2 minDiscrepancyBox, out float2 maxDiscrepancyBox);

            Assert.IsTrue(discrepancy < expectedDiscrepancy, $"Expected discrepancy measure to be less than {expectedDiscrepancy} was {discrepancy}.");
#if false
            string uvsMessage = new($"BN {width} x {height} - {uvSampleCount} samples - discrepancy: {discrepancy}\n");
            foreach (var uv in uvSamples)
            {
                uvsMessage += $"{uv.x}\t{uv.y}\n";
            }
            Console.WriteLine(uvsMessage);
#endif

            uvMesh?.Dispose();
            uvAS?.Dispose();
            uvFB?.Dispose();
        }

        private static readonly (int width, int height, int sampleCount, uint superSamplingWidth, Vector3 expectedBoundsSize, Vector3 expectedBoundsOrigin, float expectedDiscrepancy)[] SuperSamplingVariations =
        {
            (1, 1, 32, 1, new Vector3(0.0f, 0.0f, 0.0f), new Vector3(0.5f, 0.5f, 0), 0.74f),
            (1, 1, 32, 2, new Vector3(0.5f, 0.5f, 0.0f), new Vector3(0.25f, 0.25f, 0), 0.42f),
            (1, 1, 32, 4, new Vector3(0.75f, 0.75f, 0.0f), new Vector3(0.125f, 0.125f, 0), 0.23f),
            (1, 1, 64, 8, new Vector3(0.875f, 0.875f, 0.0f), new Vector3(0.0625f, 0.0625f, 0), 0.12f),
            (2, 2, 32, 2, new Vector3(0.75f, 0.75f, 0.0f), new Vector3(0.125f, 0.125f, 0), 0.23f),
        };

        [Test]
        public void Sample_SingleQuad_SuperSamplesAreCorrect([ValueSource(nameof(SuperSamplingVariations))] (int width, int height, int uvSampleCount, uint superSamplingWidth, Vector3 expectedBoundsSize, Vector3 expectedBoundsOrigin, float expectedDiscrepancy) testData, [ValueSource(nameof(_bvhBuildFlags))] BuildFlags buildFlags)
        {
            (int width, int height, int uvSampleCount, uint superSamplingWidth, Vector3 expectedBoundsSize, Vector3 expectedBoundsOrigin, float expectedDiscrepancy) = testData;

            Vector2Int instanceTexelOffset = new Vector2Int(0, 0);
            Mesh mesh = TestUtils.CreateQuadMesh();

            UVFallbackBufferResources.BuildUVFallbackBuffer(_resources, mesh, buildFlags, width, height, out UVMesh uvMesh, out UVAccelerationStructure uvAS, out UVFallbackBuffer uvFB);
            {
                Color[] fallbackData = TestUtils.GetRenderTextureData(uvFB.UVFallbackRT);
                UVFallbackBufferResources.FallbackBufferStats(fallbackData, out int numOccupiedPixels, out int numInvalidFallbackPixels);
                Assert.AreEqual(width * height, numOccupiedPixels, $"Unexpected number of occupied pixels in the uv fallback buffer.");
                Assert.AreEqual(0, numInvalidFallbackPixels, "Unexpected number of invalid pixels in the uv fallback buffer.");
            }

            bool useRandomSuperSampling = false;
            HitEntry[] hits = GetSampleHitEntries(_resources._cmd, _gBufferShader, uvMesh, uvAS, uvFB, _samplingResources, instanceTexelOffset, uvSampleCount, useRandomSuperSampling, superSamplingWidth);
            Assert.IsTrue(hits.Length == uvSampleCount * width * height, "Expected that every sample produces a hit.");


            var hitUvs = ComputeHitUvs(mesh, hits);
            Bounds pointBounds = ComputeUvBounds(hitUvs);

            // check that the bounds matches
            Assert.AreEqual(expectedBoundsSize.x, pointBounds.size.x, 0.001f, "Expected bounds size x is incorrect.");
            Assert.AreEqual(expectedBoundsSize.y, pointBounds.size.y, 0.001f, "Expected bounds size y is incorrect.");
            Assert.AreEqual(expectedBoundsOrigin.x, pointBounds.min.x, 0.001f, "Expected bounds origin x is incorrect.");
            Assert.AreEqual(expectedBoundsOrigin.y, pointBounds.min.y, 0.001f, "Expected bounds origin y is incorrect.");

            // check that the discrepancy is as expected
            float discrepancy = Discrepancy(hitUvs, 6000, out float2 minDiscrepancyBox, out float2 maxDiscrepancyBox);
            Assert.AreEqual(expectedDiscrepancy, discrepancy, 0.01f, $"Expected discrepancy measure is incorrect.");

            uvMesh?.Dispose();
            uvAS?.Dispose();
            uvFB?.Dispose();
        }
    }
}
