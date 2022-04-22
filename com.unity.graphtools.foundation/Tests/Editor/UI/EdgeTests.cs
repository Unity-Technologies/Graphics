using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine.TestTools;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.UI
{
    class EdgeTests : BaseUIFixture
    {
        protected override bool CreateGraphOnStartup => true;

        [UnityTest]
        public IEnumerator SelectingSourceNodeShowsReorderablePortEdgeLabels()
        {
            var nodeModel1 = GraphModel.CreateNode<Type0FakeNodeModel>("Node0", new Vector2(0, 0));
            var nodeModel2 = GraphModel.CreateNode<Type0FakeNodeModel>("Node1", new Vector2(210, 0));
            var nodeModel3 = GraphModel.CreateNode<Type0FakeNodeModel>("Node2", new Vector2(210, 210));
            var edge1 = GraphModel.CreateEdge(nodeModel2.ExeInput0, nodeModel1.ExeOutput0);
            var edge2 = GraphModel.CreateEdge(nodeModel3.ExeInput0, nodeModel1.ExeOutput0);
            MarkGraphModelStateDirty();
            yield return null;

            Assert.IsFalse(ShouldShowLabel(edge1));
            Assert.IsFalse(ShouldShowLabel(edge2));

            GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, nodeModel1));
            yield return null;

            Assert.IsTrue(ShouldShowLabel(edge1));
            Assert.AreEqual(edge1.EdgeLabel, "1");
            Assert.IsTrue(ShouldShowLabel(edge2));
            Assert.AreEqual(edge2.EdgeLabel, "2");
        }

        bool ShouldShowLabel(IEdgeModel edge)
        {
            return EdgeBubblePart.EdgeShouldShowLabel(edge, GraphView.GraphViewModel.SelectionState);
        }

        [UnityTest]
        public IEnumerator SelectingDestinationNodeDoesNotShowEdgeOrder()
        {
            var nodeModel1 = GraphModel.CreateNode<Type0FakeNodeModel>("Node0", new Vector2(0, 0));
            var nodeModel2 = GraphModel.CreateNode<Type0FakeNodeModel>("Node1", new Vector2(210, 0));
            var nodeModel3 = GraphModel.CreateNode<Type0FakeNodeModel>("Node2", new Vector2(210, 210));
            var edge1 = GraphModel.CreateEdge(nodeModel2.ExeInput0, nodeModel1.ExeOutput0);
            var edge2 = GraphModel.CreateEdge(nodeModel3.ExeInput0, nodeModel1.ExeOutput0);
            MarkGraphModelStateDirty();
            yield return null;

            Assert.IsFalse(ShouldShowLabel(edge1));
            Assert.IsFalse(ShouldShowLabel(edge2));

            GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, nodeModel2));
            yield return null;

            Assert.IsFalse(ShouldShowLabel(edge1));
            Assert.IsFalse(ShouldShowLabel(edge2));
        }

        [UnityTest]
        public IEnumerator SelectingEdgeShowsEdgeOrderOnSelfAndSibling()
        {
            var nodeModel1 = GraphModel.CreateNode<Type0FakeNodeModel>("Node0", new Vector2(0, 0));
            var nodeModel2 = GraphModel.CreateNode<Type0FakeNodeModel>("Node1", new Vector2(210, 0));
            var nodeModel3 = GraphModel.CreateNode<Type0FakeNodeModel>("Node2", new Vector2(210, 210));
            var edge1 = GraphModel.CreateEdge(nodeModel2.ExeInput0, nodeModel1.ExeOutput0);
            var edge2 = GraphModel.CreateEdge(nodeModel3.ExeInput0, nodeModel1.ExeOutput0);
            MarkGraphModelStateDirty();
            yield return null;

            Assert.IsFalse(ShouldShowLabel(edge1));
            Assert.IsFalse(ShouldShowLabel(edge2));

            GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, edge1));
            yield return null;

            Assert.IsTrue(ShouldShowLabel(edge1));
            Assert.AreEqual(edge1.EdgeLabel, "1");
            Assert.IsTrue(ShouldShowLabel(edge2));
            Assert.AreEqual(edge2.EdgeLabel, "2");
        }

        [UnityTest]
        public IEnumerator DeleteSelectedNodesAndEdgeDoesNotThrow()
        {
            var operatorModel = GraphModel.CreateNode<Type0FakeNodeModel>("Node0", new Vector2(-100, -100));
            var intModel = GraphModel.CreateConstantNode(typeof(int).GenerateTypeHandle(), "int", new Vector2(-150, -100));
            var edge = GraphModel.CreateEdge(operatorModel.Input0, intModel.OutputPort);
            yield return null;

            var elements = new IGraphElementModel[] { operatorModel, intModel, edge };
            GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, elements));
            yield return null;

            var nodes = new[] { operatorModel, intModel as IGraphElementModel };
            GraphModel.DeleteElements(nodes);
            yield return null;
        }
    }
}
