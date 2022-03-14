using System;
using NUnit.Framework;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEngine.GraphToolsFoundation.Overdrive.Tests.CommandSystem
{
    public class StateTests
    {
        void PurgeAllChangesets(IState state)
        {
            foreach (var stateComponent in state.AllStateComponents)
            {
                stateComponent.PurgeOldChangesets(uint.MaxValue);
            }
        }

        [Test]
        public void GetUpdateTypeUsingAnotherComponentVersionReturnsComplete()
        {
            // This tests the case where a component instance is replaced by another instance in GraphToolState.

            var state = new TestGraphToolState(42);
            var otherComponentVersion = state.FooBarStateComponent.GetStateComponentVersion();
            state = new TestGraphToolState(43);
            var version = state.FooBarStateComponent.GetStateComponentVersion();
            Assert.AreEqual(UpdateType.None, state.FooBarStateComponent.GetUpdateType(version));
            Assert.AreEqual(UpdateType.Complete, state.FooBarStateComponent.GetUpdateType(otherComponentVersion));
        }

        [Test]
        public void UpdatingAStateComponentMakesHasChangesTrue()
        {
            var state = new TestGraphToolState(42);
            PurgeAllChangesets(state);
            Assert.IsFalse((state.FooBarStateComponent as IStateComponent).HasChanges());

            using (state.FooBarStateComponent.UpdateScope)
            {
                // This block intentionally left blank.
            }

            Assert.IsTrue((state.FooBarStateComponent as IStateComponent).HasChanges());
        }

        [Test]
        [TestCase(UpdateType.Partial)]
        [TestCase(UpdateType.Complete)]
        public void PurgeAllChangesetsClearsHasChangesAndUpdateType(UpdateType updateType)
        {
            var state = new TestGraphToolState(42);

            // Make some changes.
            using (state.FooBarStateComponent.UpdateScope)
            {
                state.FooBarStateComponent.SetUpdateType(updateType);
            }
            Assert.IsTrue((state.FooBarStateComponent as IStateComponent).HasChanges());

            PurgeAllChangesets(state);
            Assert.IsFalse((state.FooBarStateComponent as IStateComponent).HasChanges());
        }

        [Test]
        public void PurgeAllChangesetsMakesUpdateTypeCompleteForOutdatedObservers()
        {
            var state = new TestGraphToolState(42);
            var version = state.FooBarStateComponent.GetStateComponentVersion();

            // Make some changes.
            using (state.FooBarStateComponent.UpdateScope)
            {
                state.FooBarStateComponent.SetUpdateType(UpdateType.Complete);
            }
            Assert.AreNotEqual(UpdateType.None, state.FooBarStateComponent.GetUpdateType(version));

            PurgeAllChangesets(state);
            Assert.AreEqual(UpdateType.Complete, state.FooBarStateComponent.GetUpdateType(version));
        }

        [Test]
        public void SettingUpdateTypeToPartialMakesGetUpdateTypeReturnsPartial()
        {
            var state = new TestGraphToolState(42);
            var version = state.FooBarStateComponent.GetStateComponentVersion();

            PurgeAllChangesets(state);
            Assert.AreEqual(UpdateType.None, state.FooBarStateComponent.GetUpdateType(version));

            using (state.FooBarStateComponent.UpdateScope)
            {
                state.FooBarStateComponent.SetUpdateType(UpdateType.Partial);
            }

            Assert.AreEqual(UpdateType.Partial, state.FooBarStateComponent.GetUpdateType(version));
        }

        [Test]
        public void SettingUpdateTypeToCompleteMakesGetUpdateTypeReturnsComplete()
        {
            var state = new TestGraphToolState(42);
            var version = state.FooBarStateComponent.GetStateComponentVersion();

            PurgeAllChangesets(state);
            Assert.AreEqual(UpdateType.None, state.FooBarStateComponent.GetUpdateType(version));

            using (state.FooBarStateComponent.UpdateScope)
            {
                state.FooBarStateComponent.SetUpdateType(UpdateType.Complete);
            }

            Assert.AreEqual(UpdateType.Complete, state.FooBarStateComponent.GetUpdateType(version));
        }

        [Test]
        public void SettingUpdateTypeToNoneMakesGetUpdateTypeReturnsNone()
        {
            var state = new TestGraphToolState(42);
            var version = state.FooBarStateComponent.GetStateComponentVersion();

            PurgeAllChangesets(state);
            Assert.AreEqual(UpdateType.None, state.FooBarStateComponent.GetUpdateType(version));

            using (state.FooBarStateComponent.UpdateScope)
            {
                state.FooBarStateComponent.SetUpdateType(UpdateType.None);
            }

            Assert.AreEqual(UpdateType.None, state.FooBarStateComponent.GetUpdateType(version));
        }

        [Test]
        public void NotSettingUpdateTypeMakesGetUpdateTypeReturnsNone()
        {
            var state = new TestGraphToolState(42);
            var version = state.FooBarStateComponent.GetStateComponentVersion();

            PurgeAllChangesets(state);
            Assert.AreEqual(UpdateType.None, state.FooBarStateComponent.GetUpdateType(version));

            using (state.FooBarStateComponent.UpdateScope)
            {
                // This block intentionally left blank.
            }

            Assert.AreEqual(UpdateType.None, state.FooBarStateComponent.GetUpdateType(version));
        }
    }
}
