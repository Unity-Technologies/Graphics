using System;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.TestModels;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.CommandSystem
{
    public class SelectionStateTests
    {
        [Test]
        public void AddingToSelectionWorks()
        {
            var graphModel = new GraphModel();
            var node1 = graphModel.CreateNode<NodeModel>();
            var node2 = graphModel.CreateNode<NodeModel>();

            var state = new SelectionStateComponent();
            using (var selectionUpdater = state.UpdateScope)
            {
                selectionUpdater.ClearSelection(graphModel);
            }

            Assert.IsFalse(state.IsSelected(node1));
            Assert.IsFalse(state.IsSelected(node2));

            using (var selectionUpdater = state.UpdateScope)
            {
                selectionUpdater.SelectElements(new[] { node1 }, true);
            }

            Assert.IsTrue(state.IsSelected(node1));
            Assert.IsFalse(state.IsSelected(node2));
        }

        [Test]
        public void RemovingFromSelectionWorks()
        {
            var graphModel = new GraphModel();
            var node1 = graphModel.CreateNode<NodeModel>();
            var node2 = graphModel.CreateNode<NodeModel>();

            var state = new SelectionStateComponent();

            using (var selectionUpdater = state.UpdateScope)
            {
                selectionUpdater.ClearSelection(graphModel);
                selectionUpdater.SelectElements(new[] { node1, node2 }, true);
            }

            Assert.IsTrue(state.IsSelected(node1));
            Assert.IsTrue(state.IsSelected(node2));

            using (var selectionUpdater = state.UpdateScope)
            {
                selectionUpdater.SelectElements(new[] { node1 }, false);
            }

            Assert.IsFalse(state.IsSelected(node1));
            Assert.IsTrue(state.IsSelected(node2));
        }

        [Test]
        public void ClearSelectionWorks()
        {
            var graphModel = new GraphModel();
            var node1 = graphModel.CreateNode<NodeModel>();
            var node2 = graphModel.CreateNode<NodeModel>();

            var viewGuid1 = SerializableGUID.Generate();
            var state = new SelectionStateComponent();

            using (var selectionUpdater = state.UpdateScope)
            {
                selectionUpdater.ClearSelection(graphModel);
                selectionUpdater.SelectElements(new[] { node1, node2 }, true);
            }

            Assert.IsTrue(state.IsSelected(node1));
            Assert.IsTrue(state.IsSelected(node2));

            using (var selectionUpdater = state.UpdateScope)
            {
                selectionUpdater.ClearSelection(graphModel);
            }

            Assert.IsFalse(state.IsSelected(node1));
            Assert.IsFalse(state.IsSelected(node2));
        }
    }
}
