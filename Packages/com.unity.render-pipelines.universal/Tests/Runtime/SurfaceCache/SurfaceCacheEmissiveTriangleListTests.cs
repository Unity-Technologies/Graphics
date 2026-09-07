#if SURFACE_CACHE_SUPPORTED || UNITY_EDITOR

using System;
using System.Runtime.InteropServices;
using NUnit.Framework;
using UnityEngine.PathTracing.Core;
using UnityEngine.Rendering.UnifiedRayTracing;

using MaterialHandle = UnityEngine.PathTracing.Core.Handle<UnityEngine.PathTracing.Core.MaterialPool.MaterialDescriptor>;

namespace UnityEngine.Rendering.Universal.Tests
{
    class SurfaceCacheEmissiveTriangleListTests
    {
        [StructLayout(LayoutKind.Sequential)]
        struct EmissiveTriangle
        {
            public uint InstanceId;
            public uint PrimitiveIndex;
        }

        class Environment : IDisposable
        {
            public RayTracingContext Context;
            public SurfaceCacheWorld World;
            public GraphicsBuffer ScratchBuffer;

            public void Dispose()
            {
                World?.Dispose();
                Context?.Dispose();
                ScratchBuffer?.Dispose();
            }

            public void CommitWorld()
            {
                uint cubemapResolution = 32;
                Light sun = null;
                using CommandBuffer cmd = new CommandBuffer();
                World.Commit(cmd, ref ScratchBuffer, cubemapResolution, sun, out _);
                Graphics.ExecuteCommandBuffer(cmd);
            }

            public int ReadbackEmissiveTriangleCount()
            {
                var data = new int[1];
                World.GetEmissiveTriangleCounterBuffer().GetData(data);
                return data[0];
            }

            public EmissiveTriangle[] ReadbackEmissiveTriangles()
            {
                int triCount = ReadbackEmissiveTriangleCount();
                var triBuffer = World.GetEmissiveTriangleBuffer();
                var triArray = new EmissiveTriangle[triBuffer.count];
                triBuffer.GetData(triArray, 0, 0, triCount);
                return triArray;
            }
        }

        static Environment CreateEnvironmentOrIgnoreTest()
        {
            if (!RayTracingContext.GetCapabilities(RayTracingBackend.Compute).HasFlag(CapabilityMask.RayTracingShaders))
                Assert.Ignore("Requires the Compute ray-tracing backend.");
            if (Application.platform == RuntimePlatform.PS5)
                Assert.Ignore("The Compute ray-tracing backend is not supported on this platform.");

            var rtResources = new RayTracingResources();
            if (!rtResources.LoadFromRenderPipelineResources())
                Assert.Ignore("Ray tracing resources are unavailable (requires an active SRP with the resources registered).");

            var worldResources = new WorldResourceSet();
            if (!worldResources.LoadFromRenderPipelineResources())
                Assert.Ignore("Surface cache world resources are unavailable (requires an active SRP with the resources registered).");

            var coreResourceSet = GraphicsSettings.GetRenderPipelineSettings<Rendering.SurfaceCacheRenderPipelineResourceSet>();
            if (coreResourceSet == null)
                Assert.Ignore("Surface cache core render pipeline resources are unavailable.");

            if (coreResourceSet.emissiveTriangleAdditionComputeShader == null
                || !coreResourceSet.emissiveTriangleAdditionComputeShader.HasKernel("Add")
                || coreResourceSet.emissiveTriangleRemovalComputeShader == null
                || !coreResourceSet.emissiveTriangleRemovalComputeShader.HasKernel("Remove"))
                Assert.Ignore("Surface cache emissive triangle compute kernels are unavailable on this graphics API.");

            var env = new Environment();
            env.Context = new RayTracingContext(RayTracingBackend.Compute, rtResources);
            env.World = new SurfaceCacheWorld();
            env.World.Init(env.Context, worldResources, coreResourceSet.emissiveTriangleAdditionComputeShader, coreResourceSet.emissiveTriangleRemovalComputeShader);
            return env;
        }

