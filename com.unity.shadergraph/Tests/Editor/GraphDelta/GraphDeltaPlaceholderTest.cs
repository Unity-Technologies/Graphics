using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.ContextLayeredDataStorage;
using UnityEditor.ShaderFoundry;
using UnityEditor.ShaderGraph.Registry;
using UnityEditor.ShaderGraph.Registry.Defs;
using UnityEditor.ShaderGraph.Registry.Types;

namespace UnityEditor.ShaderGraph.GraphDelta.UnitTests
{
    class GraphTestStorage : ContextLayeredDataStorage.ContextLayeredDataStorage
    {
        [TestFixture]
        class GraphUtilFixture
        {
            class TestNode : Registry.Defs.INodeDefinitionBuilder
            {
                public void BuildNode(NodeHandler node, Registry.Registry registry)
                {

                }

                public RegistryFlags GetRegistryFlags()
                {
                    return RegistryFlags.Type;
                }

                public RegistryKey GetRegistryKey()
                {
                    return new RegistryKey() { Name = "TestNode", Version = 1 };
                }

                public ShaderFunction GetShaderFunction(NodeHandler node, ShaderContainer container, Registry.Registry registry)
                {
                    throw new System.NotImplementedException();
                }
            }

            private Registry.Registry registry = null;
            private GraphHandler graphHandler = null;

            [SetUp]
            public void Setup()
            {
                registry = new Registry.Registry();
                registry.Register<TestNode>();
                registry.Register<GraphType>();
                registry.Register<AddNode>();
                graphHandler = new GraphHandler();
            }

            [Test]
            public void CanCreateEmptyGraph()
            {
                Assert.NotNull(graphHandler);
            }

            [Test]
            public void CanAddEmptyNode()
            {
                graphHandler.AddNode<TestNode>("foo", registry);
            }

            [Test]
            public void CanAddAndGetNode()
            {
                graphHandler.AddNode<TestNode>("foo", registry);
                Assert.NotNull(graphHandler.GetNode("foo"));
            }

            [Test]
            public void CanAddAndRemoveNode()
            {
                graphHandler.AddNode<TestNode>("foo", registry);
                Assert.NotNull(graphHandler.GetNode("foo"));
                graphHandler.RemoveNode("foo");
            }

            [Test]
            public void CanAddNodeAndPorts()
            {
                var fooNode = graphHandler.AddNode(Registry.Registry.ResolveKey<TestNode>(), "foo", registry);
                fooNode.AddPort("A",   true,  true);
                fooNode.AddPort("B",   true,  true);
                fooNode.AddPort("Out", false, true);

                PortHandler port = fooNode.GetPort("A");
                Assert.NotNull(port);
                Assert.NotNull(port.GetNode());
                Assert.AreEqual(port.GetNode().ID.FullPath, "foo");
                port = fooNode.GetPort("B");
                Assert.NotNull(port);
                Assert.NotNull(port.GetNode());
                Assert.AreEqual(port.GetNode().ID.FullPath, "foo");
                port = fooNode.GetPort("Out");
                Assert.NotNull(port);
                Assert.NotNull(port.GetNode());
                Assert.AreEqual(port.GetNode().ID.FullPath, "foo");
            }

            [Test]
            public void CanAddTwoNodesAndConnect()
            {
                var fooNode = graphHandler.AddNode(Registry.Registry.ResolveKey<TestNode>(), "foo", registry);
                fooNode.AddPort("A",   true,  true);
                fooNode.AddPort("B",   true,  true);
                fooNode.AddPort("Out", false, true);

                var barNode = graphHandler.AddNode(Registry.Registry.ResolveKey<TestNode>(), "bar", registry);
                barNode.AddPort("A",   true,  true);
                barNode.AddPort("B",   true,  true);
                barNode.AddPort("Out", false, true);

                var edge = graphHandler.AddEdge("foo.Out", "bar.A");
                Assert.NotNull(edge);
            }

            [Test]
            public void ConcretizationTest()
            {
                graphHandler.AddNode<AddNode>("AddNodeRef", registry);
                GraphStorage storage = graphHandler.graphDelta.m_data;
                var concreteLayer = storage.GetLayerRoot(GraphDelta.k_concrete);

                var userLayer = storage.GetLayerRoot(GraphDelta.k_user);
                Assert.NotNull(userLayer);
                Assert.IsTrue(userLayer.Children.Count == 1);
                var userAdd = userLayer.Children[0];
                Assert.NotNull(userAdd);
                Assert.AreEqual(userAdd.ID.FullPath, "AddNodeRef");
                Assert.IsTrue(userAdd.Children.Count == 0);
                var addRegKey = storage.GetMetadata<RegistryKey>(userAdd.ID, GraphDelta.kRegistryKeyName);
                Assert.NotNull(addRegKey);
                Assert.IsFalse(string.IsNullOrEmpty(addRegKey.ToString()));


                Assert.NotNull(concreteLayer);
                Assert.IsTrue(concreteLayer.Children.Count == 1);
                var concreteAdd = concreteLayer.Children[0];
                Assert.NotNull(concreteAdd);
                Assert.IsTrue(concreteAdd.Children.Count == 3);
                var concreteAddPort = concreteAdd.Children[0];
                Assert.NotNull(concreteAddPort);
                Assert.IsTrue(concreteAddPort.Children.Count == 1);
                var concretePortTypeField = concreteAddPort.Children[0];
                Assert.NotNull(concretePortTypeField);
                Assert.IsTrue(concretePortTypeField.Children.Count == 8);
            }

            public class TestDescriptor : Registry.Defs.IContextDescriptor
            {
                public IReadOnlyCollection<IContextDescriptor.ContextEntry> GetEntries()
                {
                    return new List<IContextDescriptor.ContextEntry>()
                    {
                        new IContextDescriptor.ContextEntry()
                        {
                            fieldName = "Foo",
                            primitive = Registry.Types.GraphType.Primitive.Int,
                            height = Registry.Types.GraphType.Height.One,
                            length = Registry.Types.GraphType.Length.One,
                            precision = Registry.Types.GraphType.Precision.Fixed,
                            isFlat = true
                        }
                    };
                }

                public RegistryFlags GetRegistryFlags()
                {
                    return RegistryFlags.Base;
                }

                public RegistryKey GetRegistryKey()
                {
                    return new RegistryKey() { Name = "TestContextDescriptor", Version = 1 };
                }
            }

            [Test]
            public void CanSetupContext()
            {
                registry.Register<TestDescriptor>();
                registry.Register<Registry.Types.GraphType>();
                //graphHandler.SetupContextNodes(new List<Registry.Defs.IContextDescriptor>() { new TestDescriptor() }, registry);
                graphHandler.AddContextNode(Registry.Registry.ResolveKey<TestDescriptor>(), registry);
                var contextNode = graphHandler.GetNode("TestContextDescriptor");
                Assert.NotNull(contextNode);
                var fooReader = contextNode.GetPort("Foo");
                Assert.NotNull(fooReader);
                fooReader = contextNode.GetPorts().First();
                Assert.NotNull(fooReader);
            }
        }
    }

}
