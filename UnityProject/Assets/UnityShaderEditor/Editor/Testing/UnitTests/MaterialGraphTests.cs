using System.Linq;
using NUnit.Framework;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.MaterialGraph.Tests
{
    [TestFixture]
    public class MaterialGraphTests
    {
        [SetUpFixture]
        public class SetUpClass
        {
            [SetUp]
            void RunBeforeAnyTests()
            {
                Debug.logger.logHandler = new ConsoleLogHandler();
            }
        }

        [Test]
        public void TestCreateMaterialGraph()
        {
            MaterialGraph graph = new MaterialGraph();

            Assert.IsNotNull(graph.currentGraph);
            Assert.IsNotNull(graph.materialOptions);

            graph.PostCreate();
            
            Assert.AreEqual(1, graph.currentGraph.nodes.Count());
            Assert.IsInstanceOf(typeof(PixelShaderNode), graph.currentGraph.nodes.FirstOrDefault());
        }
    }
}
