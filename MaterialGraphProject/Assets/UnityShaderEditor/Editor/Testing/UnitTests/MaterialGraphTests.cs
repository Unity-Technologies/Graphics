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
        [OneTimeSetUp]
        public void RunBeforeAnyTests()
        {
            Debug.unityLogger.logHandler = new ConsoleLogHandler();
        }

        [Test]
        public void TestCreateMaterialGraph()
        {
            var graph = new UnityEngine.MaterialGraph.MaterialGraph();

            Assert.IsNotNull(graph);

            Assert.AreEqual(0, graph.GetNodes<AbstractMaterialNode>().Count());
        }
    }
}
