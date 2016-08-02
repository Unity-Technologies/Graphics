using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Graphing;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph.UnitTests
{
    [TestFixture]
    public class AbstractMaterialGraphTests
    {
        private class TestableMGraph : AbstractMaterialGraph
        {}

        private class TestableMNode : AbstractMaterialNode
        {}

        [TestFixtureSetUp]
        public void RunBeforeAnyTests()
        {
            Debug.logger.logHandler = new ConsoleLogHandler();
        }

        [Test]
        public void TestCanCreateMaterialGraph()
        {
            TestableMGraph graph = new TestableMGraph();
            Assert.AreEqual(0, graph.edges.Count());
            Assert.AreEqual(0, graph.GetNodes<AbstractMaterialNode>().Count());
        }

        [Test]
        public void TestCanAddMaterialNodeToMaterialGraph()
        {
            TestableMGraph graph = new TestableMGraph();

            var node = new TestableMNode();
            graph.AddNode(node);
            Assert.AreEqual(0, graph.edges.Count());
            Assert.AreEqual(1, graph.GetNodes<AbstractMaterialNode>().Count());
        }

        [Test]
        public void TestCanNotAddSerializableNodeToMaterialGraph()
        {
            TestableMGraph graph = new TestableMGraph();

            var node = new SerializableNode();
            graph.AddNode(node);
            Assert.AreEqual(0, graph.edges.Count());
            Assert.AreEqual(0, graph.GetNodes<AbstractMaterialNode>().Count());
        }

        [Test]
        public void TestCanGetMaterialNodeFromMaterialGraph()
        {
            TestableMGraph graph = new TestableMGraph();

            var node = new TestableMNode();
            graph.AddNode(node);
            Assert.AreEqual(0, graph.edges.Count());
            Assert.AreEqual(1, graph.GetNodes<AbstractMaterialNode>().Count());

            Assert.AreEqual(node, graph.GetNodeFromGuid(node.guid));
            Assert.AreEqual(node, graph.GetNodeFromGuid<TestableMNode>(node.guid));
        }

        [Test]
        public void TestCreatePixelShaderGraphWorks()
        {
            var graph = new PixelGraph();
            Assert.AreEqual(0, graph.GetNodes<AbstractMaterialNode>().Count());

            var psn = new PixelShaderNode();
            graph.AddNode(psn);
            Assert.AreEqual(0, graph.edges.Count());
            Assert.AreEqual(1, graph.GetNodes<AbstractMaterialNode>().Count());
            Assert.IsInstanceOf(typeof(PixelShaderNode), graph.GetNodes<AbstractMaterialNode>().FirstOrDefault());
            Assert.IsNotNull(graph.pixelMasterNode);
            Assert.AreEqual(1, graph.activeNodes.Count());
        }

        [Test]
        public void TestCanOnlyAddOnePixelShaderNode()
        {
            var graph = new PixelGraph();
            Assert.AreEqual(0, graph.GetNodes<AbstractMaterialNode>().Count());

            var psn = new PixelShaderNode();
            graph.AddNode(psn);
            Assert.AreEqual(0, graph.edges.Count());
            Assert.AreEqual(1, graph.GetNodes<AbstractMaterialNode>().Count());

            var psn2 = new PixelShaderNode();
            graph.AddNode(psn2);
            Assert.AreEqual(0, graph.edges.Count());
            Assert.AreEqual(1, graph.GetNodes<AbstractMaterialNode>().Count());
        }
    }
}
