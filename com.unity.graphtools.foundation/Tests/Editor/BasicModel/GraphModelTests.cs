using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.BasicModelTests
{
    public class GraphModelTests
    {
        IGraphAssetModel m_GraphAsset;

        [SetUp]
        public void SetUp()
        {
            m_GraphAsset = GraphAssetCreationHelpers<ClassGraphAssetModel>.CreateInMemoryGraphAsset(typeof(ClassStencil), "Test");
            m_GraphAsset.CreateGraph("Graph");
        }

        [Test]
        public void TryGetModelByGUIDWorks()
        {
            var graphModel = m_GraphAsset.GraphModel;
            var node1 = graphModel.CreateNode<Type0FakeNodeModel>();
            var node2 = graphModel.CreateNode<Type0FakeNodeModel>();
            var edge = graphModel.CreateEdge(node1.ExeInput0, node2.ExeOutput0);
            var placemat = graphModel.CreatePlacemat(new Rect(100, 100, 300, 300));
            var stickyNote = graphModel.CreateStickyNote(new Rect(-100, -100, 100, 100));
            var constant = graphModel.CreateConstantNode(TypeHandle.Float, "constant", new Vector2(42, 42));
            var variableDeclaration = graphModel.CreateGraphVariableDeclaration(TypeHandle.Float, "varDecl", ModifierFlags.None, true);
            var variable = graphModel.CreateVariableNode(variableDeclaration, new Vector2(-76, 245));
            var portal = graphModel.CreateEntryPortalFromEdge(edge);
            var badge = new BadgeModel(node1);
            graphModel.AddBadge(badge);

            var graphElements = new IGraphElementModel[] { node1, node2, edge, placemat, stickyNote, constant, variableDeclaration, variable, portal, badge };
            foreach (var element in graphElements)
            {
                Assert.IsTrue(graphModel.TryGetModelFromGuid(element.Guid, out var retrieved), element + " was not found");
                Assert.AreSame(element, retrieved);
            }

            graphModel.DeleteBadges();
            graphModel.DeleteEdges(new[] { edge });
            graphModel.DeleteNodes(new IInputOutputPortsNodeModel[] { node1, node2, constant, variable, portal }, true);
            graphModel.DeletePlacemats(new[] { placemat });
            graphModel.DeleteStickyNotes(new[] { stickyNote });
            graphModel.DeleteVariableDeclarations(new[] { variableDeclaration });
            foreach (var element in graphElements)
            {
                Assert.IsFalse(graphModel.TryGetModelFromGuid(element.Guid, out _), element + " was found after removal");
            }
        }

        [Test]
        public void GetEdgesForPortWorksAfterAddingAndRemovingEdge()
        {
            var graphModel = m_GraphAsset.GraphModel;
            var node1 = graphModel.CreateNode<Type0FakeNodeModel>();
            var node2 = graphModel.CreateNode<Type0FakeNodeModel>();
            var edge = graphModel.CreateEdge(node1.ExeInput0, node2.ExeOutput0);

            var expectedEdges = new List<IEdgeModel> { edge };
            Assert.AreEqual(expectedEdges, graphModel.GetEdgesForPort(node1.ExeInput0));
            Assert.AreEqual(expectedEdges, graphModel.GetEdgesForPort(node2.ExeOutput0));

            graphModel.DeleteEdges(expectedEdges);
            Assert.AreEqual(0, graphModel.GetEdgesForPort(node1.ExeInput0).Count);
            Assert.AreEqual(0, graphModel.GetEdgesForPort(node2.ExeOutput0).Count);
        }

        [Test]
        public void GetEdgesForPortWorksAfterChangingEdgeToPort()
        {
            var graphModel = m_GraphAsset.GraphModel;
            var node1 = graphModel.CreateNode<Type0FakeNodeModel>();
            var node2 = graphModel.CreateNode<Type0FakeNodeModel>();
            var edge = graphModel.CreateEdge(node1.ExeInput0, node2.ExeOutput0);

            var expectedEdges = new List<IEdgeModel> { edge };
            Assert.AreEqual(expectedEdges, graphModel.GetEdgesForPort(node1.ExeInput0));
            Assert.AreEqual(expectedEdges, graphModel.GetEdgesForPort(node2.ExeOutput0));

            edge.ToPort = node1.Input0;
            Assert.AreEqual(0, graphModel.GetEdgesForPort(node1.ExeInput0).Count);
            Assert.AreEqual(expectedEdges, graphModel.GetEdgesForPort(node1.Input0));
            Assert.AreEqual(expectedEdges, graphModel.GetEdgesForPort(node2.ExeOutput0));
        }

        [Test]
        public void GetEdgesForPortWorksAfterChangingEdgeFromPort()
        {
            var graphModel = m_GraphAsset.GraphModel;
            var node1 = graphModel.CreateNode<Type0FakeNodeModel>();
            var node2 = graphModel.CreateNode<Type0FakeNodeModel>();
            var edge = graphModel.CreateEdge(node1.ExeInput0, node2.ExeOutput0);

            var expectedEdges = new List<IEdgeModel> { edge };
            Assert.AreEqual(expectedEdges, graphModel.GetEdgesForPort(node1.ExeInput0));
            Assert.AreEqual(expectedEdges, graphModel.GetEdgesForPort(node2.ExeOutput0));

            edge.FromPort = node2.Output0;
            Assert.AreEqual(0, graphModel.GetEdgesForPort(node2.ExeOutput0).Count);
            Assert.AreEqual(expectedEdges, graphModel.GetEdgesForPort(node1.ExeInput0));
            Assert.AreEqual(expectedEdges, graphModel.GetEdgesForPort(node2.Output0));
        }

        [Test]
        public void GetEdgesForPortWorksAfterChangingEdgePortTitle()
        {
            var graphModel = m_GraphAsset.GraphModel;
            var node1 = graphModel.CreateNode<Type0FakeNodeModel>();
            var node2 = graphModel.CreateNode<Type0FakeNodeModel>();
            var edge = graphModel.CreateEdge(node1.ExeInput0, node2.ExeOutput0);

            var expectedEdges = new List<IEdgeModel> { edge };
            Assert.AreEqual(expectedEdges, graphModel.GetEdgesForPort(node1.ExeInput0));
            Assert.AreEqual(expectedEdges, graphModel.GetEdgesForPort(node2.ExeOutput0));

            var titledPort = node2.ExeOutput0 as IHasTitle;
            Assert.IsNotNull(titledPort);
            // This checks that the port is using the Title as its UniqueName (i.e. m_PortId is null).
            // If not, changing the Title will not change the UniqueName and this test will test nothing.
            Assert.AreEqual(titledPort.Title, node2.ExeOutput0.UniqueName);
            titledPort.Title = "Blah";

            Assert.AreEqual(expectedEdges, graphModel.GetEdgesForPort(node1.ExeInput0));
            Assert.AreEqual(expectedEdges, graphModel.GetEdgesForPort(node2.ExeOutput0));
        }

        class NodeThatUsesPortEdgeIndexInItsDefineNode : NodeModel
        {
            /// <inheritdoc />
            protected override void OnDefineNode()
            {
                base.OnDefineNode();

                var port = this.AddDataInputPort<float>("in");
                port.GetConnectedPorts();

                this.AddDataOutputPort<float>("out");
            }

            public void ClearPorts()
            {
                m_InputsById = new OrderedPorts();
                m_OutputsById = new OrderedPorts();
                m_PreviousInputs = null;
                m_PreviousOutputs = null;
            }
        }

        [Test]
        public void GraphWithNodeThatUsesPortEdgeIndexInItsDefineNodeDoesNotCorruptThePortEdgeIndex()
        {
            // Define a graph with nodes that use the PortEdgeIndex in their OnDefineNode.
            var graphModel = m_GraphAsset.GraphModel;
            var node1 = graphModel.CreateNode<NodeThatUsesPortEdgeIndexInItsDefineNode>();
            var node2 = graphModel.CreateNode<NodeThatUsesPortEdgeIndexInItsDefineNode>();
            var node3 = graphModel.CreateNode<NodeThatUsesPortEdgeIndexInItsDefineNode>();
            var port1 = node1.GetOutputPorts().FirstOrDefault();
            var port2 = node2.GetInputPorts().FirstOrDefault();
            var port3 = node2.GetOutputPorts().FirstOrDefault();
            var port4 = node3.GetInputPorts().FirstOrDefault();

            Assert.IsNotNull(port1);
            Assert.IsNotNull(port2);
            Assert.IsNotNull(port3);
            Assert.IsNotNull(port4);

            var edge1 = graphModel.CreateEdge(port2, port1);
            var edge2 = graphModel.CreateEdge(port4, port3);

            // Check that PortEdgeIndex works.
            var expectedEdges12 = new List<IEdgeModel> { edge1 };
            var expectedEdges34 = new List<IEdgeModel> { edge2 };
            Assert.AreEqual(expectedEdges12, graphModel.GetEdgesForPort(port1));
            Assert.AreEqual(expectedEdges12, graphModel.GetEdgesForPort(port2));
            Assert.AreEqual(expectedEdges34, graphModel.GetEdgesForPort(port3));
            Assert.AreEqual(expectedEdges34, graphModel.GetEdgesForPort(port4));

            // Simulate the state of nodes and edges after a reload.
            // - clear the ports on the nodes
            // - clear the port cache on the edges
            // - mark the PortEdgeIndex as dirty
            // - call DefineNode on all nodes
            node1.ClearPorts();
            node2.ClearPorts();
            node3.ClearPorts();
            (edge1 as EdgeModel)?.ResetPortCache();
            (edge2 as EdgeModel)?.ResetPortCache();
            (graphModel as GraphModel)?.PortEdgeIndex.MarkDirty();
            node1.DefineNode();
            node2.DefineNode();
            node3.DefineNode();

            // Check that PortEdgeIndex works.
            Assert.AreEqual(expectedEdges12, graphModel.GetEdgesForPort(port1));
            Assert.AreEqual(expectedEdges12, graphModel.GetEdgesForPort(port2));
            Assert.AreEqual(expectedEdges34, graphModel.GetEdgesForPort(port3));
            Assert.AreEqual(expectedEdges34, graphModel.GetEdgesForPort(port4));
        }
    }
}
