using System;
using System.Runtime.InteropServices;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.UnifiedRayTracing;

namespace UnityEngine.PathTracing.Tests
{
    [TestFixture("Compute")]
    [TestFixture("Hardware")]
    class TerrainRayMarchingTests
    {
        [StructLayout(LayoutKind.Sequential)]
        struct TestRay
        {
            public float3 origin;
            public float tMin;
            public float3 direction;
            public float tMax;
            public uint rayMask;
            public uint padding0;
            public uint padding1;
            public uint padding2;
        }

        [StructLayout(LayoutKind.Sequential)]
        struct TestHitResult
        {
            public float3 worldPosition;
            public float hitDistance;
            public float3 worldNormal;
            public uint isValid;
            public float2 uv;
            public float2 padding;
        }

        readonly RayTracingBackend m_Backend;
        RayTracingContext m_Context;
        AccelStructAdapter m_AccelStructAdapter;
        IRayTracingShader m_Shader;

        public TerrainRayMarchingTests(string backendAsString)
        {
            m_Backend = Enum.Parse<RayTracingBackend>(backendAsString);
        }

        [SetUp]
        public void SetUp()
        {
            if (!SystemInfo.supportsRayTracing && m_Backend == RayTracingBackend.Hardware)
                Assert.Ignore("Cannot run test on this Graphics API. Hardware RayTracing is not supported");

            if (!SystemInfo.supportsComputeShaders && m_Backend == RayTracingBackend.Compute)
                Assert.Ignore("Cannot run test on this Graphics API. Compute shaders are not supported");

            if (SystemInfo.graphicsDeviceName.Contains("llvmpipe"))
                Assert.Ignore("Cannot run test on this device (Renderer: llvmpipe). Software rasterizers diverge from real GPUs for terrain heightmap sampling. Mirrors the skip in ResourceCacheTests.SetUp.");

            var resources = new RayTracingResources();
            resources.Load();
            m_Context = new RayTracingContext(m_Backend, resources);

            var options = new AccelerationStructureOptions { buildFlags = BuildFlags.None };
#if UNITY_EDITOR
            options.useCPUBuild = true;
#endif
            var accelStruct = m_Context.CreateAccelerationStructure(options);
            m_AccelStructAdapter = new AccelStructAdapter(accelStruct, resources);

            var shaderType = BackendHelpers.GetTypeOfShader(m_Context.BackendType);
            var shaderAsset = AssetDatabase.LoadAssetAtPath(
                "Packages/com.unity.render-pipelines.core/Tests/Editor/PathTracing/Shaders/TerrainRayMarchingTest.urtshader",
                shaderType);
            Assert.IsNotNull(shaderAsset, "Failed to load TerrainRayMarchingTest shader.");
            m_Shader = m_Context.CreateRayTracingShader(shaderAsset);
            s_NextHandle = 1;
        }

        [TearDown]
        public void TearDown()
        {
            m_AccelStructAdapter?.Dispose();
            m_Context?.Dispose();
        }

        // Nudge ray positions to avoid landing exactly on geometry boundaries.
        // Different values for X and Z ensure we also avoid the cell diagonal (where frac(x) == frac(z)).
        // This avoids: tile AABB boundaries, cell edge boundaries, and the per-cell triangle split diagonal.
        const float k_BoundaryEpsilonX = 0.013f;
        const float k_BoundaryEpsilonZ = 0.027f;

        public enum TerrainMode { Mesh, Procedural }

        static short WorldHeightToShort(float worldHeight, float heightmapScaleY)
        {
            // Engine terrain values are in [0, 32766]; the texture encoding adds +1 and 32767+1
            // would overflow to -32768 which the shader misinterprets as a hole sentinel.
            return (short)math.min(32766, (int)(worldHeight / heightmapScaleY * 32767.0f));
        }

        static short[] CreateFlatHeightmap(int resolution, float worldHeight, float heightmapScaleY)
        {
            short h = WorldHeightToShort(worldHeight, heightmapScaleY);
            var heightData = new short[resolution * resolution];
            for (int i = 0; i < heightData.Length; i++)
                heightData[i] = h;
            return heightData;
        }

        // Half-cylinder terrain: cross-section is a semicircle across X, constant along Z.
        // At x=0 and x=extent: height = 0 (edges). At x=extent/2: height = radius (center).
        // Using a true semicircle: h = sqrt(r² - (x-r)²) where r = extent/2.
        // At quarter (x=r/2): slope = 1/sqrt(3), dot(normal, up) = sqrt(3)/2 ≈ 0.866
        // At center (x=r): slope = 0, dot(normal, up) = 1
        // At edge (x→0): slope → infinity, dot(normal, up) → 0
        static short[] CreateHalfCylinderHeightmap(int resolution, float radius, float heightmapScaleY)
        {
            var heightData = new short[resolution * resolution];
            for (int i = 0; i < heightData.Length; i++)
            {
                int x = i % resolution;
                float xPos = (float)x / (resolution - 1) * 2.0f * radius; // [0, 2*radius]
                float dx = xPos - radius; // [-radius, radius]
                float worldHeight = math.sqrt(math.max(0, radius * radius - dx * dx));
                heightData[i] = WorldHeightToShort(worldHeight, heightmapScaleY);
            }
            return heightData;
        }

        static ulong s_NextHandle = 1;

        void AddTerrain(TerrainMode mode, short[] heightData, int resolution, float3 heightmapScale, Matrix4x4 localToWorld, uint instanceMask = 0xFFFFFFFF)
        {
            ulong handle = s_NextHandle++;
            if (mode == TerrainMode.Procedural)
            {
                m_AccelStructAdapter.AddTerrainInstance(
                    handle, heightData, resolution, heightmapScale,
                    null, 0, localToWorld, 0, 0xFFFFFFFF, instanceMask);
            }
            else
            {
                var mesh = TerrainToMesh.Convert(resolution, resolution, heightData,
                    new Vector3(heightmapScale.x, heightmapScale.y, heightmapScale.z), 0, 0, null);
                m_AccelStructAdapter.AddInstance(handle, mesh, localToWorld,
                    new uint[] { instanceMask }, new uint[] { 0 }, new bool[] { true }, 0xFFFFFFFF);
            }
        }

        TestHitResult[] TraceRays(TestRay[] rays)
        {
            int rayCount = rays.Length;
            using var raysBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, rayCount, Marshal.SizeOf<TestRay>());
            raysBuffer.SetData(rays);
            using var resultsBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, rayCount, Marshal.SizeOf<TestHitResult>());

