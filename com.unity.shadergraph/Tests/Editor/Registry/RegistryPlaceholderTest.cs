using NUnit.Framework;
using System.Linq;
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
        [Test]
        public void RegisterFunctionDescriptorTest()
        {
            var graph = GraphDelta.GraphUtil.CreateGraph();
            var registry = new Registry();
            registry.Register<Types.GraphType>();

            var parameters = new LinkedList<ParameterDescriptor>();
            parameters.AddFirst(new ParameterDescriptor("In", TYPE.Vector, Usage.In));
            parameters.AddFirst(new ParameterDescriptor("Out", TYPE.Vector, Usage.Out));
            FunctionDescriptor fd = new FunctionDescriptor(1, "Test", parameters, "Out = In;");
            RegistryKey registryKey = registry.Register(fd);
            GraphDelta.INodeWriter nodeWriter = graph.AddNode(registryKey, $"{fd.Name}-01", registry);
            var nodeReader = graph.GetNodeReader($"{fd.Name}-01");
            bool didRead = nodeReader.GetField("In.Length", out Length len);
            Assert.IsTrue(didRead);
            Assert.AreEqual(Length.Four, len);
            didRead = nodeReader.GetField("Out.Length", out len);
            Assert.IsTrue(didRead);
            Assert.AreEqual(Length.Four, len);

            nodeWriter.SetPortField("In", kLength, Length.Three);
            nodeReader = graph.GetNodeReader($"{fd.Name}-01");
            didRead = nodeReader.GetField("In.Length", out len);
            Assert.IsTrue(didRead);
            Assert.AreEqual(Length.Three, len);
            didRead = nodeReader.GetField("Out.Length", out len);
            Assert.IsTrue(didRead);
            Assert.AreEqual(Length.Four, len);

            bool didReconcretize = graph.ReconcretizeNode($"{fd.Name}-01", registry);
            Assert.IsTrue(didReconcretize);
            nodeReader = graph.GetNodeReader($"{fd.Name}-01");
            didRead = nodeReader.GetField("In.Length", out len);
            Assert.IsTrue(didRead);
            Assert.AreEqual(Length.Three, len);
            didRead = nodeReader.GetField("Out.Length", out len);
            Assert.IsTrue(didRead);
            Assert.AreEqual(Length.Three, len);
        }
    }
}
