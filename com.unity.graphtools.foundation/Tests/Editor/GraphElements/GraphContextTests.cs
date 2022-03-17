using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor.GraphToolsFoundation.Overdrive.Tests.TestModels;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.GraphElements
{
    class GraphContextTests : GraphViewTester
    {
        IContextNodeModel m_Node;

        [SetUp]
        public override void SetUp()
        {
            base.SetUp();

            m_Node = CreateContext<ContextNodeModel>("context", Vector2.zero);
        }

        [Test]
        public void CreateContextFromModelGivesContext()
        {
            Assert.IsFalse(m_Node.IsCollapsible());

            GraphView.RebuildUI();
            List<Node> nodeList = GraphModel.NodeModels.Select(n => n.GetView<Node>(GraphView)).ToList();

            Assert.AreEqual(1, nodeList.Count);

            Assert.IsTrue(nodeList.First() is ContextNode);
        }

        [Test]
        public void CreateBlockFromModelGivesBlockNode()
        {
            var blockNodeModel = CreateBlock(m_Node);

            GraphView.RebuildUI();
            List<Node> nodeList = GraphModel.NodeAndBlockModels.Select(n => n.GetView<Node>(GraphView)).ToList();

            // the block inside the context is found by the query as well as the context itself.
            Assert.AreEqual(2, nodeList.Count);

            ContextNode nodeUI = m_Node.GetView<ContextNode>(GraphView);

            var blocks = nodeUI.Query<BlockNode>().ToList();

            Assert.AreEqual(1, blocks.Count);

            Assert.AreEqual(blocks.First().BlockNodeModel, blockNodeModel);
        }

        [Test]
        public void DuplicateContextWorks()
        {
            var blockNodeModel1 = CreateBlock(m_Node);
            var blockNodeModel2 = CreateBlock(m_Node);

            var duplicatedNode = (IContextNodeModel)GraphModel.DuplicateNode(m_Node, new Vector2());

            Assert.AreNotSame(m_Node.Guid, duplicatedNode.Guid);


            Assert.AreEqual(2, duplicatedNode.GraphElementModels.Count());

            var duplicatedBlockNodeModel1 = duplicatedNode.GraphElementModels.First();
            Assert.AreNotSame(blockNodeModel1.Guid, duplicatedBlockNodeModel1.Guid);
            var duplicatedBlockNodeModel2 = duplicatedNode.GraphElementModels.Last();
            Assert.AreNotSame(blockNodeModel2.Guid, duplicatedBlockNodeModel2.Guid);

            var testGraphModel = GraphModel as GraphModel;
            Assert.IsTrue(testGraphModel.IsRegistered(duplicatedNode));
            Assert.IsTrue(testGraphModel.IsRegistered(duplicatedBlockNodeModel1));
            Assert.IsTrue(testGraphModel.IsRegistered(duplicatedBlockNodeModel2));
        }

        [Test]
        public void GraphElementContentsTest()
        {
            var stickyNote = GraphModel.CreateStickyNote(new Rect(0, 0, 100, 100));

            var block = CreateBlock(m_Node);

            var graphContents = GraphModel.GraphElementModels.ToArray();

            Assert.AreEqual(2 + GraphModel.SectionModels.Count, graphContents.Length);

            Assert.IsTrue(graphContents.Contains(m_Node));
            Assert.IsTrue(graphContents.Contains(stickyNote));

            var contextContents = m_Node.GraphElementModels.ToArray();

            Assert.AreEqual(1, contextContents.Length);

            Assert.IsTrue(contextContents.Contains(block));
        }

        [Test]
        public void InsertAndDeleteBlocksWork()
        {
            var blockNodeModel = CreateBlock(m_Node);
            var blockNodeModel0 = CreateBlock(m_Node, 0);
            var blockNodeModel2 = CreateBlock(m_Node);

            Assert.AreEqual(3, m_Node.GraphElementModels.Count());

            Assert.AreEqual(m_Node.GraphElementModels.First(), blockNodeModel0);
            Assert.AreEqual(m_Node.GraphElementModels.Last(), blockNodeModel2);

            m_Node.RemoveElements(new[] { blockNodeModel });

            Assert.AreEqual(2, m_Node.GraphElementModels.Count());

            Assert.AreEqual(m_Node.GraphElementModels.First(), blockNodeModel0);
            Assert.AreEqual(m_Node.GraphElementModels.Last(), blockNodeModel2);
        }

        [Test]
        public void CreateBlockCmdWorks()
        {
            var searcherItem = new GraphNodeModelSearcherItem("Sample Block", null,
                t => NodeDataCreationExtensions.CreateBlock(t, typeof(BlockNodeModel)));

            GraphView.Dispatch(new CreateBlockFromSearcherCommand(searcherItem, m_Node));

            Assert.AreEqual(1, m_Node.GraphElementModels.Count());
        }

        [Test]
        public void InsertBlocksCommandWorks()
        {
            var blockNodeModel = CreateBlock(m_Node);
            var blockNodeModel0 = CreateBlock(m_Node, 0);
            var blockNodeModel2 = CreateBlock(m_Node);
            var node2 = CreateContext<ContextNodeModel>("context 2", Vector2.zero);

            // insert (move) blocks
            GraphView.Dispatch(new InsertBlocksInContextCommand(node2, 0, new[] { blockNodeModel0, blockNodeModel, blockNodeModel2 }));

            Assert.AreEqual(3, node2.GraphElementModels.Count());
            Assert.AreEqual(0, m_Node.GraphElementModels.Count());

            Assert.AreEqual(node2.GraphElementModels.First(), blockNodeModel0);
            Assert.AreEqual(node2.GraphElementModels.Skip(1).First(), blockNodeModel);
            Assert.AreEqual(node2.GraphElementModels.Last(), blockNodeModel2);

            // this time duplicate and insert blocks
            GraphView.Dispatch(new InsertBlocksInContextCommand(m_Node, 0, new[] { blockNodeModel0, blockNodeModel, blockNodeModel2 }, true));

            Assert.AreEqual(3, node2.GraphElementModels.Count());
            Assert.AreEqual(3, m_Node.GraphElementModels.Count());

            Assert.AreEqual(node2.GraphElementModels.First(), blockNodeModel0);
            Assert.AreEqual(node2.GraphElementModels.Skip(1).First(), blockNodeModel);
            Assert.AreEqual(node2.GraphElementModels.Last(), blockNodeModel2);
        }

        [UnityTest]
        public IEnumerator DeleteContextRemoveBlocksEdges()
        {
            var blockNodeModel = CreateBlock(m_Node, inCount: 1);
            var nodeModel = CreateNode(outCount: 1);

            var edgeModel = GraphModel.CreateEdge(nodeModel.GetOutputPorts().First(), blockNodeModel.GetInputPorts().First());

            Assert.AreEqual(1, GraphModel.EdgeModels.Count);

            GraphView.RebuildUI();
            var edge = edgeModel.GetView<Edge>(GraphView);

            GraphView.Dispatch(new DeleteElementsCommand(m_Node));

            Assert.AreEqual(0, GraphModel.EdgeModels.Count);

            yield return null;

            // Make sure the ui representation of the edge has been removed as well
            Assert.IsNull(edge.panel);
        }
    }
}
