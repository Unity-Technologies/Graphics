using System;
using System.Linq;
using NUnit.Framework;
using Object = UnityEngine.Object;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.CommandSystem
{
    public class GraphModelStateTests
    {
        IGraphAsset m_Asset1;

        [SetUp]
        public void SetUp()
        {
            m_Asset1 = GraphAssetCreationHelpers<ClassGraphAsset>.CreateInMemoryGraphAsset(typeof(ClassStencil), "Test1");
        }

        [Test]
        public void EmptyChangeSetsDoNotDirtyAsset()
        {
            var state = new GraphModelStateComponent();
            var initialDirtyCount = EditorUtility.GetDirtyCount(m_Asset1 as Object);

            using (var graphUpdater = state.UpdateScope)
            {
                graphUpdater.MarkNew(Enumerable.Empty<IGraphElementModel>());
                graphUpdater.MarkChanged(Enumerable.Empty<IGraphElementModel>());
                graphUpdater.MarkDeleted(Enumerable.Empty<IGraphElementModel>());
            }

            Assert.AreEqual(initialDirtyCount, EditorUtility.GetDirtyCount(m_Asset1 as Object));
        }

        [Test]
        public void MarkNewRemovesModelFromTheChangedList()
        {
            var state = new GraphModelStateComponent();
            var dummyModel = new TestNodeModel();
            using (var graphUpdater = state.UpdateScope)
            {
                graphUpdater.MarkChanged(dummyModel);
            }

            var changes = state.GetAggregatedChangeset(0);
            Assert.IsTrue(changes.ChangedModels.Contains(dummyModel));

            using (var graphUpdater = state.UpdateScope)
            {
                graphUpdater.MarkNew(dummyModel);
            }

            changes = state.GetAggregatedChangeset(0);
            Assert.IsFalse(changes.ChangedModels.Contains(dummyModel));
            Assert.IsTrue(changes.NewModels.Contains(dummyModel));
        }

        [Test]
        public void MarkNewHasNoEffectIfModelIsDeleted()
        {
            var state = new GraphModelStateComponent();
            var dummyModel = new TestNodeModel();
            using (var graphUpdater = state.UpdateScope)
            {
                graphUpdater.MarkDeleted(dummyModel);
            }

            var changes = state.GetAggregatedChangeset(0);
            Assert.IsTrue(changes.DeletedModels.Contains(dummyModel));

            using (var graphUpdater = state.UpdateScope)
            {
                graphUpdater.MarkNew(dummyModel);
            }

            changes = state.GetAggregatedChangeset(0);
            Assert.IsFalse(changes.NewModels.Contains(dummyModel));
        }

        [Test]
        public void MarkChangedHasNoEffectIfModelIsNew()
        {
            var state = new GraphModelStateComponent();
            var dummyModel = new TestNodeModel();
            using (var graphUpdater = state.UpdateScope)
            {
                graphUpdater.MarkNew(dummyModel);
            }

            var changes = state.GetAggregatedChangeset(0);
            Assert.IsTrue(changes.NewModels.Contains(dummyModel));

            using (var graphUpdater = state.UpdateScope)
            {
                graphUpdater.MarkChanged(dummyModel);
            }

            changes = state.GetAggregatedChangeset(0);
            Assert.IsFalse(changes.ChangedModels.Contains(dummyModel));
        }

        [Test]
        public void MarkChangedHasNoEffectIfModelIsDeleted()
        {
            var state = new GraphModelStateComponent();
            var dummyModel = new TestNodeModel();
            using (var graphUpdater = state.UpdateScope)
            {
                graphUpdater.MarkDeleted(dummyModel);
            }

            var changes = state.GetAggregatedChangeset(0);
            Assert.IsTrue(changes.DeletedModels.Contains(dummyModel));

            using (var graphUpdater = state.UpdateScope)
            {
                graphUpdater.MarkChanged(dummyModel);
            }

            changes = state.GetAggregatedChangeset(0);
            Assert.IsFalse(changes.ChangedModels.Contains(dummyModel));
        }

        [Test]
        public void MarkChangedHintsAreCumulative()
        {
            var state = new GraphModelStateComponent();
            var dummyModel = new TestNodeModel();
            using (var graphUpdater = state.UpdateScope)
            {
                graphUpdater.MarkChanged(dummyModel, ChangeHint.Data);
                graphUpdater.MarkChanged(dummyModel, ChangeHint.Layout);
            }

            var changes = state.GetAggregatedChangeset(0);
            Assert.IsTrue(changes.ChangedModelsAndHints.First(kv => ReferenceEquals(kv.Key, dummyModel)).Value.Contains(ChangeHint.Data));
            Assert.IsTrue(changes.ChangedModelsAndHints.First(kv => ReferenceEquals(kv.Key, dummyModel)).Value.Contains(ChangeHint.Layout));
        }

        [Test]
        public void MarkChangedHintsApplyToAllModels()
        {
            var state = new GraphModelStateComponent();
            var dummyModel1 = new TestNodeModel();
            var dummyModel2 = new TestNodeModel();
            using (var graphUpdater = state.UpdateScope)
            {
                graphUpdater.MarkChanged(new [] {dummyModel1, dummyModel2}, ChangeHint.Data);
            }

            var changes = state.GetAggregatedChangeset(0);
            Assert.IsTrue(changes.ChangedModelsAndHints.First(kv => ReferenceEquals(kv.Key, dummyModel1)).Value.Contains(ChangeHint.Data));
            Assert.IsTrue(changes.ChangedModelsAndHints.First(kv => ReferenceEquals(kv.Key, dummyModel2)).Value.Contains(ChangeHint.Data));
        }

        [Test]
        public void MarkDeletedRemovesModelFromTheNewList()
        {
            var state = new GraphModelStateComponent();
            var dummyModel = new TestNodeModel();
            using (var graphUpdater = state.UpdateScope)
            {
                graphUpdater.MarkNew(dummyModel);
            }

            var changes = state.GetAggregatedChangeset(0);
            Assert.IsTrue(changes.NewModels.Contains(dummyModel));

            using (var graphUpdater = state.UpdateScope)
            {
                graphUpdater.MarkDeleted(dummyModel);
            }

            changes = state.GetAggregatedChangeset(0);
            Assert.IsFalse(changes.NewModels.Contains(dummyModel));
            Assert.IsTrue(changes.DeletedModels.Contains(dummyModel));
        }

        [Test]
        public void MarkDeletedRemovesModelFromTheChangedList()
        {
            var state = new GraphModelStateComponent();
            var dummyModel = new TestNodeModel();
            using (var graphUpdater = state.UpdateScope)
            {
                graphUpdater.MarkChanged(dummyModel);
            }

            var changes = state.GetAggregatedChangeset(0);
            Assert.IsTrue(changes.ChangedModels.Contains(dummyModel));

            using (var graphUpdater = state.UpdateScope)
            {
                graphUpdater.MarkDeleted(dummyModel);
            }

            changes = state.GetAggregatedChangeset(0);
            Assert.IsFalse(changes.ChangedModels.Contains(dummyModel));
            Assert.IsTrue(changes.DeletedModels.Contains(dummyModel));
        }
    }
}
