using System.Linq;
using NUnit.Framework;

namespace UnityEditor.MaterialGraph.Tests
{
    [TestFixture]
    public class MaterialGraphTests
    {
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
