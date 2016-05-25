using System;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace UnityEditor.MaterialGraph.Tests
{
    [TestFixture]
    public class ScriptableGraphTests
    {
        [SetUpFixture]
        public class SetUpClass
        {
            [SetUp]
            void RunBeforeAnyTests()
            {
                Debug.logger.logHandler = new ConsoleLogHandler();
            }
        }

        [Test]
        public void TestCanCreateSerializableGraph()
        {
            var graph = new SerializableGraph();

            Assert.AreEqual(0, graph.edges.Count());
            Assert.AreEqual(0, graph.nodes.Count());
        }

        [Test]
        public void TestCanAddNodeToSerializableGraph()
        {
            var graph = new SerializableGraph();
            var node = new SerializableNode(graph);
            node.name = "Test Node";
            graph.AddNode(node);

            Assert.AreEqual(1, graph.nodes.Count());
            Assert.AreEqual("Test Node", graph.nodes.FirstOrDefault().name);
        }

        [Test]
        public void TestCanRemoveNodeFromSerializableGraph()
        {
            var graph = new SerializableGraph();
            var node = new SerializableNode(graph);
            node.name = "Test Node";
            graph.AddNode(node);
            Assert.AreEqual(1, graph.nodes.Count());

            graph.RemoveNode(graph.nodes.FirstOrDefault());
            Assert.AreEqual(0, graph.nodes.Count());
        }

        private class NoDeleteNode : SerializableNode
        {
            public NoDeleteNode(SerializableGraph theOwner) : base(theOwner)
            {}

            public override bool canDeleteNode { get { return false; } }
        }

        [Test]
        public void TestCanNotRemoveNoDeleteNodeFromSerializableGraph()
        {
            var graph = new SerializableGraph();
            var node = new NoDeleteNode(graph);
            node.name = "Test Node";
            graph.AddNode(node);
            Assert.AreEqual(1, graph.nodes.Count());

            graph.RemoveNode(graph.nodes.FirstOrDefault());
            Assert.AreEqual(1, graph.nodes.Count());
        }

        [Test]
        public void TestCanFindNodeInSerializableGraph()
        {
            var graph = new SerializableGraph();
            var node = new SerializableNode(graph);
            graph.AddNode(node);

            Assert.AreEqual(1, graph.nodes.Count());
            Assert.IsNotNull(graph.GetNodeFromGuid(node.guid));
            Assert.IsNull(graph.GetNodeFromGuid(Guid.NewGuid()));
        }

        [Test]
        public void TestCanAddSlotToSerializableNode()
        {
            var graph = new SerializableGraph();
            var node = new SerializableNode(graph);
            node.AddSlot(new SerializableSlot("output", "output", SlotType.Output));
            node.AddSlot(new SerializableSlot("input", "input", SlotType.Input));
            node.name = "Test Node";
            graph.AddNode(node);

            Assert.AreEqual(1, graph.nodes.Count());
            var found = graph.nodes.FirstOrDefault();
            Assert.AreEqual(1, found.inputSlots.Count());
            Assert.AreEqual("input", found.inputSlots.FirstOrDefault().name);
            Assert.AreEqual(1, found.outputSlots.Count());
            Assert.AreEqual("output", found.outputSlots.FirstOrDefault().name);
            Assert.AreEqual(2, found.slots.Count());
        }

        [Test]
        public void TestCanRemoveSlotFromSerializableNode()
        {
            var node = new SerializableNode(null);
            node.AddSlot(new SerializableSlot("output", "output", SlotType.Output));
            node.AddSlot(new SerializableSlot("input", "input", SlotType.Input));

            Assert.AreEqual(2, node.slots.Count());
            Assert.AreEqual(1, node.inputSlots.Count());
            Assert.AreEqual(1, node.outputSlots.Count());

            node.RemoveSlot("input");

            Assert.AreEqual(1, node.slots.Count());
            Assert.AreEqual(0, node.inputSlots.Count());
            Assert.AreEqual(1, node.outputSlots.Count());
        }

        [Test]
        public void TestCanRemoveSlotsWithNonMathingNameFromSerializableNode()
        {
            var node = new SerializableNode(null);
            node.AddSlot(new SerializableSlot("input1", "input", SlotType.Input));
            node.AddSlot(new SerializableSlot("input2", "input", SlotType.Input));
            node.AddSlot(new SerializableSlot("input3", "input", SlotType.Input));
            node.AddSlot(new SerializableSlot("input4", "input", SlotType.Input));

            Assert.AreEqual(4, node.slots.Count());
            Assert.AreEqual(4, node.inputSlots.Count());
            Assert.AreEqual(0, node.outputSlots.Count());

            node.RemoveSlotsNameNotMatching(new []{"input1", "input3"});

            Assert.AreEqual(2, node.slots.Count());
            Assert.AreEqual(2, node.inputSlots.Count());
            Assert.AreEqual(0, node.outputSlots.Count());

            Assert.IsNotNull(node.FindInputSlot("input1"));
            Assert.IsNull(node.FindInputSlot("input2"));
            Assert.IsNotNull(node.FindInputSlot("input3"));
            Assert.IsNull(node.FindInputSlot("input4"));
        }

        [Test]
        public void TestCanNotAddDuplicateSlotToSerializableNode()
        {
            var graph = new SerializableGraph();
            var node = new SerializableNode(graph);
            node.AddSlot(new SerializableSlot("output", "output", SlotType.Output));
            node.AddSlot(new SerializableSlot("output", "output", SlotType.Output));
            node.name = "Test Node";
            graph.AddNode(node);

            Assert.AreEqual(1, graph.nodes.Count());
            var found = graph.nodes.FirstOrDefault();
            Assert.AreEqual(0, found.inputSlots.Count());
            Assert.AreEqual(1, found.outputSlots.Count());
            Assert.AreEqual(1, found.slots.Count());
        }

        [Test]
        public void TestCanUpdateDisplaynameByReaddingSlotToSerializableNode()
        {
            var graph = new SerializableGraph();
            var node = new SerializableNode(graph);
            node.AddSlot(new SerializableSlot("output", "output", SlotType.Output));
            node.AddSlot(new SerializableSlot("output", "output_updated", SlotType.Output));
            node.name = "Test Node";
            graph.AddNode(node);

            Assert.AreEqual(1, graph.nodes.Count());
            var found = graph.nodes.FirstOrDefault();
            Assert.AreEqual(0 ,found.inputSlots.Count());
            Assert.AreEqual(1, found.outputSlots.Count());
            Assert.AreEqual(1, found.slots.Count());

            var slot = found.outputSlots.FirstOrDefault();
            Assert.AreEqual("output_updated", slot.displayName);
        }

        [Test]
        public void TestCanFindSlotOnSerializableNode()
        {
            var node = new SerializableNode(null);
            node.AddSlot(new SerializableSlot("output", "output", SlotType.Output));
            node.AddSlot(new SerializableSlot("input", "input", SlotType.Input));

            Assert.AreEqual(2, node.slots.Count());
            Assert.IsNotNull(node.FindInputSlot("input"));
            Assert.IsNull(node.FindInputSlot("output"));
            Assert.IsNotNull(node.FindOutputSlot("output"));
            Assert.IsNull(node.FindOutputSlot("input"));

            Assert.IsNotNull(node.FindSlot("input"));
            Assert.IsNotNull(node.FindSlot("output"));
            Assert.IsNull(node.FindSlot("invalid"));
        }

        [Test]
        public void TestCanFindSlotReferenceOnSerializableNode()
        {
            var node = new SerializableNode(null);
            node.AddSlot(new SerializableSlot("output", "output", SlotType.Output));
            node.AddSlot(new SerializableSlot("input", "input", SlotType.Input));

            Assert.AreEqual(2, node.slots.Count());
            Assert.IsNotNull(node.GetSlotReference("input"));
            Assert.IsNotNull(node.GetSlotReference("output"));
            Assert.Null(node.GetSlotReference("invalid"));
        }

        [Test]
        public void TestCanConnectAndTraverseTwoNodesOnSerializableGraph()
        {
            var graph = new SerializableGraph();
            var outputNode = new SerializableNode(graph);
            var outputSlot = new SerializableSlot("output", "output", SlotType.Output);
            outputNode.AddSlot(outputSlot);
            graph.AddNode(outputNode);

            var inputNode = new SerializableNode(graph);
            var inputSlot = new SerializableSlot("input", "input", SlotType.Input);
            inputNode.AddSlot(inputSlot);
            graph.AddNode(inputNode);

            Assert.AreEqual(2, graph.nodes.Count());


            var createdEdge = graph.Connect(outputNode.GetSlotReference("output"), inputNode.GetSlotReference("input"));
            Assert.AreEqual(1, graph.edges.Count());

            var edge = graph.edges.FirstOrDefault();

            Assert.AreEqual(createdEdge, edge);

            var foundOutputNode = graph.GetNodeFromGuid(edge.outputSlot.nodeGuid);
            var foundOutputSlot = foundOutputNode.FindOutputSlot(edge.outputSlot.slotName);
            Assert.AreEqual(outputNode, foundOutputNode);
            Assert.AreEqual(outputSlot, foundOutputSlot);

            var foundInputNode = graph.GetNodeFromGuid(edge.inputSlot.nodeGuid);
            var foundInputSlot = foundInputNode.FindInputSlot(edge.inputSlot.slotName);
            Assert.AreEqual(inputNode, foundInputNode);
            Assert.AreEqual(inputSlot, foundInputSlot);
        }

        [Test]
        public void TestCanNotConnectTwoOuputSlotsOnSerializableGraph()
        {
            var graph = new SerializableGraph();
            var outputNode = new SerializableNode(graph);
            var outputSlot = new SerializableSlot("output", "output", SlotType.Output);
            outputNode.AddSlot(outputSlot);
            graph.AddNode(outputNode);

            var outputNode2 = new SerializableNode(graph);
            var outputSlot2 = new SerializableSlot("output", "output", SlotType.Output);
            outputNode2.AddSlot(outputSlot2);
            graph.AddNode(outputNode2);

            Assert.AreEqual(2, graph.nodes.Count());

            var createdEdge = graph.Connect(outputNode.GetSlotReference("output"), outputNode2.GetSlotReference("output"));
            Assert.IsNull(createdEdge);
            Assert.AreEqual(0, graph.edges.Count());
        }

        [Test]
        public void TestCanNotConnectTwoInputSlotsOnSerializableGraph()
        {
            var graph = new SerializableGraph();
            var inputNode = new SerializableNode(graph);
            var inputSlot = new SerializableSlot("input", "input", SlotType.Input);
            inputNode.AddSlot(inputSlot);
            graph.AddNode(inputNode);

            var inputNode2 = new SerializableNode(graph);
            var inputSlot2 = new SerializableSlot("input", "input", SlotType.Input);
            inputNode2.AddSlot(inputSlot2);
            graph.AddNode(inputNode2);

            Assert.AreEqual(2, graph.nodes.Count());

            var createdEdge = graph.Connect(inputNode.GetSlotReference("input"), inputNode2.GetSlotReference("input"));
            Assert.IsNull(createdEdge);
            Assert.AreEqual(0, graph.edges.Count());
        }

        [Test]
        public void TestRemovingNodeRemovesConectedEdgesOnSerializableGraph()
        {
            var graph = new SerializableGraph();
            var outputNode = new SerializableNode(graph);
            var outputSlot = new SerializableSlot("output", "output", SlotType.Output);
            outputNode.AddSlot(outputSlot);
            graph.AddNode(outputNode);

            var inputNode = new SerializableNode(graph);
            var inputSlot = new SerializableSlot("input", "input", SlotType.Input);
            inputNode.AddSlot(inputSlot);
            graph.AddNode(inputNode);

            Assert.AreEqual(2, graph.nodes.Count());
            graph.Connect(outputNode.GetSlotReference("output"), inputNode.GetSlotReference("input"));
            Assert.AreEqual(1, graph.edges.Count());

            graph.RemoveNode(graph.nodes.FirstOrDefault());
            Assert.AreEqual(1, graph.nodes.Count());
            Assert.AreEqual(0, graph.edges.Count());
        }

        [Test]
        public void TestRemovingEdgeOnSerializableGraph()
        {
            var graph = new SerializableGraph();
            var outputNode = new SerializableNode(graph);
            var outputSlot = new SerializableSlot("output", "output", SlotType.Output);
            outputNode.AddSlot(outputSlot);
            graph.AddNode(outputNode);

            var inputNode = new SerializableNode(graph);
            var inputSlot = new SerializableSlot("input", "input", SlotType.Input);
            inputNode.AddSlot(inputSlot);
            graph.AddNode(inputNode);

            Assert.AreEqual(2, graph.nodes.Count());
            graph.Connect(outputNode.GetSlotReference("output"), inputNode.GetSlotReference("input"));
            Assert.AreEqual(1, graph.edges.Count());

            graph.RemoveEdge(graph.edges.FirstOrDefault());
            Assert.AreEqual(2, graph.nodes.Count());
            Assert.AreEqual(0, graph.edges.Count());
        }

        [Test]
        public void TestRemovingElementsFromSerializableGraph()
        {
            var graph = new SerializableGraph();
            var outputNode = new SerializableNode(graph);
            var outputSlot = new SerializableSlot("output", "output", SlotType.Output);
            outputNode.AddSlot(outputSlot);
            graph.AddNode(outputNode);

            var inputNode = new SerializableNode(graph);
            var inputSlot = new SerializableSlot("input", "input", SlotType.Input);
            inputNode.AddSlot(inputSlot);
            graph.AddNode(inputNode);

            Assert.AreEqual(2, graph.nodes.Count());
            graph.Connect(outputNode.GetSlotReference("output"), inputNode.GetSlotReference("input"));
            Assert.AreEqual(1, graph.edges.Count());

            graph.RemoveElements(graph.nodes, graph.edges);
            Assert.AreEqual(0, graph.nodes.Count());
            Assert.AreEqual(0, graph.edges.Count());
        }

        [Test]
        public void TestCanGetEdgesOnSerializableGraphFromSlotReference()
        {
            var graph = new SerializableGraph();
            var outputNode = new SerializableNode(graph);
            var outputSlot = new SerializableSlot("output", "output", SlotType.Output);
            outputNode.AddSlot(outputSlot);
            graph.AddNode(outputNode);

            var inputNode = new SerializableNode(graph);
            var inputSlot = new SerializableSlot("input", "input", SlotType.Input);
            inputNode.AddSlot(inputSlot);
            graph.AddNode(inputNode);

            Assert.AreEqual(2, graph.nodes.Count());
            graph.Connect(outputNode.GetSlotReference("output"), inputNode.GetSlotReference("input"));
            Assert.AreEqual(1, graph.edges.Count());
            
            Assert.AreEqual(1, graph.GetEdges(inputNode.GetSlotReference("input")).Count());
            Assert.AreEqual(1, graph.GetEdges(outputNode.GetSlotReference("output")).Count());
            Assert.AreEqual(0, graph.GetEdges(outputNode.GetSlotReference("badslot")).Count());
        }
    }
}
