using NUnit.Framework;
using System;
using UnityEditor;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.Rendering.RadeonRays;

namespace UnityEngine.Rendering.UnifiedRayTracing.Tests
{
    internal static class MeshUtil
    {
        static internal Mesh CreateSingleTriangleMesh(float2 scaling, float3 translation)
        {
            Mesh mesh = new Mesh();

            Vector3[] vertices = new Vector3[]
            {
                (Vector3)translation + new Vector3(0.0f, 0.0f, 0),
                (Vector3)translation + new Vector3(1.0f * scaling.x, 0.0f, 0),
                (Vector3)translation + new Vector3(0.0f, 1.0f * scaling.y, 0)
            };
            mesh.vertices = vertices;

            Vector3[] normals = new Vector3[]
            {
                -Vector3.forward,
                -Vector3.forward,
                -Vector3.forward
            };
            mesh.normals = normals;

            Vector2[] uv = new Vector2[]
            {
                new Vector2(0, 1),
                new Vector2(1, 1),
                new Vector2(0, 0)
            };
            mesh.uv = uv;

            int[] tris = new int[3]
            {
                0, 2, 1
            };
            mesh.triangles = tris;

            return mesh;
        }

        static internal Mesh CreateQuadMesh()
        {
            Mesh mesh = new Mesh();

            Vector3[] vertices = new Vector3[]
            {
                new Vector3(-0.5f, -0.5f, 0.0f),
                new Vector3(0.5f, -0.5f, 0.0f),
                new Vector3(-0.5f, 0.5f, 0.0f),
                new Vector3(0.5f, 0.5f, 0.0f)
            };
            mesh.vertices = vertices;

            Vector3[] normals = new Vector3[]
            {
                -Vector3.forward,
                -Vector3.forward,
                -Vector3.forward,
                -Vector3.forward
            };
            mesh.normals = normals;

            Vector2[] uv = new Vector2[]
            {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(0, 1),
                new Vector2(1, 1)
            };
            mesh.uv = uv;

            int[] tris = new int[6]
            {
                0, 2, 1,
                2, 3, 1
            };
            mesh.triangles = tris;

            return mesh;
        }
    }

    internal class ComputeRayTracingAccelStructTests
    {
        static private void AssertFloat3sAreEqual(float3 expected, float3 actual, float tolerance)
        {
            Assert.AreEqual(expected.x, actual.x, tolerance);
            Assert.AreEqual(expected.y, actual.y, tolerance);
            Assert.AreEqual(expected.z, actual.z, tolerance);
        }

        static private void AssertAABBsAreEqual(float3 expectedMin, float3 expectedMax, float3 actualMin, float3 actualMax, float tolerance)
        {
            AssertFloat3sAreEqual(expectedMin, actualMin, tolerance);
            AssertFloat3sAreEqual(expectedMax, actualMax, tolerance);
        }

