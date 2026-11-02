using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine.PathTracing.Core;
using UnityEngine.Profiling;
using UnityEngine.Rendering.UnifiedRayTracing;
using UnityEngine.TestTools;
using InstanceHandle = UnityEngine.PathTracing.Core.Handle<UnityEngine.Rendering.SurfaceCacheWorld.Instance>;
using MaterialHandle = UnityEngine.PathTracing.Core.Handle<UnityEngine.PathTracing.Core.MaterialPool.MaterialDescriptor>;

namespace UnityEngine.Rendering.Tests
{
    [TestFixture("Compute")]
    [TestFixture("Hardware")]
    internal class SurfaceCacheWorldAllocationTests
    {
        const int k_InstanceCount = 1000;
        const int k_AnchorInstanceCount = 2;

        readonly RayTracingBackend m_Backend;
        readonly InstanceHandle[] m_Instances = new InstanceHandle[k_InstanceCount];
        readonly MaterialHandle[] m_Materials = new MaterialHandle[1];
        readonly MaterialHandle[] m_OtherMaterials = new MaterialHandle[1];
        readonly uint[] m_Masks = { 0xFFFFFFFFu };
        TestHarness m_Harness;
        Mesh m_Mesh;

        public SurfaceCacheWorldAllocationTests(string backendAsString)
        {
            m_Backend = Enum.Parse<RayTracingBackend>(backendAsString);
        }

        [SetUp]
        public void SetUp()
        {
            TestHarness.EnsureSupportedOrIgnore(m_Backend);

            m_Harness = new TestHarness(m_Backend);
            m_Mesh = new Mesh
            {
                hideFlags = HideFlags.DontSave,
                vertices = new[] { Vector3.zero, Vector3.right, Vector3.forward },
                normals = new[] { Vector3.up, Vector3.up, Vector3.up },
                uv = new[] { Vector2.zero, Vector2.zero, Vector2.zero },
                triangles = new[] { 0, 2, 1 },
            };
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            m_Harness?.Dispose();
            CoreUtils.Destroy(m_Mesh);
            return TestHarness.DrainDisposedGpuMemory();
        }

        [Test]
        public void AddInstance_ManyInstances_DoesNotAllocateGCMemory([Values] bool emissive)
        {
            AddMaterialsAndAnchorInstances(emissive);
            WarmUpInstanceStorage();

            Assert.AreEqual(0, CountGCAllocations(AddAllInstances), "GC allocations while adding instances.");
            Assert.AreEqual(k_AnchorInstanceCount + k_InstanceCount, m_Harness.World.GetInstanceCount());
        }

        [Test]
        public void RemoveInstance_ManyInstances_DoesNotAllocateGCMemory([Values] bool emissive)
        {
            AddMaterialsAndAnchorInstances(emissive);
            WarmUpInstanceStorage();
            AddAllInstances();

            Assert.AreEqual(0, CountGCAllocations(RemoveAllInstances), "GC allocations while removing instances.");
            Assert.AreEqual(k_AnchorInstanceCount, m_Harness.World.GetInstanceCount());
        }

        [Test]
        public void UpdateInstanceMaterials_ManyInstances_DoesNotAllocateGCMemory([Values] bool emissive)
        {
            AddMaterialsAndAnchorInstances(emissive);
            AddAllInstances();
            WarmUpMaterialUpdates();

            Assert.AreEqual(0, CountGCAllocations(() => SetAllInstanceMaterials(m_OtherMaterials)),
                "GC allocations while updating instance materials.");
        }

        [Test]
        public void UpdateInstanceTransform_ManyInstances_DoesNotAllocateGCMemory()
        {
            AddMaterialsAndAnchorInstances(emissive: false);
            AddAllInstances();
            WarmUpTransformUpdates();

            var movedTransform = Matrix4x4.Translate(Vector3.right);
            Assert.AreEqual(0, CountGCAllocations(() => SetAllInstanceTransforms(movedTransform)),
                "GC allocations while updating instance transforms.");
        }

