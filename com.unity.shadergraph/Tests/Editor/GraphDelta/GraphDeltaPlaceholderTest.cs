using NUnit.Framework;
using System.Linq;

namespace UnityEditor.ShaderGraph.GraphDelta.UnitTests
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

        /*
        [Test]
        public void CanAddEmptyNode()
        {
            GraphDelta graphHandler = GraphUtil.CreateGraph() as GraphDelta;
            using (INodeRef node = graphHandler.AddNode("foo"))
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
            using(INodeRef node = graphHandler.AddNode("Add"))
            {
                node.AddInputPort("A");
                node.AddInputPort("B");
                node.AddOutputPort("Out");
            }

            var nodeRef = graphHandler.GetNode("Add");
            Assert.NotNull(nodeRef);
            var portRef = nodeRef.GetInputPort("A");
            Assert.NotNull(portRef);
            portRef = nodeRef.GetInputPort("B");
            Assert.NotNull(portRef);
            portRef = nodeRef.GetOutputPort("Out");
            Assert.NotNull(portRef);
        }

        [Test]
        public void CanAddTwoNodesAndConnect()
        {
            GraphDelta graphHandler = GraphUtil.CreateGraph() as GraphDelta;
            using(INodeRef foo = graphHandler.AddNode("Foo"))
            using(INodeRef bar = graphHandler.AddNode("Bar"))
            {
                foo.AddInputPort("A");
                foo.AddInputPort("B");
                var output = foo.AddOutputPort("Out");
                var input = bar.AddInputPort("A");
                Assert.True(graphHandler.TryMakeConnection(output, input));
            }
            var thruEdge = graphHandler.m_data.Search("Foo.Out.A");
            var normSearch = graphHandler.m_data.Search("Bar.A");
            Assert.NotNull(thruEdge);
            Assert.NotNull(normSearch);
            Assert.AreEqual(thruEdge, normSearch);
        }
        */
    }

    [TestFixture]
    class ContextLayeredDataStorageFixture
    {

        [Test]
        public void CanCreateDataStore()
        {
            ContextLayeredDataStorage store = new ContextLayeredDataStorage();
            Assert.NotNull(store);
        }

        [Test]
        public void CanAddDataToStore()
        {
            ContextLayeredDataStorage store = new ContextLayeredDataStorage();
            store.AddData("foo", 10);
        }

        [Test]
        public void CanAddAndGetDataFromStoreSimple()
        {
            ContextLayeredDataStorage store = new ContextLayeredDataStorage();
            store.AddData("foo");
            var elemA = store.Search("foo");
            Assert.NotNull(elemA);
            store.AddData("bar");
            var elemB = store.Search("bar");
            Assert.NotNull(elemB);
        }

        [Test]
        public void CanAddLayerToStoreSimple()
        {
            ContextLayeredDataStorage store = new ContextLayeredDataStorage();
            store.AddNewTopLayer("foo");
            store.AddData("bar");
            var elemB = store.Search("bar");
            Assert.NotNull(elemB);
        }

        [Test]
        public void CannotAddNullLayerToStore()
        {
            ContextLayeredDataStorage store = new ContextLayeredDataStorage();
            Assert.Throws(typeof(System.ArgumentException), () => store.AddNewTopLayer(""));
            Assert.Throws(typeof(System.ArgumentException), () => store.AddNewTopLayer(null));
        }


        [Test]
        public void CanAddAndGetDataFromStoreContextual()
        {
            ContextLayeredDataStorage store = new ContextLayeredDataStorage();
            store.AddData("a");
            store.AddData("a.b", 13);
            store.AddData("a.b.c.d", 35.4f);
            var elem = store.Search("a");
            Assert.NotNull(elem);
            Assert.AreEqual(elem.ID, "a");
            Assert.True(elem.Children.MoveNext());
            int data;
            elem = store.Search("a.b");
            Assert.NotNull(elem);
            Assert.True(elem.TryGetData(out data));
            Assert.AreEqual(data, 13);
            Assert.AreEqual(elem.ID, "b");
            Assert.True(elem.Children.MoveNext());
            float data2;
            elem = store.Search("a.b.c.d");
            Assert.NotNull(elem);
            Assert.True(elem.TryGetData(out data2));
            Assert.AreEqual(data2, 35.4f);
            Assert.AreEqual(elem.ID, "c.d");
            Assert.False(elem.Children.MoveNext());
        }

        [Test]
        public void CanAddRemoveGetDataFromStoreContextual()
        {
            ContextLayeredDataStorage store = new ContextLayeredDataStorage();
            store.AddData("a");
            store.AddData("a.b");
            store.AddData("a.b.c.d", 18);
            var elem = store.Search("a");
            Assert.NotNull(elem);
            Assert.AreEqual(elem.ID, "a");
            Assert.True(elem.Children.MoveNext());
            elem = store.Search("a.b");
            Assert.NotNull(elem);
            Assert.AreEqual(elem.ID, "b");
            Assert.True(elem.Children.MoveNext());

            int data;

            elem = store.Search("a.b.c.d");
            Assert.NotNull(elem);
            Assert.True(elem.TryGetData(out data));
            Assert.AreEqual(data, 18);
            Assert.AreEqual(elem.ID, "c.d");
            Assert.False(elem.Children.MoveNext());
            store.RemoveData("a.b");
            elem = store.Search("a.b");
            Assert.Null(elem);
            elem = store.Search("a.b.c.d");
            Assert.NotNull(elem);
            Assert.True(elem.TryGetData(out data));
            Assert.AreEqual(data, 18);
            Assert.AreEqual(elem.ID, "b.c.d");
            Assert.False(elem.Children.MoveNext());
        }



        [Test]
        public void CanAddAndGetDataFromStoreLayered()
        {
            ContextLayeredDataStorage store = new ContextLayeredDataStorage();
            store.AddData("foo", 10);
            int data;
            var elemA = store.Search("foo");
            Assert.NotNull(elemA);
            Assert.True(elemA.TryGetData(out data));
            Assert.AreEqual(data, 10);
            store.AddNewTopLayer("a");
            store.AddData("foo", 15);
            var elemB = store.Search("foo");
            Assert.NotNull(elemB);
            Assert.True(elemB.TryGetData(out data));
            Assert.AreEqual(data, 15);
            store.AddData("Root","foo.bar", 12);
            var search = store.Search("foo.bar");
            Assert.NotNull(search);
            Assert.True(search.TryGetData(out data));
            Assert.AreEqual(data, 12);
            store.AddData("foo.bar", 20);
            search = store.Search("foo.bar");
            Assert.NotNull(search);
            Assert.True(search.TryGetData(out data));
            Assert.AreEqual(data, 20);
        }

    }
}