        [Test]
        public void Build_TwoInstancesOfASingleTriangleMesh_ShouldGenerateCorrectResult()
        {
            var resources = new RayTracingResources();
            resources.Load();

            using var accelStruct = new ComputeRayTracingAccelStruct(
                new AccelerationStructureOptions() { buildFlags = BuildFlags.PreferFastBuild },
                resources,
                new ReferenceCounter());

            uint instanceCount = 2;

            {
                var mesh = MeshUtil.CreateSingleTriangleMesh(new float2(1.0f, 1.0f), float3.zero);
                var globalTranslation = new float3(1.0f, 1.0f, 0.0f);
                for (uint i = 0; i < instanceCount; i++)
                {
                    var instanceDesc = new MeshInstanceDesc(mesh);
                    instanceDesc.localToWorldMatrix = Matrix4x4.Translate(globalTranslation + new float3(2.0f * i, 0.0f, 0.0f));
                    accelStruct.AddInstance(instanceDesc);
                }

                using var scratchBuffer = RayTracingHelper.CreateScratchBufferForBuild(accelStruct);
                using var cmd = new CommandBuffer();
                accelStruct.Build(cmd, scratchBuffer);
                Graphics.ExecuteCommandBuffer(cmd);
                Object.DestroyImmediate(mesh);
            }

            var tolerance = 0.001f;
            {
                // Verify bottom level BVH.
                uint expectedTotalNodeCount = 1;
                var bottomLevelNodes = new BvhNode[(int)expectedTotalNodeCount + 1]; // plus one for header
                accelStruct.bottomLevelBvhBuffer.GetData(bottomLevelNodes);

                var header = UnsafeUtility.As<BvhNode, BvhHeader>(ref bottomLevelNodes[0]);
                Assert.AreEqual(expectedTotalNodeCount, header.internalNodeCount + header.leafNodeCount);
                Assert.AreEqual(1, header.leafNodeCount);
                Assert.AreEqual(expectedTotalNodeCount, header.internalNodeCount + header.leafNodeCount);
                AssertAABBsAreEqual(new float3(0.0f, 0.0f, 0.0f), new float3(1.0f, 1.0f, 0.0f), header.globalAabbMin, header.globalAabbMax, tolerance);
            }

            {
                // Verify top level BVH.
                uint expectedInternalNodeCount = instanceCount - 1;
                uint expectedLeafNodeCount = instanceCount;
                var topLevelNodes = new BvhNode[(int)expectedInternalNodeCount + 1]; // plus one for header
                accelStruct.topLevelBvhBuffer.GetData(topLevelNodes);

                var header = UnsafeUtility.As<BvhNode, BvhHeader>(ref topLevelNodes[0]);
                Assert.AreEqual(expectedInternalNodeCount, header.internalNodeCount);
                Assert.AreEqual(expectedLeafNodeCount, header.leafNodeCount);

                AssertAABBsAreEqual(new float3(1.0f, 1.0f, 0.0f), new float3(4.0f, 2.0f, 0.0f), header.globalAabbMin, header.globalAabbMax, tolerance);

                var instanceBvhRoot = topLevelNodes[1];
                Assert.AreEqual(0u | (1u << 31), instanceBvhRoot.child0); // MSB is set for leaf node indices
                Assert.AreEqual(1u | (1u << 31), instanceBvhRoot.child1);
                AssertAABBsAreEqual(new float3(1.0f, 1.0f, 0.0f), new float3(2.0f, 2.0f, 0.0f), instanceBvhRoot.aabb0_min, instanceBvhRoot.aabb0_max, tolerance);
                AssertAABBsAreEqual(new float3(3.0f, 1.0f, 0.0f), new float3(4.0f, 2.0f, 0.0f), instanceBvhRoot.aabb1_min, instanceBvhRoot.aabb1_max, tolerance);
            }
        }

        [Test]
        [Ignore("Test too unstable on Yamato (UUM-95662, UUM-67382)")]
        public void AddInstance_MeshWith2GBWorthOfVertices_Throws()
        {
            var resources = new RayTracingResources();
            resources.Load();

            using var accelStruct = new ComputeRayTracingAccelStruct(
                new AccelerationStructureOptions() { buildFlags = BuildFlags.PreferFastBuild },
                resources,
                new ReferenceCounter());

            Random.InitState(1987);

            int vertexCount = 200000001; // 200 millions
            int indexCount = vertexCount; // must be a multiple of 3

            Mesh mesh = new Mesh();
            mesh.indexFormat = IndexFormat.UInt32;

            Vector3[] vertices = new Vector3[vertexCount];
            for (int i = 0; i < vertexCount; ++i)
            {
                vertices[i] = new Vector3(0.0f, 0.0f, Random.Range(0.0f, 1.0f));
            }
            mesh.SetVertices(vertices);

            Vector3[] normals = new Vector3[vertexCount];
            mesh.SetNormals(normals);

            Vector2[] uv = new Vector2[vertexCount];
            mesh.SetUVs(0, uv);

            int[] tris = new int[indexCount];
            for (int i = 0; i < indexCount; ++i)
                tris[i] = i;

            mesh.SetIndices(tris, MeshTopology.Triangles, 0);

            MeshInstanceDesc instanceDesc = new MeshInstanceDesc(mesh);
            Assert.Throws<UnifiedRayTracingException>(() => accelStruct.AddInstance(instanceDesc));

            Assert.That(accelStruct.m_BlasAllocator.allocatedSize, Is.Zero);
            Assert.That(accelStruct.m_BlasLeavesAllocator.allocatedSize, Is.Zero);
        }
    }


