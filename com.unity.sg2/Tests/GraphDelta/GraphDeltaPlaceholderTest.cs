using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.ShaderFoundry;
using static UnityEditor.ShaderGraph.GraphDelta.GraphType;

namespace UnityEditor.ShaderGraph.GraphDelta.UnitTests
{
    class GraphTestStorage : ContextLayeredDataStorage.ContextLayeredDataStorage
    {
        [TestFixture]
        class GraphUtilFixture
        {
            class TestNode : INodeDefinitionBuilder
            {
                public const string k_counter = "ConcretizationCounter";
                public void BuildNode(NodeHandler node, Registry registry)
                {
                    node.AddPort<GraphType>("Input", true, registry);
                    node.AddPort<GraphType>("Output", false, registry);
                    node.DefaultLayer = GraphDelta.k_user;
                    var field = node.GetField<int>(k_counter);
                    if (field != null)
                    {
                        field.SetData(field.GetData() + 1);
                    }
                    else
                    { 
                        node.AddField(k_counter,1,false);
                    }
                    node.DefaultLayer = GraphDelta.k_concrete;
                }

                public RegistryFlags GetRegistryFlags()
                {
                    return RegistryFlags.Type;
                }

                public RegistryKey GetRegistryKey()
                {
                    return new RegistryKey() { Name = "TestNode", Version = 1 };
                }

                public ShaderFunction GetShaderFunction(NodeHandler node, ShaderContainer container, Registry registry)
                {
                    throw new System.NotImplementedException();
                }
            }

            private Registry registry = null;
            private GraphHandler graphHandler = null;

            [SetUp]
            public void Setup()
            {
                registry = new Registry();
                registry.Register<TestNode>();
                registry.Register<GraphType>();
                registry.Register<TestAddNode>();
                graphHandler = new GraphHandler(registry);
            }

            [Test]
            public void CanCreateEmptyGraph()
            {
                Assert.NotNull(graphHandler);
            }

            [Test]
            public void CanAddEmptyNode()
            {
                graphHandler.AddNode<TestNode>("foo");
            }

            [Test]
            public void CanAddAndGetNode()
            {
                graphHandler.AddNode<TestNode>("foo");
                Assert.NotNull(graphHandler.GetNode("foo"));
            }

            [Test]
            public void CanAddAndRemoveNode()
            {
                graphHandler.AddNode<TestNode>("foo");
                Assert.NotNull(graphHandler.GetNode("foo"));
                graphHandler.RemoveNode("foo");
            }

            [Test]
            public void CanAddNodeAndPorts()
            {
                var fooNode = graphHandler.AddNode(Registry.ResolveKey<TestNode>(), "foo");
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
                var fooNode = graphHandler.AddNode(Registry.ResolveKey<TestNode>(), "foo");
                fooNode.AddPort("A",   true,  true);
                fooNode.AddPort("B",   true,  true);
                fooNode.AddPort("Out", false, true);

                var barNode = graphHandler.AddNode(Registry.ResolveKey<TestNode>(), "bar");
                barNode.AddPort("A",   true,  true);
                barNode.AddPort("B",   true,  true);
                barNode.AddPort("Out", false, true);

                var edge = graphHandler.AddEdge("foo.Out", "bar.A");
                Assert.NotNull(edge);
            }

