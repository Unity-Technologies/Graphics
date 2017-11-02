using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEditor.Graphing;

namespace UnityEditor.Graphing.UnitTests
{
    [TestFixture]
    public class SerializableGraphTests
    {
        [OneTimeSetUp]
        public void RunBeforeAnyTests()
        {
            Debug.unityLogger.logHandler = new ConsoleLogHandler();
        }

        [Test]
        public void TestCanCreateSerializableGraph()
        {
            var graph = new SerializableGraph();

            Assert.AreEqual(0, graph.edges.Count());
            Assert.AreEqual(0, graph.GetNodes<INode>().Count());
        }

        [Test]
        public void TestCanAddNodeToSerializableGraph()
        {
            var graph = new SerializableGraph();
            var node = new SerializableNode();
            node.name = "Test Node";
            graph.AddNode(node);

            Assert.AreEqual(1, graph.GetNodes<INode>().Count());
            Assert.AreEqual("Test Node", graph.GetNodes<INode>().FirstOrDefault().name);
            Assert.AreEqual(graph, node.owner);
        }

        [Test]
        public void TestCanRemoveNodeFromSerializableGraph()
        {
            var graph = new SerializableGraph();
            var node = new SerializableNode();
            node.name = "Test Node";
            graph.AddNode(node);
            Assert.AreEqual(1, graph.GetNodes<INode>().Count());

            graph.RemoveNode(graph.GetNodes<INode>().FirstOrDefault());
            Assert.AreEqual(0, graph.GetNodes<INode>().Count());
        }

        [Test]
        public void TestCanModifyNodeDrawState()
        {
            var node = new SerializableNode();
            node.name = "Test Node";

            var drawState = node.drawState;
            var newPos = new Rect(10, 10, 0, 0);
            drawState.position = newPos;
            drawState.expanded = false;
            node.drawState = drawState;

            Assert.AreEqual(drawState, node.drawState);
            Assert.AreEqual(newPos, node.drawState.position);
            Assert.IsFalse(node.drawState.expanded);
        }

        private class SetErrorNode : SerializableNode
        {
            public void SetError()
            {
                hasError = true;
            }

            public void ClearError()
            {
                hasError = false;
            }
        }

        [Test]
        public void TestChildClassCanModifyErrorState()
        {
            var node = new SetErrorNode();
            node.SetError();
            Assert.IsTrue(node.hasError);
            node.ClearError();
            Assert.IsFalse(node.hasError);
        }

        [Test]
        public void TestNodeGUIDCanBeRewritten()
        {
            var node = new SerializableNode();
            var guid = node.guid;
            var newGuid = node.RewriteGuid();
            Assert.AreNotEqual(guid, newGuid);
        }

        public class TestableNode : SerializableNode
        {
            public const int Input0 = 0;
            public const int Input1 = 1;
            public const int Input2 = 2;

            public const int Output0 = 3;
            public const int Output1 = 4;
            public const int Output2 = 5;

            public TestableNode()
            {
                AddSlot(new SerializableSlot(Input0, "Input", SlotType.Input));
                AddSlot(new SerializableSlot(Input1, "Input", SlotType.Input));
                AddSlot(new SerializableSlot(Input2, "Input", SlotType.Input));

                AddSlot(new SerializableSlot(Output0, "Output", SlotType.Output));
                AddSlot(new SerializableSlot(Output1, "Output", SlotType.Output));
                AddSlot(new SerializableSlot(Output2, "Output", SlotType.Output));
            }
        }

