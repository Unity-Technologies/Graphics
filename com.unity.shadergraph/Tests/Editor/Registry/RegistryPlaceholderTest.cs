
using NUnit.Framework;
using UnityEngine;
using System.Collections;
using System.Linq;
namespace UnityEditor.ShaderGraph.Registry.UnitTests
{
    [TestFixture]
    class RegistryPlaceholderFixture
    {
        [Test]
        public void RegistryPlaceholderTest()
        {
            var graph = GraphDelta.GraphUtil.CreateGraph();
            var registry = new Registry();

            registry.RegisterNodeBuilder<Exploration.GraphTypeDefinition>();
            registry.RegisterNodeBuilder<Exploration.AddDefinition>();

            graph.AddNode<Exploration.GraphTypeDefinition>("vecA", registry);
            graph.AddNode<Exploration.GraphTypeDefinition>("vecB", registry);
            graph.AddNode<Exploration.AddDefinition>("Add1", registry);

            Assert.IsTrue(graph.TestConnection("Add1", "A", "vecA", "Out", registry));
            Assert.IsTrue(graph.TestConnection("Add1", "B", "vecB", "Out", registry));

            Assert.IsTrue(graph.TryConnect("Add1", "A", "vecA", "Out", registry));
            Assert.IsTrue(graph.TryConnect("Add1", "B", "vecB", "Out", registry));

            Assert.IsTrue(3 == graph.GetNode("Add1").GetPorts().Count());
            Assert.IsTrue(1 == graph.GetNode("vecA").GetPorts().Count());
            // GetConnections not implemented
        }
    }
}
