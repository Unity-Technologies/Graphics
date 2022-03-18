using System;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
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
            GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Add, edgeModel, secondNodeModel));

            Assert.AreEqual(0.0, GraphView.ContentViewContainer.transform.position.x);
            Assert.AreEqual(0.0, GraphView.ContentViewContainer.transform.position.y);

            GraphView.DispatchFrameSelectionCommand();
            yield return null;

            Assert.LessOrEqual(GraphView.ContentViewContainer.transform.position.x, -firstNodePosition.x / 2);
            Assert.LessOrEqual(GraphView.ContentViewContainer.transform.position.y, -firstNodePosition.y / 2);
        }

        [SuppressMessage("ReSharper", "AccessToStaticMemberViaDerivedType")]
        void AssertSingleSelectedElementTypeAndName(Type modelType, string name)
        {
            Assert.That(GraphView.GetSelection().Count, Is.EqualTo(1));
            Assert.That(GraphView.GetSelection().First(), Is.AssignableTo(typeof(INodeModel)));
            Assert.That(GraphView.GetSelection().First(), Is.AssignableTo(modelType));
            Assert.That((GraphView.GetSelection().First() as IHasTitle)?.Title, Is.EqualTo(name));
        }

        [Test]
        public void FrameNextPrevTest()
        {
            CreateNode<FooNode>("N0", Vector2.zero);
            CreateNode<FooNode>("N1", Vector2.zero);
            CreateNode<FooNode>("N2", Vector2.zero);
            CreateNode<FooNode>("N3", Vector2.zero);

            GraphView.RebuildUI();

            GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, GraphModel.NodeModels.First()));

            GraphView.DispatchFrameNextCommand(_ => true);
            AssertSingleSelectedElementTypeAndName(typeof(FooNode), "N1");

            GraphView.DispatchFrameNextCommand(_ => true);
            AssertSingleSelectedElementTypeAndName(typeof(FooNode), "N2");

            GraphView.DispatchFrameNextCommand(_ => true);
            AssertSingleSelectedElementTypeAndName(typeof(FooNode), "N3");

            GraphView.DispatchFrameNextCommand(_ => true);
            AssertSingleSelectedElementTypeAndName(typeof(FooNode), "N0");

            GraphView.DispatchFramePrevCommand(_ => true);
            AssertSingleSelectedElementTypeAndName(typeof(FooNode), "N3");

            GraphView.DispatchFramePrevCommand(_ => true);
            AssertSingleSelectedElementTypeAndName(typeof(FooNode), "N2");

            GraphView.DispatchFramePrevCommand(_ => true);
            AssertSingleSelectedElementTypeAndName(typeof(FooNode), "N1");

            GraphView.DispatchFramePrevCommand(_ => true);
            AssertSingleSelectedElementTypeAndName(typeof(FooNode), "N0");
        }

        [Test]
        public void FrameNextPrevWithoutSelectionTest()
        {
            CreateNode<FooNode>("N0", Vector2.zero);
            CreateNode<FooNode>("N1", Vector2.zero);
            CreateNode<FooNode>("N2", Vector2.zero);
            CreateNode<FooNode>("N3", Vector2.zero);

            GraphView.RebuildUI();

            // Reset selection for next test
            GraphView.Dispatch(new ClearSelectionCommand());

            GraphView.DispatchFrameNextCommand(_ => true);
            AssertSingleSelectedElementTypeAndName(typeof(FooNode), "N0");

            // Reset selection for prev test
            GraphView.Dispatch(new ClearSelectionCommand());

            GraphView.DispatchFramePrevCommand(_ => true);
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

            GraphView.RebuildUI();

            GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, f0));

            GraphView.DispatchFrameNextCommand(x => x.Model is FooNode);
            AssertSingleSelectedElementTypeAndName(typeof(FooNode), "F1");

            GraphView.DispatchFrameNextCommand(IsFooNode);
            AssertSingleSelectedElementTypeAndName(typeof(FooNode), "F2");

            GraphView.DispatchFrameNextCommand(x => x.Model is FooNode);
            AssertSingleSelectedElementTypeAndName(typeof(FooNode), "F0");

            GraphView.DispatchFramePrevCommand(IsFooNode);
            AssertSingleSelectedElementTypeAndName(typeof(FooNode), "F2");

            GraphView.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Replace, f0));

            GraphView.DispatchFrameNextCommand(x => (x.Model as IHasTitle)?.Title.Contains("0") ?? false);
            AssertSingleSelectedElementTypeAndName(typeof(NodeModel), "B0");

            GraphView.DispatchFrameNextCommand(x => (x.Model as IHasTitle)?.Title.Contains("0") ?? false);
            AssertSingleSelectedElementTypeAndName(typeof(NodeModel), "F0");
        }

        static bool IsFooNode(ModelView element)
        {
            return element.Model is FooNode;
        }
    }
}