        [Test]
        public void UpdateMaterial_EmissiveStateChanged_DoesNotAllocatePerInstance()
        {
            AddMaterialsAndAnchorInstances(emissive: false);
            WarmUpEmissiveStateChanges();
            long bytesWithAnchorsOnly = CountGCAllocatedBytes(() => SetMaterialEmissive(true));
            SetMaterialEmissive(false);

            AddAllInstances();
            WarmUpEmissiveStateChanges();
            long bytesWithInstances = CountGCAllocatedBytes(() => SetMaterialEmissive(true));

            Assert.AreEqual(bytesWithAnchorsOnly, bytesWithInstances,
                "GC memory allocated while retracking instances after a material became emissive grows with the instance count.");
        }

        void WarmUpEmissiveStateChanges()
        {
            SetMaterialEmissive(true);
            SetMaterialEmissive(false);
        }

        void SetMaterialEmissive(bool emissive)
        {
            m_Harness.World.UpdateMaterial(m_Materials[0], CreateUntexturedMaterialDescriptor(emissive), UVChannel.UV0);
        }

        static MaterialPool.MaterialDescriptor CreateUntexturedMaterialDescriptor(bool emissive)
        {
            var descriptor = CreateMaterialDescriptor(emissive);
            descriptor.Albedo = null;
            return descriptor;
        }

        void AddMaterialsAndAnchorInstances(bool emissive)
        {
            m_Materials[0] = m_Harness.World.AddMaterial(CreateMaterialDescriptor(emissive), UVChannel.UV0);
            m_OtherMaterials[0] = m_Harness.World.AddMaterial(CreateMaterialDescriptor(emissive), UVChannel.UV0);

            m_Harness.World.AddInstance(m_Mesh, m_Materials, m_Masks, Matrix4x4.identity);
            m_Harness.World.AddInstance(m_Mesh, m_OtherMaterials, m_Masks, Matrix4x4.identity);
        }

        static MaterialPool.MaterialDescriptor CreateMaterialDescriptor(bool emissive)
        {
            return new MaterialPool.MaterialDescriptor
            {
                Albedo = Texture2D.whiteTexture,
                AlbedoScale = Vector2.one,
                AlbedoOffset = Vector2.zero,
                Alpha = 1f,
                PointSampleAlbedo = true,
                EmissionType = emissive
                    ? PathTracing.Core.MaterialPropertyType.Color
                    : PathTracing.Core.MaterialPropertyType.None,
                EmissionColor = emissive ? Vector3.one : Vector3.zero,
                PointSampleEmission = true,
            };
        }

        void WarmUpInstanceStorage()
        {
            AddAllInstances();
            RemoveAllInstances();
        }

        void WarmUpMaterialUpdates()
        {
            SetAllInstanceMaterials(m_OtherMaterials);
            SetAllInstanceMaterials(m_Materials);
        }

        void WarmUpTransformUpdates()
        {
            SetAllInstanceTransforms(Matrix4x4.Translate(Vector3.up));
        }

        void AddAllInstances()
        {
            for (int i = 0; i < k_InstanceCount; i++)
            {
                m_Instances[i] = m_Harness.World.AddInstance(m_Mesh, m_Materials, m_Masks, Matrix4x4.identity);
            }
        }

        void RemoveAllInstances()
        {
            for (int i = 0; i < k_InstanceCount; i++)
            {
                m_Harness.World.RemoveInstance(m_Instances[i]);
            }
        }

        void SetAllInstanceMaterials(MaterialHandle[] materials)
        {
            for (int i = 0; i < k_InstanceCount; i++)
            {
                m_Harness.World.UpdateInstanceMaterials(m_Instances[i], materials);
            }
        }

        void SetAllInstanceTransforms(Matrix4x4 localToWorld)
        {
            for (int i = 0; i < k_InstanceCount; i++)
            {
                m_Harness.World.UpdateInstanceTransform(m_Instances[i], localToWorld);
            }
        }

        static long CountGCAllocatedBytes(TestDelegate action)
        {
            long before = GC.GetAllocatedBytesForCurrentThread();
            action();
            return GC.GetAllocatedBytesForCurrentThread() - before;
        }

        static int CountGCAllocations(TestDelegate action)
        {
            var recorder = Recorder.Get("GC.Alloc");
            recorder.enabled = false;
            recorder.FilterToCurrentThread();
            recorder.enabled = true;
            try
            {
                action();
            }
            finally
            {
                recorder.enabled = false;
                recorder.CollectFromAllThreads();
            }

            return recorder.sampleBlockCount;
        }
    }
}
