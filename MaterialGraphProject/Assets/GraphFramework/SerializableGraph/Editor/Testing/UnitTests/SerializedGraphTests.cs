using System;
using System.Collections.Generic;
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
        public void TestCanModifyNodeDrawState()
        {
            var node = new SerializableNode();
            node.name = "Test Node";

            var drawState = node.drawState;
            var newPos = new Rect(10,10,10,10);
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
            node.AddSlot(new SerializableSlot("output", "output", SlotType.Output, 0));
            node.AddSlot(new SerializableSlot("input", "input", SlotType.Input, 0));
            graph.AddNode(node);

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
            var graph = new SerializableGraph();
            var node = new SerializableNode();
            graph.AddNode(node);
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
        public void TestCanUpdateSlotPriority()
        {
            var graph = new SerializableGraph();
            var node = new SerializableNode();
            node.AddSlot(new SerializableSlot("output", "output", SlotType.Output, 0));
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
        public void TestCanUpdateSlotDisplayName()
        {
            var node = new SerializableNode();
            node.AddSlot(new SerializableSlot("output", "output", SlotType.Output, 0));
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
        public void TestCanConnectAndTraverseThreeNodesOnSerializableGraph()
        {
            var graph = new SerializableGraph();
            var outputNode = new SerializableNode();
            var outputNodeOutputSlot = new SerializableSlot("output", "output", SlotType.Output, 0);
            outputNode.AddSlot(outputNodeOutputSlot);
            graph.AddNode(outputNode);

            var middleNode = new SerializableNode();
            var middleNodeInputSlot = new SerializableSlot("input", "input", SlotType.Input, 0);
            var middleNodeoutputSlot = new SerializableSlot("output", "output", SlotType.Output, 0);
            middleNode.AddSlot(middleNodeInputSlot);
            middleNode.AddSlot(middleNodeoutputSlot);
            graph.AddNode(middleNode);

            var inputNode = new SerializableNode();
            var inputNodeInputSlot1 = new SerializableSlot("input1", "input1", SlotType.Input, 0);
            var inputNodeInputSlot2 = new SerializableSlot("input2", "input2", SlotType.Input, 1);
            inputNode.AddSlot(inputNodeInputSlot1);
            inputNode.AddSlot(inputNodeInputSlot2);
            graph.AddNode(inputNode);
            
            Assert.AreEqual(3, graph.GetNodes<INode>().Count());

            graph.Connect(outputNode.GetSlotReference("output"), middleNode.GetSlotReference("input"));
            Assert.AreEqual(1, graph.edges.Count());

            graph.Connect(middleNode.GetSlotReference("output"), inputNode.GetSlotReference("input1"));
            Assert.AreEqual(2, graph.edges.Count());
            
            var edgesOnMiddleNode = NodeUtils.GetAllEdges(middleNode);
            Assert.AreEqual(2, edgesOnMiddleNode.Count());

            List<INode> result = new List<INode>();
            NodeUtils.DepthFirstCollectNodesFromNode(result, inputNode);
            Assert.AreEqual(3, result.Count);

            result.Clear();
            NodeUtils.DepthFirstCollectNodesFromNode(result, inputNode, inputNodeInputSlot1);
            Assert.AreEqual(3, result.Count);

            result.Clear();
            NodeUtils.DepthFirstCollectNodesFromNode(result, inputNode, inputNodeInputSlot2);
            Assert.AreEqual(1, result.Count);

            result.Clear();
            NodeUtils.DepthFirstCollectNodesFromNode(result, inputNode, inputNodeInputSlot1, false);
            Assert.AreEqual(2, result.Count);

            result.Clear();
            NodeUtils.DepthFirstCollectNodesFromNode(result, inputNode, inputNodeInputSlot2, false);
            Assert.AreEqual(0, result.Count);
            
            result.Clear();
            NodeUtils.DepthFirstCollectNodesFromNode(result, null);
            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void TestConectionToSameInputReplacesOldInput()
        {
            var graph = new SerializableGraph();
            var outputNode = new SerializableNode();
            var outputSlot1 = new SerializableSlot("output1", "output1", SlotType.Output, 0);
            var outputSlot2 = new SerializableSlot("output2", "output2", SlotType.Output, 1);
            outputNode.AddSlot(outputSlot1);
            outputNode.AddSlot(outputSlot2);
            graph.AddNode(outputNode);

            var inputNode = new SerializableNode();
            var inputSlot = new SerializableSlot("input", "input", SlotType.Input, 0);
            inputNode.AddSlot(inputSlot);
            graph.AddNode(inputNode);

            Assert.AreEqual(2, graph.GetNodes<INode>().Count());

            var createdEdge = graph.Connect(outputNode.GetSlotReference("output1"), inputNode.GetSlotReference("input"));
            Assert.AreEqual(1, graph.edges.Count());
            var edge = graph.edges.FirstOrDefault();
            Assert.AreEqual(createdEdge, edge);

            var createdEdge2 = graph.Connect(outputNode.GetSlotReference("output2"), inputNode.GetSlotReference("input"));
            Assert.AreEqual(1, graph.edges.Count());
            var edge2 = graph.edges.FirstOrDefault();
            Assert.AreEqual(createdEdge2, edge2);
        }

        [Test]
        public void TestRemovingSlotRemovesConnectedEdges()
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
           
            inputNode.RemoveSlot("input");
            Assert.AreEqual(0, graph.edges.Count());
        }

        [Test]
        public void TestCanNotConnectToNullSlot()
        {
            var graph = new SerializableGraph();
            var outputNode = new SerializableNode();
            var outputSlot = new SerializableSlot("output", "output", SlotType.Output, 0);
            outputNode.AddSlot(outputSlot);
            graph.AddNode(outputNode);

            var inputNode = new SerializableNode();
            graph.AddNode(inputNode);

            Assert.AreEqual(2, graph.GetNodes<INode>().Count());

            var createdEdge = graph.Connect(outputNode.GetSlotReference("output"), null);
            Assert.AreEqual(0, graph.edges.Count());
            Assert.IsNull(createdEdge);

            var createdEdge2 = graph.Connect(outputNode.GetSlotReference("output"), new SlotReference(Guid.NewGuid(), "nope"));
            Assert.AreEqual(0, graph.edges.Count());
            Assert.IsNull(createdEdge2);
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
