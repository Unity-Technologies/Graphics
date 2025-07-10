using System;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace UnityEngine.Rendering.UnifiedRayTracing.Tests
{
    [TestFixture("Compute")]
    [TestFixture("Hardware")]
    internal class InvalidInputsTests
    {
        readonly RayTracingBackend m_Backend;
        RayTracingContext m_Context;
        RayTracingResources m_Resources;
        IRayTracingAccelStruct m_AccelStruct;
        IRayTracingShader m_Shader;

        [Flags]
        enum MeshField { Positions=1, Normals=2, Uvs=4, Indices=8 }

        static Mesh CreateSingleTriangleMesh(MeshField fields)
        {
            Mesh mesh = new Mesh();

            if ((fields & MeshField.Positions) != 0)
            {
                Vector3[] vertices = new Vector3[]
                {
                    new Vector3(1.0f, 0.0f, 0),
                    new Vector3(0.0f, 0.0f, 0),
                    new Vector3(0.0f, 0.0f, 0)
                };
                mesh.vertices = vertices;
            }


            if ((fields & MeshField.Normals) != 0)
            {
                Vector3[] normals = new Vector3[]
                {
                    -Vector3.forward,
                    -Vector3.forward,
                    -Vector3.forward
                };
                mesh.normals = normals;
            }

            if ((fields & MeshField.Uvs) != 0)
            {
                Vector2[] uv = new Vector2[]
                {
                    new Vector2(0, 1),
                    new Vector2(1, 1),
                    new Vector2(0, 0)
                };
                mesh.uv = uv;
            }

            if ((fields & MeshField.Indices) != 0)
            {
                int[] tris = new int[3]
                {
                    0, 2, 1
                };
                mesh.triangles = tris;
            }

            return mesh;
        }

        public InvalidInputsTests(string backendAsString)
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

            m_Resources = new RayTracingResources();
            m_Resources.Load();

            m_Context = new RayTracingContext(m_Backend, m_Resources);
            m_AccelStruct = m_Context.CreateAccelerationStructure(new AccelerationStructureOptions() {  useCPUBuild = false  });
            m_Shader = m_Context.LoadRayTracingShader("Packages/com.unity.render-pipelines.core/Tests/Editor/UnifiedRayTracing/TraceRays.urtshader");
        }

        [TearDown]
        public void TearDown()
        {
            m_AccelStruct?.Dispose();
            m_Context?.Dispose();
        }


        [Test]
        public void AccelStruct_AddInstance_ThrowOnNullMesh()
        {
            var instanceDesc = new MeshInstanceDesc(null);
            Assert.Throws<ArgumentNullException>(() => m_AccelStruct.AddInstance(instanceDesc));
        }

        [Test]
        public void AccelStruct_AddInstance_ThrowOnMeshWithNoPositions()
        {
            var mesh = new Mesh();
            var instanceDesc = new MeshInstanceDesc(mesh);
            Assert.Throws<ArgumentException>(() => m_AccelStruct.AddInstance(instanceDesc));
        }

        [Test]
        public void AccelStruct_AddInstance_ThrowOnInvalidSubmeshIndex()
        {
            var mesh = CreateSingleTriangleMesh(MeshField.Positions | MeshField.Indices | MeshField.Normals | MeshField.Uvs);
            var instanceDesc = new MeshInstanceDesc(mesh);
            instanceDesc.subMeshIndex = -1;
            Assert.Throws<ArgumentOutOfRangeException>(() => m_AccelStruct.AddInstance(instanceDesc));
        }

        [Test]
        public void AccelStruct_AddInstance_ThrowOnInvalidInstanceHandle()
        {
            var mesh = CreateSingleTriangleMesh(MeshField.Positions | MeshField.Indices | MeshField.Normals | MeshField.Uvs);
            var instanceDesc = new MeshInstanceDesc(mesh);
            var handle = m_AccelStruct.AddInstance(instanceDesc);
            Assert.Throws<ArgumentException>(() => m_AccelStruct.RemoveInstance(handle + 1));

            m_AccelStruct.ClearInstances();
            Assert.Throws<ArgumentException>(() => m_AccelStruct.RemoveInstance(handle));
        }

        [Test]
        public void RayTracingShader_SetFloatParam_ThrowOnNullCmdBuffer()
        {
            Assert.Throws<ArgumentNullException>(() => m_Shader.SetFloatParam(null, 0, 1.0f));
        }

        [Test]
        public void RayTracingShader_Dispatch_ThrowOnSmallScratchBuffer()
        {
            if (m_Backend == RayTracingBackend.Hardware)
            {
                Assert.Ignore("scratch buffer is lawfully null with hardware backend");
                return;
            }

            using GraphicsBuffer scratch = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 10, 4);
            using CommandBuffer cmd = new CommandBuffer();
            var except = Assert.Throws<ArgumentException>(() => m_Shader.Dispatch(cmd, scratch, 20, 20, 20));
            Assert.That(except.Message, Does.Contain("scratch"));
        }

        [Test]
        public void RayTracingShader_Dispatch_ThrowOnScratchBufferWithInvalidTarget()
        {
            if (m_Backend == RayTracingBackend.Hardware)
            {
                Assert.Ignore("scratch buffer is lawfully null with hardware backend");
                return;
            }

            using GraphicsBuffer scratch = new GraphicsBuffer(GraphicsBuffer.Target.Raw, 20*20*20*100, 4);
            using CommandBuffer cmd = new CommandBuffer();
            var except = Assert.Throws<ArgumentException>(() => m_Shader.Dispatch(cmd, scratch, 20, 20, 20));
            Assert.That(except.Message, Does.Contain("target"));
        }

    }
}

