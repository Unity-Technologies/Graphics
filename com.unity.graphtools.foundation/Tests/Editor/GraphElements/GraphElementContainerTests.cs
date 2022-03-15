using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    class GraphElementContainerModel : TestModels.NodeModel, IGraphElementContainer
    {
        [SerializeReference]
        List<IGraphElementModel> m_GraphElements = new List<IGraphElementModel>();

        IGraphElementContainer m_Container;

        public void SetContainer(IGraphElementContainer container)
        {
            m_Container = container;
        }

        public override IGraphElementContainer Container => m_Container;

        public IEnumerable<IGraphElementModel> GraphElementModels
        {
            get => m_GraphElements;
        }

        public void AddGraphElement(GraphElementModel model)
        {
            m_GraphElements.Add(model);

            GraphModel.RegisterElement(model);
        }

        public void RemoveElements(IReadOnlyCollection<IGraphElementModel> elementModels)
        {
            foreach (var elementModel in elementModels)
                GraphModel.UnregisterElement(elementModel);

            m_GraphElements.RemoveAll(elementModels.Contains);
        }

        public void Repair()
        {
        }
    }

    class ContainedNodeModel : TestModels.NodeModel
    {
        IGraphElementContainer m_Container;

        public void SetContainer(IGraphElementContainer container)
        {
            m_Container = container;
        }

        public override IGraphElementContainer Container => m_Container;
    }

    class GraphElementContainerTests : GraphViewTester
    {
        [Test]
        public void GraphElementContainerPortsTest()
        {
            var newContainer = GraphModel.CreateNode<GraphElementContainerModel>();
            newContainer.SetContainer(GraphModel);

            var newSubContainer = GraphModel.CreateNode<GraphElementContainerModel>(spawnFlags: SpawnFlags.Orphan);
            newContainer.AddGraphElement(newSubContainer);
            newSubContainer.SetContainer(newContainer);

            var node = GraphModel.CreateNode<ContainedNodeModel>(spawnFlags: SpawnFlags.Orphan);

            var input1 = node.AddInputPort("input1", PortType.Data, TypeHandle.Void);
            var output1 = node.AddOutputPort("output1", PortType.Data, TypeHandle.Void);

            newContainer.AddGraphElement(node);
            node.SetContainer(newContainer);

            var node2 = GraphModel.CreateNode<ContainedNodeModel>(spawnFlags: SpawnFlags.Orphan);

            var input2 = node2.AddInputPort("input2", PortType.Data, TypeHandle.Void);
            var output2 = node2.AddOutputPort("output2", PortType.Data, TypeHandle.Void);

            newSubContainer.AddGraphElement(node2);
            node2.SetContainer(newSubContainer);

            // Testing recursive getPortsModels
            var portModels = GraphModel.GetPortModels().ToArray();

            Assert.AreEqual(4, portModels.Length);

            Assert.IsTrue(portModels.Contains(input1));
            Assert.IsTrue(portModels.Contains(output1));
            Assert.IsTrue(portModels.Contains(input2));
            Assert.IsTrue(portModels.Contains(output2));

            //testing recursive AssignNewGuid
            var newContainerDuplicate = (GraphElementContainerModel)GraphModel.DuplicateNode(newContainer, Vector2.zero);

            Assert.AreNotSame(newContainer.Guid, newContainerDuplicate.Guid);

            var newSubContainerDuplicate = newContainerDuplicate.GraphElementModels.OfType<GraphElementContainerModel>().First();
            var nodeDuplicate = newContainerDuplicate.GraphElementModels.OfType<ContainedNodeModel>().First();

            Assert.AreNotSame(newSubContainer.Guid, newSubContainerDuplicate.Guid);
            Assert.AreNotSame(node.Guid, nodeDuplicate.Guid);

            var node2Duplicate = newSubContainerDuplicate.GraphElementModels.OfType<ContainedNodeModel>().First();
            Assert.AreNotSame(node2.Guid, node2Duplicate.Guid);

            //Testing recursive BuildElementByGuidDictionary
            var graphModel = ((TestModels.GraphModel)GraphModel);

            Assert.IsTrue(graphModel.IsRegistered(newContainer));
            Assert.IsTrue(graphModel.IsRegistered(newSubContainer));
            Assert.IsTrue(graphModel.IsRegistered(node));
            Assert.IsTrue(graphModel.IsRegistered(node2));
        }
    }
}
