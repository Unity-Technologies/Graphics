using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Graphing;
using UnityEngine.MaterialGraph;

namespace UnityEditor.MaterialGraph.UnitTests
{
    [TestFixture]
    public class MaterialGraphTests
    {
        [TestFixtureSetUp]
        public void RunBeforeAnyTests()
        {
            Debug.logger.logHandler = new ConsoleLogHandler();
        }

        [Test]
        public void TestCreateMaterialGraph()
        {
            var graph = new UnityEngine.MaterialGraph.MaterialGraph();

            Assert.IsNotNull(graph.currentGraph);

            graph.PostCreate();

            Assert.AreEqual(0, graph.currentGraph.GetNodes<AbstractMaterialNode>().Count());
        }
    }
}
