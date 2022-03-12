using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.ContextLayeredDataStorage;
using UnityEditor.ShaderGraph.Registry;
using UnityEditor.ShaderGraph.Registry.Defs;

namespace UnityEditor.ShaderGraph.GraphDelta.UnitTests
{
    class GraphTestStorage : ContextLayeredDataStorage.ContextLayeredDataStorage
    {
        [TestFixture]
        class GraphUtilFixture
        {
            [Test]
            public void CanCreateEmptyGraph()
            {
                GraphHandler graphHandler = new GraphHandler();
                Assert.NotNull(graphHandler);
            }

            [Test]
            public void CanAddEmptyNode()
            {
                GraphDelta graphHandler = new GraphHandler().graphDelta;
                using (INodeWriter node = graphHandler.AddNode("foo"))
                {
                    Assert.NotNull(node);
                }
            }

            [Test]
            public void CanAddAndGetNode()
            {
                GraphDelta graphHandler = new GraphHandler().graphDelta;
                graphHandler.AddNode("foo");
                Assert.NotNull(graphHandler.GetNodeReader("foo"));
            }

            [Test]
            public void CanAddAndRemoveNode()
            {
                GraphDelta graphHandler = new GraphHandler().graphDelta;
                graphHandler.AddNode("foo");
                Assert.NotNull(graphHandler.GetNodeReader("foo"));
                graphHandler.RemoveNode("foo");
            }

            [Test]
            public void CanAddNodeAndPorts()
            {
                GraphDelta graphHandler = new GraphHandler().graphDelta;
                using (INodeWriter node = graphHandler.AddNode("Add"))
                {
                    node.TryAddPort("A", true, true, out IPortWriter _);
                    node.TryAddPort("B", true, true, out IPortWriter _);
                    node.TryAddPort("Out", false, true, out IPortWriter _);
                }

                var nodeRef = graphHandler.GetNodeReader("Add");
                Assert.NotNull(nodeRef);
                Assert.IsTrue(nodeRef.TryGetPort("A", out IPortReader portReader));
                Assert.NotNull(portReader);
                Assert.NotNull(portReader.GetNode());
                Assert.AreEqual(portReader.GetNode().GetName(), "Add");
                Assert.IsTrue(nodeRef.TryGetPort("B", out portReader));
                Assert.NotNull(portReader);
                Assert.NotNull(portReader.GetNode());
                Assert.AreEqual(portReader.GetNode().GetName(), "Add");
                Assert.IsTrue(nodeRef.TryGetPort("Out", out portReader));
                Assert.NotNull(portReader);
                Assert.NotNull(portReader.GetNode());
                Assert.AreEqual(portReader.GetNode().GetName(), "Add");
            }

            [Test]
            public void CanAddTwoNodesAndConnect()
            {
                GraphDelta graphHandler = new GraphHandler().graphDelta;
                using (INodeWriter foo = graphHandler.AddNode("Foo"))
                using (INodeWriter bar = graphHandler.AddNode("Bar"))
                {
                    Assert.IsTrue(foo.TryAddPort("A", true, true, out IPortWriter _));
                    Assert.IsTrue(foo.TryAddPort("B", true, true, out IPortWriter _));
                    Assert.IsTrue(foo.TryAddPort("Out", false, true, out IPortWriter output));
                    Assert.IsTrue(bar.TryAddPort("A", true, true, out IPortWriter input));
                    Assert.IsNotNull(output);
                    Assert.IsNotNull(input);
                    Assert.IsTrue(output.TryAddConnection(input));
                }
                var thruEdge = (graphHandler.m_data.Search("Foo.Out._Output") as Element<List<Element>>).data[0];
                var normSearch = graphHandler.m_data.Search("Bar.A");
                Assert.NotNull(thruEdge);
                Assert.NotNull(normSearch);
                Assert.AreEqual(thruEdge, normSearch);
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
                GraphDelta graphHandler = new GraphHandler().graphDelta;
                var registry = new Registry.Registry();
                registry.Register<TestDescriptor>();
                registry.Register<Registry.Types.GraphType>();
                graphHandler.SetupContextNodes(new List<Registry.Defs.IContextDescriptor>() { new TestDescriptor() }, registry);
                var contextNode = graphHandler.GetNodeReader("TestContextDescriptor");
                Assert.NotNull(contextNode);
                Assert.IsTrue(contextNode.TryGetPort("Foo", out var fooReader));
                Assert.NotNull(fooReader);
            }

