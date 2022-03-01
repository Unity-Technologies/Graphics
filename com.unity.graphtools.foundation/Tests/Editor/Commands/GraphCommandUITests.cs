using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.UI.Commands
{
    class GraphCommandUITests : BaseUIFixture
    {
        protected override bool CreateGraphOnStartup => true;
        protected override Type CreatedGraphType => typeof(ClassStencil);

        [UnityTest]
        public IEnumerator PanToNodeChangesViewTransform()
        {
            var operatorModel = GraphModel.CreateNode<Type0FakeNodeModel>("Node0", new Vector2(-100, -100));
            var nodeA = GraphModel.CreateNode<Type0FakeNodeModel>("A", Vector2.zero);
            var nodeB = GraphModel.CreateNode<Type0FakeNodeModel>("B", new Vector2(100, 100));
            GraphView.Dispatch(new ReframeGraphViewCommand(Vector3.zero, Vector3.one));
            MarkGraphViewStateDirty();
            yield return null;

            yield return SendPanToNodeAndRefresh(operatorModel);
            yield return SendPanToNodeAndRefresh(nodeA);
            yield return SendPanToNodeAndRefresh(nodeB);

            IEnumerator SendPanToNodeAndRefresh(NodeModel nodeModel)
            {
                var node = nodeModel.GetUI<GraphElement>(GraphView);
                Vector3 pOrig = GraphView.ContentViewContainer.transform.position;
                GraphView.DispatchFrameAndSelectElementsCommand(true, node);
                yield return null;

                Vector3 p = GraphView.ContentViewContainer.transform.position;
                Assert.AreNotEqual(pOrig, p, "ViewTransform position did not change");
                Assert.That(GraphView.GetSelection().
                    Where(n => ReferenceEquals(n, nodeModel)).Any,
                    () =>
                    {
                        var graphViewSelection = String.Join(",", GraphView.GetSelection());
                        return $"Selection doesn't contain {nodeModel} {nodeModel.Title} but {graphViewSelection}";
                    });
            }
        }

        [UnityTest]
        public IEnumerator RefreshUIPreservesSelection()
        {
            var nodeA = GraphModel.CreateNode<Type0FakeNodeModel>("A", new Vector2(100, -100));
            var nodeB = GraphModel.CreateNode<Type0FakeNodeModel>("B", new Vector2(100, 100));

            MarkGraphViewStateDirty();
            yield return null;

            GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, nodeA));
            yield return SendPanToNodeAndRefresh(nodeA);
            GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, nodeB));
            yield return SendPanToNodeAndRefresh(nodeB);

            IEnumerator SendPanToNodeAndRefresh(NodeModel nodeModel)
            {
                yield return null;
                Assert.That(GraphView.GetSelection().
                    Where(n => ReferenceEquals(n, nodeModel)).Any,
                    () =>
                    {
                        var graphViewSelection = String.Join(",", GraphView.GetSelection().Select(x =>
                            x.ToString()));
                        return $"Selection doesn't contain {nodeModel} {nodeModel.Title} but {graphViewSelection}";
                    });
            }
        }

        [UnityTest]
        public IEnumerator DuplicateNodeAndEdgeCreatesEdgeToOriginalNode()
        {
            var declaration0 = GraphModel.CreateGraphVariableDeclaration(typeof(int).GenerateTypeHandle(), "decl0", ModifierFlags.None, true);

            var nodeA = GraphModel.CreateVariableNode(declaration0, new Vector2(100, -100));
            var nodeB = GraphModel.CreateNode<Type0FakeNodeModel>("A", new Vector2(100, 100));

            var edge = GraphModel.CreateEdge(nodeB.Input0, nodeA.OutputPort) as EdgeModel;

            MarkGraphViewStateDirty();
            yield return null;

            GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, new GraphElementModel[] { nodeB, edge }));

            GraphView.Focus();
            using (var evt = ExecuteCommandEvent.GetPooled("Duplicate"))
            {
                evt.target = GraphView;
                GraphView.SendEvent(evt);
            }
            yield return null;

            Assert.AreEqual(3, GraphModel.NodeModels.Count);
            Assert.AreEqual(2, GraphModel.EdgeModels.Count);
            foreach (var edgeModel in GraphModel.EdgeModels)
            {
                Assert.AreEqual(nodeA.OutputPort, edgeModel.FromPort);
            }
        }

        [UnityTest]
        public IEnumerator ReframeGraphViewWorks()
        {
            var node = GraphModel.CreateNode<Type0FakeNodeModel>("A", new Vector2(100, 100));

            GraphView.Dispatch(new ReframeGraphViewCommand(Vector3.zero, Vector3.one));
            yield return null;

            var position = GraphView.ContentViewContainer.transform.position;
            var scale = GraphView.ContentViewContainer.transform.scale;

            Assert.AreEqual(Vector3.zero, position);
            Assert.AreEqual(Vector3.one, scale);
            Assert.IsFalse(GraphView.SelectionState.IsSelected(node));

            var newPos = new Vector3(32, 45, 0);
            var newScale = new Vector3(0.42f, 0.42f, 1.0f);
            GraphView.Dispatch(new ReframeGraphViewCommand(newPos, newScale, new List<IGraphElementModel> { node }));
            yield return null;

            position = GraphView.ContentViewContainer.transform.position;
            scale = GraphView.ContentViewContainer.transform.scale;

            Assert.AreEqual(newPos, position);
            Assert.AreEqual(newScale, scale);
            Assert.IsTrue(GraphView.SelectionState.IsSelected(node));
        }

        [UnityTest]
        public IEnumerator FrameAllWorks()
        {
            for (int i = 0; i < 4; ++i)
            {
                GraphModel.CreateNode<Type0FakeNodeModel>("", new Vector2(10 + 50 * i, 30 * i));
            }

            MarkGraphViewStateDirty();
            yield return null;

            GraphView.Dispatch(new ReframeGraphViewCommand(new Vector3(-500, -500, 0), new Vector3(.9f, .9f, .9f)));
            yield return null;

            GraphView.DispatchFrameAllCommand();
            yield return null;

            foreach (var nodeModel in GraphModel.NodeModels)
            {
                var nodeUI = nodeModel.GetUI<GraphElement>(GraphView);
                Assert.IsNotNull(nodeUI);
                Assert.IsTrue(GraphView.layout.Contains(nodeUI.layout.min),
                    $"Node {GraphModel.NodeModels.IndexOfInternal(nodeModel)} min is outside graph view.");
                Assert.IsTrue(GraphView.layout.Contains(nodeUI.layout.max),
                    $"Node {GraphModel.NodeModels.IndexOfInternal(nodeModel)} max is outside graph view.");
            }
        }

        [UnityTest]
        public IEnumerator FramePreviousElementWorks()
        {
            var nodeA = GraphModel.CreateNode<Type0FakeNodeModel>("A", new Vector2(-1000, 100));
            var nodeB = GraphModel.CreateNode<Type0FakeNodeModel>("B", new Vector2(100, 100));
            var nodeC = GraphModel.CreateNode<Type0FakeNodeModel>("C", new Vector2(1000, 100));

            GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, nodeB));
            MarkGraphViewStateDirty();
            yield return null;

            var initialPos = GraphView.ContentViewContainer.transform.position;
            Assert.IsFalse(GraphView.SelectionState.IsSelected(nodeA));
            Assert.IsTrue(GraphView.SelectionState.IsSelected(nodeB));
            Assert.IsFalse(GraphView.SelectionState.IsSelected(nodeC));

            GraphView.DispatchFramePrevCommand(e => true);
            yield return null;

            Assert.AreNotEqual(initialPos, GraphView.ContentViewContainer.transform.position);
            Assert.IsTrue(GraphView.SelectionState.IsSelected(nodeA));
            Assert.IsFalse(GraphView.SelectionState.IsSelected(nodeB));
            Assert.IsFalse(GraphView.SelectionState.IsSelected(nodeC));

            // Check that cycling work.
            GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, nodeA));
            yield return null;

            GraphView.DispatchFramePrevCommand(e => true);
            yield return null;

            Assert.IsFalse(GraphView.SelectionState.IsSelected(nodeA));
            Assert.IsFalse(GraphView.SelectionState.IsSelected(nodeB));
            Assert.IsTrue(GraphView.SelectionState.IsSelected(nodeC));
        }

        [UnityTest]
        public IEnumerator FrameNextElementWorks()
        {
            var nodeA = GraphModel.CreateNode<Type0FakeNodeModel>("A", new Vector2(-1000, 100));
            var nodeB = GraphModel.CreateNode<Type0FakeNodeModel>("B", new Vector2(100, 100));
            var nodeC = GraphModel.CreateNode<Type0FakeNodeModel>("C", new Vector2(1000, 100));

            GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, nodeB));
            MarkGraphViewStateDirty();
            yield return null;

            var initialPos = GraphView.ContentViewContainer.transform.position;
            Assert.IsFalse(GraphView.SelectionState.IsSelected(nodeA));
            Assert.IsTrue(GraphView.SelectionState.IsSelected(nodeB));
            Assert.IsFalse(GraphView.SelectionState.IsSelected(nodeC));

            GraphView.DispatchFrameNextCommand(e => true);
            yield return null;

            Assert.AreNotEqual(initialPos, GraphView.ContentViewContainer.transform.position);
            Assert.IsFalse(GraphView.SelectionState.IsSelected(nodeA));
            Assert.IsFalse(GraphView.SelectionState.IsSelected(nodeB));
            Assert.IsTrue(GraphView.SelectionState.IsSelected(nodeC));

            // Check that cycling work.
            GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, nodeC));
            yield return null;

            GraphView.DispatchFrameNextCommand(e => true);
            yield return null;

            Assert.IsTrue(GraphView.SelectionState.IsSelected(nodeA));
            Assert.IsFalse(GraphView.SelectionState.IsSelected(nodeB));
            Assert.IsFalse(GraphView.SelectionState.IsSelected(nodeC));
        }
    }
}