            [Test]
            public void ConcretizationOnBuildTest()
            {
                graphHandler.AddNode<TestAddNode>("AddNodeRef");
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

            public class TestDescriptor : IContextDescriptor
            {
                public IEnumerable<ContextEntry> GetEntries()
                {
                    return new List<ContextEntry>()
                    {
                        new ContextEntry()
                        {
                            fieldName = "Foo",
                            primitive = GraphType.Primitive.Int,
                            height = GraphType.Height.One,
                            length = GraphType.Length.One,
                            precision = GraphType.Precision.Fixed,
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
                registry.Register<GraphType>();
                graphHandler.AddContextNode(Registry.ResolveKey<TestDescriptor>());
                var contextNode = graphHandler.GetNode("TestContextDescriptor");
                Assert.NotNull(contextNode);
                var fooReader = contextNode.GetPort("Foo");
                Assert.NotNull(fooReader);
                fooReader = contextNode.GetPorts().First();
                Assert.NotNull(fooReader);
            }

            [Test]
            public void CanDeserializeNodeAndPorts()
            {
                GraphDelta graphDelta = graphHandler.graphDelta;
                NodeHandler node = graphDelta.AddNode<TestNode>("Add", registry);

                node.AddPort("A", true, true);
                node.AddPort("B", true, true);
                node.AddPort("Out", false, true);


                var nodeRef = graphHandler.GetNode("Add");
                Assert.NotNull(nodeRef);
                var portReader = nodeRef.GetPort("A");
                Assert.NotNull(portReader);
                Assert.NotNull(portReader.GetNode());
                Assert.AreEqual(portReader.GetNode().ID.LocalPath, "Add");
                portReader = nodeRef.GetPort("B");
                Assert.NotNull(portReader);
                Assert.NotNull(portReader.GetNode());
                Assert.AreEqual(portReader.GetNode().ID.LocalPath, "Add");
                portReader = nodeRef.GetPort("Out");
                Assert.NotNull(portReader);
                Assert.NotNull(portReader.GetNode());
                Assert.AreEqual(portReader.GetNode().ID.LocalPath, "Add");

                var roundTrip = graphHandler.ToSerializedFormat();
                var deserializedHandler = new GraphHandler(roundTrip, registry);

                // Rerun tests on the deserialized version
                nodeRef = deserializedHandler.GetNode("Add");
                Assert.NotNull(nodeRef);
                portReader = nodeRef.GetPort("A");
                Assert.NotNull(portReader);
                Assert.NotNull(portReader.GetNode());
                Assert.AreEqual(portReader.GetNode().ID.LocalPath, "Add");
                portReader = nodeRef.GetPort("B");
                Assert.NotNull(portReader);
                Assert.NotNull(portReader.GetNode());
                Assert.AreEqual(portReader.GetNode().ID.LocalPath, "Add");
                portReader = nodeRef.GetPort("Out");
                Assert.NotNull(portReader);
                Assert.NotNull(portReader.GetNode());
                Assert.AreEqual(portReader.GetNode().ID.LocalPath, "Add");
            }


            [Test]
            public void CanDeserializeConnections()
            {
                GraphDelta graphDelta = graphHandler.graphDelta;
                NodeHandler foo = graphDelta.AddNode<TestNode>("Foo", registry);
                NodeHandler bar = graphDelta.AddNode<TestNode>("Bar", registry);
                Assert.IsTrue(foo.AddPort("A", true, true) != null);
                Assert.IsTrue(foo.AddPort("B", true, true) != null);
                var output = foo.AddPort("Out", false, true);
                var input = bar.AddPort("A", true, true);
                Assert.IsNotNull(output);
                Assert.IsNotNull(input);
                Assert.IsTrue(graphHandler.AddEdge(output.ID, input.ID) != null);
                var thruEdge = graphDelta.GetConnectedPorts("Foo.Out", registry).FirstOrDefault();
                Assert.NotNull(thruEdge);
                Assert.IsTrue(thruEdge.ID.Equals("Bar.A"));

                var roundTrip = graphHandler.ToSerializedFormat();
                var deserializedHandler = new GraphHandler(roundTrip, registry).graphDelta;

                // rerun tests on the serialized version
                try
                {
                    thruEdge = deserializedHandler.GetConnectedPorts("Foo.Out", registry).FirstOrDefault();
                }
                catch(System.Exception)
                {
                    Assert.Fail("Connected element could not be found.");
                }
                Assert.NotNull(thruEdge);
                Assert.IsTrue(thruEdge.ID.Equals("Bar.A"));
            }

            [Test]
            public void ReconcretizeGraph()
            {
                var registry = new Registry();
                registry.Register<GraphType>();
                registry.Register<TestAddNode>();
                registry.Register<GraphTypeAssignment>();
                var graph = new GraphHandler(registry);

                // should default concretize length to 4.
                graph.AddNode<TestAddNode>("Add1");
                var reader = graph.GetNode("Add1");
                var len = reader.GetPort("In1").GetTypeField().GetSubField<GraphType.Length>("Length").GetData();
                Assert.AreEqual(4, (int)len);

                var roundTrip = graph.ToSerializedFormat();
                var deserializedHandler = new GraphHandler(roundTrip, registry);
                deserializedHandler.ReconcretizeAll();

                reader = deserializedHandler.GetNode("Add1");
                len = reader.GetPort("In2").GetTypeField().GetSubField<GraphType.Length>("Length").GetData();

                Assert.AreEqual(4, (int)len);
            }

            [Test]
            public void ReferenceNodes()
            {
                registry.Register<TestDescriptor>();
                registry.Register<GraphType>();
                graphHandler.AddContextNode(Registry.ResolveKey<TestDescriptor>());
                var contextNode = graphHandler.GetNode("TestContextDescriptor");

                ContextEntry entry = new()
                {
                    fieldName = "TestContextEntry",
                    precision = GraphType.Precision.Single,
                    primitive = GraphType.Primitive.Float,
                    length = GraphType.Length.Four,
                    height = GraphType.Height.One
                };

                ContextBuilder.AddReferableEntry(contextNode, entry, registry, ContextEntryEnumTags.PropertyBlockUsage.Excluded, displayName: "Foo", defaultValue: "(0,0,0,0)");
                graphHandler.AddReferenceNode("testNodeRef", "TestContextDescriptor", "TestContextEntry");
                graphHandler.AddReferenceNode("fooNodeRef", "TestContextDescriptor", "Foo");


                Assert.AreEqual("TestContextDescriptor", graphHandler.GetConnectedNodes("testNodeRef").First().ID.LocalPath);
                Assert.AreEqual("TestContextDescriptor", graphHandler.GetConnectedNodes("fooNodeRef").First().ID.LocalPath);

                var testField = graphHandler.GetNode("testNodeRef").GetPort(ReferenceNodeBuilder.kOutput).GetTypeField(); // float4
                var fooField = graphHandler.GetNode("fooNodeRef").GetPort(ReferenceNodeBuilder.kOutput).GetTypeField(); // fixed int

                Assert.AreEqual(GraphType.Primitive.Float, GraphTypeHelpers.GetPrimitive(testField));
                // ReferenceNodes don't properly propagate TypeField information to their outgoing port.
                // TODO: Maybe have some sort of Clone operation on ITypeDefinitionBuilder?
                // Assert.AreEqual(GraphType.Primitive.Int, GraphTypeHelpers.GetPrimitive(fooField));
            }

            [Test]
            public void CanStoreAndLoad()
            {
                var registry = new Registry();
                registry.Register<GraphType>();
                registry.Register<TestAddNode>();
                registry.Register<GraphTypeAssignment>();
                var graph = new GraphHandler(registry);

                var node = graph.AddNode<TestAddNode>("Add1");
                node.AddField("myData", 45);
                var field = node.GetField("myData");
                var data = field.GetData<int>();
                Assert.AreEqual(data, 45);
            }
			
            [Test]
            public void ConcretizationTests()
            {
                var test1 = graphHandler.AddNode<TestNode>("test1");
                var cCounter1 = test1.GetField<int>(TestNode.k_counter);
                Assert.IsNotNull(cCounter1);
                Assert.AreEqual(1, cCounter1.GetData()); //initializes to 1 as AddNode calls BuildNode
                var test2 = graphHandler.AddNode<TestNode>("test2");
                var cCounter2 = test2.GetField<int>(TestNode.k_counter);
                Assert.IsNotNull(cCounter2);
                Assert.AreEqual(1, cCounter2.GetData()); //initializes to 1 as AddNode calls BuildNode

                graphHandler.ReconcretizeNode("test2");
                Assert.AreEqual(2, cCounter2.GetData()); //Explicitly calling reconcretize should increment the value

                graphHandler.AddEdge("test1.Output", "test2.Input");
                Assert.AreEqual(3, cCounter2.GetData()); //Connecting an edge should reconcretize downstream
                Assert.AreEqual(1, cCounter1.GetData()); //Connecting an edge should not affect source node 

                test1.SetPortField("Input", "Length", Length.Two); //Should cause reconcretization downstream, includind test1
                Assert.AreEqual(4, cCounter2.GetData()); 
                Assert.AreEqual(2, cCounter1.GetData());

                graphHandler.RemoveEdge("test1.Output", "test2.Input");
                Assert.AreEqual(5, cCounter2.GetData()); 
                Assert.AreEqual(2, cCounter1.GetData());
            }

        }
    }
}
