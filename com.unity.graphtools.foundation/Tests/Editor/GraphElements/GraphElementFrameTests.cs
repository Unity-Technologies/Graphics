using System;
using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.TestModels;
using UnityEngine;
using UnityEngine.TestTools;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    class GraphElementFrameTests : GraphViewTester
    {
        class FooNode : IONodeModel
        {
        }

        class BarNode : IONodeModel
        {
        }

        [UnityTest]
        public IEnumerator FrameSelectedNodeAndEdge()
        {
            Vector2 firstNodePosition = new Vector2(400, 400);
            Vector2 secondNodePosition = new Vector2(800, 800);

            var firstNodeModel = CreateNode("First Node", firstNodePosition, 0, 2);
            var secondNodeModel = CreateNode("Second Node", secondNodePosition, 2);

            var startPort = firstNodeModel.GetOutputPorts().First();
            var endPort = secondNodeModel.GetInputPorts().First();

            MarkGraphViewStateDirty();
            yield return null;

            var actions = ConnectPorts(startPort, endPort);
            while (actions.MoveNext())
            {
                yield return null;
            }

            var edgeModel = startPort.GetConnectedEdges().First();
            graphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Add, edgeModel, secondNodeModel));

            Assert.AreEqual(0.0, graphView.ContentViewContainer.transform.position.x);
            Assert.AreEqual(0.0, graphView.ContentViewContainer.transform.position.y);

            graphView.DispatchFrameSelectionCommand();
            yield return null;

            Assert.LessOrEqual(graphView.ContentViewContainer.transform.position.x, -firstNodePosition.x / 2);
            Assert.LessOrEqual(graphView.ContentViewContainer.transform.position.y, -firstNodePosition.y / 2);
        }

        void AssertSingleSelectedElementTypeAndName(Type modelType, string name)
        {
            Assert.That(graphView.GetSelection().Count, NUnit.Framework.Is.EqualTo(1));
            Assert.That(graphView.GetSelection().First(), NUnit.Framework.Is.AssignableTo(typeof(INodeModel)));
            Assert.That(graphView.GetSelection().First(), NUnit.Framework.Is.AssignableTo(modelType));
            Assert.That((graphView.GetSelection().First() as IHasTitle)?.Title, NUnit.Framework.Is.EqualTo(name));
        }

        [Test]
        public void FrameNextPrevTest()
        {
            CreateNode<FooNode>("N0", Vector2.zero);
            CreateNode<FooNode>("N1", Vector2.zero);
            CreateNode<FooNode>("N2", Vector2.zero);
            CreateNode<FooNode>("N3", Vector2.zero);

            graphView.RebuildUI();

            graphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, GraphModel.GraphElementModels.First()));

            graphView.DispatchFrameNextCommand(e => true);
            AssertSingleSelectedElementTypeAndName(typeof(FooNode), "N1");

            graphView.DispatchFrameNextCommand(e => true);
            AssertSingleSelectedElementTypeAndName(typeof(FooNode), "N2");

            graphView.DispatchFrameNextCommand(e => true);
            AssertSingleSelectedElementTypeAndName(typeof(FooNode), "N3");

            graphView.DispatchFrameNextCommand(e => true);
            AssertSingleSelectedElementTypeAndName(typeof(FooNode), "N0");

            graphView.DispatchFramePrevCommand(e => true);
            AssertSingleSelectedElementTypeAndName(typeof(FooNode), "N3");

            graphView.DispatchFramePrevCommand(e => true);
            AssertSingleSelectedElementTypeAndName(typeof(FooNode), "N2");

            graphView.DispatchFramePrevCommand(e => true);
            AssertSingleSelectedElementTypeAndName(typeof(FooNode), "N1");

            graphView.DispatchFramePrevCommand(e => true);
            AssertSingleSelectedElementTypeAndName(typeof(FooNode), "N0");
        }

        [Test]
        public void FrameNextPrevWithoutSelectionTest()
        {
            CreateNode<FooNode>("N0", Vector2.zero);
            CreateNode<FooNode>("N1", Vector2.zero);
            CreateNode<FooNode>("N2", Vector2.zero);
            CreateNode<FooNode>("N3", Vector2.zero);

            graphView.RebuildUI();

            // Reset selection for next test
            graphView.Dispatch(new ClearSelectionCommand());

            graphView.DispatchFrameNextCommand(e => true);
            AssertSingleSelectedElementTypeAndName(typeof(FooNode), "N0");

            // Reset selection for prev test
            graphView.Dispatch(new ClearSelectionCommand());

            graphView.DispatchFramePrevCommand(e => true);
            AssertSingleSelectedElementTypeAndName(typeof(FooNode), "N3");
        }

        [Test]
        public void FrameNextPrevPredicateTest()
        {
            var f0 = CreateNode<FooNode>("F0", Vector2.zero);
            CreateNode<FooNode>("F1", Vector2.zero);
            CreateNode<BarNode>("B0", Vector2.zero);
            CreateNode<BarNode>("B1", Vector2.zero);
            CreateNode<FooNode>("F2", Vector2.zero);
            CreateNode<BarNode>("B2", Vector2.zero);

            graphView.RebuildUI();

            graphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, f0));

            graphView.DispatchFrameNextCommand(x => x.Model is FooNode);
            AssertSingleSelectedElementTypeAndName(typeof(FooNode), "F1");

            graphView.DispatchFrameNextCommand(IsFooNode);
            AssertSingleSelectedElementTypeAndName(typeof(FooNode), "F2");

            graphView.DispatchFrameNextCommand(x => x.Model is FooNode);
            AssertSingleSelectedElementTypeAndName(typeof(FooNode), "F0");

            graphView.DispatchFramePrevCommand(IsFooNode);
            AssertSingleSelectedElementTypeAndName(typeof(FooNode), "F2");

            graphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, f0));

            graphView.DispatchFrameNextCommand(x => (x.Model as IHasTitle)?.Title.Contains("0") ?? false);
            AssertSingleSelectedElementTypeAndName(typeof(NodeModel), "B0");

            graphView.DispatchFrameNextCommand(x => (x.Model as IHasTitle)?.Title.Contains("0") ?? false);
            AssertSingleSelectedElementTypeAndName(typeof(NodeModel), "F0");
        }

        private bool IsFooNode(ModelUI element)
        {
            return element.Model is FooNode;
        }
    }
}
