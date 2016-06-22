using System.Linq;
using NUnit.Framework;
using UnityEngine.Graphing;

namespace UnityEditor.Graphing.IntegrationTests
{
    [TestFixture]
    public class SerializationTests
    {
        [Test]
        public void TestCanCreateSerializableGraph()
        {
            var graph = new SerializableGraph();

            Assert.AreEqual(0, graph.edges.Count());
            Assert.AreEqual(0, graph.GetNodes<INode>().Count());
        }
    }
}
