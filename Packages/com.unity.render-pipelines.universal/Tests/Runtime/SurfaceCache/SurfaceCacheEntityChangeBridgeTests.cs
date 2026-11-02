#if SURFACE_CACHE_SUPPORTED || UNITY_EDITOR

using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine.TestTools;

namespace UnityEngine.Rendering.Universal.Tests
{
    class SurfaceCacheEntityChangeBridgeTests
    {
        Mesh m_KeyDonor;

        [SetUp]
        public void SetUp()
        {
            Assume.That(!SurfaceCacheEntityChangeBridge.IsConsumerPresent,
                "These tests require exclusive ownership of the SurfaceCacheEntityChangeBridge.");

            m_KeyDonor = new Mesh { name = "SurfaceCacheEntityChangeBridgeTests KeyDonor" };
        }

        [TearDown]
        public void TearDown()
        {
            if (SurfaceCacheEntityChangeBridge.IsConsumerPresent)
                SurfaceCacheEntityChangeBridge.UnregisterConsumer();

            Object.DestroyImmediate(m_KeyDonor);
        }

        [Test]
        public void EntityChangeSource_BridgeAlreadyTaken_DetachesInsteadOfThrowing()
        {
            var first = new EntityChangeSource();
            var second = new EntityChangeSource();
            Assert.IsFalse(SurfaceCacheEntityChangeBridge.IsConsumerPresent, "Constructing sources alone should not register a consumer.");

            first.CollectChanges(new SurfaceCacheWorldChangeSet());
            first.EndCollectChanges();

            SurfaceCacheEntityChangeBridge.PushChanged(MakeRecord(), default);

            var secondChanges = new SurfaceCacheWorldChangeSet();
            LogAssert.Expect(LogType.Error, EntityChangeSource.k_ContentionError);
            second.CollectChanges(secondChanges);
            Assert.IsEmpty(secondChanges.EntityInstanceChangedList, "A detached source should not receive records.");
            second.EndCollectChanges();

            var firstChanges = new SurfaceCacheWorldChangeSet();
            first.CollectChanges(firstChanges);
            CollectionAssert.IsNotEmpty(firstChanges.EntityInstanceChangedList, "The registered source should receive the pushed record.");
            first.EndCollectChanges();

            second.Dispose();
            Assert.IsTrue(SurfaceCacheEntityChangeBridge.IsConsumerPresent, "Disposing a detached source must not unregister the active consumer.");

            first.Dispose();
            Assert.IsFalse(SurfaceCacheEntityChangeBridge.IsConsumerPresent);
        }

        [Test]
        public void EntityChangeSource_CompetitorDisposed_AcquiresOnNextCollect()
        {
            var first = new EntityChangeSource();
            var second = new EntityChangeSource();

            first.CollectChanges(new SurfaceCacheWorldChangeSet());
            first.EndCollectChanges();

            LogAssert.Expect(LogType.Error, EntityChangeSource.k_ContentionError);
            second.CollectChanges(new SurfaceCacheWorldChangeSet());
            second.EndCollectChanges();

            // A second contended collect must not log the contention error again.
            second.CollectChanges(new SurfaceCacheWorldChangeSet());
            second.EndCollectChanges();

            first.Dispose();

            second.CollectChanges(new SurfaceCacheWorldChangeSet());
            second.EndCollectChanges();
            Assert.IsTrue(SurfaceCacheEntityChangeBridge.IsConsumerPresent, "The surviving source should take over the freed bridge.");

            SurfaceCacheEntityChangeBridge.PushChanged(MakeRecord(), default);

            var changes = new SurfaceCacheWorldChangeSet();
            second.CollectChanges(changes);
            CollectionAssert.IsNotEmpty(changes.EntityInstanceChangedList, "A source that took over the bridge should receive records.");
            second.EndCollectChanges();

            second.Dispose();
            Assert.IsFalse(SurfaceCacheEntityChangeBridge.IsConsumerPresent);
        }