        static MaterialPool.MaterialDescriptor CreateMaterialDescriptor(bool emissive)
        {
            return new MaterialPool.MaterialDescriptor
            {
                Albedo = Texture2D.whiteTexture,
                AlbedoScale = Vector2.one,

                EmissionColor = emissive ? Vector3.one : Vector3.zero,
                EmissionType = emissive ? PathTracing.Core.MaterialPropertyType.Color : PathTracing.Core.MaterialPropertyType.None,

                Alpha = 1.0f,
            };
        }

        [Test]
        public void Commit_AfterAddingAndRemovingEmissiveInstance_UpdatesEmissiveTriangleList()
        {
            using var env = CreateEnvironmentOrIgnoreTest();
            var material = CreateMaterialDescriptor(emissive: true);
            var mesh = Resources.GetBuiltinResource<Mesh>("New-Sphere.fbx");
            int triCount = (int)mesh.GetIndexCount(0) / 3;

            // Add emissive renderer
            var materialHandle = env.World.AddMaterial(material, UVChannel.UV0);
            Span<MaterialHandle> matHandles = stackalloc MaterialHandle[] { materialHandle };
            Span<uint> mask = stackalloc uint[] { 1 };
            var instanceHandle = env.World.AddInstance(mesh, matHandles, mask, Matrix4x4.identity);
            env.CommitWorld();
            Assert.AreEqual(triCount, env.ReadbackEmissiveTriangleCount());

            // Remove emissive renderer
            env.World.RemoveInstance(instanceHandle);
            env.CommitWorld();
            Assert.AreEqual(0u, env.ReadbackEmissiveTriangleCount() );
        }

        [Test]
        public void Commit_AfterAddingNonEmissiveInstance_LeavesEmissiveTriangleListEmpty()
        {
            using var env = CreateEnvironmentOrIgnoreTest();
            var mesh = Resources.GetBuiltinResource<Mesh>("New-Sphere.fbx");

            // Add non-emissive renderer, list stays empty
            var materialHandle = env.World.AddMaterial(CreateMaterialDescriptor(emissive: false), UVChannel.UV0);
            Span<MaterialHandle> matHandles = stackalloc MaterialHandle[] { materialHandle };
            Span<uint> mask = stackalloc uint[] { 1 };
            env.World.AddInstance(mesh, matHandles, mask, Matrix4x4.identity);
            env.CommitWorld();

            Assert.AreEqual(0, env.ReadbackEmissiveTriangleCount());
        }

        [Test]
        public void Commit_AfterTogglingMaterialEmissiveState_UpdatesEmissiveTriangleList()
        {
            using var env = CreateEnvironmentOrIgnoreTest();
            var mesh = Resources.GetBuiltinResource<Mesh>("New-Sphere.fbx");
            int triCount = (int)mesh.GetIndexCount(0) / 3;

            // Add an instance referencing a non-emissive material
            var materialHandle = env.World.AddMaterial(CreateMaterialDescriptor(emissive: false), UVChannel.UV0);
            Span<MaterialHandle> matHandles = stackalloc MaterialHandle[] { materialHandle };
            Span<uint> mask = stackalloc uint[] { 1 };
            env.World.AddInstance(mesh, matHandles, mask, Matrix4x4.identity);
            env.CommitWorld();
            Assert.AreEqual(0, env.ReadbackEmissiveTriangleCount());

            // Make the material emissive
            env.World.UpdateMaterial(materialHandle, CreateMaterialDescriptor(emissive: true), UVChannel.UV0);
            env.CommitWorld();
            Assert.AreEqual(triCount, env.ReadbackEmissiveTriangleCount());

            // Make the material non-emissive again
            env.World.UpdateMaterial(materialHandle, CreateMaterialDescriptor(emissive: false), UVChannel.UV0);
            env.CommitWorld();
            Assert.AreEqual(0, env.ReadbackEmissiveTriangleCount());
        }

