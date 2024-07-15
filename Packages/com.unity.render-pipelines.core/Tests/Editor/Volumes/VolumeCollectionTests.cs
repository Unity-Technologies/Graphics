using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace UnityEngine.Rendering.Tests
{
    class VolumeCollectionTests
    {
        const int k_Iterations = 100;
        private static readonly LayerMask s_Default = 1 << 0;
        private static readonly LayerMask s_First = 1 << 1;
        private static readonly LayerMask s_Fifth = 1 << 5;
        private static readonly LayerMask s_Everything = ~0;
        private VolumeCollection m_VolumeCollection;
        private List<Volume> m_Volumes = new();

        [SetUp]
        public void Setup()
        {
            m_VolumeCollection = new VolumeCollection();
        }

        [TearDown]
        public void Teardown()
        {
            foreach (var volume in m_Volumes)
            {
                Object.DestroyImmediate(volume.gameObject);
            }

            m_Volumes.Clear();
        }

        private Volume CreateVolume()
        {
            string name = $"Volume Collection Test ({m_Volumes.Count})";

            var go = new GameObject(name);
            var v = go.AddComponent<Volume>();
            m_Volumes.Add(v);
            return v;
        }

        [Test]
        public void IsRegistered()
        {
            var volume = CreateVolume();

            Assert.IsTrue(m_VolumeCollection.Register(volume, volume.gameObject.layer));
            Assert.IsFalse(m_VolumeCollection.Register(volume, volume.gameObject.layer));

            Assert.AreEqual(1, m_VolumeCollection.count);

            Assert.IsTrue(m_VolumeCollection.Unregister(volume, volume.gameObject.layer));
            Assert.AreEqual(0, m_VolumeCollection.count);
        }

        [Test]
        public void GrabVolumesLayerAndEverything()
        {
            var volume = CreateVolume();

            volume.gameObject.layer = 5;

            Assert.IsTrue(m_VolumeCollection.Register(volume, 5));

            var volumes = m_VolumeCollection.GrabVolumes(s_Default);
            Assert.AreEqual(0, volumes.Count);

            volumes = m_VolumeCollection.GrabVolumes(s_Fifth);
            Assert.AreEqual(1, volumes.Count);

            volumes = m_VolumeCollection.GrabVolumes(s_Everything);
            Assert.AreEqual(1, volumes.Count);
        }

        [Test]
        public void GrabVolumesFromMultipleLayers()
        {
            var volume = CreateVolume();
            volume.gameObject.layer = 5;
            Assert.IsTrue(m_VolumeCollection.Register(volume,5));

            var volume2 = CreateVolume();
            volume2.gameObject.layer = 1;
            Assert.IsTrue(m_VolumeCollection.Register(volume2, 1));

            CollectionAssert.AreEqual(m_VolumeCollection.GrabVolumes(s_First | s_Fifth),
                m_VolumeCollection.GrabVolumes(s_Everything));

            Assert.AreEqual(volume, m_VolumeCollection.GrabVolumes(s_Fifth).FirstOrDefault());
            Assert.AreEqual(volume2, m_VolumeCollection.GrabVolumes(s_First).FirstOrDefault());

        }

        public static IEnumerable<TestCaseData> VolumeTestCases()
        {
            yield return new TestCaseData((Action<Volume, int>)((Volume v, int i) => v.priority = 5.0f)).SetName("All Same Priority");
            yield return new TestCaseData((Action<Volume, int>)((Volume v, int i) => v.priority = (float)(i))).SetName("Ascending Order");
            yield return new TestCaseData((Action<Volume, int>)((Volume v, int i) => v.priority = (float)(k_Iterations - 1 - i))).SetName("Descending Order");
            yield return new TestCaseData((Action<Volume, int>)((Volume v, int i) => v.priority = (float)(i % 5))).SetName("Mod 5");
            yield return new TestCaseData((Action<Volume, int>)((Volume v, int i) => v.priority = (float)(i % 2))).SetName("Mod 2");
            yield return new TestCaseData((Action<Volume, int>)((Volume v, int i) => v.priority = (float)(i % 3))).SetName("Mod 3");
            yield return new TestCaseData((Action<Volume, int>)((Volume v, int i) => v.priority = (float)(i - 5))).SetName("Negative Priorities");
            yield return new TestCaseData((Action<Volume, int>)((Volume v, int i) => v.priority = (float)(i / 3.0))).SetName("Floating Point Precision");
            yield return new TestCaseData((Action<Volume, int>)((Volume v, int i) => v.priority = (float)(i * 1000))).SetName("Large Range of Priorities");
        }

        [Test, TestCaseSource(nameof(VolumeTestCases))]
        public void VolumesAreSortedByPriority(Action<Volume, int> action)
        {
            var expectedOrder = new List<Volume>();
            for (int i = 0; i < k_Iterations; i++)
            {
                var volume = CreateVolume();
                action(volume, i);

                Assert.IsTrue(m_VolumeCollection.Register(volume, volume.gameObject.layer));
                expectedOrder.Add(volume);
            }

            Assert.IsTrue(IsSortedByPriority(m_VolumeCollection.GrabVolumes(s_Default)));
        }

        [Test, TestCaseSource(nameof(VolumeTestCases))]
        public void VolumesAreSortedByPriorityChanges(Action<Volume, int> action)
        {
            var expectedOrder = new List<Volume>();
            for (int i = 0; i < k_Iterations; i++)
            {
                var volume = CreateVolume();
                action(volume, i);
                m_VolumeCollection.SetLayerIndexDirty(1 << volume.gameObject.layer);

                Assert.IsTrue(m_VolumeCollection.Register(volume, volume.gameObject.layer));
                expectedOrder.Add(volume);
            }

            // Swap priorities after they are added into the collection
            (expectedOrder[1].priority, expectedOrder[8].priority) = (expectedOrder[8].priority, expectedOrder[1].priority);

            // Notify the collection that the layer is dirty
            m_VolumeCollection.SetLayerIndexDirty(1 << 0);

            Assert.IsTrue(IsSortedByPriority(m_VolumeCollection.GrabVolumes(s_Default)));
        }

        private bool IsSortedByPriority(List<Volume> volumes)
        {
            for (int i = 0; i < volumes.Count - 1; ++i)
            {
                if (volumes[i].priority > volumes[i + 1].priority)
                    return false;
            }

            return true;
        }

        [Test]
        public void UpdateLayer()
        {
            var volume = CreateVolume();
            volume.gameObject.layer = 0;

            Assert.IsTrue(m_VolumeCollection.Register(volume, 0));

            for (int i = 0; i < VolumeCollection.k_MaxLayerCount; ++i)
            {
                var previousLayer = volume.gameObject.layer;
                volume.gameObject.layer = i;
                m_VolumeCollection.ChangeLayer(volume, previousLayer, i);

                for (int j = 0; j < VolumeCollection.k_MaxLayerCount; ++j)
                {
                    var volumes = m_VolumeCollection.GrabVolumes(1 << j);
                    if (i != j)
                        Assert.IsEmpty(volumes);
                    else
                        Assert.AreEqual(1, volumes.Count);
                }

                var volumesEverything = m_VolumeCollection.GrabVolumes(s_Everything);
                Assert.AreEqual(1, volumesEverything.Count);
            }
        }

        [Test]
        public void UpdateLayerMultipleVolumes()
        {
            for (int i = 0; i < k_Iterations; i++)
            {
                var volume = CreateVolume();
                volume.priority = (float)(i % 2);

                if (i < k_Iterations / 2)
                {
                    volume.gameObject.layer = 0;
                }
                else
                {
                    volume.gameObject.layer = 1;
                }

                Assert.IsTrue(m_VolumeCollection.Register(volume, volume.gameObject.layer));
            }

            var arrayVolumes = m_VolumeCollection.GrabVolumes(s_Everything).ToArray();
            foreach (var volume in arrayVolumes)
            {
                var previousLayerIndex = volume.gameObject.layer;
                volume.gameObject.layer = 5;
                m_VolumeCollection.ChangeLayer(volume, previousLayerIndex, volume.gameObject.layer);
            }

            Assert.IsEmpty(m_VolumeCollection.GrabVolumes(s_Default));
            Assert.IsEmpty(m_VolumeCollection.GrabVolumes(s_First));
            Assert.IsTrue(IsSortedByPriority(m_VolumeCollection.GrabVolumes(s_Fifth)));
        }

        [Test]
        public void VolumeOnMultipleLayers()
        {
            var volumesExpected = new List<Volume>();
            for (int i = 0; i < VolumeCollection.k_MaxLayerCount; i++)
            {
                var volume = CreateVolume();
                volume.gameObject.layer = i;
                volumesExpected.Add(volume);
                Assert.IsTrue(m_VolumeCollection.Register(volume, i));
            }

            for (int i = 0; i < VolumeCollection.k_MaxLayerCount; i++)
            {
                LayerMask mask = (1 << i);
                var volumes = m_VolumeCollection.GrabVolumes(mask);
                Assert.AreEqual(1, volumes.Count);
                Assert.AreEqual(volumesExpected[i], volumes[0]);
            }

            CollectionAssert.AreEqual(volumesExpected, m_VolumeCollection.GrabVolumes(s_Everything));
        }

        [Test]
        public void VolumeOnMultipleLayersGrabMultiple()
        {
            var volumesExpected = new List<Volume>();
            for (int i = 0; i < VolumeCollection.k_MaxLayerCount; i++)
            {
                var volume = CreateVolume();
                volume.gameObject.layer = i;
                volumesExpected.Add(volume);
                Assert.IsTrue(m_VolumeCollection.Register(volume, i));
            }

            for (int i = 0; i < VolumeCollection.k_MaxLayerCount - 1; i++)
            {
                LayerMask mask = 1 << i | 1 << i + 1;
                var volumes = m_VolumeCollection.GrabVolumes(mask);
                Assert.AreEqual(2, volumes.Count);
                Assert.AreEqual(volumesExpected[i], volumes[0]);
                Assert.AreEqual(volumesExpected[i + 1], volumes[1]);
            }

            CollectionAssert.AreEqual(volumesExpected, m_VolumeCollection.GrabVolumes(s_Everything));
        }
    }
}