        [Test]
        public void CollectChanges_DrainAborted_PendingIsRedeliveredOnNextCollect()
        {
            var source = new EntityChangeSource();
            source.CollectChanges(new SurfaceCacheWorldChangeSet());
            source.EndCollectChanges();

            SurfaceCacheEntityChangeBridge.PushChanged(MakeRecord(), default);

            source.CollectChanges(new SurfaceCacheWorldChangeSet());
            source.AbortCollectChanges();

            var retried = new SurfaceCacheWorldChangeSet();
            source.CollectChanges(retried);
            CollectionAssert.IsNotEmpty(retried.EntityInstanceChangedList, "An aborted drain should redeliver its records on the next collect.");
            source.EndCollectChanges();

            var afterCommit = new SurfaceCacheWorldChangeSet();
            source.CollectChanges(afterCommit);
            Assert.IsEmpty(afterCommit.EntityInstanceChangedList, "A committed drain should clear the delivered records.");
            source.EndCollectChanges();

            source.Dispose();
        }

        [Test]
        public void CollectChanges_TransformPushedAfterAbort_FoldsIntoRetainedRecord()
        {
            var source = new EntityChangeSource();
            source.CollectChanges(new SurfaceCacheWorldChangeSet());
            source.EndCollectChanges();

            SurfaceCacheEntityChangeBridge.PushChanged(MakeRecord(), default);

            source.CollectChanges(new SurfaceCacheWorldChangeSet());
            source.AbortCollectChanges();

            var moved = Matrix4x4.Translate(new Vector3(1.0f, 2.0f, 3.0f));
            var transforms = SurfaceCacheEntityChangeBridge.AcquireTransformChangedList();
            transforms.Add(new SurfaceCacheEntityTransformRecord { Key = m_KeyDonor.GetEntityId(), LocalToWorld = moved });

            var retried = new SurfaceCacheWorldChangeSet();
            source.CollectChanges(retried);
            Assert.AreEqual(0, retried.EntityInstanceTransformChangedList.Length, "A transform superseded by a pending record should not be delivered separately.");
            var retriedRecord = retried.EntityInstanceChangedList.Single();
            source.EndCollectChanges();

            Assert.AreEqual(moved, retriedRecord.LocalToWorld, "The newer transform should be folded into the retained record.");

            source.Dispose();
        }

        [Test]
        public void CollectChanges_RecordPushedAfterAbort_SupersedesRetainedTransform()
        {
            var source = new EntityChangeSource();
            source.CollectChanges(new SurfaceCacheWorldChangeSet());
            source.EndCollectChanges();

            var stale = Matrix4x4.Translate(new Vector3(4.0f, 5.0f, 6.0f));
            var transforms = SurfaceCacheEntityChangeBridge.AcquireTransformChangedList();
            transforms.Add(new SurfaceCacheEntityTransformRecord { Key = m_KeyDonor.GetEntityId(), LocalToWorld = stale });

            source.CollectChanges(new SurfaceCacheWorldChangeSet());
            source.AbortCollectChanges();

            var moved = Matrix4x4.Translate(new Vector3(1.0f, 2.0f, 3.0f));
            var record = MakeRecord();
            record.LocalToWorld = moved;
            SurfaceCacheEntityChangeBridge.PushChanged(record, default);

            var retried = new SurfaceCacheWorldChangeSet();
            source.CollectChanges(retried);
            Assert.AreEqual(0, retried.EntityInstanceTransformChangedList.Length, "A transform retained from before the record must not be redelivered.");
            var retriedRecord = retried.EntityInstanceChangedList.Single();
            source.EndCollectChanges();

            Assert.AreEqual(moved, retriedRecord.LocalToWorld, "A retained transform must not be folded over a newer record.");

            source.Dispose();
        }

        [Test]
        public void UnregisterConsumer_DuringStuckDrain_NextConsumerDrainsCleanly()
        {
            Assert.IsTrue(SurfaceCacheEntityChangeBridge.TryRegisterConsumer());

            SurfaceCacheEntityChangeBridge.BeginDrain(out _, out _, out _, out _);

            SurfaceCacheEntityChangeBridge.UnregisterConsumer();

            Assert.IsTrue(SurfaceCacheEntityChangeBridge.TryRegisterConsumer());
            SurfaceCacheEntityChangeBridge.BeginDrain(out _, out _, out _, out _);
            SurfaceCacheEntityChangeBridge.EndDrain();
            SurfaceCacheEntityChangeBridge.UnregisterConsumer();
        }

