using System;
using System.Linq;
using NUnit.Framework;
using Object = UnityEngine.Object;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.CommandSystem
{
    public class GraphViewStateTests
    {
        IGraphAssetModel m_Asset1;

        [SetUp]
        public void SetUp()
        {
            m_Asset1 = GraphAssetCreationHelpers<ClassGraphAssetModel>.CreateInMemoryGraphAsset(typeof(ClassStencil), "Test1");
        }

        [Test]
        public void EmptyChangeSetsDoNotDirtyAsset()
        {
            var state = new GraphViewStateComponent();
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
            var state = new GraphViewStateComponent();
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
            var state = new GraphViewStateComponent();
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
            var state = new GraphViewStateComponent();
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
            var state = new GraphViewStateComponent();
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
        public void MarkDeletedRemovesModelFromTheNewList()
        {
            var state = new GraphViewStateComponent();
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
            var state = new GraphViewStateComponent();
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
