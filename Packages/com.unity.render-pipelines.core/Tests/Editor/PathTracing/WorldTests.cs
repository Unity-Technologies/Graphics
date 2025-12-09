using System;
using System.Linq;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEngine.PathTracing.Core;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Sampling;
using UnityEngine.Rendering.UnifiedRayTracing;
using RayTracingBackend = UnityEngine.Rendering.UnifiedRayTracing.RayTracingBackend;

namespace UnityEngine.PathTracing.Tests
{
    using InstanceHandle = Handle<World.InstanceKey>;
    using MaterialHandle = Handle<MaterialPool.MaterialDescriptor>;

    [TestFixture("Compute")]
    [TestFixture("Hardware")]
    internal class WorldTests
    {
        private readonly RayTracingBackend _backend;
        private World _world;
        private bool _respectLightLayers;
        private bool _autoEstimateLUTRange;
        private GraphicsBuffer _buildScratchBuffer;
        private RayTracingContext _rayTracingContext;
        private Material _defaultMaterial;
        private CommandBuffer _cmd;
        private SamplingResources _samplingResources;

        public WorldTests(string backendAsString)
        {
            _backend = Enum.Parse<RayTracingBackend>(backendAsString);
        }

        [SetUp]
        public void Setup()
        {
            if (!SystemInfo.supportsRayTracing && _backend == RayTracingBackend.Hardware)
                Assert.Ignore("Cannot run test on this Graphics API. Hardware RayTracing is not supported");

            var resources = new RayTracingResources();
            resources.Load();
            _rayTracingContext = new RayTracingContext(_backend, resources);
            _defaultMaterial = UnityEditor.AssetDatabase.GetBuiltinExtraResource<Material>("Default-Material.mat");
            _respectLightLayers = false;
            _autoEstimateLUTRange = false;
            _cmd = new CommandBuffer();
            _cmd.name = "PathTracing.Tests Command Buffer";
            _world = new World();
            var worldResources = new WorldResourceSet();
            worldResources.LoadFromAssetDatabase();
            _world.Init(_rayTracingContext, worldResources);
            _world.AddMaterial(MaterialPool.ConvertUnityMaterialToMaterialDescriptor(_defaultMaterial), UVChannel.UV0);

            _samplingResources = new SamplingResources();
            _samplingResources.Load((uint)SamplingResources.ResourceType.All);
        }

        [TearDown]
        public void TearDown()
        {
            _world?.Dispose();
            _buildScratchBuffer?.Dispose();
            _rayTracingContext?.Dispose();
            _cmd?.Dispose();

            _samplingResources?.Dispose();
            // Null this buffer so nobody attempts to access properties of a disposed buffer
            _buildScratchBuffer = null;
        }

        private static World.LightDescriptor[] CreateLights(int lightCount)
        {
            var lights = new Light[lightCount];
            for (int i = 0; i < lightCount; ++i)
            {
                var lightGameObject = new GameObject($"Test Light {i}");
                var light = lightGameObject.AddComponent<Light>();
                lights[i] = light;
            }
            return Util.ConvertUnityLightsToLightDescriptors(lights, false);
        }

        [Test]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void World_AddLight_IncreaseLightCount(int lightCount)
        {
        #if UNITY_EDITOR_OSX
			if (System.Runtime.InteropServices.RuntimeInformation.OSArchitecture != System.Runtime.InteropServices.Architecture.Arm64)
				NUnit.Framework.Assert.Ignore("Fails on MacOS13 UUM-111386");
        #endif
            var lights = CreateLights(lightCount);
            _world.AddLights(lights, _respectLightLayers, _autoEstimateLUTRange, MixedLightingMode.IndirectOnly);
            _world.lightPickingMethod = LightPickingMethod.LightGrid;
            _world.Build(new Bounds(), _cmd, ref _buildScratchBuffer, _samplingResources, false, cubemapResolution);
            Graphics.ExecuteCommandBuffer(_cmd);
            Assert.AreEqual(lightCount, _world.LightCount);
        }

        [Test]
        public void World_AddAndRemoveLight_CountUpdates()
        {
            Assert.AreEqual(0, _world.LightCount);
            var light = CreateLights(1);
            var handles = _world.AddLights(light, _respectLightLayers, _autoEstimateLUTRange, MixedLightingMode.IndirectOnly);
            Assert.AreEqual(1, _world.LightCount);
            _world.RemoveLights(handles);
            Assert.AreEqual(0, _world.LightCount);
        }