        [Test]
        public void TestRemoveNodeFromSerializableGraphCleansEdges()
        {
            var graph = new SerializableGraph();
            var outputNode = new TestableNode();
            graph.AddNode(outputNode);

            var inputNode = new TestableNode();
            graph.AddNode(inputNode);

            Assert.AreEqual(2, graph.GetNodes<INode>().Count());
            var createdEdge = graph.Connect(outputNode.GetSlotReference(TestableNode.Output0), inputNode.GetSlotReference(TestableNode.Input0));
            Assert.AreEqual(1, graph.edges.Count());

            var edge = graph.edges.FirstOrDefault();

            Assert.AreEqual(createdEdge, edge);

            graph.RemoveNode(outputNode);

            Assert.AreEqual(1, graph.GetNodes<INode>().Count());
            Assert.AreEqual(0, graph.edges.Count());
            Assert.AreEqual(inputNode, graph.GetNodes<INode>().FirstOrDefault());
        }

        private class NoDeleteNode : SerializableNode
        {
            public override bool canDeleteNode { get { return false; } }
        }

        [Test]
        public void TestCanNotRemoveNoDeleteNodeFromSerializableGraph()
        {
            var graph = new SerializableGraph();
            var node = new NoDeleteNode();
            node.name = "Test Node";
            graph.AddNode(node);
            Assert.AreEqual(1, graph.GetNodes<INode>().Count());

            graph.RemoveNode(graph.GetNodes<INode>().FirstOrDefault());
            Assert.AreEqual(1, graph.GetNodes<INode>().Count());
        }

        private class OnEnableNode : SerializableNode, IOnAssetEnabled
        {
            public bool called = false;
            public void OnEnable()
            {
                called = true;
            }
        }

        [Test]
        public void TestSerializedGraphDelegatesOnEnableCalls()
        {
            var graph = new SerializableGraph();
            var node = new OnEnableNode();
            node.name = "Test Node";
            graph.AddNode(node);

            Assert.IsFalse(node.called);
            graph.OnEnable();
            Assert.IsTrue(node.called);
        }

        [Test]
        public void TestCanFindNodeInSerializableGraph()
        {
            var graph = new SerializableGraph();
            var node = new SerializableNode();
            graph.AddNode(node);

            Assert.AreEqual(1, graph.GetNodes<INode>().Count());
            Assert.IsNotNull(graph.GetNodeFromGuid(node.guid));
            Assert.IsNull(graph.GetNodeFromGuid(Guid.NewGuid()));
        }

        [Test]
        public void TestCanAddSlotToSerializableNode()
        {
            var graph = new SerializableGraph();
            var node = new SerializableNode();
            node.AddSlot(new SerializableSlot(0, "output", SlotType.Output));
            node.AddSlot(new SerializableSlot(1, "input", SlotType.Input));
            node.name = "Test Node";
            graph.AddNode(node);

            Assert.AreEqual(1, graph.GetNodes<INode>().Count());
            var found = graph.GetNodes<INode>().FirstOrDefault();
            Assert.AreEqual(1, found.GetInputSlots<ISlot>().Count());
            Assert.AreEqual(1, found.GetInputSlots<ISlot>().FirstOrDefault().id);
            Assert.AreEqual(1, found.GetOutputSlots<ISlot>().Count());
            Assert.AreEqual(0, found.GetOutputSlots<ISlot>().FirstOrDefault().id);
            Assert.AreEqual(2, found.GetSlots<ISlot>().Count());
        }

        [Test]
        public void TestCanNotAddNullSlotToSerializableNode()
        {
            var node = new SerializableNode();
            node.AddSlot(null);
            node.name = "Test Node";
            Assert.AreEqual(0, node.GetOutputSlots<ISlot>().Count());
        }

        [Test]
        public void TestCanRemoveSlotFromSerializableNode()
        {
            var graph = new SerializableGraph();
            var node = new SerializableNode();
            node.AddSlot(new SerializableSlot(0, "output", SlotType.Output));
            node.AddSlot(new SerializableSlot(1, "input", SlotType.Input));
            graph.AddNode(node);

            Assert.AreEqual(2, node.GetSlots<ISlot>().Count());
            Assert.AreEqual(1, node.GetInputSlots<ISlot>().Count());
            Assert.AreEqual(1, node.GetOutputSlots<ISlot>().Count());

            node.RemoveSlot(1);

            Assert.AreEqual(1, node.GetSlots<ISlot>().Count());
            Assert.AreEqual(0, node.GetInputSlots<ISlot>().Count());
            Assert.AreEqual(1, node.GetOutputSlots<ISlot>().Count());
        }