        [Test]
        public void PushDestroyed_BeforeDrain_OtherPendingRecordStaysAddressable()
        {
            Assert.IsTrue(SurfaceCacheEntityChangeBridge.TryRegisterConsumer());
            var keyA = CreateKey(1);
            var keyB = CreateKey(2);
            PushRecord(keyA, Assignment(0, 10));
            PushRecord(keyB, Assignment(0, 20), Assignment(1, 21));
            SurfaceCacheEntityChangeBridge.PushDestroyed(keyA);
            PushRecord(keyB, Assignment(0, 30), Assignment(1, 31));

            var drained = Drain();

            Assert.AreEqual(1, drained.Changed.Length, "The destroyed key's pending record should be gone and the re-pushed key coalesced.");
            Assert.AreEqual(keyB, drained.Changed[0].Key);
            CollectionAssert.AreEqual(new[] { Assignment(0, 30), Assignment(1, 31) }, drained.MaterialsOf(0),
                "The record moved into the destroyed record's slot should still be found by its key.");
            CollectionAssert.AreEqual(new[] { keyA }, drained.Destroyed);
        }

        [Test]
        public void PushChanged_SameKeyShrinksThenGrows_DrainCarriesLatestAssignments()
        {
            Assert.IsTrue(SurfaceCacheEntityChangeBridge.TryRegisterConsumer());
            var key = CreateKey(1);
            PushRecord(key, Assignment(0, 10), Assignment(1, 11), Assignment(2, 12));
            PushRecord(key, Assignment(0, 20));
            PushRecord(key, Assignment(0, 30), Assignment(1, 31));

            var drained = Drain();

            Assert.AreEqual(1, drained.Changed.Length, "Pushes for one key between drains should coalesce into one record.");
            CollectionAssert.AreEqual(new[] { Assignment(0, 30), Assignment(1, 31) }, drained.MaterialsOf(0),
                "The record's range should read back only the latest assignments, not a tail left by an earlier push.");
        }

        readonly struct Drained
        {
            public readonly SurfaceCacheEntityInstanceRecord[] Changed;
            public readonly SurfaceCacheSubMeshMaterial[] Materials;
            public readonly EntityId[] Destroyed;

            public Drained(SurfaceCacheEntityInstanceRecord[] changed, SurfaceCacheSubMeshMaterial[] materials, EntityId[] destroyed)
            {
                Changed = changed;
                Materials = materials;
                Destroyed = destroyed;
            }

            public SurfaceCacheSubMeshMaterial[] MaterialsOf(int recordIndex)
            {
                var record = Changed[recordIndex];
                return Materials.AsSpan(record.MaterialsStart, record.MaterialsCount).ToArray();
            }
        }

        static Drained Drain()
        {
            SurfaceCacheEntityChangeBridge.BeginDrain(out var changed, out var materials, out var destroyed, out _);
            try
            {
                return new Drained(changed.ToArray(), materials.ToArray(), destroyed.ToArray());
            }
            finally
            {
                SurfaceCacheEntityChangeBridge.EndDrain();
            }
        }

        static void PushRecord(EntityId key, params SurfaceCacheSubMeshMaterial[] materials)
        {
            SurfaceCacheEntityChangeBridge.PushChanged(new SurfaceCacheEntityInstanceRecord { Key = key }, materials);
        }

        static SurfaceCacheSubMeshMaterial Assignment(int subMeshIndex, ulong material)
        {
            return new SurfaceCacheSubMeshMaterial { SubMeshIndex = subMeshIndex, Material = EntityId.FromULong(material) };
        }

        static EntityId CreateKey(int index)
        {
            return EntityId.FromULong((1ul << 40) | (uint)index);
        }

        SurfaceCacheEntityInstanceRecord MakeRecord()
        {
            return new SurfaceCacheEntityInstanceRecord
            {
                Key = m_KeyDonor.GetEntityId(),
                Visible = true,
            };
        }
    }
}

#endif
