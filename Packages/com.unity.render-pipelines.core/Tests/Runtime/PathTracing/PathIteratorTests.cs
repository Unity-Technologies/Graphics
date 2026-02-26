// These tests relies on RayTracingResources.Load() so cannot run in a player.
#if UNITY_EDITOR

using System.Runtime.InteropServices;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine.PathTracing.Core;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Sampling;
using UnityEngine.Rendering.UnifiedRayTracing;

namespace UnityEngine.PathTracing.Tests
{
    internal class PathIteratorTests
    {
        private static class MeshUtil
        {
            internal static Mesh CreateQuadMesh()
            {
                Mesh mesh = new Mesh();

                Vector3[] vertices =
                {
                    new(-0.5f, -0.5f, 0.0f),
                    new(0.5f, -0.5f, 0.0f),
                    new(-0.5f, 0.5f, 0.0f),
                    new(0.5f, 0.5f, 0.0f)
                };
                mesh.vertices = vertices;

                Vector3[] normals =
                {
                    Vector3.forward,
                    Vector3.forward,
                    Vector3.forward,
                    Vector3.forward
                };
                mesh.normals = normals;

                Vector2[] uv =
                {
                    new(0, 0),
                    new(1, 0),
                    new(0, 1),
                    new(1, 1)
                };
                mesh.uv = uv;

                int[] tris =
                {
                    0, 2, 1,
                    2, 3, 1
                };
                mesh.triangles = tris;

                return mesh;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct TestRay
        {
            public float3 Origin;
            public float3 Direction;
        }

        private World _world;
        private RayTracingContext _rayTracingContext;
        private CommandBuffer _cmd;
        private SamplingResources _samplingResources;
        private RTHandle _emptyExposureTexture;

        private const RayTracingBackend _backend = RayTracingBackend.Compute;

        [SetUp]
        public void Setup()
        {
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLCore ||
                SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3)
            {
                Assert.Ignore("PathTracingCore is incompatible with OpenGL as it places a harsh limitation of buffer count.");
                return;
            }

            var resources = new RayTracingResources();
            resources.Load();
            _rayTracingContext = new RayTracingContext(_backend, resources);

            _cmd = new CommandBuffer();
            _cmd.name = "PathTracingCore/PathIteratorTests";

            var worldResources = new WorldResourceSet();
            worldResources.LoadFromAssetDatabase();
            _world = new World();
            _world.Init(_rayTracingContext, worldResources);

            _samplingResources = new SamplingResources();
            _samplingResources.Load((uint)SamplingResources.ResourceType.All);

            _emptyExposureTexture = RTHandles.Alloc(1, 1, enableRandomWrite: true, name: "Empty EV100 Exposure");
        }

        [TearDown]
        public void TearDown()
        {
            _world?.Dispose();
            _rayTracingContext?.Dispose();
            _cmd?.Dispose();
            _emptyExposureTexture?.Release();

            _samplingResources?.Dispose();
        }

        [Test]
        [TestCase(1, 1, 1, 1u)]
        [TestCase(10, 20, 30, 1u)]
        [TestCase(10, 20, 30, 8u)]
        [TestCase(0, 42, 0, 1u)]
        [TestCase(0, 42, 0, 8u)]
        [Ignore("Unstable: https://jira.unity3d.com/browse/UUM-134752")]
        public void EmptyWorldWithEnvironmentLight_ShouldOutputEnvironmentLight(float envRed, float envGreen, float envBlue, uint sampleCount)
        {
            using var deviceInputBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 1, Marshal.SizeOf<TestRay>());
            {
                var inputHost = new Ray[1];
                inputHost[0].origin = float3.zero;
                inputHost[0].direction = new float3(0, 1, 0);
                deviceInputBuffer.SetData(inputHost);
            }
            using var deviceOutputBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 1, sizeof(float) * 3);

            var cubemapMat = new Material(Shader.Find("PathIteratorTesting/UniformCubemap"));
            cubemapMat.SetVector("_Radiance", new Vector4(envRed, envGreen, envBlue, 1.0f));
            _world.SetEnvironmentMaterial(cubemapMat);
            GraphicsBuffer buildScratchBuffer = null;
            _world.Build(new Bounds(), _cmd, ref buildScratchBuffer, _samplingResources, true, 8);
            var shader = _rayTracingContext.LoadRayTracingShader("Packages/com.unity.render-pipelines.core/Tests/Runtime/PathTracing/PathIteratorTest.urtshader");

            Util.BindWorld(_cmd, shader, _world);
            Util.BindPathTracingInputs(_cmd, shader, false, 1, false, 4, 1.0f, RenderedGameObjectsFilter.OnlyStatic, _samplingResources, _emptyExposureTexture);

            shader.SetIntParam(_cmd, Shader.PropertyToID("g_SampleCount"), (int)sampleCount);
            shader.SetBufferParam(_cmd, Shader.PropertyToID("_Output"), deviceOutputBuffer);
            shader.SetBufferParam(_cmd, Shader.PropertyToID("_InputRay"), deviceInputBuffer);

            GraphicsBuffer traceScratchBuffer = null;
            RayTracingHelper.ResizeScratchBufferForTrace(shader, 1, 1, 1, ref traceScratchBuffer);
            shader.Dispatch(_cmd, traceScratchBuffer, 1, 1, 1);