        [Test]
        public void TestCanRemoveSlotsWithNonMathingNameFromSerializableNode()
        {
            var graph = new SerializableGraph();
            var node = new TestableNode();
            graph.AddNode(node);

            Assert.AreEqual(6, node.GetSlots<ISlot>().Count());
            Assert.AreEqual(3, node.GetInputSlots<ISlot>().Count());
            Assert.AreEqual(3, node.GetOutputSlots<ISlot>().Count());

            node.RemoveSlotsNameNotMatching(new[] {TestableNode.Input1});

            Assert.AreEqual(1, node.GetSlots<ISlot>().Count());
            Assert.AreEqual(1, node.GetInputSlots<ISlot>().Count());
            Assert.AreEqual(0, node.GetOutputSlots<ISlot>().Count());

            Assert.IsNull(node.FindInputSlot<ISlot>(TestableNode.Input0));
            Assert.IsNotNull(node.FindInputSlot<ISlot>(TestableNode.Input1));
            Assert.IsNull(node.FindInputSlot<ISlot>(TestableNode.Input2));
        }

        [Test]
        public void TestCanNotAddDuplicateSlotToSerializableNode()
        {
            var graph = new SerializableGraph();
            var node = new SerializableNode();
            node.AddSlot(new SerializableSlot(0, "output", SlotType.Output));
            node.AddSlot(new SerializableSlot(0, "output", SlotType.Output));
            node.name = "Test Node";
            graph.AddNode(node);

            Assert.AreEqual(1, graph.GetNodes<INode>().Count());
            var found = graph.GetNodes<INode>().FirstOrDefault();
            Assert.AreEqual(0, found.GetInputSlots<ISlot>().Count());
            Assert.AreEqual(1, found.GetOutputSlots<ISlot>().Count());
            Assert.AreEqual(1, found.GetSlots<ISlot>().Count());
        }

        [Test]
        public void TestCanUpdateDisplaynameByReaddingSlotToSerializableNode()
        {
            var graph = new SerializableGraph();
            var node = new SerializableNode();
            node.AddSlot(new SerializableSlot(0, "output", SlotType.Output));
            node.AddSlot(new SerializableSlot(0, "output_updated", SlotType.Output));
            node.name = "Test Node";
            graph.AddNode(node);

            Assert.AreEqual(1, graph.GetNodes<INode>().Count());
            var found = graph.GetNodes<INode>().FirstOrDefault();
            Assert.AreEqual(0, found.GetInputSlots<ISlot>().Count());
            Assert.AreEqual(1, found.GetOutputSlots<ISlot>().Count());
            Assert.AreEqual(1, found.GetSlots<ISlot>().Count());

            var slot = found.GetOutputSlots<ISlot>().FirstOrDefault();
            Assert.AreEqual("output_updated", slot.displayName);
        }

        [Test]
        public void TestCanUpdateSlotPriority()
        {
            var graph = new SerializableGraph();
            var node = new SerializableNode();
            node.AddSlot(new SerializableSlot(0, "output", SlotType.Output, 0));
            node.name = "Test Node";
            graph.AddNode(node);

            Assert.AreEqual(1, graph.GetNodes<INode>().Count());
            var found = graph.GetNodes<INode>().FirstOrDefault();
            Assert.AreEqual(0, found.GetInputSlots<ISlot>().Count());
            Assert.AreEqual(1, found.GetOutputSlots<ISlot>().Count());
            Assert.AreEqual(1, found.GetSlots<ISlot>().Count());

            var slot = found.GetOutputSlots<ISlot>().FirstOrDefault();
            Assert.AreEqual(0, slot.priority);
            slot.priority = 2;
            Assert.AreEqual(2, slot.priority);
        }

