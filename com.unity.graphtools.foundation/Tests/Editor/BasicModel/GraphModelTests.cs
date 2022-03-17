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

            var graphElements = new IGraphElementModel[] { node1, node2, edge, placemat, stickyNote, constant, variableDeclaration, variable, portal };
            foreach (var element in graphElements)
            {
                Assert.IsTrue(graphModel.TryGetModelFromGuid(element.Guid, out var retrieved), element + " was not found");
                Assert.AreSame(element, retrieved);
            }

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


        class NodeThatHaveAnInputAndAnOutputWithTheSameUniqueName : NodeModel, IFakeNode
        {
            protected override void OnDefineNode()
            {
                base.OnDefineNode();

                this.AddDataInputPort<float>("sameName");
                this.AddDataOutputPort<float>("sameName");
            }
        }

        [Test]
        public void NodeThatHaveAnInputAndAnOutputWithTheSameUniqueNameWorks()
        {
            var graphModel = m_GraphAsset.GraphModel;
            var node1 = graphModel.CreateNode<NodeThatHaveAnInputAndAnOutputWithTheSameUniqueName>();
            var node2 = graphModel.CreateNode<NodeThatHaveAnInputAndAnOutputWithTheSameUniqueName>();

            graphModel.CreateEdge(node2.GetInputPorts().First(), node1.GetOutputPorts().First());

            Assert.AreEqual(1,node2.GetInputPorts().First().GetConnectedEdges().Count);
            Assert.AreEqual(1,node1.GetOutputPorts().First().GetConnectedEdges().Count);
            Assert.AreEqual(0,node1.GetInputPorts().First().GetConnectedEdges().Count);
            Assert.AreEqual(0,node2.GetOutputPorts().First().GetConnectedEdges().Count);
        }

        [Test]
        public void GraphModelNodesRepairTest()
        {
            var graphModel = m_GraphAsset.GraphModel;

            SerializedObject assetAccess = new SerializedObject(m_GraphAsset as GraphAssetModel);

            var nodeProp = assetAccess.FindProperty("m_GraphModel.m_GraphNodeModels");
            nodeProp.InsertArrayElementAtIndex(0);
            assetAccess.ApplyModifiedPropertiesWithoutUndo();
            Assert.IsTrue(graphModel.NodeModels.Any(t=> t == null));
            graphModel.Repair();
            Assert.IsFalse(graphModel.NodeModels.Any(t=> t == null));
        }

        [Test]
        public void GraphModelContextsRepairTest()
        {
            var graphModel = m_GraphAsset.GraphModel;

            var context = graphModel.CreateNode<ContextNodeModel>("context");

            SerializedObject assetAccess = new SerializedObject(m_GraphAsset as GraphAssetModel);

            var nodesProp = assetAccess.FindProperty("m_GraphModel.m_GraphNodeModels");
            int count = nodesProp.arraySize;

            var contextProp = nodesProp.GetArrayElementAtIndex(count-1);

            var blocksProp = contextProp.FindPropertyRelative("m_Blocks");

            blocksProp.InsertArrayElementAtIndex(0);
            assetAccess.ApplyModifiedPropertiesWithoutUndo();

            Assert.IsTrue(context.GraphElementModels.Any(t=> t == null));
            graphModel.Repair();
            Assert.IsFalse(context.GraphElementModels.Any(t=> t == null));
        }

        [Test]
        public void GraphModelBadgesRepairTest()
        {
            var graphModel = m_GraphAsset.GraphModel;

            SerializedObject assetAccess = new SerializedObject(m_GraphAsset as GraphAssetModel);

            var badgeProp = assetAccess.FindProperty("m_GraphModel.m_BadgeModels");
            badgeProp.InsertArrayElementAtIndex(0);
            assetAccess.ApplyModifiedPropertiesWithoutUndo();
            Assert.IsTrue(graphModel.BadgeModels.Any(t=> t == null));
            graphModel.Repair();
            Assert.IsFalse(graphModel.BadgeModels.Any(t=> t == null));
        }

        [Test]
        public void GraphModelEdgesRepairTest()
        {
            var graphModel = m_GraphAsset.GraphModel;
            var node1 = graphModel.CreateNode<NodeThatHaveAnInputAndAnOutputWithTheSameUniqueName>();

            Assert.AreEqual(0,graphModel.EdgeModels.Count);
            SerializedObject assetAccess = new SerializedObject(m_GraphAsset as GraphAssetModel);

            var edgesProp = assetAccess.FindProperty("m_GraphModel.m_GraphEdgeModels");
            edgesProp.InsertArrayElementAtIndex(0);
            assetAccess.ApplyModifiedPropertiesWithoutUndo();
            Assert.IsTrue(graphModel.EdgeModels.Any(t=> t == null));
            graphModel.Repair();
            Assert.IsFalse(graphModel.EdgeModels.Any(t=> t == null));

            assetAccess.Update();
            edgesProp.InsertArrayElementAtIndex(0);
            var edgeProp = edgesProp.GetArrayElementAtIndex(0);

            edgeProp.managedReferenceValue = new EdgeModel();

            assetAccess.ApplyModifiedPropertiesWithoutUndo();

            Assert.AreEqual(1,graphModel.EdgeModels.Count);

            graphModel.Repair();

            Assert.AreEqual(0,graphModel.EdgeModels.Count);
        }

        [Test]
        public void GraphModelStickyNotesRepairTest()
        {
            var graphModel = m_GraphAsset.GraphModel;

            SerializedObject assetAccess = new SerializedObject(m_GraphAsset as GraphAssetModel);

            var stickyNotesProp = assetAccess.FindProperty("m_GraphModel.m_GraphStickyNoteModels");
            stickyNotesProp.InsertArrayElementAtIndex(0);
            assetAccess.ApplyModifiedPropertiesWithoutUndo();
            Assert.IsTrue(graphModel.StickyNoteModels.Any(t=> t == null));
            graphModel.Repair();
            Assert.IsFalse(graphModel.StickyNoteModels.Any(t=> t == null));
        }

        [Test]
        public void GraphModelPlacematsRepairTest()
        {
            var graphModel = m_GraphAsset.GraphModel;

            SerializedObject assetAccess = new SerializedObject(m_GraphAsset as GraphAssetModel);

            var placematProp = assetAccess.FindProperty("m_GraphModel.m_GraphPlacematModels");
            placematProp.InsertArrayElementAtIndex(0);
            assetAccess.ApplyModifiedPropertiesWithoutUndo();
            Assert.IsTrue(graphModel.PlacematModels.Any(t=> t == null));
            graphModel.Repair();
            Assert.IsFalse(graphModel.PlacematModels.Any(t=> t == null));
        }

        [Test]
        public void GraphModelVariablesRepairTest()
        {
            var graphModel = m_GraphAsset.GraphModel;

            SerializedObject assetAccess = new SerializedObject(m_GraphAsset as GraphAssetModel);

            var variableProp = assetAccess.FindProperty("m_GraphModel.m_GraphVariableModels");
            variableProp.InsertArrayElementAtIndex(0);
            assetAccess.ApplyModifiedPropertiesWithoutUndo();
            Assert.IsTrue(graphModel.VariableDeclarations.Any(t=> t == null));
            graphModel.Repair();
            Assert.IsFalse(graphModel.VariableDeclarations.Any(t=> t == null));
        }

        [Test]
        public void GraphModelPortalsRepairTest()
        {
            var graphModel = m_GraphAsset.GraphModel;

            SerializedObject assetAccess = new SerializedObject(m_GraphAsset as GraphAssetModel);

            var portalProp = assetAccess.FindProperty("m_GraphModel.m_GraphPortalModels");
            portalProp.InsertArrayElementAtIndex(0);
            assetAccess.ApplyModifiedPropertiesWithoutUndo();
            Assert.IsTrue(graphModel.PortalDeclarations.Any(t=> t == null));
            graphModel.Repair();
            Assert.IsFalse(graphModel.PortalDeclarations.Any(t=> t == null));
        }

        [Test]
        public void GraphModelSectionsRepairTest()
        {
            var graphModel = m_GraphAsset.GraphModel;

            SerializedObject assetAccess = new SerializedObject(m_GraphAsset as GraphAssetModel);

            var sectionProp = assetAccess.FindProperty("m_GraphModel.m_SectionModels");
            sectionProp.InsertArrayElementAtIndex(0);
            assetAccess.ApplyModifiedPropertiesWithoutUndo();
            Assert.IsFalse(graphModel.SectionModels.Any(t=> t == null));
        }
    }
}