            [Test]
            public void CanDeserializeNodeAndPorts()
            {
                GraphHandler graphHandler = new GraphHandler();
                GraphDelta graphDelta = graphHandler.graphDelta;
                using (INodeWriter node = graphDelta.AddNode("Add"))
                {
                    node.TryAddPort("A", true, true, out IPortWriter _);
                    node.TryAddPort("B", true, true, out IPortWriter _);
                    node.TryAddPort("Out", false, true, out IPortWriter _);
                }

                var nodeRef = graphDelta.GetNodeReader("Add");
                Assert.NotNull(nodeRef);
                Assert.IsTrue(nodeRef.TryGetPort("A", out IPortReader portReader));
                Assert.NotNull(portReader);
                Assert.NotNull(portReader.GetNode());
                Assert.AreEqual(portReader.GetNode().GetName(), "Add");
                Assert.IsTrue(nodeRef.TryGetPort("B", out portReader));
                Assert.NotNull(portReader);
                Assert.NotNull(portReader.GetNode());
                Assert.AreEqual(portReader.GetNode().GetName(), "Add");
                Assert.IsTrue(nodeRef.TryGetPort("Out", out portReader));
                Assert.NotNull(portReader);
                Assert.NotNull(portReader.GetNode());
                Assert.AreEqual(portReader.GetNode().GetName(), "Add");

                var roundTrip = graphHandler.ToSerializedFormat();
                var deserializedHandler = new GraphHandler(roundTrip).graphDelta;

                // Rerun tests on the deserialized version
                nodeRef = deserializedHandler.GetNodeReader("Add");
                Assert.NotNull(nodeRef);
                Assert.IsTrue(nodeRef.TryGetPort("A", out portReader));
                Assert.NotNull(portReader);
                Assert.NotNull(portReader.GetNode());
                Assert.AreEqual(portReader.GetNode().GetName(), "Add");
                Assert.IsTrue(nodeRef.TryGetPort("B", out portReader));
                Assert.NotNull(portReader);
                Assert.NotNull(portReader.GetNode());
                Assert.AreEqual(portReader.GetNode().GetName(), "Add");
                Assert.IsTrue(nodeRef.TryGetPort("Out", out portReader));
                Assert.NotNull(portReader);
                Assert.NotNull(portReader.GetNode());
                Assert.AreEqual(portReader.GetNode().GetName(), "Add");
            }


            [Test]
            public void CanDeserializeConnections()
            {
                GraphHandler graphHandler = new GraphHandler();
                GraphDelta graphDelta = graphHandler.graphDelta;
                using (INodeWriter foo = graphDelta.AddNode("Foo"))
                using (INodeWriter bar = graphDelta.AddNode("Bar"))
                {
                    Assert.IsTrue(foo.TryAddPort("A", true, true, out IPortWriter _));
                    Assert.IsTrue(foo.TryAddPort("B", true, true, out IPortWriter _));
                    Assert.IsTrue(foo.TryAddPort("Out", false, true, out IPortWriter output));
                    Assert.IsTrue(bar.TryAddPort("A", true, true, out IPortWriter input));
                    Assert.IsNotNull(output);
                    Assert.IsNotNull(input);
                    Assert.IsTrue(output.TryAddConnection(input));
                }
                var thruEdge = (graphDelta.m_data.Search("Foo.Out._Output") as Element<List<Element>>).data[0];
                var normSearch = graphDelta.m_data.Search("Bar.A");
                Assert.NotNull(thruEdge);
                Assert.NotNull(normSearch);
                Assert.AreEqual(thruEdge, normSearch);


                var roundTrip = graphHandler.ToSerializedFormat();
                var deserializedHandler = new GraphHandler(roundTrip).graphDelta;

                // rerun tests on the serialized version
                try
                {
                    thruEdge = (deserializedHandler.m_data.Search("Foo.Out._Output") as Element<List<Element>>).data[0];
                    normSearch = deserializedHandler.m_data.Search("Bar.A");
                }
                catch(System.Exception)
                {
                    Assert.Fail("Connected element could not be found.");
                }
                Assert.NotNull(thruEdge);
                Assert.NotNull(normSearch);
                Assert.AreEqual(thruEdge, normSearch);
            }
        }
    }

}