        [Test]
        public void TestCanUpdateSlotPriorityByReaddingSlotToSerializableNode()
        {
            var graph = new SerializableGraph();
            var node = new SerializableNode();
            node.AddSlot(new SerializableSlot(0, "output", SlotType.Output, 0));
            node.AddSlot(new SerializableSlot(0, "output", SlotType.Output, 5));
            node.name = "Test Node";
            graph.AddNode(node);

            Assert.AreEqual(1, graph.GetNodes<INode>().Count());
            var found = graph.GetNodes<INode>().FirstOrDefault();
            Assert.AreEqual(0, found.GetInputSlots<ISlot>().Count());
            Assert.AreEqual(1, found.GetOutputSlots<ISlot>().Count());
            Assert.AreEqual(1, found.GetSlots<ISlot>().Count());

            var slot = found.GetOutputSlots<ISlot>().FirstOrDefault();
            Assert.AreEqual(5, slot.priority);
        }

        [Test]
        public void TestCanUpdateSlotDisplayName()
        {
            var node = new SerializableNode();
            node.AddSlot(new SerializableSlot(0, "output", SlotType.Output));
            node.name = "Test Node";

            Assert.AreEqual(0, node.GetInputSlots<ISlot>().Count());
            Assert.AreEqual(1, node.GetOutputSlots<ISlot>().Count());
            Assert.AreEqual(1, node.GetSlots<ISlot>().Count());

            var slot = node.GetOutputSlots<ISlot>().FirstOrDefault();
            Assert.IsNotNull(slot);
            Assert.AreEqual("output", slot.displayName);
            slot.displayName = "test";
            Assert.AreEqual("test", slot.displayName);
        }

        [Test]
        public void TestCanFindSlotOnSerializableNode()
        {
            var node = new TestableNode();

            Assert.AreEqual(6, node.GetSlots<ISlot>().Count());
            Assert.IsNotNull(node.FindInputSlot<ISlot>(TestableNode.Input0));
            Assert.IsNull(node.FindInputSlot<ISlot>(TestableNode.Output0));
            Assert.IsNotNull(node.FindOutputSlot<ISlot>(TestableNode.Output0));
            Assert.IsNull(node.FindOutputSlot<ISlot>(TestableNode.Input0));

            Assert.IsNotNull(node.FindSlot<ISlot>(TestableNode.Input0));
            Assert.IsNotNull(node.FindSlot<ISlot>(TestableNode.Output0));
            Assert.IsNull(node.FindSlot<ISlot>(555));
        }

        [Test]
        public void TestCanFindSlotReferenceOnSerializableNode()
        {
            var node = new TestableNode();

            Assert.AreEqual(6, node.GetSlots<ISlot>().Count());
            Assert.IsNotNull(node.GetSlotReference(TestableNode.Input0));
            Assert.IsNotNull(node.GetSlotReference(TestableNode.Output0));
            Assert.Null(node.GetSlotReference(555));
        }

        [Test]
        public void TestCanConnectAndTraverseTwoNodesOnSerializableGraph()
        {
            var graph = new SerializableGraph();

            var outputNode = new TestableNode();
            graph.AddNode(outputNode);

            var inputNode = new TestableNode();
            graph.AddNode(inputNode);

            Assert.AreEqual(2, graph.GetNodes<INode>().Count());


            var createdEdge = graph.Connect(outputNode.GetSlotReference(TestableNode.Output0), inputNode.GetSlotReference(TestableNode.Input0));
            Assert.AreEqual(1, graph.edges.Count());

            var edge = graph.edges.FirstOrDefault();

            Assert.AreEqual(createdEdge, edge);

            var foundOutputNode = graph.GetNodeFromGuid(edge.outputSlot.nodeGuid);
            var foundOutputSlot = foundOutputNode.FindOutputSlot<ISlot>(edge.outputSlot.slotId);
            Assert.AreEqual(outputNode, foundOutputNode);
            Assert.IsNotNull(foundOutputSlot);

            var foundInputNode = graph.GetNodeFromGuid(edge.inputSlot.nodeGuid);
            var foundInputSlot = foundInputNode.FindInputSlot<ISlot>(edge.inputSlot.slotId);
            Assert.AreEqual(inputNode, foundInputNode);
            Assert.IsNotNull(foundInputSlot);
        }