            GraphicsBuffer buildScratch = null;
            var buildCmd = new CommandBuffer();
            m_AccelStructAdapter.Build(buildCmd, ref buildScratch);
            Graphics.ExecuteCommandBuffer(buildCmd);
            buildCmd.Dispose();

            var traceScratchSize = m_Shader.GetTraceScratchBufferRequiredSizeInBytes((uint)rayCount, 1, 1);
            var traceScratch = new GraphicsBuffer(GraphicsBuffer.Target.Structured,
                Math.Max(1, (int)((traceScratchSize + 3) / 4)), 4);

            var cmd = new CommandBuffer();
            m_AccelStructAdapter.Bind(cmd, "g_SceneAccelStruct", m_Shader);
            m_AccelStructAdapter.BindTerrainResources(cmd, m_Shader);
            m_Shader.SetBufferParam(cmd, Shader.PropertyToID("_TestRays"), raysBuffer);
            // Note: _TestRays buffer now uses TestRay struct (ray + rayMask + padding)
            m_Shader.SetBufferParam(cmd, Shader.PropertyToID("_TestResults"), resultsBuffer);
            m_Shader.Dispatch(cmd, traceScratch, (uint)rayCount, 1, 1);
            Graphics.ExecuteCommandBuffer(cmd);
            cmd.Dispose();
            traceScratch.Dispose();
            buildScratch?.Dispose();