    internal class AccelStructTestsBase
    {
        readonly protected RayTracingBackend m_Backend;
        protected RayTracingContext m_Context;
        protected RayTracingResources m_Resources;
        protected IRayTracingAccelStruct m_AccelStruct;
        protected IRayTracingShader m_Shader;

        public AccelStructTestsBase(string backendAsString)
        {
            m_Backend = Enum.Parse<RayTracingBackend>(backendAsString);
        }

        protected void CreateRayTracingResources(string shaderFilename)
        {
            if (!SystemInfo.supportsRayTracing && m_Backend == RayTracingBackend.Hardware)
            {
                Assert.Ignore("Cannot run test on this Graphics API. Hardware RayTracing is not supported");
            }

            if (!SystemInfo.supportsComputeShaders && m_Backend == RayTracingBackend.Compute)
            {
                Assert.Ignore("Cannot run test on this Graphics API. Compute shaders are not supported");
            }

            if (SystemInfo.graphicsDeviceName.Contains("llvmpipe"))
            {
                Assert.Ignore("Cannot run test on this device (Renderer: llvmpipe (LLVM 10.0.0, 128 bits)). Tests are disabled because they fail on some platforms (that do not support 11 SSBOs). Once we do not run Ubuntu 18.04 try removing this");
            }

            m_Resources = new RayTracingResources();
            m_Resources.Load();

            m_Context = new RayTracingContext(m_Backend, m_Resources);
            m_AccelStruct = m_Context.CreateAccelerationStructure(new AccelerationStructureOptions());
            m_Shader = m_Context.LoadRayTracingShader("Packages/com.unity.render-pipelines.core/Tests/Editor/UnifiedRayTracing/" + shaderFilename);
        }

        protected void DisposeRayTracingResources()
        {
            m_AccelStruct?.Dispose();
            m_Context?.Dispose();
        }

        protected THit[] TraceRays<THit>(RayWithFlags[] rays, AnyHitDecision anyHitDecision = AnyHitDecision.Invalid)
        {
            var bufferTarget = GraphicsBuffer.Target.Structured;
            var rayCount = rays.Length;
            using var raysBuffer = new GraphicsBuffer(bufferTarget, rayCount, Marshal.SizeOf<RayWithFlags>());
            raysBuffer.SetData(rays);
            using var hitsBuffer = new GraphicsBuffer(bufferTarget, rayCount, Marshal.SizeOf<THit>());

            using var scratchBuffer = RayTracingHelper.CreateScratchBufferForBuildAndDispatch(m_AccelStruct, m_Shader, (uint)rayCount, 1, 1);

            var cmd = new CommandBuffer();
            m_AccelStruct.Build(cmd, scratchBuffer);

            if (anyHitDecision != AnyHitDecision.Invalid)
                m_Shader.SetIntParam(cmd, Shader.PropertyToID("_AnyHitDecision"), (int)anyHitDecision);

            m_Shader.SetAccelerationStructure(cmd, "_AccelStruct", m_AccelStruct);
            m_Shader.SetBufferParam(cmd, Shader.PropertyToID("_Rays"), raysBuffer);
            m_Shader.SetBufferParam(cmd, Shader.PropertyToID("_Hits"), hitsBuffer);
            m_Shader.Dispatch(cmd, scratchBuffer, (uint)rayCount, 1, 1);
            Graphics.ExecuteCommandBuffer(cmd);

            var hits = new THit[rayCount];
            hitsBuffer.GetData(hits);

            return hits;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RayWithFlags
        {
            public float3 origin;
            public float minT;
            public float3 direction;
            public float maxT;
            public uint culling;
            public uint instanceMask;
            uint padding;
            uint padding2;

            public RayWithFlags(float3 origin, float3 direction)
            {
                this.origin = origin;
                this.direction = direction;
                minT = 0.0f;
                maxT = float.MaxValue;
                instanceMask = 0xFFFFFFFF;
                culling = 0;
                padding = 0;
                padding2 = 0;
            }
        }

        protected enum AnyHitDecision { Invalid = -1, IgnoreHit = 0, AcceptHit = 1, AcceptHitAndEndSearch = 2 };

        [System.Flags]
        protected enum RayFlags { None = 0, ForceOpaque = 0x01, ForceNonOpaque = 0x02, AcceptFirstHitAndEndSearch = 0x04, SkipClosestHit = 0x08, CullBackFace = 0x10, CullFrontFace = 0x20, CullOpaque = 0x40, CullNonOpaque = 0x80 }
    }


