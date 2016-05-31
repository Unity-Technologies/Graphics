using System.Linq;
using NUnit.Framework;
using UnityEngine.Graphing;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph.Tests
{
    [TestFixture]
    public class AbstractMaterialGraphTests
    {
        private class TestableMGraph : AbstractMaterialGraph
        {}

        private class TestableMNode : AbstractMaterialNode
        {
            public TestableMNode(AbstractMaterialGraph theOwner) : base(theOwner)
            {}
        }
        private class TimeTestableMNode : AbstractMaterialNode, IRequiresTime
        {
            public TimeTestableMNode(AbstractMaterialGraph theOwner) : base(theOwner)
            {}
        }

        [Test]
        public void TestCanCreateMaterialGraph()
        {
            TestableMGraph graph = new TestableMGraph();
            Assert.AreEqual(0, graph.edges.Count());
            Assert.AreEqual(0, graph.nodes.Count());
        }

        [Test]
        public void TestCanAddMaterialNodeToMaterialGraph()
        {
            TestableMGraph graph = new TestableMGraph();
            
            var node = new TestableMNode(graph);
            graph.AddNode(node);
            Assert.AreEqual(0, graph.edges.Count());
            Assert.AreEqual(1, graph.nodes.Count());
            Assert.AreEqual(1, graph.materialNodes.Count());
        }

        [Test]
        public void TestCanNotAddSerializableNodeToMaterialGraph()
        {
            TestableMGraph graph = new TestableMGraph();
            
            var node = new SerializableNode(graph);
            graph.AddNode(node);
            Assert.AreEqual(0, graph.edges.Count());
            Assert.AreEqual(0, graph.nodes.Count());
            Assert.AreEqual(0, graph.materialNodes.Count());
        }

        [Test]
        public void TestCanGetMaterialNodeFromMaterialGraph()
        {
            TestableMGraph graph = new TestableMGraph();

            var node = new TestableMNode(graph);
            graph.AddNode(node);
            Assert.AreEqual(0, graph.edges.Count());
            Assert.AreEqual(1, graph.nodes.Count());

            Assert.AreEqual(node, graph.GetMaterialNodeFromGuid(node.guid));
        }

        [Test]
        public void TestMaterialGraphNeedsRepaintWhenTimeNodePresent()
        {
            TestableMGraph graph = new TestableMGraph();
            graph.AddNode(new TestableMNode(graph));
            Assert.AreEqual(0, graph.edges.Count());
            Assert.AreEqual(1, graph.nodes.Count());
            Assert.IsFalse(graph.requiresRepaint);

            graph.AddNode(new TimeTestableMNode(graph));
            Assert.IsTrue(graph.requiresRepaint);
        }

        [Test]
        public void TestCreatePixelShaderGraphWorks()
        {
            var graph = new PixelGraph();
            Assert.AreEqual(0, graph.nodes.Count());

            var psn = new PixelShaderNode(graph);
            graph.AddNode(psn);
            Assert.AreEqual(0, graph.edges.Count());
            Assert.AreEqual(1, graph.nodes.Count());
            Assert.IsInstanceOf(typeof(PixelShaderNode), graph.nodes.FirstOrDefault());
            Assert.IsNotNull(graph.pixelMasterNode);
            Assert.AreEqual(1, graph.activeNodes.Count());
        }

        [Test]
        public void TestCanOnlyAddOnePixelShaderNode()
        {
            var graph = new PixelGraph();
            Assert.AreEqual(0, graph.nodes.Count());

            var psn = new PixelShaderNode(graph);
            graph.AddNode(psn);
            Assert.AreEqual(0, graph.edges.Count());
            Assert.AreEqual(1, graph.nodes.Count());

            var psn2 = new PixelShaderNode(graph);
            graph.AddNode(psn2);
            Assert.AreEqual(0, graph.edges.Count());
            Assert.AreEqual(1, graph.nodes.Count());
        }
    }
}