        static Bounds CalculateBounds(Mesh mesh, Matrix4x4 localToWorld)
        {
            Bounds bounds = new Bounds(localToWorld.GetPosition(), Vector3.zero);
            var verts = mesh.vertices;
            foreach (var vert in verts)
            {
                var worldPos = localToWorld.MultiplyPoint(vert);
                bounds.Encapsulate(worldPos);
            }
            return bounds;
        }

        InstanceHandle AddInstanceToWorld(Mesh mesh, Matrix4x4 localToWorld, Material material)
        {
            Bounds bounds = CalculateBounds(mesh, localToWorld);
            const bool isStatic = true;
            uint objectLayerMask = 1;
            bool enableEmissiveSampling = true;
            var materialHandle = _world.AddMaterial(MaterialPool.ConvertUnityMaterialToMaterialDescriptor(material), UVChannel.UV0);
            var mask = World.GetInstanceMask(ShadowCastingMode.On, isStatic, RenderedGameObjectsFilter.OnlyStatic);

            return _world.AddInstance(mesh, new MaterialHandle[] { materialHandle }, new uint[] { mask }, objectLayerMask, localToWorld, bounds, isStatic, RenderedGameObjectsFilter.OnlyStatic, enableEmissiveSampling);
        }

        static AccelStructInstances GetAccelStructInstancesFromWorld(World world)
        {
            return world.GetAccelerationStructure().Instances;
        }

        const int cubemapResolution = 8;

        [Test]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void World_AddInstance_IncreasesInstanceCount(int instanceCount)
        {
            Mesh mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            for (int i = 0; i < instanceCount; i++)
            {
                Matrix4x4 localToWorld = Matrix4x4.Translate(Vector3.one * i);
                AddInstanceToWorld(mesh, localToWorld, _defaultMaterial);
            }

            _world.Build(new Bounds(), _cmd, ref _buildScratchBuffer, _samplingResources, false, cubemapResolution);
            Graphics.ExecuteCommandBuffer(_cmd);

            var instances = GetAccelStructInstancesFromWorld(_world);
            Assert.AreEqual(instanceCount, instances.GetInstanceCount());
        }

        // Struct for reading geometry pool data back to CPU
        struct VertexData
        {
            public Vector3 Position;
            public float2 UV0;
            public float2 UV1;
            public float OctahedralNormal;
        }

        [Test]
        public void World_AddInstances_UploadedVertexDataMatches()
        {
            // Add some meshes to world
            Mesh[] meshes = new[] {
                Resources.GetBuiltinResource<Mesh>("Cube.fbx"),
                Resources.GetBuiltinResource<Mesh>("Sphere.fbx")
            };
            for (int i = 0; i < meshes.Length; i++)
            {
                Matrix4x4 localToWorld = Matrix4x4.Translate(Vector3.one * i);
                AddInstanceToWorld(meshes[i], localToWorld, _defaultMaterial);
            }

            // Build world
            _world.Build(new Bounds(), _cmd, ref _buildScratchBuffer, _samplingResources, false, cubemapResolution);
            Graphics.ExecuteCommandBuffer(_cmd);

            // Readback vertex buffer data from geo pool
            var instances = GetAccelStructInstancesFromWorld(_world);
            int totalVertexCount = meshes.Sum(x => x.vertexCount);
            var geoPoolVertices = new VertexData[totalVertexCount];
            instances.vertexBuffer.GetData(geoPoolVertices);

            // Check that data matches source
            int vertexOffset = 0;
            for (int i = 0; i < meshes.Length; i++)
            {
                var meshVerts = meshes[i].vertices;
                var meshUVs = meshes[i].uv;

                for (int j = 0; j < meshVerts.Length; j++)
                {
                    Assert.AreEqual(meshVerts[j], geoPoolVertices[vertexOffset + j].Position, $"Vertex positions at index {vertexOffset + j} didn't match");
                    Assert.AreEqual(new float2(meshUVs[j].x, meshUVs[j].y), geoPoolVertices[vertexOffset + j].UV0, $"UVs at index {vertexOffset + j} didn't match");
                }

                vertexOffset += meshVerts.Length;
            }
        }