    [TestFixture("Compute")]
    [TestFixture("Hardware")]
    internal class AccelStructTests : AccelStructTestsBase
    {
        public AccelStructTests(string backendAsString) : base(backendAsString)
        {
        }

        [SetUp]
        public void SetUp()
        {
            CreateRayTracingResources("TraceRays.urtshader");
        }

        [TearDown]
        public void TearDown()
        {
            DisposeRayTracingResources();
        }

        [Test]
        public void RayTracePixelsInUnitQuad([Values(1, 10, 100)] int rayResolution, [Values(0, 1, 2, 3)] int buildFlagsAsInteger)
        {
            var buildFlags = (BuildFlags)buildFlagsAsInteger; // We do this ugly but simple cast hack because the BuildFlags type is not public as time of this writing (test methods must be public and so must their argument types).

            // re-create the acceleration structure with suitable options
            m_AccelStruct?.Dispose();
            var options = new AccelerationStructureOptions() { buildFlags = buildFlags };
            m_AccelStruct = m_Context.CreateAccelerationStructure(options);

            Mesh mesh = MeshUtil.CreateQuadMesh();

            var instanceDesc = new MeshInstanceDesc(mesh);
            instanceDesc.localToWorldMatrix = Matrix4x4.identity;
            instanceDesc.localToWorldMatrix.SetTRS(new Vector3(0.5f, 0.5f, 0.0f), Quaternion.identity, new Vector3(1.0f, 1.0f, 1.0f));
            instanceDesc.enableTriangleCulling = false;
            instanceDesc.frontTriangleCounterClockwise = true;
            m_AccelStruct.AddInstance(instanceDesc);

            // trace N*N rays towards the quad and expect to hit it
            int N = rayResolution;
            var rays = new RayWithFlags[N * N];
            int rayI = 0;
            for (int v = 0; v < N; ++v)
            {
                for (int u = 0; u < N; ++u)
                {
                    float2 uv = new float2((float)u, (float)v);
                    uv += 0.5f;
                    uv /= N;
                    float3 origin = new float3(uv.x, uv.y, 1.0f);
                    float3 direction = new float3(0.0f, 0.0f, -1.0f);
                    rays[rayI] = new RayWithFlags(origin, direction);
                    rays[rayI].culling = (uint)RayFlags.None;
                    rayI++;
                }
            }

            var hits = TraceRays<Hit>(rays);
            for (int i = 0; i < hits.Length; ++i)
            {
                Assert.IsTrue(hits[i].Valid(), $"Expected all rays to hit the quad but ray {i} missed.");
            }
        }

        [Test]
        public void FrontOrBackFaceCulling()
        {
            const int instanceCount = 4;
            Mesh mesh = MeshUtil.CreateSingleTriangleMesh(new float2(1.5f, 1.5f), new float3(-0.5f, -0.5f, 0.0f));
            CreateMatchingRaysAndInstanceDescs(instanceCount, mesh, out RayWithFlags[] rays, out MeshInstanceDesc[] instanceDescs);

            var raysDuplicated = new RayWithFlags[instanceCount * 3];
            Array.Copy(rays, 0, raysDuplicated, 0, instanceCount);
            Array.Copy(rays, 0, raysDuplicated, instanceCount, instanceCount);
            Array.Copy(rays, 0, raysDuplicated, 2 * instanceCount, instanceCount);

            for (int i = 0; i < instanceCount; ++i)
            {
                raysDuplicated[i].culling = (uint)RayFlags.None;
                raysDuplicated[i + instanceCount].culling = (uint)RayFlags.CullBackFace;
                raysDuplicated[i + instanceCount * 2].culling = (uint)RayFlags.CullFrontFace;
            }

            instanceDescs[0].enableTriangleCulling = false;
            instanceDescs[0].frontTriangleCounterClockwise = true;

            instanceDescs[1].enableTriangleCulling = false;
            instanceDescs[1].frontTriangleCounterClockwise = false;

            instanceDescs[2].enableTriangleCulling = true;
            instanceDescs[2].frontTriangleCounterClockwise = true;

            instanceDescs[3].enableTriangleCulling = true;
            instanceDescs[3].frontTriangleCounterClockwise = false;

            for (int i = 0; i < instanceCount; ++i)
            {
                m_AccelStruct.AddInstance(instanceDescs[i]);
            }

            var hits = TraceRays<Hit>(raysDuplicated);
            // No culling
            Assert.IsTrue(hits[0].Valid());
            Assert.IsTrue(hits[1].Valid());
            Assert.IsTrue(hits[2].Valid());
            Assert.IsTrue(hits[3].Valid());

            // FrontFace culling
            Assert.IsTrue(hits[4].Valid());
            Assert.IsTrue(hits[5].Valid());
            Assert.IsTrue(hits[6].Valid());
            Assert.IsTrue(!hits[7].Valid());

            // BackFace culling
            Assert.IsTrue(hits[8].Valid());
            Assert.IsTrue(hits[9].Valid());
            Assert.IsTrue(!hits[10].Valid());
            Assert.IsTrue(hits[11].Valid());


        }