            var results = new TestHitResult[rayCount];
            resultsBuffer.GetData(results);
            return results;
        }

        static TestRay RayDown(float x, float z, float startY = 200f, uint rayMask = 0xFFFFFFFF)
        {
            return new TestRay
            {
                origin = new float3(x, startY, z),
                direction = new float3(0, -1, 0),
                tMin = 0,
                tMax = 1000,
                rayMask = rayMask
            };
        }

        [Test]
        public void TraceRayDownToFlatTerrain_HitsAtCorrectPosition(
            [Values(TerrainMode.Mesh, TerrainMode.Procedural)] TerrainMode terrainMode)
        {
            int resolution = 33;
            float3 heightmapScale = new float3(1.0f, 100.0f, 1.0f);
            float terrainHeight = 50.0f;

            AddTerrain(terrainMode, CreateFlatHeightmap(resolution, terrainHeight, heightmapScale.y),
                resolution, heightmapScale, Matrix4x4.identity);

            float terrainExtent = (resolution - 1) * heightmapScale.x; // 32m
            float rayStartY = 200f;
            float edgeInset = 0.1f;
            float outsideOffset = 0.1f;
            float ex = k_BoundaryEpsilonX;
            float ez = k_BoundaryEpsilonZ;

            var results = TraceRays(new TestRay[]
            {
                // Center
                RayDown(terrainExtent * 0.5f + ex, terrainExtent * 0.5f + ez),
                // Near edges (inside)
                RayDown(edgeInset + ex, terrainExtent * 0.5f + ez),                     // left edge
                RayDown(terrainExtent - edgeInset + ex, terrainExtent * 0.5f + ez),     // right edge
                RayDown(terrainExtent * 0.5f + ex, edgeInset + ez),                     // bottom edge
                RayDown(terrainExtent * 0.5f + ex, terrainExtent - edgeInset + ez),     // top edge
                // Near corners (inside)
                RayDown(edgeInset + ex, edgeInset + ez),                                // bottom-left
                RayDown(terrainExtent - edgeInset + ex, edgeInset + ez),                // bottom-right
                RayDown(edgeInset + ex, terrainExtent - edgeInset + ez),                // top-left
                RayDown(terrainExtent - edgeInset + ex, terrainExtent - edgeInset + ez),// top-right
                // Outside edges (no nudge needed — these should miss)
                RayDown(-outsideOffset, terrainExtent * 0.5f),                // left outside
                RayDown(terrainExtent + outsideOffset, terrainExtent * 0.5f), // right outside
                RayDown(terrainExtent * 0.5f, -outsideOffset),                // bottom outside
                RayDown(terrainExtent * 0.5f, terrainExtent + outsideOffset), // top outside
                // Outside corners
                RayDown(-outsideOffset, -outsideOffset),                      // bottom-left outside
                RayDown(terrainExtent + outsideOffset, -outsideOffset),        // bottom-right outside
                RayDown(-outsideOffset, terrainExtent + outsideOffset),        // top-left outside
                RayDown(terrainExtent + outsideOffset, terrainExtent + outsideOffset), // top-right outside
            });

            float expectedHitDist = rayStartY - terrainHeight;

            // All inside rays (0-8) should hit
            for (int i = 0; i <= 8; i++)
            {
                Assert.AreEqual(1u, results[i].isValid, $"Ray {i} ({terrainMode}) should hit the terrain.");
                Assert.AreEqual(expectedHitDist, results[i].hitDistance, 1.0f, $"Ray {i} hit distance should be ~{expectedHitDist}.");
                Assert.AreEqual(terrainHeight, results[i].worldPosition.y, 1.0f, $"Ray {i} hit Y should be at terrain surface.");
            }

            // All outside rays (9-16) should miss
            for (int i = 9; i <= 16; i++)
            {
                Assert.AreEqual(0u, results[i].isValid, $"Ray {i} should miss the terrain.");
            }
        }

        [Test]
        public void TraceRayDownToFlatTerrain_ReturnsCorrectNormal(
            [Values(TerrainMode.Mesh, TerrainMode.Procedural)] TerrainMode terrainMode)
        {
            int resolution = 33;
            float3 heightmapScale = new float3(1.0f, 100.0f, 1.0f);
            float terrainHeight = 50.0f;

            AddTerrain(terrainMode, CreateFlatHeightmap(resolution, terrainHeight, heightmapScale.y),
                resolution, heightmapScale, Matrix4x4.identity);

            float terrainExtent = (resolution - 1) * heightmapScale.x;
            float edgeInset = 0.1f;
            float ex = k_BoundaryEpsilonX;
            float ez = k_BoundaryEpsilonZ;

            var results = TraceRays(new TestRay[]
            {
                RayDown(terrainExtent * 0.5f + ex, terrainExtent * 0.5f + ez),                  // center
                RayDown(edgeInset + ex, edgeInset + ez),                                         // bottom-left
                RayDown(terrainExtent - edgeInset + ex, edgeInset + ez),                         // bottom-right
                RayDown(edgeInset + ex, terrainExtent - edgeInset + ez),                         // top-left
                RayDown(terrainExtent - edgeInset + ex, terrainExtent - edgeInset + ez),         // top-right
            });

            string[] labels = { "center", "bottom-left", "bottom-right", "top-left", "top-right" };
            for (int i = 0; i < results.Length; i++)
            {
                Assert.AreEqual(1u, results[i].isValid, $"{labels[i]} ray should hit the terrain.");
                Assert.AreEqual(0f, results[i].worldNormal.x, 0.05f, $"{labels[i]} normal X should be ~0.");
                Assert.AreEqual(1f, results[i].worldNormal.y, 0.05f, $"{labels[i]} normal Y should be ~1.");
                Assert.AreEqual(0f, results[i].worldNormal.z, 0.05f, $"{labels[i]} normal Z should be ~0.");
            }
        }

        [Test]
        public void TraceRayDownToFlatTerrain_ReturnsCorrectUV(
            [Values(TerrainMode.Mesh, TerrainMode.Procedural)] TerrainMode terrainMode)
        {
            int resolution = 33;
            float3 heightmapScale = new float3(1.0f, 100.0f, 1.0f);
            float terrainHeight = 50.0f;

            AddTerrain(terrainMode, CreateFlatHeightmap(resolution, terrainHeight, heightmapScale.y),
                resolution, heightmapScale, Matrix4x4.identity);

            float terrainExtent = (resolution - 1) * heightmapScale.x;
            float edgeInset = 0.1f;
            float ex = k_BoundaryEpsilonX;
            float ez = k_BoundaryEpsilonZ;

            // Ray positions include boundary epsilon nudge to avoid landing on geometry boundaries.
            // Expected UVs are computed from the actual (nudged) world position: uv = pos / (scale * resolution)
            float[] rayX = { terrainExtent * 0.5f + ex, edgeInset + ex, terrainExtent - edgeInset + ex, edgeInset + ex, terrainExtent - edgeInset + ex };
            float[] rayZ = { terrainExtent * 0.5f + ez, edgeInset + ez, edgeInset + ez, terrainExtent - edgeInset + ez, terrainExtent - edgeInset + ez };
            string[] labels = { "center", "bottom-left", "bottom-right", "top-left", "top-right" };

            var rays = new TestRay[rayX.Length];
            for (int i = 0; i < rays.Length; i++)
                rays[i] = RayDown(rayX[i], rayZ[i]);
            var results = TraceRays(rays);

            float tolerance = 0.02f;
            for (int i = 0; i < results.Length; i++)
            {
                Assert.AreEqual(1u, results[i].isValid, $"{labels[i]} ray should hit the terrain.");
                float expectedU = rayX[i] / (heightmapScale.x * (resolution - 1));
                float expectedV = rayZ[i] / (heightmapScale.z * (resolution - 1));
                Assert.AreEqual(expectedU, results[i].uv.x, tolerance, $"{labels[i]} UV.x should be ~{expectedU:F3}.");
                Assert.AreEqual(expectedV, results[i].uv.y, tolerance, $"{labels[i]} UV.y should be ~{expectedV:F3}.");
            }
        }

        [Test]
        public void TraceRayDownToFlatTerrain_LightmapUvMatchesMeshConvention(
            [Values(TerrainMode.Mesh, TerrainMode.Procedural)] TerrainMode terrainMode)
        {
            // Production lightmap UV (res.uv0 in GetTerrainHitGeomInfo) is hit.uvBarycentrics
            // straight from the procedural intersection. It must match the mesh path's vertex
            // UVs in extent convention (vertex.xz / (resolution-1) = cellCoord / (resolution-1)
            // — see TerrainToMesh.cs:189) so both paths agree with each other and with the
            // Progressive GPU baker.
            //
            // A divisor of resolution instead of (resolution - 1) compresses the procedural UV
            // by (W-1)/W — about 3% at 33-res — so the same world point lands at different
            // lightmap atlas positions for mesh vs procedural terrain. This appears as the
            // terrain being offset in the baked lightmap.
            //
            // This test probes positions near the right/top edges (where cellCoord is largest
            // and the offset is most pronounced) with a tolerance tight enough to catch a
            // ~3% error but well above floating-point noise.
            int resolution = 33;
            float3 heightmapScale = new float3(1.0f, 100.0f, 1.0f);
            float terrainHeight = 50.0f;

            AddTerrain(terrainMode, CreateFlatHeightmap(resolution, terrainHeight, heightmapScale.y),
                resolution, heightmapScale, Matrix4x4.identity);

            float terrainExtent = (resolution - 1) * heightmapScale.x;
            float edgeInset = 0.1f;
            float ex = k_BoundaryEpsilonX;
            float ez = k_BoundaryEpsilonZ;

            string[] labels = { "right edge", "top edge", "top-right corner" };
            float[] rayX = {
                terrainExtent - edgeInset + ex,
                terrainExtent * 0.5f + ex,
                terrainExtent - edgeInset + ex,
            };
            float[] rayZ = {
                terrainExtent * 0.5f + ez,
                terrainExtent - edgeInset + ez,
                terrainExtent - edgeInset + ez,
            };

            var rays = new TestRay[rayX.Length];
            for (int i = 0; i < rays.Length; i++)
                rays[i] = RayDown(rayX[i], rayZ[i]);
            var results = TraceRays(rays);

            // 0.01 tolerance is well above float noise but well below the ~3% offset the
            // wrong divisor introduces at the right/top edges of a 33-res terrain.
            float tolerance = 0.01f;
            for (int i = 0; i < results.Length; i++)
            {
                Assert.AreEqual(1u, results[i].isValid, $"{labels[i]} ray ({terrainMode}) should hit the terrain.");
                float expectedU = rayX[i] / (heightmapScale.x * (resolution - 1));
                float expectedV = rayZ[i] / (heightmapScale.z * (resolution - 1));
                Assert.AreEqual(expectedU, results[i].uv.x, tolerance,
                    $"{labels[i]} UV.x should match extent convention (cellCoord / (resolution-1)); was {results[i].uv.x:F4}, expected {expectedU:F4}.");
                Assert.AreEqual(expectedV, results[i].uv.y, tolerance,
                    $"{labels[i]} UV.y should match extent convention (cellCoord / (resolution-1)); was {results[i].uv.y:F4}, expected {expectedV:F4}.");
            }
        }

        [Test]
        public void TraceRayDownToOffsetTerrain_HitsAtCorrectWorldPosition(
            [Values(TerrainMode.Mesh, TerrainMode.Procedural)] TerrainMode terrainMode)
        {
            int resolution = 33;
            float3 heightmapScale = new float3(1.0f, 100.0f, 1.0f);
            float terrainHeight = 50.0f;
            // Offset large enough that the terrain doesn't overlap with a terrain at origin
            float3 terrainOffset = new float3(100.0f, 5.0f, 200.0f);

            AddTerrain(terrainMode, CreateFlatHeightmap(resolution, terrainHeight, heightmapScale.y),
                resolution, heightmapScale, Matrix4x4.Translate(terrainOffset));

            float terrainExtent = (resolution - 1) * heightmapScale.x;
            float ex = k_BoundaryEpsilonX;
            float ez = k_BoundaryEpsilonZ;

            // Shoot a single ray at the center of the offset terrain
            float rayX = terrainOffset.x + terrainExtent * 0.5f + ex;
            float rayZ = terrainOffset.z + terrainExtent * 0.5f + ez;
            var results = TraceRays(new TestRay[] { RayDown(rayX, rayZ) });

            Assert.AreEqual(1u, results[0].isValid, "Ray should hit the offset terrain.");
            float expectedY = terrainOffset.y + terrainHeight;
            Assert.AreEqual(rayX, results[0].worldPosition.x, 0.5f, "Hit X should match ray X.");
            Assert.AreEqual(expectedY, results[0].worldPosition.y, 1.0f, $"Hit Y should be at offset surface (~{expectedY}).");
            Assert.AreEqual(rayZ, results[0].worldPosition.z, 0.5f, "Hit Z should match ray Z.");
        }

        [Test]
        public void TraceRayDownBelowMeshToTerrain_HitsTerrain(
            [Values(TerrainMode.Mesh, TerrainMode.Procedural)] TerrainMode terrainMode)
        {
            // A mesh quad above a terrain.
            // Rays from between the quad and terrain should hit the terrain.
            int resolution = 33;
            float3 heightmapScale = new float3(1.0f, 100.0f, 1.0f);
            float terrainHeight = 50.0f;

            AddTerrain(terrainMode, CreateFlatHeightmap(resolution, terrainHeight, heightmapScale.y),
                resolution, heightmapScale, Matrix4x4.identity);

            // Add a mesh quad above the terrain at y = 60
            float quadY = 60.0f;
            float terrainExtent = (resolution - 1) * heightmapScale.x;
            var quad = new Mesh();
            quad.vertices = new Vector3[] {
                new(0, quadY, 0), new(terrainExtent, quadY, 0),
                new(terrainExtent, quadY, terrainExtent), new(0, quadY, terrainExtent) };
            quad.normals = new Vector3[] { Vector3.down, Vector3.down, Vector3.down, Vector3.down };
            quad.uv = new Vector2[] { new(0,0), new(1,0), new(1,1), new(0,1) };
            quad.triangles = new int[] { 0, 2, 1, 0, 3, 2 };
            m_AccelStructAdapter.AddInstance(2, quad, Matrix4x4.identity,
                new uint[] { 0xFFFFFFFF }, new uint[] { 0 }, new bool[] { true }, 0xFFFFFFFF);

            float ex = k_BoundaryEpsilonX;
            float ez = k_BoundaryEpsilonZ;

            // Ray starts between the quad and terrain, going down — should hit terrain, not quad
            float rayY = quadY - 0.1f;
            var results = TraceRays(new TestRay[]
            {
                RayDown(terrainExtent * 0.5f + ex, terrainExtent * 0.5f + ez, rayY),
            });

            Assert.AreEqual(1u, results[0].isValid, $"Ray should hit the {terrainMode} terrain below the mesh quad.");
            Assert.AreEqual(terrainHeight, results[0].worldPosition.y, 1.0f,
                $"Hit Y should be at terrain surface (~{terrainHeight}), not the quad ({quadY}).");
        }

        [Test]
        public void TraceRayDownToHalfCylinder_ReturnsCorrectNormals(
            [Values(TerrainMode.Mesh, TerrainMode.Procedural)] TerrainMode terrainMode)
        {
            // Use equal extent and max height so the cylinder cross-section is a semicircle.
            // This makes the expected normals easier to reason about:
            // - at the edges the slope is vertical → dot(normal, up) ≈ 0
            // - at quarter/three-quarter the slope is 45° → dot(normal, up) ≈ 0.7
            // - at the center the slope is flat → dot(normal, up) ≈ 1
            int resolution = 65;
            float radius = 32.0f;
            float3 heightmapScale = new float3(2.0f * radius / (resolution - 1), radius, 2.0f * radius / (resolution - 1));

            AddTerrain(terrainMode, CreateHalfCylinderHeightmap(resolution, radius, heightmapScale.y),
                resolution, heightmapScale, Matrix4x4.identity);

            float terrainExtent = (resolution - 1) * heightmapScale.x; // = 2 * radius
            float midZ = terrainExtent * 0.5f;
            float edgeInset = 0.1f;

            // 45° slope on a semicircle is at x = r/sqrt(2) from center
            float fortyFiveOffset = radius / math.sqrt(2f);
            float ex = k_BoundaryEpsilonX;
            float ez = k_BoundaryEpsilonZ;

            var results = TraceRays(new TestRay[]
            {
                RayDown(edgeInset + ex, midZ + ez, radius + 50),                                      // 0: left edge
                RayDown(radius - fortyFiveOffset + ex, midZ + ez, radius + 50),                        // 1: left 45°
                RayDown(terrainExtent * 0.5f + ex, midZ + ez, radius + 50),                            // 2: center
                RayDown(radius + fortyFiveOffset + ex, midZ + ez, radius + 50),                        // 3: right 45°
                RayDown(terrainExtent - edgeInset + ex, midZ + ez, radius + 50),                       // 4: right edge
            });

            string[] labels = { "left edge", "left 45", "center", "right 45", "right edge" };
            for (int i = 0; i < results.Length; i++)
                Assert.AreEqual(1u, results[i].isValid, $"{labels[i]} ray should hit the terrain.");

            float3 up = new float3(0, 1, 0);
            float tolerance = 0.15f;

            float dotEdgeL = math.dot(results[0].worldNormal, up);
            float dot45L   = math.dot(results[1].worldNormal, up);
            float dotCenter = math.dot(results[2].worldNormal, up);
            float dot45R   = math.dot(results[3].worldNormal, up);
            float dotEdgeR = math.dot(results[4].worldNormal, up);

            // Center: flat top → dot ≈ 1
            Assert.AreEqual(1f, dotCenter, tolerance, $"Center dot(normal, up) should be ~1, was {dotCenter:F3}.");

            // 45° slope points: dot(normal, up) = 1/sqrt(2) ≈ 0.707
            float expected45Dot = 1f / math.sqrt(2f);
            Assert.AreEqual(expected45Dot, dot45L, tolerance, $"Left 45 dot(normal, up) should be ~{expected45Dot:F3}, was {dot45L:F3}.");
            Assert.AreEqual(expected45Dot, dot45R, tolerance, $"Right 45 dot(normal, up) should be ~{expected45Dot:F3}, was {dot45R:F3}.");

            // Edges: steep slope → dot close to 0
            Assert.Less(dotEdgeL, 0.3f, $"Left edge dot(normal, up) should be < 0.3, was {dotEdgeL:F3}.");
            Assert.Less(dotEdgeR, 0.3f, $"Right edge dot(normal, up) should be < 0.3, was {dotEdgeR:F3}.");

            // Normals should be symmetric
            Assert.AreEqual(dot45L, dot45R, tolerance, "Left and right 45 should be symmetric.");
            Assert.AreEqual(dotEdgeL, dotEdgeR, tolerance, "Left and right edges should be symmetric.");

            // Z component should be ~0 for all (cylinder axis is along Z)
            for (int i = 0; i < results.Length; i++)
                Assert.AreEqual(0f, results[i].worldNormal.z, 0.05f, $"{labels[i]} normal Z should be ~0.");
        }

        [Test]
        public void TraceRayWithMask_HitsCorrectGeometry(
            [Values(TerrainMode.Mesh, TerrainMode.Procedural)] TerrainMode terrainMode)
        {
            int resolution = 33;
            float3 heightmapScale = new float3(1.0f, 100.0f, 1.0f);
            float terrainHeight = 60.0f;
            float quadHeight = 30.0f;
            uint terrainMask = 1;
            uint quadMask = 2;
            float terrainExtent = (resolution - 1) * heightmapScale.x;

            // Terrain at y=60 (above quad) with mask=1
            AddTerrain(terrainMode, CreateFlatHeightmap(resolution, terrainHeight, heightmapScale.y),
                resolution, heightmapScale, Matrix4x4.identity, terrainMask);

            // Mesh quad at y=30 (below terrain) with mask=2
            var quad = new UnityEngine.Mesh();
            quad.vertices = new Vector3[] {
                new(0, quadHeight, 0), new(terrainExtent, quadHeight, 0),
                new(terrainExtent, quadHeight, terrainExtent), new(0, quadHeight, terrainExtent) };
            quad.normals = new Vector3[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
            quad.uv = new Vector2[] { new(0,0), new(1,0), new(1,1), new(0,1) };
            quad.triangles = new int[] { 0, 2, 1, 0, 3, 2 };
            m_AccelStructAdapter.AddInstance(s_NextHandle++, quad, Matrix4x4.identity,
                new uint[] { quadMask }, new uint[] { 0 }, new bool[] { true }, 0xFFFFFFFF);

            float ex = k_BoundaryEpsilonX;
            float ez = k_BoundaryEpsilonZ;
            float cx = terrainExtent * 0.5f + ex;
            float cz = terrainExtent * 0.5f + ez;

            var results = TraceRays(new TestRay[]
            {
                RayDown(cx, cz, rayMask: terrainMask),              // 0: only terrain visible → hits terrain at y=60
                RayDown(cx, cz, rayMask: quadMask),                 // 1: only quad visible → hits quad at y=30 (terrain masked out)
                RayDown(cx, cz, rayMask: terrainMask | quadMask),   // 2: both visible → hits terrain at y=60 (closest from above)
            });

            // Ray 0: should hit terrain
            Assert.AreEqual(1u, results[0].isValid, "Ray with terrain mask should hit.");
            Assert.AreEqual(terrainHeight, results[0].worldPosition.y, 1.0f, "Should hit terrain at y=60.");

            // Ray 1: terrain masked out, should pass through and hit quad
            Assert.AreEqual(1u, results[1].isValid, "Ray with quad mask should hit.");
            Assert.AreEqual(quadHeight, results[1].worldPosition.y, 1.0f, "Should hit quad at y=30 (terrain masked out).");

            // Ray 2: both visible, should hit terrain (closest from above)
            Assert.AreEqual(1u, results[2].isValid, "Ray with both masks should hit.");
            Assert.AreEqual(terrainHeight, results[2].worldPosition.y, 1.0f, "Should hit terrain at y=60 (closest).");
        }

        [Test]
        public void TraceRayDownToZeroHeightTerrain_FindsIntersection(
            [Values(TerrainMode.Mesh, TerrainMode.Procedural)] TerrainMode terrainMode)
        {
            int resolution = 33;
            float3 heightmapScale = new float3(1.0f, 100.0f, 1.0f);
            float terrainHeight = 0.0f;

            AddTerrain(terrainMode, CreateFlatHeightmap(resolution, terrainHeight, heightmapScale.y),
                resolution, heightmapScale, Matrix4x4.identity);

            float terrainExtent = (resolution - 1) * heightmapScale.x;
            float ex = k_BoundaryEpsilonX;
            float ez = k_BoundaryEpsilonZ;

            var results = TraceRays(new TestRay[]
            {
                RayDown(terrainExtent * 0.5f + ex, terrainExtent * 0.5f + ez),
            });

            Assert.AreEqual(1u, results[0].isValid, $"Center ray ({terrainMode}) should hit the zero-height terrain.");
        }

        static short[] CreateBowlHeightmap(int resolution, float rimHeight, float heightmapScaleY)
        {
            var heightData = new short[resolution * resolution];
            for (int i = 0; i < heightData.Length; i++)
            {
                int x = i % resolution;
                int y = i / resolution;
                // Normalized position [0,1]
                float nx = (float)x / (resolution - 1);
                float ny = (float)y / (resolution - 1);
                // Distance from center [0, ~0.707]
                float dx = nx - 0.5f;
                float dy = ny - 0.5f;
                float dist = math.sqrt(dx * dx + dy * dy);
                // Bowl shape: height = rimHeight * (2 * dist)^2, clamped to rimHeight
                float worldHeight = math.min(rimHeight, rimHeight * 4.0f * dist * dist);
                heightData[i] = WorldHeightToShort(worldHeight, heightmapScaleY);
            }
            return heightData;
        }

        [Test]
        public void TraceRaysInBowl_AllLowerHemisphereRaysHit(
            [Values(TerrainMode.Mesh, TerrainMode.Procedural)] TerrainMode terrainMode)
        {
            // A bowl terrain: 1x1m, flat center at height ~0, raised rim at 0.5m.
            // A ray origin at the center at 0.4m (below rim) shooting in any downward
            // direction should hit the bowl surface.
            int resolution = 33;
            float rimHeight = 0.5f;
            float3 heightmapScale = new float3(1.0f / (resolution - 1), rimHeight, 1.0f / (resolution - 1));

            AddTerrain(terrainMode, CreateBowlHeightmap(resolution, rimHeight, heightmapScale.y),
                resolution, heightmapScale, Matrix4x4.identity);

            float terrainExtent = (resolution - 1) * heightmapScale.x; // = 1.0m
            float originX = terrainExtent * 0.5f + k_BoundaryEpsilonX;
            float originY = 0.4f; // below the 0.5m rim
            float originZ = terrainExtent * 0.5f + k_BoundaryEpsilonZ;

            // Generate rays spread over the lower hemisphere
            int raysPerAxis = 8;
            var rays = new System.Collections.Generic.List<TestRay>();
            for (int xi = 0; xi < raysPerAxis; xi++)
            {
                for (int zi = 0; zi < raysPerAxis; zi++)
                {
                    // Uniform directions over the lower hemisphere
                    float u = (xi + 0.5f) / raysPerAxis;
                    float v = (zi + 0.5f) / raysPerAxis;
                    float phi = 2.0f * math.PI * u;
                    float cosTheta = -v; // negative = downward hemisphere
                    float sinTheta = math.sqrt(1.0f - cosTheta * cosTheta);
                    float3 dir = math.normalize(new float3(sinTheta * math.cos(phi), cosTheta, sinTheta * math.sin(phi)));

                    rays.Add(new TestRay
                    {
                        origin = new float3(originX, originY, originZ),
                        direction = dir,
                        tMin = 0,
                        tMax = 10,
                        rayMask = 0xFFFFFFFF
                    });
                }
            }

            var results = TraceRays(rays.ToArray());

            int hitCount = 0;
            int missCount = 0;
            for (int i = 0; i < results.Length; i++)
            {
                if (results[i].isValid == 1)
                    hitCount++;
                else
                    missCount++;
            }

            Assert.AreEqual(results.Length, hitCount,
                $"All {results.Length} lower hemisphere rays should hit the bowl terrain, but {missCount} missed.");
        }

        [Test]
        public void TraceRayDownToTwoTerrains_HitsBoth(
            [Values(TerrainMode.Mesh, TerrainMode.Procedural)] TerrainMode terrainMode)
        {
            int resolution = 33;
            float3 heightmapScale = new float3(1.0f, 100.0f, 1.0f);
            float heightA = 30.0f;
            float heightB = 60.0f;
            float terrainExtent = (resolution - 1) * heightmapScale.x;
            float3 offsetB = new float3(terrainExtent + 10.0f, 0, 0);

            AddTerrain(terrainMode, CreateFlatHeightmap(resolution, heightA, heightmapScale.y),
                resolution, heightmapScale, Matrix4x4.identity);
            AddTerrain(terrainMode, CreateFlatHeightmap(resolution, heightB, heightmapScale.y),
                resolution, heightmapScale, Matrix4x4.Translate(offsetB));

            if (terrainMode == TerrainMode.Procedural)
                Assert.AreEqual(2, m_AccelStructAdapter.TerrainCount, "Should have two terrain slices in the texture array.");

            float ex = k_BoundaryEpsilonX;
            float ez = k_BoundaryEpsilonZ;
            float midZ = terrainExtent * 0.5f + ez;

            var results = TraceRays(new TestRay[]
            {
                RayDown(terrainExtent * 0.5f + ex, midZ),                        // terrain A center
                RayDown(offsetB.x + terrainExtent * 0.5f + ex, midZ),            // terrain B center
            });

            Assert.AreEqual(1u, results[0].isValid, "Ray should hit terrain A.");
            Assert.AreEqual(heightA, results[0].worldPosition.y, 1.0f, $"Should hit terrain A at y={heightA}.");

            Assert.AreEqual(1u, results[1].isValid, "Ray should hit terrain B.");
            Assert.AreEqual(heightB, results[1].worldPosition.y, 1.0f, $"Should hit terrain B at y={heightB}.");
        }

        // For mixed-resolution coverage we drive the procedural path explicitly.
        // The mesh path doesn't share heightmap storage between terrains, so it is the natural
        // oracle and we keep it parameterized too — the procedural cases are the ones being
        // implemented for and rely on the regrow-and-repad logic in AccelStructAdapter.
        [Test]
        public void TraceRayDownToMixedResolutionTerrains_SmallThenLarge_HitsBoth(
            [Values(TerrainMode.Mesh, TerrainMode.Procedural)] TerrainMode terrainMode)
        {
            int resA = 33;
            int resB = 129;
            float3 heightmapScale = new float3(1.0f, 100.0f, 1.0f);
            float heightA = 30.0f;
            float heightB = 60.0f;

            float extentA = (resA - 1) * heightmapScale.x;
            float extentB = (resB - 1) * heightmapScale.x;
            float3 offsetB = new float3(extentA + 10.0f, 0, 0);

            // Order matters here: small first, then large. Adding the large one must regrow the
            // texture array and re-pad slot 0 (the small terrain) into the new larger slice.
            AddTerrain(terrainMode, CreateFlatHeightmap(resA, heightA, heightmapScale.y),
                resA, heightmapScale, Matrix4x4.identity);
            AddTerrain(terrainMode, CreateFlatHeightmap(resB, heightB, heightmapScale.y),
                resB, heightmapScale, Matrix4x4.Translate(offsetB));

            if (terrainMode == TerrainMode.Procedural)
                Assert.AreEqual(2, m_AccelStructAdapter.TerrainCount, "Should have two terrain slices in the texture array.");

            float ex = k_BoundaryEpsilonX;
            float ez = k_BoundaryEpsilonZ;

            var results = TraceRays(new TestRay[]
            {
                RayDown(extentA * 0.5f + ex, extentA * 0.5f + ez),                    // 0: terrain A (33-res, slot 0 after regrow)
                RayDown(offsetB.x + extentB * 0.5f + ex, extentB * 0.5f + ez),        // 1: terrain B (129-res, slot 1)
            });

            Assert.AreEqual(1u, results[0].isValid, "Ray should hit small (33-res) terrain A after regrow.");
            Assert.AreEqual(heightA, results[0].worldPosition.y, 1.0f, $"Should hit terrain A at y={heightA}.");

            Assert.AreEqual(1u, results[1].isValid, "Ray should hit large (129-res) terrain B.");
            Assert.AreEqual(heightB, results[1].worldPosition.y, 1.0f, $"Should hit terrain B at y={heightB}.");
        }

        [Test]
        public void TraceRayDownToMixedResolutionTerrains_LargeThenSmall_HitsBoth(
            [Values(TerrainMode.Mesh, TerrainMode.Procedural)] TerrainMode terrainMode)
        {
            int resA = 129;
            int resB = 33;
            float3 heightmapScale = new float3(1.0f, 100.0f, 1.0f);
            float heightA = 30.0f;
            float heightB = 60.0f;

            float extentA = (resA - 1) * heightmapScale.x;
            float extentB = (resB - 1) * heightmapScale.x;
            float3 offsetB = new float3(extentA + 10.0f, 0, 0);

            // Large first, then small: no regrow on the second add. The small terrain must be
            // copied into the upper-left of a slot sized to the existing (larger) atlas width.
            AddTerrain(terrainMode, CreateFlatHeightmap(resA, heightA, heightmapScale.y),
                resA, heightmapScale, Matrix4x4.identity);
            AddTerrain(terrainMode, CreateFlatHeightmap(resB, heightB, heightmapScale.y),
                resB, heightmapScale, Matrix4x4.Translate(offsetB));

            float ex = k_BoundaryEpsilonX;
            float ez = k_BoundaryEpsilonZ;

            var results = TraceRays(new TestRay[]
            {
                RayDown(extentA * 0.5f + ex, extentA * 0.5f + ez),                    // 0: terrain A (129-res)
                RayDown(offsetB.x + extentB * 0.5f + ex, extentB * 0.5f + ez),        // 1: terrain B (33-res, padded)
            });

            Assert.AreEqual(1u, results[0].isValid, "Ray should hit large (129-res) terrain A.");
            Assert.AreEqual(heightA, results[0].worldPosition.y, 1.0f, $"Should hit terrain A at y={heightA}.");

            Assert.AreEqual(1u, results[1].isValid, "Ray should hit small (33-res) terrain B in padded slot.");
            Assert.AreEqual(heightB, results[1].worldPosition.y, 1.0f, $"Should hit terrain B at y={heightB}.");
        }

        [Test]
        public void TraceRayDownToMixedResolutionTerrains_LargeThenSmall_PaddingDoesNotHit(
            [Values(TerrainMode.Mesh, TerrainMode.Procedural)] TerrainMode terrainMode)
        {
            // The small terrain occupies the upper-left of a slot sized to the larger atlas.
            // The padding region must not produce spurious hits — the AABB tiles only cover the
            // real terrain extent, so this is mainly a regression guard.
            int resA = 129;
            int resB = 33;
            float3 heightmapScale = new float3(1.0f, 100.0f, 1.0f);
            float heightA = 30.0f;
            float heightB = 60.0f;

            float extentA = (resA - 1) * heightmapScale.x;
            float extentB = (resB - 1) * heightmapScale.x;
            float3 offsetB = new float3(extentA + 10.0f, 0, 0);

            AddTerrain(terrainMode, CreateFlatHeightmap(resA, heightA, heightmapScale.y),
                resA, heightmapScale, Matrix4x4.identity);
            AddTerrain(terrainMode, CreateFlatHeightmap(resB, heightB, heightmapScale.y),
                resB, heightmapScale, Matrix4x4.Translate(offsetB));

            // Pick world coords that are outside terrain B's extent but inside what would be
            // its slot if the atlas were treated as world-space (it isn't — but a buggy UV
            // remap could erroneously make the padded region rayable).
            float outsideOffset = 0.1f;
            float beyondBX = offsetB.x + extentB + outsideOffset;
            float beyondBZ = extentB + outsideOffset;
            // Stay clear of terrain A's extent (offsetB.x = extentA + 10).
            Assert.Less(extentA, beyondBX, "Test setup: ray X should not hit terrain A.");

            var results = TraceRays(new TestRay[]
            {
                RayDown(beyondBX, extentB * 0.5f),                                    // just past terrain B's right edge
                RayDown(offsetB.x + extentB * 0.5f, beyondBZ),                        // just past terrain B's far edge
            });

            Assert.AreEqual(0u, results[0].isValid, "Ray past terrain B's right edge must miss (no padding hits).");
            Assert.AreEqual(0u, results[1].isValid, "Ray past terrain B's far edge must miss (no padding hits).");
        }

        [Test]
        public void TraceRayDownToThreeMixedResolutionTerrains_HitsAll(
            [Values(TerrainMode.Mesh, TerrainMode.Procedural)] TerrainMode terrainMode)
        {
            // Two regrows: 33 → 65 → 257. Verifies that re-padding existing slots remains
            // correct after multiple grows (slot 0 padded twice, slot 1 padded once).
            int[] resolutions = { 33, 65, 257 };
            float[] heights = { 20.0f, 40.0f, 70.0f };
            float3 heightmapScale = new float3(1.0f, 100.0f, 1.0f);

            float gap = 10.0f;
            float[] offsetsX = new float[resolutions.Length];
            float runningX = 0;
            for (int i = 0; i < resolutions.Length; i++)
            {
                offsetsX[i] = runningX;
                runningX += (resolutions[i] - 1) * heightmapScale.x + gap;
            }

            for (int i = 0; i < resolutions.Length; i++)
            {
                AddTerrain(terrainMode,
                    CreateFlatHeightmap(resolutions[i], heights[i], heightmapScale.y),
                    resolutions[i], heightmapScale, Matrix4x4.Translate(new float3(offsetsX[i], 0, 0)));
            }

            if (terrainMode == TerrainMode.Procedural)
                Assert.AreEqual(3, m_AccelStructAdapter.TerrainCount, "Should have three terrain slices.");

            float ex = k_BoundaryEpsilonX;
            float ez = k_BoundaryEpsilonZ;
            var rays = new TestRay[resolutions.Length];
            for (int i = 0; i < resolutions.Length; i++)
            {
                float extent = (resolutions[i] - 1) * heightmapScale.x;
                rays[i] = RayDown(offsetsX[i] + extent * 0.5f + ex, extent * 0.5f + ez);
            }
            var results = TraceRays(rays);

            for (int i = 0; i < resolutions.Length; i++)
            {
                Assert.AreEqual(1u, results[i].isValid, $"Ray {i} should hit terrain {resolutions[i]}-res.");
                Assert.AreEqual(heights[i], results[i].worldPosition.y, 1.0f,
                    $"Terrain {resolutions[i]}-res should be at y={heights[i]}.");
            }
        }

        [Test]
        public void TraceRayDownToFlatTerrain_SmallSliceInLargeAtlas_NormalAtEdgesAndCornersIsCorrect(
            [Values(TerrainMode.Mesh, TerrainMode.Procedural)] TerrainMode terrainMode)
        {
            // A flat terrain placed inside an atlas slot that is larger than the terrain itself.
            // Rays land just inside every corner and edge midpoint (8 in total). Sobel normal
            // sampling reaches one atlas-texel beyond the hit position; with a tight inset those
            // taps fall on or past the boundary between real terrain data and the zero-padded
            // region. If the implementation samples the padding, the normal will tilt away from
            // up. With correct sampling the normal must remain ≈ (0, 1, 0) everywhere.
            int resLarge = 257;
            int resSmall = 33;
            float3 heightmapScaleLarge = new float3(1.0f, 100.0f, 1.0f);
            float3 heightmapScaleSmall = new float3(1.0f, 100.0f, 1.0f);
            float terrainHeight = 50.0f;

            float extentLarge = (resLarge - 1) * heightmapScaleLarge.x;
            float3 offsetSmall = new float3(extentLarge + 20.0f, 0, 0);

            // Add the large terrain first to force the atlas to its size; the small terrain
            // ends up padded into the upper-left of its slot.
            AddTerrain(terrainMode, CreateFlatHeightmap(resLarge, 5.0f, heightmapScaleLarge.y),
                resLarge, heightmapScaleLarge, Matrix4x4.identity);
            AddTerrain(terrainMode, CreateFlatHeightmap(resSmall, terrainHeight, heightmapScaleSmall.y),
                resSmall, heightmapScaleSmall, Matrix4x4.Translate(offsetSmall));

            float extentSmall = (resSmall - 1) * heightmapScaleSmall.x; // = 32 m

            // Tight inset so the Sobel taps reach right up to the terrain boundary.
            float inset = 0.001f;
            // Tiny but different X and Z epsilons keep midpoint rays off cell/tile/AABB
            // boundaries and break the in-cell diagonal (x.frac == z.frac) symmetry.
            // Smaller than `inset` so upper-edge rays stay inside the terrain extent.
            float ex = 0.00013f;
            float ez = 0.00027f;
            float ox = offsetSmall.x;
            float oz = offsetSmall.z;
            float lo = inset;
            float hi = extentSmall - inset;
            float mid = extentSmall * 0.5f;

            string[] labels = {
                "BL corner", "BR corner", "TL corner", "TR corner",
                "L edge", "R edge", "B edge", "T edge",
            };
            var rays = new TestRay[]
            {
                RayDown(ox + lo  + ex, oz + lo  + ez),            // 0: bottom-left corner
                RayDown(ox + hi  + ex, oz + lo  + ez),            // 1: bottom-right corner
                RayDown(ox + lo  + ex, oz + hi  + ez),            // 2: top-left corner
                RayDown(ox + hi  + ex, oz + hi  + ez),            // 3: top-right corner
                RayDown(ox + lo  + ex, oz + mid + ez),            // 4: left edge midpoint
                RayDown(ox + hi  + ex, oz + mid + ez),            // 5: right edge midpoint
                RayDown(ox + mid + ex, oz + lo  + ez),            // 6: bottom edge midpoint
                RayDown(ox + mid + ex, oz + hi  + ez),            // 7: top edge midpoint
            };
            var results = TraceRays(rays);

            for (int i = 0; i < results.Length; i++)
            {
                Assert.AreEqual(1u, results[i].isValid, $"{labels[i]} ray ({terrainMode}) should hit the terrain.");
                Assert.AreEqual(0f, results[i].worldNormal.x, 0.05f,
                    $"{labels[i]} normal X should be ~0 (was {results[i].worldNormal.x:F3}); padded region likely sampled.");
                Assert.AreEqual(1f, results[i].worldNormal.y, 0.05f,
                    $"{labels[i]} normal Y should be ~1 (was {results[i].worldNormal.y:F3}); padded region likely sampled.");
                Assert.AreEqual(0f, results[i].worldNormal.z, 0.05f,
                    $"{labels[i]} normal Z should be ~0 (was {results[i].worldNormal.z:F3}); padded region likely sampled.");
            }
        }

        [Test]
        public void TraceRayDownToHalfCylinder_SmallSliceInLargeAtlas_ReturnsCorrectNormals(
            [Values(TerrainMode.Mesh, TerrainMode.Procedural)] TerrainMode terrainMode)
        {
            // Sobel normal sampling uses invAtlasWidthInTexels for the texel offset eps. If the
            // small terrain's eps still used its own resolution, Sobel taps would land at the
            // wrong texels in the padded slot and the normal would be wrong.
            // Setup: a large flat terrain first (forces a large atlas), then a small half-cylinder
            // terrain whose normals we assert.
            int resLarge = 257;
            int resSmall = 65;
            float radius = 32.0f;
            float3 heightmapScaleLarge = new float3(1.0f, 100.0f, 1.0f);
            float3 heightmapScaleSmall = new float3(2.0f * radius / (resSmall - 1), radius, 2.0f * radius / (resSmall - 1));

            float extentLarge = (resLarge - 1) * heightmapScaleLarge.x;
            float3 offsetSmall = new float3(extentLarge + 20.0f, 0, 0);

            AddTerrain(terrainMode, CreateFlatHeightmap(resLarge, 5.0f, heightmapScaleLarge.y),
                resLarge, heightmapScaleLarge, Matrix4x4.identity);
            AddTerrain(terrainMode, CreateHalfCylinderHeightmap(resSmall, radius, heightmapScaleSmall.y),
                resSmall, heightmapScaleSmall, Matrix4x4.Translate(offsetSmall));

            float extentSmall = (resSmall - 1) * heightmapScaleSmall.x; // = 2 * radius
            float midZ = extentSmall * 0.5f + k_BoundaryEpsilonZ;
            float ex = k_BoundaryEpsilonX;

            var results = TraceRays(new TestRay[]
            {
                RayDown(offsetSmall.x + extentSmall * 0.5f + ex, midZ, radius + 50),        // 0: cylinder center → normal up
                RayDown(offsetSmall.x + radius - radius / math.sqrt(2f) + ex, midZ, radius + 50), // 1: 45° slope
            });

            Assert.AreEqual(1u, results[0].isValid, "Center ray should hit small half-cylinder in padded slot.");
            Assert.AreEqual(1u, results[1].isValid, "45-degree ray should hit small half-cylinder in padded slot.");

            float3 up = new float3(0, 1, 0);
            float dotCenter = math.dot(results[0].worldNormal, up);
            float dot45 = math.dot(results[1].worldNormal, up);

            Assert.AreEqual(1f, dotCenter, 0.15f, $"Padded-slot center dot(normal, up) should be ~1, was {dotCenter:F3}.");
            float expected45Dot = 1f / math.sqrt(2f);
            Assert.AreEqual(expected45Dot, dot45, 0.15f, $"Padded-slot 45 dot(normal, up) should be ~{expected45Dot:F3}, was {dot45:F3}.");
        }
    }
}