            Graphics.ExecuteCommandBuffer(_cmd);

            var hostOutputBuffer = new float3[1];
            deviceOutputBuffer.GetData(hostOutputBuffer);

            const float tolerance = 0.001f;
            Assert.AreEqual(envRed, hostOutputBuffer[0].x, tolerance);
            Assert.AreEqual(envGreen, hostOutputBuffer[0].y, tolerance);
            Assert.AreEqual(envBlue, hostOutputBuffer[0].z, tolerance);

            CoreUtils.Destroy(cubemapMat);
            buildScratchBuffer.Dispose();
            traceScratchBuffer.Dispose();
        }

        [Test]
        [TestCase(1, 0, 0, 1u)]
        [TestCase(0, 1, 0, 1u)]
        [TestCase(0, 0, 1, 1u)]
        [TestCase(0, 0, 1, 8u)]
        public void RayHittingPlaneLitByWhiteEnvironmentLight_ShouldMatchAnalyticDerivation(float albedoRed, float albedoGreen, float albedoBlue, uint sampleCount)
        {
            Assert.Ignore("Fails on MacEditor ARM64, Windows x64 and other platforms, probably due to it looking for output in the wrong place. See https://jira.unity3d.com/browse/GFXLIGHT-1796");
            using var deviceInputBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 1, Marshal.SizeOf<TestRay>());
            {
                var hostInputBuffer = new Ray[1];
                hostInputBuffer[0].origin = new float3(0.1f, 0.1f, -1); // We avoid numerical precision issues at the diagonal.
                hostInputBuffer[0].direction = new float3(0, 0, 1);
                deviceInputBuffer.SetData(hostInputBuffer);
            }
            using var deviceOutputBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 1, sizeof(float) * 3);

            var cubemapMaterial = new Material(Shader.Find("PathIteratorTesting/UniformCubemap"));
            cubemapMaterial.SetVector("_Radiance", new Vector4(1.0f, 1.0f, 1.0f, 1.0f));
            GraphicsBuffer buildScratchBuffer = null;

            var mesh = MeshUtil.CreateQuadMesh();
            var material = new Material(Shader.Find("PathIteratorTesting/UniformAlbedoMetaPass"));
            material.SetColor("_Albedo", new Color(albedoRed, albedoGreen, albedoBlue, 1));

            var matDesc = MaterialPool.ConvertUnityMaterialToMaterialDescriptor(material);

            _world.SetEnvironmentMaterial(cubemapMaterial);
            var matHandle = _world.AddMaterial(matDesc, UVChannel.UV0);

            var mask = World.GetInstanceMask(ShadowCastingMode.On, true, RenderedGameObjectsFilter.OnlyStatic);

            _world.AddInstance(
                mesh,
                new [] { matHandle },
                new[] { mask },
                1,
                Matrix4x4.identity,
                new Bounds(Vector3.zero, Vector3.one * 20.0f),
                true,
                RenderedGameObjectsFilter.OnlyStatic,
                true);
            _world.Build(new Bounds(), _cmd, ref buildScratchBuffer, _samplingResources, true, 8);

            var shader = _rayTracingContext.LoadRayTracingShader("Packages/com.unity.render-pipelines.core/Tests/Runtime/PathTracing/PathIteratorTest.urtshader");

            Util.BindWorld(_cmd, shader, _world);
            Util.BindPathTracingInputs(_cmd, shader, false, 1, false, 4, 1.0f, RenderedGameObjectsFilter.OnlyStatic, _samplingResources, _emptyExposureTexture);

            shader.SetIntParam(_cmd, Shader.PropertyToID("g_SampleCount"), (int)sampleCount);
            shader.SetBufferParam(_cmd, Shader.PropertyToID("_Output"), deviceOutputBuffer);
            shader.SetBufferParam(_cmd, Shader.PropertyToID("_InputRay"), deviceInputBuffer);

            GraphicsBuffer traceScratchBuffer = null;
            RayTracingHelper.ResizeScratchBufferForTrace(shader, 1, 1, 1, ref traceScratchBuffer);
            shader.Dispatch(_cmd, traceScratchBuffer, 1, 1, 1);

            Graphics.ExecuteCommandBuffer(_cmd);

            var outputHost = new float3[1];
            deviceOutputBuffer.GetData(outputHost);

            // We have a non-emissive lambertian plane with albedo c which receives light from a uniform environment light with radiance 1. How much light is reflected in the direction of the normal of the plane?
            // The incoming light L_i is 1 and the BRDF is c/π.
            // L_o = ∫ c/π L_i(ω) cos(θ) dω = c/π ∫ cos(θ) dω = c.
            // The last equality holds because the hemispherical integral of cos(θ) is π.
            Assert.AreEqual(albedoRed, outputHost[0].x);
            Assert.AreEqual(albedoGreen, outputHost[0].y);
            Assert.AreEqual(albedoBlue, outputHost[0].z);

            CoreUtils.Destroy(cubemapMaterial);
            CoreUtils.Destroy(material);
            buildScratchBuffer.Dispose();
            traceScratchBuffer.Dispose();
        }
    }
}
#endif
