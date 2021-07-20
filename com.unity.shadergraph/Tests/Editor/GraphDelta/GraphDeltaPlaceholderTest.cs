using NUnit.Framework;

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

        [Test]
        public void CanAddEmptyNode()
        {
            GraphDelta graphHandler = GraphUtil.CreateGraph() as GraphDelta;
            using (NodeRef node = graphHandler.AddNode("foo"))
            {
                Assert.NotNull(node);
            }
        }

        [Test]
        public void CanAddAndGetNode()
        {
            GraphDelta graphHandler = GraphUtil.CreateGraph() as GraphDelta;
            graphHandler.AddNode("foo");
            Assert.NotNull(graphHandler.GetNodes());
        }
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
            Assert.AreEqual(elem.id, "a");
            Assert.IsNotEmpty(elem.children);
            elem = store.Search("a.b");
            Assert.NotNull(elem);
            Assert.AreEqual(elem.GetData<int>(), 13);
            Assert.AreEqual(elem.id, "b");
            Assert.IsNotEmpty(elem.children);
            elem = store.Search("a.b.c.d");
            Assert.NotNull(elem);
            Assert.AreEqual(elem.GetData<float>(), 35.4f);
            Assert.AreEqual(elem.id, "c.d");
            Assert.IsEmpty(elem.children);
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
            Assert.AreEqual(elem.id, "a");
            Assert.IsNotEmpty(elem.children);
            elem = store.Search("a.b");
            Assert.NotNull(elem);
            Assert.AreEqual(elem.id, "b");
            Assert.IsNotEmpty(elem.children);
            elem = store.Search("a.b.c.d");
            Assert.NotNull(elem);
            Assert.AreEqual(elem.GetData<int>(), 18);
            Assert.AreEqual(elem.id, "c.d");
            Assert.IsEmpty(elem.children);
            store.RemoveData("a.b");
            elem = store.Search("a.b");
            Assert.Null(elem);
            elem = store.Search("a.b.c.d");
            Assert.NotNull(elem);
            Assert.AreEqual(elem.GetData<int>(), 18);
            Assert.AreEqual(elem.id, "b.c.d");
            Assert.IsEmpty(elem.children);
        }



        [Test]
        public void CanAddAndGetDataFromStoreLayered()
        {
            ContextLayeredDataStorage store = new ContextLayeredDataStorage();
            store.AddData("foo", 10);
            var elemA = store.Search("foo");
            Assert.NotNull(elemA);
            Assert.AreEqual(elemA.GetData<int>(), 10);
            store.AddNewTopLayer("a");
            store.AddData("foo", 15);
            var elemB = store.Search("foo");
            Assert.NotNull(elemB);
            Assert.AreEqual(elemB.GetData<int>(), 15);
            store.AddData("Root","foo.bar", 12);
            var search = store.Search("foo.bar");
            Assert.NotNull(search);
            Assert.AreEqual(search.GetData<int>(), 12);
            store.AddData("foo.bar", 20);
            search = store.Search("foo.bar");
            Assert.NotNull(search);
            Assert.AreEqual(search.GetData<int>(), 20);
        }

    }
}
