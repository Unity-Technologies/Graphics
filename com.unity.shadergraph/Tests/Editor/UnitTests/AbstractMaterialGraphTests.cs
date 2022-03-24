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
            GraphData graph = new ();
            Assert.AreEqual(0, graph.edges.Count());
            Assert.AreEqual(0, graph.GetNodes<AbstractMaterialNode>().Count());
        }

        [Test]
        public void TestCanAddMaterialNodeToMaterialGraph()
        {
            GraphData graph = new ();

            var node = new TestableMNode();
            graph.AddNode(node);
            Assert.AreEqual(0, graph.edges.Count());
            Assert.AreEqual(1, graph.GetNodes<AbstractMaterialNode>().Count());
        }

        [Test]
        public void TestCanGetMaterialNodeFromMaterialGraph()
        {
            GraphData graph = new ();

            var node = new TestableMNode();
            graph.AddNode(node);
            Assert.AreEqual(0, graph.edges.Count());
            Assert.AreEqual(1, graph.GetNodes<AbstractMaterialNode>().Count());

            Assert.AreEqual(node, graph.GetNodeFromId(node.objectId));
            Assert.AreEqual(node, graph.GetNodeFromId<TestableMNode>(node.objectId));
        }
    }
}