        [Test]
        public void Commit_AfterChangingEmissiveRendererMaterial_UpdatesEmissiveTriangleList()
        {
            using var env = CreateEnvironmentOrIgnoreTest();
            var mesh = Resources.GetBuiltinResource<Mesh>("New-Sphere.fbx");
            int triCount = (int)mesh.GetIndexCount(0) / 3;

            var emissiveMaterial = env.World.AddMaterial(CreateMaterialDescriptor(emissive: true), UVChannel.UV0);
            var nonEmissiveMaterial = env.World.AddMaterial(CreateMaterialDescriptor(emissive: false), UVChannel.UV0);

            // Add instance using the emissive material
            Span<MaterialHandle> emissiveHandles = stackalloc MaterialHandle[] { emissiveMaterial };
            Span<uint> mask = stackalloc uint[] { 1 };
            var instanceHandle = env.World.AddInstance(mesh, emissiveHandles, mask, Matrix4x4.identity);
            env.CommitWorld();
            Assert.AreEqual(triCount, env.ReadbackEmissiveTriangleCount());

            // Point the instance at the non-emissive material
            Span<MaterialHandle> nonEmissiveHandles = stackalloc MaterialHandle[] { nonEmissiveMaterial };
            env.World.UpdateInstanceMaterials(instanceHandle, nonEmissiveHandles);
            env.CommitWorld();
            Assert.AreEqual(0, env.ReadbackEmissiveTriangleCount());
        }

        [Test]
        public void Commit_WhenInstanceAddedAndRemovedTogether_LeavesEmissiveTriangleListUnchanged()
        {
            using var env = CreateEnvironmentOrIgnoreTest();
            var material = CreateMaterialDescriptor(emissive: true);
            var mesh = Resources.GetBuiltinResource<Mesh>("New-Sphere.fbx");
            int triCount = (int)mesh.GetIndexCount(0) / 3;

            var materialHandle = env.World.AddMaterial(material, UVChannel.UV0);
            Span<MaterialHandle> matHandles = stackalloc MaterialHandle[] { materialHandle };
            Span<uint> mask = stackalloc uint[] { 1 };

            // Add one emissive instance
            env.World.AddInstance(mesh, matHandles, mask, Matrix4x4.identity);
            env.CommitWorld();
            Assert.AreEqual(triCount, env.ReadbackEmissiveTriangleCount());

            // Add and remove a second instance without committing in between, so the add and remove cancel
            var transientHandle = env.World.AddInstance(mesh, matHandles, mask, Matrix4x4.identity);
            env.World.RemoveInstance(transientHandle);
            env.CommitWorld();
            Assert.AreEqual(triCount, env.ReadbackEmissiveTriangleCount());
        }

        [Test]
        public void Commit_AfterRemovingMiddleEmissiveInstance_EmissiveTriangleListStaysDense()
        {
            using var env = CreateEnvironmentOrIgnoreTest();
            var material = CreateMaterialDescriptor(emissive: true);
            var mesh = Resources.GetBuiltinResource<Mesh>("New-Sphere.fbx");
            int triCount = (int)mesh.GetIndexCount(0) / 3;

            var materialHandle = env.World.AddMaterial(material, UVChannel.UV0);
            Span<MaterialHandle> matHandles = stackalloc MaterialHandle[] { materialHandle };
            Span<uint> mask = stackalloc uint[] { 1 };

            // Add three emissive instances
            env.World.AddInstance(mesh, matHandles, mask, Matrix4x4.identity);
            var middleHandle = env.World.AddInstance(mesh, matHandles, mask, Matrix4x4.identity);
            env.World.AddInstance(mesh, matHandles, mask, Matrix4x4.identity);
            env.CommitWorld();
            Assert.AreEqual(3 * triCount, env.ReadbackEmissiveTriangleCount());

            // Remove the middle instance
            env.World.GetAccelerationStructure().GetInstanceIDs(middleHandle.Value, out int[] removedInstanceIds);
            uint removedInstanceId = (uint)removedInstanceIds[0];
            env.World.RemoveInstance(middleHandle);
            env.CommitWorld();

            // List stays dense, and the removed instance has no remaining triangles
            int afterCount = env.ReadbackEmissiveTriangleCount();
            var after = env.ReadbackEmissiveTriangles();
            Assert.AreEqual(2 * triCount, afterCount);
            for (int i = 0; i < afterCount; i++)
            {
                Assert.AreNotEqual(removedInstanceId, after[i].InstanceId);
            }
        }
    }
}

#endif