        [Test]
        public void InstanceAndRayMask()
        {
            const int instanceCount = 8;
            Mesh mesh = MeshUtil.CreateSingleTriangleMesh(new float2(1.5f, 1.5f), new float3(-0.5f, -0.5f, 0.0f));
            CreateMatchingRaysAndInstanceDescs(instanceCount, mesh, out RayWithFlags[] rays, out MeshInstanceDesc[] instanceDescs);

            var rayAndInstanceMasks = new (uint instanceMask, uint rayMask)[]
            {
                (0, 0),
                (0xFFFFFFFF, 0xFFFFFFFF),
                (0, 0xFFFFFFFF),
                (0xFFFFFFFF, 0),
                (0x0F, 0x01),
                (0x0F, 0xF0),
                (0x90, 0xF0),
                (0xF0, 0x10),
            };

            for (int i = 0; i < instanceCount; ++i)
            {
                instanceDescs[i].mask = rayAndInstanceMasks[i].instanceMask;
                rays[i].instanceMask = rayAndInstanceMasks[i].rayMask;
            }

            for (int i = 0; i < instanceCount; ++i)
            {
                m_AccelStruct.AddInstance(instanceDescs[i]);
            }

            var hits = TraceRays<Hit>(rays);

            for (int i = 0; i < instanceCount; ++i)
            {
                bool rayShouldHit = ((rayAndInstanceMasks[i].instanceMask & rayAndInstanceMasks[i].rayMask) != 0);
                bool rayHit = hits[i].Valid();

                var message = String.Format("Ray {0} hit for InstanceMask: 0x{1:X} & RayMask: 0x{2:X}",
                    rayShouldHit ? "should" : "shouldn't",
                    rayAndInstanceMasks[i].instanceMask,
                    rayAndInstanceMasks[i].rayMask);

                Assert.AreEqual(rayShouldHit, rayHit, message);
            }
        }

        [Test]
        public void AddAndRemoveInstances()
        {
            const int instanceCount = 4;
            Mesh mesh = MeshUtil.CreateSingleTriangleMesh(new float2(1.5f, 1.5f), new float3(-0.5f, -0.5f, 0.0f));
            CreateMatchingRaysAndInstanceDescs(instanceCount, mesh, out RayWithFlags[] rays, out MeshInstanceDesc[] instanceDescs);

            var instanceHandles = new int[instanceCount];
            var expectedVisibleInstances = new bool[instanceCount];

            for (int i = 0; i < instanceCount; ++i)
            {
                instanceHandles[i] = m_AccelStruct.AddInstance(instanceDescs[i]);
                expectedVisibleInstances[i] = true;
            }

            CheckVisibleInstances(rays, expectedVisibleInstances);

            m_AccelStruct.RemoveInstance(instanceHandles[0]); expectedVisibleInstances[0] = false;
            m_AccelStruct.RemoveInstance(instanceHandles[2]); expectedVisibleInstances[2] = false;

            CheckVisibleInstances(rays, expectedVisibleInstances);

            m_AccelStruct.ClearInstances();

            Array.Fill(expectedVisibleInstances, false);

            CheckVisibleInstances(rays, expectedVisibleInstances);

            m_AccelStruct.AddInstance(instanceDescs[3]);
            expectedVisibleInstances[3] = true;

            CheckVisibleInstances(rays, expectedVisibleInstances);
        }

