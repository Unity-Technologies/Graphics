using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Graphing;

namespace UnityEditor.Graphing.UnitTests
{
    [TestFixture]
    public class ScriptableGraphTests
    {
        [TestFixtureSetUp]
        public void RunBeforeAnyTests()
        {
            Debug.logger.logHandler = new ConsoleLogHandler();
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
        public void TestRemoveNodeFromSerializableGraphCleansEdges()
        {
            var graph = new SerializableGraph();
            var outputNode = new SerializableNode();
            var outputSlot = new SerializableSlot("output", "output", SlotType.Output, 0);
            outputNode.AddSlot(outputSlot);
            graph.AddNode(outputNode);

            var inputNode = new SerializableNode();
            var inputSlot = new SerializableSlot("input", "input", SlotType.Input, 0);
            inputNode.AddSlot(inputSlot);
            graph.AddNode(inputNode);

            Assert.AreEqual(2, graph.GetNodes<INode>().Count());
            var createdEdge = graph.Connect(outputNode.GetSlotReference("output"), inputNode.GetSlotReference("input"));
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
            node.AddSlot(new SerializableSlot("output", "output", SlotType.Output, 0));
            node.AddSlot(new SerializableSlot("input", "input", SlotType.Input, 0));
            node.name = "Test Node";
            graph.AddNode(node);

            Assert.AreEqual(1, graph.GetNodes<INode>().Count());
            var found = graph.GetNodes<INode>().FirstOrDefault();
            Assert.AreEqual(1, found.GetInputSlots<ISlot>().Count());
            Assert.AreEqual("input", found.GetInputSlots<ISlot>().FirstOrDefault().name);
            Assert.AreEqual(1, found.GetOutputSlots<ISlot>().Count());
            Assert.AreEqual("output", found.GetOutputSlots<ISlot>().FirstOrDefault().name);
            Assert.AreEqual(2, found.GetSlots<ISlot>().Count());
        }

        [Test]
        public void TestCanRemoveSlotFromSerializableNode()
        {
            var node = new SerializableNode();
            node.AddSlot(new SerializableSlot("output", "output", SlotType.Output, 0));
            node.AddSlot(new SerializableSlot("input", "input", SlotType.Input, 0));

            Assert.AreEqual(2, node.GetSlots<ISlot>().Count());
            Assert.AreEqual(1, node.GetInputSlots<ISlot>().Count());
            Assert.AreEqual(1, node.GetOutputSlots<ISlot>().Count());

            node.RemoveSlot("input");

            Assert.AreEqual(1, node.GetSlots<ISlot>().Count());
            Assert.AreEqual(0, node.GetInputSlots<ISlot>().Count());
            Assert.AreEqual(1, node.GetOutputSlots<ISlot>().Count());
        }

        [Test]
        public void TestCanRemoveSlotsWithNonMathingNameFromSerializableNode()
        {
            var node = new SerializableNode();
            node.AddSlot(new SerializableSlot("input1", "input", SlotType.Input, 0));
            node.AddSlot(new SerializableSlot("input2", "input", SlotType.Input, 0));
            node.AddSlot(new SerializableSlot("input3", "input", SlotType.Input, 0));
            node.AddSlot(new SerializableSlot("input4", "input", SlotType.Input, 0));

            Assert.AreEqual(4, node.GetSlots<ISlot>().Count());
            Assert.AreEqual(4, node.GetInputSlots<ISlot>().Count());
            Assert.AreEqual(0, node.GetOutputSlots<ISlot>().Count());

            node.RemoveSlotsNameNotMatching(new []{"input1", "input3"});

            Assert.AreEqual(2, node.GetSlots<ISlot>().Count());
            Assert.AreEqual(2, node.GetInputSlots<ISlot>().Count());
            Assert.AreEqual(0, node.GetOutputSlots<ISlot>().Count());

            Assert.IsNotNull(node.FindInputSlot<ISlot>("input1"));
            Assert.IsNull(node.FindInputSlot<ISlot>("input2"));
            Assert.IsNotNull(node.FindInputSlot<ISlot>("input3"));
            Assert.IsNull(node.FindInputSlot<ISlot>("input4"));
        }

        [Test]
        public void TestCanNotAddDuplicateSlotToSerializableNode()
        {
            var graph = new SerializableGraph();
            var node = new SerializableNode();
            node.AddSlot(new SerializableSlot("output", "output", SlotType.Output, 0));
            node.AddSlot(new SerializableSlot("output", "output", SlotType.Output, 0));
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
            node.AddSlot(new SerializableSlot("output", "output", SlotType.Output, 0));
            node.AddSlot(new SerializableSlot("output", "output_updated", SlotType.Output, 0));
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
        public void TestCanUpdatePriorityByReaddingSlotToSerializableNode()
        {
            var graph = new SerializableGraph();
            var node = new SerializableNode();
            node.AddSlot(new SerializableSlot("output", "output", SlotType.Output, 0));
            node.AddSlot(new SerializableSlot("output", "output", SlotType.Output, 1));
            node.name = "Test Node";
            graph.AddNode(node);

            Assert.AreEqual(1, graph.GetNodes<INode>().Count());
            var found = graph.GetNodes<INode>().FirstOrDefault();
            Assert.AreEqual(0, found.GetInputSlots<ISlot>().Count());
            Assert.AreEqual(1, found.GetOutputSlots<ISlot>().Count());
            Assert.AreEqual(1, found.GetSlots<ISlot>().Count());

            var slot = found.GetOutputSlots<ISlot>().FirstOrDefault();
            Assert.AreEqual(1, slot.priority);
        }

        [Test]
        public void TestCanFindSlotOnSerializableNode()
        {
            var node = new SerializableNode();
            node.AddSlot(new SerializableSlot("output", "output", SlotType.Output, 0));
            node.AddSlot(new SerializableSlot("input", "input", SlotType.Input, 0));

            Assert.AreEqual(2, node.GetSlots<ISlot>().Count());
            Assert.IsNotNull(node.FindInputSlot<ISlot>("input"));
            Assert.IsNull(node.FindInputSlot<ISlot>("output"));
            Assert.IsNotNull(node.FindOutputSlot<ISlot>("output"));
            Assert.IsNull(node.FindOutputSlot<ISlot>("input"));

            Assert.IsNotNull(node.FindSlot<ISlot>("input"));
            Assert.IsNotNull(node.FindSlot<ISlot>("output"));
            Assert.IsNull(node.FindSlot<ISlot>("invalid"));
        }

        [Test]
        public void TestCanFindSlotReferenceOnSerializableNode()
        {
            var node = new SerializableNode();
            node.AddSlot(new SerializableSlot("output", "output", SlotType.Output, 0));
            node.AddSlot(new SerializableSlot("input", "input", SlotType.Input, 0));

            Assert.AreEqual(2, node.GetSlots<ISlot>().Count());
            Assert.IsNotNull(node.GetSlotReference("input"));
            Assert.IsNotNull(node.GetSlotReference("output"));
            Assert.Null(node.GetSlotReference("invalid"));
        }

        [Test]
        public void TestCanConnectAndTraverseTwoNodesOnSerializableGraph()
        {
            var graph = new SerializableGraph();
            var outputNode = new SerializableNode();
            var outputSlot = new SerializableSlot("output", "output", SlotType.Output, 0);
            outputNode.AddSlot(outputSlot);
            graph.AddNode(outputNode);

            var inputNode = new SerializableNode();
            var inputSlot = new SerializableSlot("input", "input", SlotType.Input, 0);
            inputNode.AddSlot(inputSlot);
            graph.AddNode(inputNode);

            Assert.AreEqual(2, graph.GetNodes<INode>().Count());


            var createdEdge = graph.Connect(outputNode.GetSlotReference("output"), inputNode.GetSlotReference("input"));
            Assert.AreEqual(1, graph.edges.Count());

            var edge = graph.edges.FirstOrDefault();

            Assert.AreEqual(createdEdge, edge);

            var foundOutputNode = graph.GetNodeFromGuid(edge.outputSlot.nodeGuid);
            var foundOutputSlot = foundOutputNode.FindOutputSlot<ISlot>(edge.outputSlot.slotName);
            Assert.AreEqual(outputNode, foundOutputNode);
            Assert.AreEqual(outputSlot, foundOutputSlot);

            var foundInputNode = graph.GetNodeFromGuid(edge.inputSlot.nodeGuid);
            var foundInputSlot = foundInputNode.FindInputSlot<ISlot>(edge.inputSlot.slotName);
            Assert.AreEqual(inputNode, foundInputNode);
            Assert.AreEqual(inputSlot, foundInputSlot);
        }

        [Test]
        public void TestCanNotConnectTwoOuputSlotsOnSerializableGraph()
        {
            var graph = new SerializableGraph();
            var outputNode = new SerializableNode();
            var outputSlot = new SerializableSlot("output", "output", SlotType.Output, 0);
            outputNode.AddSlot(outputSlot);
            graph.AddNode(outputNode);

            var outputNode2 = new SerializableNode();
            var outputSlot2 = new SerializableSlot("output", "output", SlotType.Output, 0);
            outputNode2.AddSlot(outputSlot2);
            graph.AddNode(outputNode2);

            Assert.AreEqual(2, graph.GetNodes<INode>().Count());

            var createdEdge = graph.Connect(outputNode.GetSlotReference("output"), outputNode2.GetSlotReference("output"));
            Assert.IsNull(createdEdge);
            Assert.AreEqual(0, graph.edges.Count());
        }

        [Test]
        public void TestCanNotConnectTwoInputSlotsOnSerializableGraph()
        {
            var graph = new SerializableGraph();
            var inputNode = new SerializableNode();
            var inputSlot = new SerializableSlot("input", "input", SlotType.Input, 0);
            inputNode.AddSlot(inputSlot);
            graph.AddNode(inputNode);

            var inputNode2 = new SerializableNode();
            var inputSlot2 = new SerializableSlot("input", "input", SlotType.Input, 0);
            inputNode2.AddSlot(inputSlot2);
            graph.AddNode(inputNode2);

            Assert.AreEqual(2, graph.GetNodes<INode>().Count());

            var createdEdge = graph.Connect(inputNode.GetSlotReference("input"), inputNode2.GetSlotReference("input"));
            Assert.IsNull(createdEdge);
            Assert.AreEqual(0, graph.edges.Count());
        }

        [Test]
        public void TestRemovingNodeRemovesConectedEdgesOnSerializableGraph()
        {
            var graph = new SerializableGraph();
            var outputNode = new SerializableNode();
            var outputSlot = new SerializableSlot("output", "output", SlotType.Output, 0);
            outputNode.AddSlot(outputSlot);
            graph.AddNode(outputNode);

            var inputNode = new SerializableNode();
            var inputSlot = new SerializableSlot("input", "input", SlotType.Input, 0);
            inputNode.AddSlot(inputSlot);
            graph.AddNode(inputNode);

            Assert.AreEqual(2, graph.GetNodes<INode>().Count());
            graph.Connect(outputNode.GetSlotReference("output"), inputNode.GetSlotReference("input"));
            Assert.AreEqual(1, graph.edges.Count());

            graph.RemoveNode(graph.GetNodes<INode>().FirstOrDefault());
            Assert.AreEqual(1, graph.GetNodes<INode>().Count());
            Assert.AreEqual(0, graph.edges.Count());
        }

        [Test]
        public void TestRemovingEdgeOnSerializableGraph()
        {
            var graph = new SerializableGraph();
            var outputNode = new SerializableNode();
            var outputSlot = new SerializableSlot("output", "output", SlotType.Output, 0);
            outputNode.AddSlot(outputSlot);
            graph.AddNode(outputNode);

            var inputNode = new SerializableNode();
            var inputSlot = new SerializableSlot("input", "input", SlotType.Input, 0);
            inputNode.AddSlot(inputSlot);
            graph.AddNode(inputNode);

            Assert.AreEqual(2, graph.GetNodes<INode>().Count());
            graph.Connect(outputNode.GetSlotReference("output"), inputNode.GetSlotReference("input"));
            Assert.AreEqual(1, graph.edges.Count());

            graph.RemoveEdge(graph.edges.FirstOrDefault());
            Assert.AreEqual(2, graph.GetNodes<INode>().Count());
            Assert.AreEqual(0, graph.edges.Count());
        }

        [Test]
        public void TestRemovingElementsFromSerializableGraph()
        {
            var graph = new SerializableGraph();
            var outputNode = new SerializableNode();
            var outputSlot = new SerializableSlot("output", "output", SlotType.Output, 0);
            outputNode.AddSlot(outputSlot);
            graph.AddNode(outputNode);

            var inputNode = new SerializableNode();
            var inputSlot = new SerializableSlot("input", "input", SlotType.Input, 0);
            inputNode.AddSlot(inputSlot);
            graph.AddNode(inputNode);

            Assert.AreEqual(2, graph.GetNodes<INode>().Count());
            graph.Connect(outputNode.GetSlotReference("output"), inputNode.GetSlotReference("input"));
            Assert.AreEqual(1, graph.edges.Count());

            graph.RemoveElements(graph.GetNodes<INode>(), graph.edges);
            Assert.AreEqual(0, graph.GetNodes<INode>().Count());
            Assert.AreEqual(0, graph.edges.Count());
        }

        [Test]
        public void TestCanGetEdgesOnSerializableGraphFromSlotReference()
        {
            var graph = new SerializableGraph();
            var outputNode = new SerializableNode();
            var outputSlot = new SerializableSlot("output", "output", SlotType.Output, 0);
            outputNode.AddSlot(outputSlot);
            graph.AddNode(outputNode);

            var inputNode = new SerializableNode();
            var inputSlot = new SerializableSlot("input", "input", SlotType.Input, 0);
            inputNode.AddSlot(inputSlot);
            graph.AddNode(inputNode);

            Assert.AreEqual(2, graph.GetNodes<INode>().Count());
            graph.Connect(outputNode.GetSlotReference("output"), inputNode.GetSlotReference("input"));
            Assert.AreEqual(1, graph.edges.Count());

            Assert.AreEqual(1, graph.GetEdges(inputNode.GetSlotReference("input")).Count());
            Assert.AreEqual(1, graph.GetEdges(outputNode.GetSlotReference("output")).Count());
            Assert.AreEqual(0, graph.GetEdges(outputNode.GetSlotReference("badslot")).Count());
        }

        [Test]
        public void TestGetInputsWithNoConnection()
        {
            var graph = new SerializableGraph();
            var outputNode = new SerializableNode();
            outputNode.AddSlot(new SerializableSlot("output", "output", SlotType.Output, 0));
            graph.AddNode(outputNode); 

            var inputNode = new SerializableNode();
            inputNode.AddSlot(new SerializableSlot("input", "input", SlotType.Input, 0));
            inputNode.AddSlot(new SerializableSlot("input2", "input2", SlotType.Input, 1));
            inputNode.AddSlot(new SerializableSlot("output", "output", SlotType.Output, 0));
            graph.AddNode(inputNode);

            Assert.AreEqual(2, graph.GetNodes<INode>().Count());
            graph.Connect(outputNode.GetSlotReference("output"), inputNode.GetSlotReference("input"));
            Assert.AreEqual(1, graph.edges.Count());

            var slots = inputNode.GetInputsWithNoConnection();
            Assert.AreEqual(1, slots.Count());
            Assert.AreEqual("input2", slots.FirstOrDefault().name);
        }

        [Test]
        public void TestCyclicConnectionsAreNotAllowedOnGraph()
        {
            var graph = new SerializableGraph();

            var nodeA = new SerializableNode();
            var inputSlotA = new SerializableSlot("input", "input", SlotType.Input, 0);
            var outputSlotA = new SerializableSlot("output", "output", SlotType.Output, 0);
            nodeA.AddSlot(inputSlotA);
            nodeA.AddSlot(outputSlotA);
            graph.AddNode(nodeA);

            var nodeB = new SerializableNode();
            var inputSlotB = new SerializableSlot("input", "input", SlotType.Input, 0);
            var outputSlotB = new SerializableSlot("output", "output", SlotType.Output, 0);
            nodeB.AddSlot(inputSlotB);
            nodeB.AddSlot(outputSlotB);
            graph.AddNode(nodeB);
            
            Assert.AreEqual(2, graph.GetNodes<INode>().Count());
            graph.Connect(nodeA.GetSlotReference("output"), nodeB.GetSlotReference("input"));
            Assert.AreEqual(1, graph.edges.Count());

            var edge = graph.Connect(nodeB.GetSlotReference("output"), nodeA.GetSlotReference("input"));
            Assert.IsNull(edge);
            Assert.AreEqual(1, graph.edges.Count());
        }
    }
}
