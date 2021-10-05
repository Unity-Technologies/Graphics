using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEditor.Graphing;

namespace UnityEditor.ShaderGraph.UnitTests
{
    [TestFixture]
    class AbstractMaterialGraphTests
    {
        private class TestableMNode : AbstractMaterialNode
        { }

        [OneTimeSetUp]
        public void RunBeforeAnyTests()
        {
            Debug.unityLogger.logHandler = new ConsoleLogHandler();
        }

        [Test]
        public void TestCanCreateMaterialGraph()
        {
            GraphData graph = new GraphData();
            Assert.AreEqual(0, graph.edges.Count());
            Assert.AreEqual(0, graph.GetNodes<AbstractMaterialNode>().Count());
        }

        [Test]
        public void TestCanAddMaterialNodeToMaterialGraph()
        {
            GraphData graph = new GraphData();

            var node = new TestableMNode();
            graph.AddNode(node);
            Assert.AreEqual(0, graph.edges.Count());
            Assert.AreEqual(1, graph.GetNodes<AbstractMaterialNode>().Count());
        }

        [Test]
        public void TestCanGetMaterialNodeFromMaterialGraph()
        {
            GraphData graph = new GraphData();

            var node = new TestableMNode();
            graph.AddNode(node);
            Assert.AreEqual(0, graph.edges.Count());
            Assert.AreEqual(1, graph.GetNodes<AbstractMaterialNode>().Count());

            Assert.AreEqual(node, graph.GetNodeFromId(node.objectId));
            Assert.AreEqual(node, graph.GetNodeFromId<TestableMNode>(node.objectId));
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
                 Assert.AreEqual(1, graph.GetNodes<AbstractMaterialNode>().Count());
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
