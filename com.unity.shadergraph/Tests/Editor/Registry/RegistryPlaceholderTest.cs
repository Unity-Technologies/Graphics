using NUnit.Framework;
using com.unity.shadergraph.defs;
using System.Collections.Generic;
using static UnityEditor.ShaderGraph.Registry.Types.GraphType;

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
            reader.GetField("In1.Length", out Length len);
            Assert.AreEqual(4, (int)len);

            // Set the length of input port 1 to 1.
            var nodeWriter = graph.GetNodeWriter("Add1");
            nodeWriter.SetPortField("In1", "Length", Length.One);

            // After reconcretization, the node definition should propagate the length.
            graph.ReconcretizeNode("Add1", registry);
            reader = graph.GetNodeReader("Add1");
            reader.GetField("In1.Length", out len);
            Assert.AreEqual(1, (int)len);
            reader.GetField("In2.Length", out len);
            Assert.AreEqual(1, (int)len);
            reader.GetField("Out.Length", out len);
            Assert.AreEqual(1, (int)len);

            // Add a second Add Node, with length 2 this time.
            var node2 = graph.AddNode<Types.AddNode>("Add2", registry);
            node2.SetPortField("In2", "Length", Length.Two);
            graph.ReconcretizeNode("Add2", registry);
            reader = graph.GetNodeReader("Add2");
            reader.GetField("In1.Length", out len);
            Assert.AreEqual(2, (int)len);
            reader.GetField("In2.Length", out len);
            Assert.AreEqual(2, (int)len);
            reader.GetField("Out.Length", out len);
            Assert.AreEqual(2, (int)len);

            // Connecting Out to In should clobber the inlined length with the new length.
            graph.TryConnect("Add2", "Out", "Add1", "In1", registry);
            graph.ReconcretizeNode("Add1", registry);
            reader = graph.GetNodeReader("Add1");
            reader.TryGetPort("In1", out var portReader);
            portReader.GetField("Length", out len);
            Assert.AreEqual(2, (int)len);
            reader.GetField("In2.Length", out len);
            Assert.AreEqual(2, (int)len);
            reader.GetField("Out.Length", out len);
            Assert.AreEqual(2, (int)len);
        }

        [Test]
        public void RegisterFunctionDescriptorTest()
        {
            // create the registry
            var registry = new Registry();
            registry.Register<Types.GraphType>();

            // create the graph
            var graph = GraphDelta.GraphUtil.CreateGraph();

            var parameters = new LinkedList<ParameterDescriptor>();
            parameters.AddFirst(new ParameterDescriptor("In", TYPE.Vector, Usage.In));
            parameters.AddFirst(new ParameterDescriptor("Out", TYPE.Vector, Usage.Out));
            FunctionDescriptor fd = new(1, "Test", parameters, "Out = In;");
            RegistryKey registryKey = registry.Register(fd);

            // add a single node to the graph
            string nodeName = $"{fd.Name}-01";
            GraphDelta.INodeWriter nodeWriter = graph.AddNode(registryKey, nodeName, registry);

            // check that the the node was added
            var nodeReader = graph.GetNodeReader(nodeName);
            bool didRead = nodeReader.GetField("In.Length", out Length len);
            Assert.IsTrue(didRead);

            // EXPECT that both In and Out are concretized into length = 4 (default)
            Assert.AreEqual(Length.Four, len);
            didRead = nodeReader.GetField("Out.Length", out len);
            Assert.IsTrue(didRead);
            Assert.AreEqual(Length.Four, len);
        }
    }
}
