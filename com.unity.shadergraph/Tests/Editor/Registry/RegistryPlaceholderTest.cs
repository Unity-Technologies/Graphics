
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

            registry.RegisterBuilder<Types.GraphType>();
            registry.RegisterBuilder<Types.AddNode>();
            registry.RegisterBuilder<Types.GraphTypeAssignment>();

            graph.AddNode<Types.AddNode>("Add1", registry);

            // Default init should set length to 4.
            var reader = graph.GetNodeReader("Add1");
            reader.GetField("In1.Length", out int len);
            Assert.AreEqual(4, len);

            // Set the length of input port 1 to 1.
            var nodeWriter = graph.GetNodeWriter("Add1");
            nodeWriter.TryAddPort("In1", true, true, out var portWriter);
            portWriter.SetField("Length", 1);

            // We just set this field to 1, it should be 1.
            reader = graph.GetNodeReader("Add1");
            reader.GetField("In1.Length", out len);
            Assert.AreEqual(1, len);

            // After reconcretization, the node definition should propagate the length.
            graph.ReconcretizeNode("Add1", registry);

            // the remaining ports should have length 1.
            reader = graph.GetNodeReader("Add1");
            reader.GetField("In2.Length", out len);
            Assert.AreEqual(1, len);
            reader.GetField("Out.Length", out len);
            Assert.AreEqual(1, len);

            // Add a second Add Node, with length 2 this time.
            var node2 = graph.AddNode<Types.AddNode>("Add2", registry);
            node2.SetField("In1.Length", 2);
            graph.ReconcretizeNode("Add2", registry); // Out should now also be 2.

            // Connecting Out to In should clobber the inlined length with the new length.
            graph.TryConnect("Add2", "Out", "Add1", "In1", registry);
            graph.ReconcretizeNode("Add1", registry);

            graph.GetNodeReader("Add1").GetField("Out.Length", out len);
            Assert.AreEqual(2, len);
        }
    }
}