        private void AddTerrainToAccelerationStructure(int heightmapResolution)
        {
            Terrain.CreateTerrainGameObject(new TerrainData());
            Terrain terrain = GameObject.FindFirstObjectByType<Terrain>();
            Assert.NotNull(terrain);

            // Set terrain texture resolution on terrain data.
            terrain.terrainData.heightmapResolution = heightmapResolution;

            // Convert to mesh.
            AsyncTerrainToMeshRequest request = TerrainToMesh.ConvertAsync(terrain);
            request.WaitForCompletion();

            // Add the terrain to the acceleration structure.
            MeshInstanceDesc instanceDesc = new MeshInstanceDesc(request.GetMesh());
            instanceDesc.localToWorldMatrix = float4x4.identity;
            m_AccelStruct.AddInstance(instanceDesc);
        }

        [Test]
        public void Add_1KTerrain_Works()
        {
            AddTerrainToAccelerationStructure(1025);
        }

        [Test]
        [Ignore("This test is disabled because of the allocation limitation of 2 GB in GraphicsBuffer.")]
        public void Add_4KTerrain_Works()
        {
            AddTerrainToAccelerationStructure(4097);
        }

        void CheckVisibleInstances(RayWithFlags[] rays, bool[] expectedVisibleInstances)
        {
            var hits = TraceRays<Hit>(rays);
            for (int i = 0; i < rays.Length; ++i)
            {
                Assert.AreEqual(expectedVisibleInstances[i], hits[i].Valid(), $"Unexpected state of intersection with instance {i}");
            }
        }

        void CreateMatchingRaysAndInstanceDescs(uint instanceCount, Mesh mesh, out RayWithFlags[] rays, out MeshInstanceDesc[] instanceDescs)
        {
            instanceDescs = new MeshInstanceDesc[instanceCount];
            rays = new RayWithFlags[instanceCount];
            var ray = new RayWithFlags(new float3(0.0f, 0.0f, 1.0f), new float3(0.0f, 0.0f, -1.0f));
            float3 step = new float3(2.0f, 0.0f, 0.0f);

            for (int i = 0; i < instanceCount; ++i)
            {
                instanceDescs[i] = new MeshInstanceDesc(mesh);
                instanceDescs[i].localToWorldMatrix = float4x4.Translate(step * i);

                rays[i] = ray;
                rays[i].origin += step * i;
            }
        }


        [StructLayout(LayoutKind.Sequential)]
        public struct Hit
        {
            public uint instanceID;
            public uint primitiveIndex;
            public float2 uvBarycentrics;
            public float hitDistance;
            public uint isFrontFace;

            public bool Valid() { return instanceID != 0xFFFFFFFF; }
        }

    }

    [TestFixture("Compute")]
    [TestFixture("Hardware")]
    internal class AccelStructTransparencyTests : AccelStructTestsBase
    {
        public AccelStructTransparencyTests(string backendAsString) : base(backendAsString)
        {
        }

        [SetUp]
        public void SetUp()
        {
            CreateRayTracingResources("TraceTransparentRays.urtshader");
        }

        [TearDown]
        public void TearDown()
        {
            DisposeRayTracingResources();
        }