        [Test]
        public void TestCanConnectAndTraverseThreeNodesOnSerializableGraph()
        {
            var graph = new SerializableGraph();

            var outputNode = new TestableNode();
            graph.AddNode(outputNode);

            var middleNode = new TestableNode();
            graph.AddNode(middleNode);

            var inputNode = new TestableNode();
            graph.AddNode(inputNode);

            Assert.AreEqual(3, graph.GetNodes<INode>().Count());

            graph.Connect(outputNode.GetSlotReference(TestableNode.Output0), middleNode.GetSlotReference(TestableNode.Input0));
            Assert.AreEqual(1, graph.edges.Count());

            graph.Connect(middleNode.GetSlotReference(TestableNode.Output0), inputNode.GetSlotReference(TestableNode.Input0));
            Assert.AreEqual(2, graph.edges.Count());

            var edgesOnMiddleNode = NodeUtils.GetAllEdges(middleNode);
            Assert.AreEqual(2, edgesOnMiddleNode.Count());

            List<INode> result = new List<INode>();
            NodeUtils.DepthFirstCollectNodesFromNode(result, inputNode);
            Assert.AreEqual(3, result.Count);

            result.Clear();
            NodeUtils.DepthFirstCollectNodesFromNode(result, inputNode, NodeUtils.IncludeSelf.Exclude);
            Assert.AreEqual(2, result.Count);

            result.Clear();
            NodeUtils.DepthFirstCollectNodesFromNode(result, null);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void TestExceptionIfBadNodeConfigurationWorks()
        {
            var node = new TestableNode();
            Assert.DoesNotThrow(
                () =>
                NodeUtils.SlotConfigurationExceptionIfBadConfiguration(
                    node,
                    new[] {TestableNode.Input0, TestableNode.Input1, TestableNode.Input2},
                    new[] {TestableNode.Output0, TestableNode.Output1, TestableNode.Output2, })
                );


            Assert.Throws<SlotConfigurationException>(
                () =>
                NodeUtils.SlotConfigurationExceptionIfBadConfiguration(
                    node,
                    new[] {666, TestableNode.Input1, TestableNode.Input2},
                    new[] {TestableNode.Output0, TestableNode.Output1, TestableNode.Output2, })
                );

            Assert.Throws<SlotConfigurationException>(
                () =>
                NodeUtils.SlotConfigurationExceptionIfBadConfiguration(
                    node,
                    new[] {TestableNode.Input0, TestableNode.Input1, TestableNode.Input2},
                    new[] {666, TestableNode.Output1, TestableNode.Output2, })
                );

            Assert.DoesNotThrow(
                () =>
                NodeUtils.SlotConfigurationExceptionIfBadConfiguration(
                    node,
                    new[] {TestableNode.Input0},
                    new[] {TestableNode.Output0})
                );
        }

        [Test]
        public void TestConectionToSameInputReplacesOldInput()
        {
            var graph = new SerializableGraph();

            var outputNode = new TestableNode();
            graph.AddNode(outputNode);

            var inputNode = new TestableNode();
            graph.AddNode(inputNode);

            Assert.AreEqual(2, graph.GetNodes<INode>().Count());

            var createdEdge = graph.Connect(outputNode.GetSlotReference(TestableNode.Output0), inputNode.GetSlotReference(TestableNode.Input0));
            Assert.AreEqual(1, graph.edges.Count());
            var edge = graph.edges.FirstOrDefault();
            Assert.AreEqual(createdEdge, edge);

            var createdEdge2 = graph.Connect(outputNode.GetSlotReference(TestableNode.Output0), inputNode.GetSlotReference(TestableNode.Input0));
            Assert.AreEqual(1, graph.edges.Count());
            var edge2 = graph.edges.FirstOrDefault();
            Assert.AreEqual(createdEdge2, edge2);
        }

        [Test]
        public void TestRemovingSlotRemovesConnectedEdges()
        {
            var graph = new SerializableGraph();

            var outputNode = new TestableNode();
            graph.AddNode(outputNode);

            var inputNode = new TestableNode();
            graph.AddNode(inputNode);

            Assert.AreEqual(2, graph.GetNodes<INode>().Count());

            graph.Connect(outputNode.GetSlotReference(TestableNode.Output0), inputNode.GetSlotReference(TestableNode.Input0));
            Assert.AreEqual(1, graph.edges.Count());

            outputNode.RemoveSlot(TestableNode.Output0);
            Assert.AreEqual(0, graph.edges.Count());
        }

        [Test]
        public void TestCanNotConnectToNullSlot()
        {
            var graph = new SerializableGraph();

            var outputNode = new TestableNode();
            graph.AddNode(outputNode);

            var inputNode = new SerializableNode();
            graph.AddNode(inputNode);

            Assert.AreEqual(2, graph.GetNodes<INode>().Count());

            var createdEdge = graph.Connect(outputNode.GetSlotReference(TestableNode.Output0), null);
            Assert.AreEqual(0, graph.edges.Count());
            Assert.IsNull(createdEdge);

            var createdEdge2 = graph.Connect(outputNode.GetSlotReference(TestableNode.Output0), new SlotReference(Guid.NewGuid(), 666));
            Assert.AreEqual(0, graph.edges.Count());
            Assert.IsNull(createdEdge2);
        }

        [Test]
        public void TestCanNotConnectTwoOuputSlotsOnSerializableGraph()
        {
            var graph = new SerializableGraph();

            var outputNode = new TestableNode();
            graph.AddNode(outputNode);

            var outputNode2 = new TestableNode();
            graph.AddNode(outputNode2);

            Assert.AreEqual(2, graph.GetNodes<INode>().Count());

            var createdEdge = graph.Connect(outputNode.GetSlotReference(TestableNode.Output0), outputNode2.GetSlotReference(TestableNode.Output0));
            Assert.IsNull(createdEdge);
            Assert.AreEqual(0, graph.edges.Count());
        }

        [Test]
        public void TestCanNotConnectTwoInputSlotsOnSerializableGraph()
        {
            var graph = new SerializableGraph();

            var inputNode = new TestableNode();
            graph.AddNode(inputNode);

            var inputNode2 = new TestableNode();
            graph.AddNode(inputNode2);

            Assert.AreEqual(2, graph.GetNodes<INode>().Count());

            var createdEdge = graph.Connect(inputNode.GetSlotReference(TestableNode.Input0), inputNode2.GetSlotReference(TestableNode.Input0));
            Assert.IsNull(createdEdge);
            Assert.AreEqual(0, graph.edges.Count());
        }

        [Test]
        public void TestRemovingNodeRemovesConectedEdgesOnSerializableGraph()
        {
            var graph = new SerializableGraph();
            var outputNode = new TestableNode();
            graph.AddNode(outputNode);

            var inputNode = new TestableNode();
            graph.AddNode(inputNode);

            Assert.AreEqual(2, graph.GetNodes<INode>().Count());
            graph.Connect(outputNode.GetSlotReference(TestableNode.Output0), inputNode.GetSlotReference(TestableNode.Input0));
            Assert.AreEqual(1, graph.edges.Count());

            graph.RemoveNode(graph.GetNodes<INode>().FirstOrDefault());
            Assert.AreEqual(1, graph.GetNodes<INode>().Count());
            Assert.AreEqual(0, graph.edges.Count());
        }

        [Test]
        public void TestRemovingEdgeOnSerializableGraph()
        {
            var graph = new SerializableGraph();
            var outputNode = new TestableNode();
            graph.AddNode(outputNode);

            var inputNode = new TestableNode();
            graph.AddNode(inputNode);

            Assert.AreEqual(2, graph.GetNodes<INode>().Count());
            graph.Connect(outputNode.GetSlotReference(TestableNode.Output0), inputNode.GetSlotReference(TestableNode.Input0));
            Assert.AreEqual(1, graph.edges.Count());

            graph.RemoveEdge(graph.edges.FirstOrDefault());
            Assert.AreEqual(2, graph.GetNodes<INode>().Count());
            Assert.AreEqual(0, graph.edges.Count());
        }

        [Test]
        public void TestRemovingElementsFromSerializableGraph()
        {
            var graph = new SerializableGraph();
            var outputNode = new TestableNode();
            graph.AddNode(outputNode);

            var inputNode = new TestableNode();
            graph.AddNode(inputNode);

            Assert.AreEqual(2, graph.GetNodes<INode>().Count());
            graph.Connect(outputNode.GetSlotReference(TestableNode.Output0), inputNode.GetSlotReference(TestableNode.Input0));
            Assert.AreEqual(1, graph.edges.Count());

            graph.RemoveElements(graph.GetNodes<INode>(), graph.edges);
            Assert.AreEqual(0, graph.GetNodes<INode>().Count());
            Assert.AreEqual(0, graph.edges.Count());
        }

        [Test]
        public void TestCanGetEdgesOnSerializableGraphFromSlotReference()
        {
            var graph = new SerializableGraph();
            var outputNode = new TestableNode();
            graph.AddNode(outputNode);

            var inputNode = new TestableNode();
            graph.AddNode(inputNode);

            Assert.AreEqual(2, graph.GetNodes<INode>().Count());
            graph.Connect(outputNode.GetSlotReference(TestableNode.Output0), inputNode.GetSlotReference(TestableNode.Input0));
            Assert.AreEqual(1, graph.edges.Count());

            Assert.AreEqual(1, graph.GetEdges(inputNode.GetSlotReference(TestableNode.Input0)).Count());
            Assert.AreEqual(1, graph.GetEdges(outputNode.GetSlotReference(TestableNode.Output0)).Count());
            Assert.AreEqual(0, graph.GetEdges(outputNode.GetSlotReference(666)).Count());
        }

        [Test]
        public void TestGetInputsWithNoConnection()
        {
            var graph = new SerializableGraph();

            var outputNode = new TestableNode();
            graph.AddNode(outputNode);

            var inputNode = new TestableNode();
            graph.AddNode(inputNode);

            Assert.AreEqual(2, graph.GetNodes<INode>().Count());
            graph.Connect(outputNode.GetSlotReference(TestableNode.Output0), inputNode.GetSlotReference(TestableNode.Input0));
            Assert.AreEqual(1, graph.edges.Count());

            var slots = inputNode.GetInputsWithNoConnection();
            Assert.AreEqual(2, slots.Count());
            CollectionAssert.AreEqual(new[] { TestableNode.Input1, TestableNode.Input2 }, slots.Select(x => x.id));
        }

        [Test]
        public void TestCyclicConnectionsAreNotAllowedOnGraph()
        {
            var graph = new SerializableGraph();

            var nodeA = new TestableNode();

            graph.AddNode(nodeA);

            var nodeB = new TestableNode();
            graph.AddNode(nodeB);

            Assert.AreEqual(2, graph.GetNodes<INode>().Count());
            graph.Connect(nodeA.GetSlotReference(TestableNode.Output0), nodeB.GetSlotReference(TestableNode.Input0));
            Assert.AreEqual(1, graph.edges.Count());

            var edge = graph.Connect(nodeB.GetSlotReference(TestableNode.Output0), nodeA.GetSlotReference(TestableNode.Input0));
            Assert.IsNull(edge);
            Assert.AreEqual(1, graph.edges.Count());
        }
    }
}
