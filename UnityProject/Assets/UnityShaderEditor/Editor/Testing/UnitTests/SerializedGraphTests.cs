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
        public void TestCreate()
        {
            var graph = new SerializableGraph();

            Assert.AreEqual(0, graph.edges.Count());
            Assert.AreEqual(0, graph.nodes.Count());
        }

        [Test]
        public void TestAddNode()
        {
            var graph = new SerializableGraph();
            var node = new SerializableNode(graph);
            node.name = "Test Node";
            graph.AddNode(node);

            Assert.AreEqual(1, graph.nodes.Count());
            Assert.AreEqual("Test Node", graph.nodes.FirstOrDefault().name);
        }

        [Test]
        public void TestAddSlot()
        {
            var graph = new SerializableGraph();
            var node = new SerializableNode(graph);
            node.AddSlot(new SerializableSlot(node, "output", "output", SlotType.Output));
            node.AddSlot(new SerializableSlot(node, "input", "input", SlotType.Input));
            node.name = "Test Node";
            graph.AddNode(node);

            Assert.AreEqual(1, graph.nodes.Count());
            var found = graph.nodes.FirstOrDefault();
            Assert.AreEqual(found.inputSlots.Count(), 1);
            Assert.AreEqual(found.outputSlots.Count(), 1);
            Assert.AreEqual(found.slots.Count(), 2);
        }

        [Test]
        public void TestCanConnectAndTraverseTwoNodes()
        {
            var graph = new SerializableGraph();
            var outputNode = new SerializableNode(graph);
            var outputSlot = new SerializableSlot(outputNode, "output", "output", SlotType.Output);
            outputNode.AddSlot(outputSlot);
            graph.AddNode(outputNode);

            var inputNode = new SerializableNode(graph);
            var inputSlot = new SerializableSlot(inputNode, "input", "input", SlotType.Input);
            inputNode.AddSlot(inputSlot);
            graph.AddNode(inputNode);

            Assert.AreEqual(2, graph.nodes.Count());

            var createdEdge = graph.Connect(outputNode.FindOutputSlot("output"), inputNode.FindInputSlot("input"));
            Assert.AreEqual(1, graph.edges.Count());

            var edge = graph.edges.FirstOrDefault();

            Assert.AreEqual(createdEdge, edge);

            var foundOutputNode = graph.GetNodeFromGuid(edge.outputSlot.nodeGuid);
            var foundOutputSlot = foundOutputNode.FindOutputSlot(edge.outputSlot.slotName);
            Assert.AreEqual(foundOutputNode, outputNode);
            Assert.AreEqual(foundOutputSlot, outputSlot);

            var foundInputNode = graph.GetNodeFromGuid(edge.inputSlot.nodeGuid);
            var foundInputSlot = foundInputNode.FindInputSlot(edge.inputSlot.slotName);
            Assert.AreEqual(foundInputNode, inputNode);
            Assert.AreEqual(foundInputSlot, inputSlot);
        }

        [Test]
        public void TestCanNotConnectTwoOuputSlots()
        {
            var graph = new SerializableGraph();
            var outputNode = new SerializableNode(graph);
            var outputSlot = new SerializableSlot(outputNode, "output", "output", SlotType.Output);
            outputNode.AddSlot(outputSlot);
            graph.AddNode(outputNode);

            var outputNode2 = new SerializableNode(graph);
            var outputSlot2 = new SerializableSlot(outputNode2, "output", "output", SlotType.Output);
            outputNode2.AddSlot(outputSlot2);
            graph.AddNode(outputNode2);

            Assert.AreEqual(2, graph.nodes.Count());

            var createdEdge = graph.Connect(outputNode.FindOutputSlot("output"), outputNode2.FindOutputSlot("output"));
            Assert.IsNull(createdEdge);
            Assert.AreEqual(0, graph.edges.Count());
        }

        [Test]
        public void TestCanNotConnectTwoInputSlots()
        {
            var graph = new SerializableGraph();
            var inputNode = new SerializableNode(graph);
            var inputSlot = new SerializableSlot(inputNode, "input", "input", SlotType.Input);
            inputNode.AddSlot(inputSlot);
            graph.AddNode(inputNode);

            var inputNode2 = new SerializableNode(graph);
            var inputSlot2 = new SerializableSlot(inputNode2, "input", "input", SlotType.Input);
            inputNode2.AddSlot(inputSlot2);
            graph.AddNode(inputNode2);

            Assert.AreEqual(2, graph.nodes.Count());

            var createdEdge = graph.Connect(inputNode.FindInputSlot("input"), inputNode.FindInputSlot("input"));
            Assert.IsNull(createdEdge);
            Assert.AreEqual(0, graph.edges.Count());
        }
    }

    [TestFixture]
    public class MaterialGraphTests
    {
        [Test]
        public void TestCreateMaterialGraph()
        {
            MaterialGraph graph = new MaterialGraph();

            Assert.IsNotNull(graph.currentGraph);
            Assert.IsNotNull(graph.materialOptions);

            graph.PostCreate();
            
            Assert.AreEqual(1, graph.currentGraph.nodes.Count());
            Assert.IsInstanceOf(typeof(PixelShaderNode), graph.currentGraph.nodes.FirstOrDefault());
         }
    }
}