        [Test]
        public void WithTransparentInstances_ClosestAndAnyHitsFuncsAreCalled()
        {
            Mesh mesh = MeshUtil.CreateSingleTriangleMesh(new float2(1.5f, 1.5f), new float3(-0.5f, -0.5f, 0.0f));

            var isOpaque = new bool[] { false, false, true, false, false, false, true, false };
            for (int i = 0; i < isOpaque.Length; ++i)
            {
                var instanceDesc = new MeshInstanceDesc(mesh);
                instanceDesc.localToWorldMatrix = float4x4.Translate(new float3(0.0f, 0.0f, 2.0f) * i);
                instanceDesc.opaqueGeometry = isOpaque[i];
                instanceDesc.instanceID = (uint)i;

                m_AccelStruct.AddInstance(instanceDesc);
            }

            {
                var testCases = new TransparencyTestCase[] {
                    new(RayFlags.None,                                  2, new int[] { 0, 1 }),
                    new(RayFlags.ForceOpaque,                           0, new int[] { }),
                    new(RayFlags.ForceNonOpaque,                       -1, new int[] { 0, 1, 2, 3, 4, 5, 6, 7 }),
                    new(RayFlags.CullOpaque,                           -1, new int[] { 0, 1, 3, 4, 5, 7 }),
                    new(RayFlags.CullNonOpaque,                         2, new int[] { }),
                    new(RayFlags.CullOpaque | RayFlags.SkipClosestHit, -1, new int[] { 0, 1, 3, 4, 5, 7 }),
                    new(RayFlags.AcceptFirstHitAndEndSearch,            2, new int[] { 0, 1 }),
                    new(RayFlags.ForceNonOpaque | RayFlags.AcceptFirstHitAndEndSearch, -1, new int[] { 0, 1, 2, 3, 4, 5, 6, 7 }),
                };

                TestTransparentInstances(testCases, AnyHitDecision.IgnoreHit);
            }

            {
                var testCases = new TransparencyTestCase[] {
                    new(RayFlags.None,                                  0, new int[] { 0 }),
                    new(RayFlags.ForceOpaque,                           0, new int[] { }),
                    new(RayFlags.ForceNonOpaque,                        0, new int[] { 0 }),
                    new(RayFlags.CullOpaque,                            0, new int[] { 0 }),
                    new(RayFlags.CullNonOpaque,                         2, new int[] { }),
                    new(RayFlags.CullOpaque | RayFlags.SkipClosestHit, -1, new int[] { 0 }),
                    new(RayFlags.AcceptFirstHitAndEndSearch,            0, new int[] { 0 }),
                    new(RayFlags.ForceNonOpaque | RayFlags.AcceptFirstHitAndEndSearch, 0, new int[] { 0 }),
                };

                TestTransparentInstances(testCases, AnyHitDecision.AcceptHit);
                TestTransparentInstances(testCases, AnyHitDecision.AcceptHitAndEndSearch);
            }
        }

        void TestTransparentInstances(TransparencyTestCase[] testCases, AnyHitDecision anyHitDecision)
        {
            var expectedResults = new List<TransparentRayResult>();
            var rayWithFlags = new List<RayWithFlags>();

            foreach (var testCase in testCases)
            {
                var ray = new RayWithFlags(new float3(0.0f, 0.0f, -1.0f), new float3(0.0f, 0.0f, 1.0f));
                ray.culling = (uint)testCase.rayFlags;
                rayWithFlags.Add(ray);

                expectedResults.Add(testCase.expectedResult);
            }

            var hits = TraceRays<TransparentRayResult>(rayWithFlags.ToArray(), anyHitDecision);

            for (int i = 0; i < testCases.Length; ++i)
            {
                Assert.AreEqual(testCases[i].expectedResult.closestHit, hits[i].closestHit, $"Unexpected closestHit with RayFlags=[{(RayFlags)rayWithFlags[i].culling}] and AnyHitDecision=[{anyHitDecision}]");
                Assert.AreEqual(testCases[i].expectedResult.anyHits, hits[i].anyHits, $"Unexpected anyHits with RayFlags=[{(RayFlags)rayWithFlags[i].culling}] and AnyHitDecision=[{anyHitDecision}]");
            }
        }



        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct TransparentRayResult
        {
            public uint anyHits;
            public int closestHit;
        }

        struct TransparencyTestCase
        {
            public TransparencyTestCase(RayFlags flags, int expectedClosestHit, int[] expectedAnyHitInvocations)
            {
                rayFlags = flags;

                expectedResult.closestHit = expectedClosestHit;
                expectedResult.anyHits = 0;
                for (int i = 0; i < expectedAnyHitInvocations.Length; ++i)
                    expectedResult.anyHits |= (1u << expectedAnyHitInvocations[i]);
            }

            public RayFlags rayFlags;
            public TransparentRayResult expectedResult;
        }
    }
}
