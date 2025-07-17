using NUnit.Framework;
using System;
using UnityEditor;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;

namespace UnityEngine.Rendering.UnifiedRayTracing.Tests
{

    [TestFixture("Compute")]
    [TestFixture("Hardware")]
    internal class AccelStructAdapterTests
    {
        readonly RayTracingBackend m_Backend;
        RayTracingContext m_Context;
        AccelStructAdapter m_AccelStruct;
        IRayTracingShader m_Shader;

        public AccelStructAdapterTests(string backendAsString)
        {
            m_Backend = Enum.Parse<RayTracingBackend>(backendAsString);
        }

        [SetUp]
        public void SetUp()
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

            CreateRayTracingResources();
        }

        [TearDown]
        public void TearDown()
        {
            DisposeRayTracingResources();
        }

        void RayTraceAndCheckUVs(Mesh mesh, float2 expected, int uvChannel, float tolerance = 0.01f)
        {
            const int instanceCount = 4;
            CreateMatchingRaysAndInstanceDescs(instanceCount, mesh, out RayWithFlags[] rays, out MeshInstanceDesc[] instanceDescs);

            for (int i = 0; i < instanceCount; ++i)
            {
                m_AccelStruct.AddInstance(i, instanceDescs[i].mesh, instanceDescs[i].localToWorldMatrix, new uint[]{ 0xFFFFFFFF }, new uint[]{ 0xFFFFFFFF }, new bool[] { true }, 1);
            }

            HitGeomAttributes[] hitAttributes = null;
            var hits = TraceRays(rays, out hitAttributes);
            for (int i = 0; i < rays.Length; ++i)
            {
                Assert.IsTrue(hits[i].Valid(), "Expected ray to hit the mesh.");
                float2 uv = uvChannel == 1 ? hitAttributes[i].uv1 : hitAttributes[i].uv0;
                Assert.AreEqual(expected.x, uv.x, tolerance, $"Expected x (from uv{uvChannel}) to be fetched correctly in the ray tracing shader.");
                Assert.AreEqual(expected.y, uv.y, tolerance, $"Expected y (from uv{uvChannel}) to be fetched correctly in the ray tracing shader.");
            }
        }

        [Test]
        [TestCase(0)]
        [TestCase(1)]
        public void GeometryPool_MeshWithTwoWideUVs_UVsAreFetchedCorrectly(int uvChannel)
        {
            Mesh mesh = MeshUtil.CreateSingleTriangleMesh(new float2(1.5f, 1.5f), new float3(-0.5f, -0.5f, 0.0f));
            float x = 100.2f;
            float y = -9.3f;
            var uvs = new Vector2[] { new Vector2(x, y), new Vector2(x, y), new Vector2(x, y) };
            // Here we use the Vector2 version for setting the UVs on the mesh
            mesh.SetUVs(uvChannel, uvs);
            RayTraceAndCheckUVs(mesh, new float2(x, y), uvChannel);
        }

        [Test]
        [TestCase(0)]
        [TestCase(1)]
        public void GeometryPool_MeshWithThreeWideUVs_UVsAreFetchedCorrectly(int uvChannel)
        {
            Mesh mesh = MeshUtil.CreateSingleTriangleMesh(new float2(1.5f, 1.5f), new float3(-0.5f, -0.5f, 0.0f));
            float x = 100.2f;
            float y = -9.3f;
            float z = 32.4f;
            var uvs = new Vector3[] { new Vector3(x, y, z), new Vector3(x, y, z), new Vector3(x, y, z) };
            // Here we use the Vector3 version for setting the UVs on the mesh
            mesh.SetUVs(uvChannel, uvs);
            RayTraceAndCheckUVs(mesh, new float2(x, y), uvChannel);
        }

        [Test]
        [TestCase(0)]
        [TestCase(1)]
        public void GeometryPool_MeshWithFourWideUVs_UVsAreFetchedCorrectly(int uvChannel)
        {
            Mesh mesh = MeshUtil.CreateSingleTriangleMesh(new float2(1.5f, 1.5f), new float3(-0.5f, -0.5f, 0.0f));
            float x = 100.2f;
            float y = -9.3f;
            float z = 32.4f;
            float w = -12.5f;
            var uvs = new Vector4[] { new Vector4(x, y, z, w), new Vector4(x, y, z, w), new Vector4(x, y, z, w) };
            // Here we use the Vector4 version for setting the UVs on the mesh
            mesh.SetUVs(uvChannel, uvs);
            RayTraceAndCheckUVs(mesh, new float2(x, y), uvChannel);
        }

