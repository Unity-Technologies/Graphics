using NUnit.Framework;
using System.Collections.Generic;

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
                using INodeWriter node = graphHandler.AddNode("foo");
                Assert.NotNull(node);
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
                var thruEdgeName = (graphHandler.m_data.Search("Foo.Out._Output") as Element<List<string>>).data[0];
                var thruEdge = graphHandler.m_data.Search(thruEdgeName);
                var normSearch = graphHandler.m_data.Search("Bar.A");
                Assert.NotNull(thruEdge);
                Assert.NotNull(normSearch);
                Assert.AreEqual(thruEdge, normSearch);
            }

            public class TestDescriptor : IContextDescriptor
            {
                public IReadOnlyCollection<IContextDescriptor.ContextEntry> GetEntries()
                {
                    return new List<IContextDescriptor.ContextEntry>()
                    {
                        new IContextDescriptor.ContextEntry()
                        {
                            fieldName = "Foo",
                            primitive = GraphType.Primitive.Int,
                            height = GraphType.Height.One,
                            length = GraphType.Length.One,
                            precision = GraphType.Precision.Fixed,
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
                var registry = new Registry();
                registry.Register<TestDescriptor>();
                registry.Register<GraphType>();
                graphHandler.SetupContextNodes(new List<IContextDescriptor>() { new TestDescriptor() }, registry);
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
                var thruEdgeName = (graphDelta.m_data.Search("Foo.Out._Output") as Element<List<string>>).data[0];
                var thruEdge = graphDelta.m_data.Search(thruEdgeName);
                var normSearch = graphDelta.m_data.Search("Bar.A");
                Assert.NotNull(thruEdge);
                Assert.NotNull(normSearch);
                Assert.AreEqual(thruEdge, normSearch);


                var roundTrip = graphHandler.ToSerializedFormat();
                var deserializedHandler = new GraphHandler(roundTrip).graphDelta;

                // rerun tests on the serialized version
                try
                {
                    thruEdgeName = (deserializedHandler.m_data.Search("Foo.Out._Output") as Element<List<string>>).data[0];
                    thruEdge = deserializedHandler.m_data.Search(thruEdgeName);
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

            [Test]
            public void ReconcretizeGraph()
            {
                var graph = new GraphHandler();
                var registry = new Registry();
                registry.Register<GraphType>();
                registry.Register<Test.AddNode>();
                registry.Register<GraphTypeAssignment>();

                // should default concretize length to 4.
                graph.AddNode<Test.AddNode>("Add1", registry);
                var reader = graph.GetNodeReader("Add1");
                reader.GetField("In1.Length", out GraphType.Length len);
                Assert.AreEqual(4, (int)len);

                var roundTrip = graph.ToSerializedFormat();
                var deserializedHandler = new GraphHandler(roundTrip);
                deserializedHandler.ReconcretizeAll(registry);

                reader = deserializedHandler.GetNodeReader("Add1");
                reader.GetField("In2.Length", out len);

                Assert.AreEqual(4, (int)len);
            }

        }
    }

}
