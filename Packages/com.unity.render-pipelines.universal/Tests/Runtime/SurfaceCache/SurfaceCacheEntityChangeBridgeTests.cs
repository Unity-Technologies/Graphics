#if SURFACE_CACHE_SUPPORTED || UNITY_EDITOR

using System.Collections.Generic;
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

            SurfaceCacheEntityChangeBridge.PushChanged(MakeRecord());

            var secondChanges = new SurfaceCacheWorldChangeSet();
            LogAssert.Expect(LogType.Error, EntityChangeSource.k_ContentionError);
            second.CollectChanges(secondChanges);
            second.EndCollectChanges();
            Assert.IsEmpty(secondChanges.EntityInstanceChangedList, "A detached source should not receive records.");

            var firstChanges = new SurfaceCacheWorldChangeSet();
            first.CollectChanges(firstChanges);
            first.EndCollectChanges();
            CollectionAssert.IsNotEmpty(firstChanges.EntityInstanceChangedList, "The registered source should receive the pushed record.");

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

            SurfaceCacheEntityChangeBridge.PushChanged(MakeRecord());

            var changes = new SurfaceCacheWorldChangeSet();
            second.CollectChanges(changes);
            second.EndCollectChanges();
            CollectionAssert.IsNotEmpty(changes.EntityInstanceChangedList, "A source that took over the bridge should receive records.");

            second.Dispose();
            Assert.IsFalse(SurfaceCacheEntityChangeBridge.IsConsumerPresent);
        }

        [Test]
        public void CollectChanges_DrainAborted_PendingIsRedeliveredOnNextCollect()
        {
            var source = new EntityChangeSource();
            source.CollectChanges(new SurfaceCacheWorldChangeSet());
            source.EndCollectChanges();

            SurfaceCacheEntityChangeBridge.PushChanged(MakeRecord());

            source.CollectChanges(new SurfaceCacheWorldChangeSet());
            source.AbortCollectChanges();

            var retried = new SurfaceCacheWorldChangeSet();
            source.CollectChanges(retried);
            source.EndCollectChanges();
            CollectionAssert.IsNotEmpty(retried.EntityInstanceChangedList, "An aborted drain should redeliver its records on the next collect.");

            var afterCommit = new SurfaceCacheWorldChangeSet();
            source.CollectChanges(afterCommit);
            source.EndCollectChanges();
            Assert.IsEmpty(afterCommit.EntityInstanceChangedList, "A committed drain should clear the delivered records.");

            source.Dispose();
        }

        [Test]
        public void CollectChanges_TransformPushedAfterAbort_FoldsIntoRetainedRecord()
        {
            var source = new EntityChangeSource();
            source.CollectChanges(new SurfaceCacheWorldChangeSet());
            source.EndCollectChanges();

            SurfaceCacheEntityChangeBridge.PushChanged(MakeRecord());

            source.CollectChanges(new SurfaceCacheWorldChangeSet());
            source.AbortCollectChanges();

            var moved = Matrix4x4.Translate(new Vector3(1.0f, 2.0f, 3.0f));
            var transforms = SurfaceCacheEntityChangeBridge.AcquireTransformChangedList();
            transforms.Add(new SurfaceCacheEntityTransformRecord { Key = m_KeyDonor.GetEntityId(), LocalToWorld = moved });

            var retried = new SurfaceCacheWorldChangeSet();
            source.CollectChanges(retried);
            Assert.AreEqual(0, retried.EntityInstanceTransformChangedList.Length, "A transform superseded by a pending record should not be delivered separately.");
            source.EndCollectChanges();

            var retriedRecord = retried.EntityInstanceChangedList.Single();
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
            SurfaceCacheEntityChangeBridge.PushChanged(record);

            var retried = new SurfaceCacheWorldChangeSet();
            source.CollectChanges(retried);
            Assert.AreEqual(0, retried.EntityInstanceTransformChangedList.Length, "A transform retained from before the record must not be redelivered.");
            source.EndCollectChanges();

            var retriedRecord = retried.EntityInstanceChangedList.Single();
            Assert.AreEqual(moved, retriedRecord.LocalToWorld, "A retained transform must not be folded over a newer record.");

            source.Dispose();
        }

        [Test]
        public void UnregisterConsumer_DuringStuckDrain_NextConsumerDrainsCleanly()
        {
            var changed = new List<SurfaceCacheEntityInstanceRecord>();
            var destroyed = new List<EntityId>();

            Assert.IsTrue(SurfaceCacheEntityChangeBridge.TryRegisterConsumer());

            SurfaceCacheEntityChangeBridge.BeginDrain(changed, destroyed, out _);

            SurfaceCacheEntityChangeBridge.UnregisterConsumer();

            Assert.IsTrue(SurfaceCacheEntityChangeBridge.TryRegisterConsumer());
            SurfaceCacheEntityChangeBridge.BeginDrain(changed, destroyed, out _);
            SurfaceCacheEntityChangeBridge.EndDrain();
            SurfaceCacheEntityChangeBridge.UnregisterConsumer();
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
