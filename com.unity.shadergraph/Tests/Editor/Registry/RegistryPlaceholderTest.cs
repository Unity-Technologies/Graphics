
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

            registry.Register<Types.GraphType>();
            registry.Register<Types.AddNode>();
            registry.Register<Types.GraphTypeAssignment>();

            // should default concretize length to 4.
            graph.AddNode<Types.AddNode>("Add1", registry);
            var reader = graph.GetNodeReader("Add1");
            reader.GetField("In1.Length", out int len);
            Assert.AreEqual(4, len);

            // Set the length of input port 1 to 1.
            var nodeWriter = graph.GetNodeWriter("Add1");
            nodeWriter.SetPortField("In1", "Length", 1);

            // After reconcretization, the node definition should propagate the length.
            graph.ReconcretizeNode("Add1", registry);
            reader = graph.GetNodeReader("Add1");
            reader.GetField("In1.Length", out len);
            Assert.AreEqual(1, len);
            reader.GetField("In2.Length", out len);
            Assert.AreEqual(1, len);
            reader.GetField("Out.Length", out len);
            Assert.AreEqual(1, len);

            // Add a second Add Node, with length 2 this time.
            var node2 = graph.AddNode<Types.AddNode>("Add2", registry);
            node2.SetPortField("In2", "Length", 2);
            graph.ReconcretizeNode("Add2", registry);
            reader = graph.GetNodeReader("Add2");
            reader.GetField("In1.Length", out len);
            Assert.AreEqual(2, len);
            reader.GetField("In2.Length", out len);
            Assert.AreEqual(2, len);
            reader.GetField("Out.Length", out len);
            Assert.AreEqual(2, len);

            // Connecting Out to In should clobber the inlined length with the new length.
            graph.TryConnect("Add2", "Out", "Add1", "In1", registry);
            graph.ReconcretizeNode("Add1", registry);
            reader = graph.GetNodeReader("Add1");
            reader.TryGetPort("In1", out var portReader);
            portReader.GetField("Length", out len);
            Assert.AreEqual(2, len);
            reader.GetField("In2.Length", out len);
            Assert.AreEqual(2, len);
            reader.GetField("Out.Length", out len);
            Assert.AreEqual(2, len);
        }
    }
}