        [Test]
        [TestCase(0)]
        [TestCase(1)]
        public void GeometryPool_MeshWithLargeUVValues_UVsAreFetchedCorrectly(int uvChannel)
        {
            Mesh mesh = MeshUtil.CreateSingleTriangleMesh(new float2(1.5f, 1.5f), new float3(-0.5f, -0.5f, 0.0f));
            float x = 100000.2f;
            float y = -900000.3f;
            float z = 32000.4f;
            float w = -1200000.5f;
            var uvs = new Vector4[] { new Vector4(x, y, z, w), new Vector4(x, y, z, w), new Vector4(x, y, z, w) };
            // Here we use the Vector4 version for setting the UVs on the mesh
            mesh.SetUVs(uvChannel, uvs);
            RayTraceAndCheckUVs(mesh, new float2(x, y), uvChannel, 0.2f);
        }

        [Test]
        [TestCase(0)]
        [TestCase(1)]
        public void GeometryPool_MeshWithDifferentVertexUVs_UVsAreInterpolatedCorrectly(int uvChannel)
        {
            Mesh mesh = MeshUtil.CreateSingleTriangleMesh(new float2(1.5f, 1.5f), new float3(-0.5f, -0.5f, 0.0f));
            float x = 1.0f;
            float y = 5.0f;
            float z = 7.0f;
            var uvs = new Vector4[] { new Vector4(x, x, x, x), new Vector4(y, y, y, y), new Vector4(z, z, z, z) };
            // Here we use the Vector4 version for setting the UVs on the mesh
            mesh.SetUVs(uvChannel, uvs);
            RayTraceAndCheckUVs(mesh, new float2(4.333f, 4.333f), uvChannel, 0.001f);
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

        Hit[] TraceRays(RayWithFlags[] rays, out HitGeomAttributes[] hitAttributes)
        {
            var bufferTarget = GraphicsBuffer.Target.Structured;
            var rayCount = rays.Length;
            using var raysBuffer = new GraphicsBuffer(bufferTarget, rayCount, Marshal.SizeOf<RayWithFlags>());
            raysBuffer.SetData(rays);
            using var hitsBuffer = new GraphicsBuffer(bufferTarget, rayCount, Marshal.SizeOf<Hit>());
            using var attributesBuffer = new GraphicsBuffer(bufferTarget, rayCount, Marshal.SizeOf<HitGeomAttributes>());

            var scratchBuffer = RayTracingHelper.CreateScratchBufferForBuildAndDispatch(m_AccelStruct.GetAccelerationStructure(), m_Shader, (uint)rayCount, 1, 1);

            var cmd = new CommandBuffer();
            m_AccelStruct.Build(cmd, ref scratchBuffer);
            m_AccelStruct.Bind(cmd, "_AccelStruct", m_Shader);
            m_Shader.SetBufferParam(cmd, Shader.PropertyToID("_Rays"), raysBuffer);
            m_Shader.SetBufferParam(cmd, Shader.PropertyToID("_Hits"), hitsBuffer);
            m_Shader.SetBufferParam(cmd, Shader.PropertyToID("_HitAttributes"), attributesBuffer);
            m_Shader.Dispatch(cmd, scratchBuffer, (uint)rayCount, 1, 1);
            Graphics.ExecuteCommandBuffer(cmd);

            var hits = new Hit[rayCount];
            hitsBuffer.GetData(hits);

            hitAttributes = new HitGeomAttributes[rayCount];
            attributesBuffer.GetData(hitAttributes);

            scratchBuffer?.Dispose();

            return hits;
        }

        void CreateRayTracingResources()
        {
            var resources = new RayTracingResources();
            resources.Load();

            m_Context = new RayTracingContext(m_Backend, resources);
            m_AccelStruct = new AccelStructAdapter(m_Context.CreateAccelerationStructure(new AccelerationStructureOptions()), resources);
            m_Shader = m_Context.LoadRayTracingShader("Packages/com.unity.render-pipelines.core/Tests/Editor/UnifiedRayTracing/TraceRaysAndFetchAttributes.urtshader");
        }

        void DisposeRayTracingResources()
        {
            m_AccelStruct?.Dispose();
            m_Context?.Dispose();
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

        [System.Flags]
        enum RayCulling { None = 0, CullFrontFace = 0x10, CullBackFace = 0x20 }

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

        [StructLayout(LayoutKind.Sequential)]
        public struct HitGeomAttributes
        {
            public float3 position;
            public float3 normal;
            public float3 faceNormal;
            public float2 uv0;
            public float2 uv1;
        }
    }
}