        [Test]
        public void World_AddAndRemoveInstances_CountIsCorrect()
        {
            // Add 3 instances
            var mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            var handles = new InstanceHandle[3];
            for (int i = 0; i < 3; i++)
            {
                Matrix4x4 localToWorld = Matrix4x4.Translate(Vector3.one * i);
                handles[i] = AddInstanceToWorld(mesh, localToWorld, _defaultMaterial);
            }

            // Remove 1 instance
            _world.RemoveInstance(handles[0]);

            // Build world
            _world.Build(new Bounds(), _cmd, ref _buildScratchBuffer, _samplingResources, false, cubemapResolution);
            Graphics.ExecuteCommandBuffer(_cmd);

            // Count should be 2
            var instances = GetAccelStructInstancesFromWorld(_world);
            Assert.AreEqual(2, instances.GetInstanceCount());
        }

        [Test]
        [TestCase(true)]
        [TestCase(false)]
        public void World_AddInstance_IncreasesMeshLightCountIfEmissive(bool isEmissive)
        {
            // Make material with emission
            Material material = new Material(_defaultMaterial);
            if (isEmissive)
            {
                material.EnableKeyword("_EMISSION");
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
                material.SetColor("_EmissionColor", Color.green);
            }

            // Add instance
            var mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            Matrix4x4 localToWorld = Matrix4x4.Translate(Vector3.one);
            AddInstanceToWorld(mesh, localToWorld, material);

            // Build world
            _world.Build(new Bounds(), _cmd, ref _buildScratchBuffer, _samplingResources, true, cubemapResolution);
            Graphics.ExecuteCommandBuffer(_cmd);

            // Check that mesh light count increased
            Assert.AreEqual(isEmissive ? 1 : 0, _world.MeshLightCount);

            CoreUtils.Destroy(material);
        }

        [Test]
        public void World_AddAndRemoveEmissiveInstance_MeshLightCountIsZero()
        {
            // Make material with emission
            Material material = new Material(_defaultMaterial);
            material.EnableKeyword("_EMISSION");
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
            material.SetColor("_EmissionColor", Color.green);

            // Add instance
            var mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            Matrix4x4 localToWorld = Matrix4x4.Translate(Vector3.one);
            InstanceHandle instance = AddInstanceToWorld(mesh, localToWorld, material);

            _world.RemoveInstance(instance);

            // Build world
            _world.Build(new Bounds(), _cmd, ref _buildScratchBuffer, _samplingResources, true, cubemapResolution);
            Graphics.ExecuteCommandBuffer(_cmd);

            // Check that mesh light count is 0
            Assert.AreEqual(0, _world.MeshLightCount);

            CoreUtils.Destroy(material);
        }

        [Test]
        public void World_UpdateInstanceTransform_UploadsCorrectData()
        {
#if UNITY_EDITOR_OSX
        if (System.Runtime.InteropServices.RuntimeInformation.OSArchitecture != System.Runtime.InteropServices.Architecture.Arm64)
            NUnit.Framework.Assert.Ignore("Fails on MacOS13 UUM-111386");
#endif
            // Add instance
            var mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            Matrix4x4 localToWorld = Matrix4x4.Translate(Vector3.one);
            InstanceHandle instance = AddInstanceToWorld(mesh, localToWorld, _defaultMaterial);

            // Update its transform
            Matrix4x4 newLocalToWorld = Matrix4x4.Translate(Vector3.one * 5);
            Assert.AreNotEqual(localToWorld, newLocalToWorld);
            _world.UpdateInstanceTransform(instance, newLocalToWorld);

            // Build world
            _world.Build(new Bounds(), _cmd, ref _buildScratchBuffer, _samplingResources, false, cubemapResolution);
            Graphics.ExecuteCommandBuffer(_cmd);

            // Readback instance buffer
            var instances = GetAccelStructInstancesFromWorld(_world);
            var instanceBuffer = instances.instanceBuffer.GetGpuBuffer(_cmd);
            Graphics.ExecuteCommandBuffer(_cmd);

            // Check that transform was updated
            var instancesArray = new AccelStructInstances.RTInstance[1];
            instanceBuffer.GetData(instancesArray);
            Assert.AreEqual((float4x4)newLocalToWorld, instancesArray[0].localToWorld);
        }
    }
}
