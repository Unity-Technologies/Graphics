using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph.UnitTests
{
    [TestFixture]
    class AbstractMaterialGraphTests
    {
        private class TestableMGraph : AbstractMaterialGraph
        {}

        private class TestableMNode : AbstractMaterialNode
        {}

        [OneTimeSetUp]
        public void RunBeforeAnyTests()
        {
            Debug.unityLogger.logHandler = new ConsoleLogHandler();
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

        /*     [Test]
             public void TestCreatePixelShaderGraphWorks()
             {
                 var graph = new UnityEngine.MaterialGraph.MaterialGraph();
                 Assert.AreEqual(0, graph.GetNodes<AbstractMaterialNode>().Count());

                 var psn = new MetallicMasterNode();
                 graph.AddNode(psn);
                 Assert.AreEqual(0, graph.edges.Count());
                 Assert.AreEqual(1, graph.GetNodes<AbstractMaterialNode>().Count());
                 Assert.IsInstanceOf(typeof(MetallicMasterNode), graph.GetNodes<AbstractMaterialNode>().FirstOrDefault());
                 Assert.IsNotNull(graph.masterNode);
                 Assert.AreEqual(1, graph.GetNodes<INode>().Count());
             }

             [Test]
             public void TestCanAddMultipleMasterNode()
             {
                 var graph = new UnityEngine.MaterialGraph.MaterialGraph();
                 Assert.AreEqual(0, graph.GetNodes<AbstractMaterialNode>().Count());

                 var psn = new MetallicMasterNode();
                 graph.AddNode(psn);
                 Assert.AreEqual(0, graph.edges.Count());
                 Assert.AreEqual(1, graph.GetNodes<AbstractMaterialNode>().Count());

                 var psn2 = new SpecularMasterNode();
                 graph.AddNode(psn2);
                 Assert.AreEqual(0, graph.edges.Count());
                 Assert.AreEqual(2, graph.GetNodes<AbstractMaterialNode>().Count());
             }*/
    }
}
