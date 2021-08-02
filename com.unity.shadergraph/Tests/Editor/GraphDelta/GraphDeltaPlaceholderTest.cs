using NUnit.Framework;
using System.Linq;
using UnityEditor.ContextLayeredDataStorage;

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
                IGraphHandler graphHandler = GraphUtil.CreateGraph();
                Assert.NotNull(graphHandler);
            }

            [Test]
            public void CanAddEmptyNode()
            {
                GraphDelta graphHandler = GraphUtil.CreateGraph() as GraphDelta;
                using (INodeWriter node = graphHandler.AddNode("foo"))
                {
                    Assert.NotNull(node);
                }
            }

            [Test]
            public void CanAddAndGetNode()
            {
                GraphDelta graphHandler = GraphUtil.CreateGraph() as GraphDelta;
                graphHandler.AddNode("foo");
                Assert.NotNull(graphHandler.GetNode("foo"));
            }

            [Test]
            public void CanAddAndRemoveNode()
            {
                GraphDelta graphHandler = GraphUtil.CreateGraph() as GraphDelta;
                graphHandler.AddNode("foo");
                Assert.NotNull(graphHandler.GetNode("foo"));
                graphHandler.RemoveNode("foo");
            }

            [Test]
            public void CanAddNodeAndPorts()
            {
                GraphDelta graphHandler = GraphUtil.CreateGraph() as GraphDelta;
                using (INodeWriter node = graphHandler.AddNode("Add"))
                {
                    node.TryAddPort("A", true, true, out IPortWriter _);
                    node.TryAddPort("B", true, true, out IPortWriter _);
                    node.TryAddPort("Out", false, true, out IPortWriter _);
                }

                var nodeRef = graphHandler.GetNode("Add");
                Assert.NotNull(nodeRef);
                Assert.IsTrue(nodeRef.TryGetPort("A", out IPortReader portReader));
                Assert.NotNull(portReader);
                Assert.IsTrue(nodeRef.TryGetPort("B", out portReader));
                Assert.NotNull(portReader);
                Assert.IsTrue(nodeRef.TryGetPort("Out", out portReader));
                Assert.NotNull(portReader);
            }

            [Test]
            public void CanAddTwoNodesAndConnect()
            {
                GraphDelta graphHandler = GraphUtil.CreateGraph() as GraphDelta;
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
                var thruEdge = graphHandler.m_data.Search("Foo.Out.A");
                var normSearch = graphHandler.m_data.Search("Bar.A");
                Assert.NotNull(thruEdge);
                Assert.NotNull(normSearch);
                Assert.AreEqual(thruEdge, normSearch);
            }
        }
    }

}
