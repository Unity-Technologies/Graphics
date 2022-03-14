using NUnit.Framework;
using UnityEngine;

namespace UnityEditor.ContextLayeredDataStorage
{
    public class TestStorage : ContextLayeredDataStorage
    {
        protected override void AddDefaultLayers()
        {
            m_layerList.AddLayer(0, "TestRoot", true);
        }


        [TestFixture]
        class ContextLayeredDataStorageFixture
        {

            [Test]
            public void CanCreateDataStore()
            {
                TestStorage store = new TestStorage();
                Assert.NotNull(store);
            }

            [Test]
            public void CanAddDataToStore()
            {
                TestStorage store = new TestStorage();
                store.AddData("foo", 10);
            }

            [Test]
            public void CanAddAndGetDataFromStoreSimple()
            {
                TestStorage store = new TestStorage();
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
                TestStorage store = new TestStorage();
                store.AddNewTopLayer("foo");
                store.AddData("bar");
                var elemB = store.Search("bar");
                Assert.NotNull(elemB);
            }

            [Test]
            public void CannotAddNullLayerToStore()
            {
                TestStorage store = new TestStorage();
                Assert.Throws(typeof(System.ArgumentException), () => store.AddNewTopLayer(""));
                Assert.Throws(typeof(System.ArgumentException), () => store.AddNewTopLayer(null));
            }


            [Test]
            public void CanAddAndGetDataFromStoreContextual()
            {
                TestStorage store = new TestStorage();
                store.AddData("a");
                store.AddData("a.b", 13);
                store.AddData("a.b.c.d", 35.4f);
                var elem = store.Search("a") as Element;
                Assert.NotNull(elem);
                Assert.AreEqual(elem.id, "a");
                Assert.True(elem.children.Count > 0);
                int data;
                elem = store.Search("a.b") as Element;
                Assert.NotNull(elem);
                Assert.True(elem.TryGetData(out data));
                Assert.AreEqual(data, 13);
                Assert.AreEqual(elem.id, "b");
                Assert.True(elem.children.Count > 0);
                float data2;
                elem = store.Search("a.b.c.d") as Element;
                Assert.NotNull(elem);
                Assert.True(elem.TryGetData(out data2));
                Assert.AreEqual(data2, 35.4f);
                Assert.AreEqual(elem.id, "c.d");
                Assert.False(elem.children.Count > 0);
            }

            [Test]
            public void CanAddRemoveGetDataFromStoreContextual()
            {
                TestStorage store = new TestStorage();
                store.AddData("a");
                store.AddData("a.b");
                store.AddData("a.b.c.d", 18);
                var elem = store.Search("a") as Element;
                Assert.NotNull(elem);
                Assert.AreEqual(elem.id, "a");
                Assert.True(elem.children.Count > 0);
                elem = store.Search("a.b") as Element;
                Assert.NotNull(elem);
                Assert.AreEqual(elem.id, "b");
                Assert.True(elem.children.Count > 0);

                int data;

                elem = store.Search("a.b.c.d") as Element;
                Assert.NotNull(elem);
                Assert.True(elem.TryGetData(out data));
                Assert.AreEqual(data, 18);
                Assert.AreEqual(elem.id, "c.d");
                Assert.False(elem.children.Count > 0);
                store.RemoveData("a.b");
                elem = store.Search("a.b") as Element;
                Assert.Null(elem);
                elem = store.Search("a.b.c.d") as Element;
                Assert.NotNull(elem);
                Assert.True(elem.TryGetData(out data));
                Assert.AreEqual(data, 18);
                Assert.AreEqual(elem.id, "b.c.d");
                Assert.False(elem.children.Count > 0);
            }



            [Test]
            public void CanAddAndGetDataFromStoreLayered()
            {
                TestStorage store = new TestStorage();
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
                store.AddData("TestRoot", "foo.bar", 12);
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

            [Test]
            public void CanRebalanceBadStructure()
            {
                TestStorage store = new TestStorage();
                store.AddData("a.b.c.d.foo.bar.baz");
                store.AddData("a.b.c.d.foo");
                store.AddData("a.b.c.d.e.f.g");
                store.AddData("a.b.c.d.e.f");
                store.AddData("a.b.c.d.e");
                store.AddData("a.b.c.d");
                store.AddData("a.b.c");
                store.AddData("a.b");
                store.AddData("a");
                var elem = store.Search("a") as Element;
                Assert.IsNotNull(elem);
                Assert.IsFalse(elem.children.Count > 0);
                store.Rebalance();
                elem = store.Search("a") as Element;
                Assert.IsNotNull(elem);
                Assert.IsTrue(elem.children.Count > 0);
                elem = store.Search("a.b.c") as Element;
                Assert.IsNotNull(elem);
                Assert.IsNotNull(elem.parent);
                Assert.AreEqual(elem.parent.id, "b");
                elem = store.Search("a.b.c.d.foo.bar.baz") as Element;
                Assert.IsNotNull(elem);
                Assert.AreEqual(elem.id, "bar.baz");

            }

            [Test]
            public void CanSerializeDeserializeComplex()
            {
                TestStorage store = new TestStorage();
                store.AddData("a");
                store.AddData("a.b", 13);
                store.AddData("a.b.c.d", 35.4f);
                store.AddData("a.b.c.f", new Vector3Int(1, 2, 3));
                string serialized = EditorJsonUtility.ToJson(store, true);
                Debug.Log(serialized);
                TestStorage storeDeserialized = new TestStorage();
                EditorJsonUtility.FromJsonOverwrite(serialized, storeDeserialized);
                var elem = storeDeserialized.Search("a.b"); 
                Assert.IsNotNull(elem);
                Assert.IsTrue(elem.TryGetData(out int data));
                Assert.AreEqual(data, 13);
                elem = storeDeserialized.Search("a.b.c.f"); 
                Assert.IsNotNull(elem);
                Assert.IsTrue(elem.TryGetData(out Vector3Int data2));
                Assert.AreEqual(data2, new Vector3Int(1,2,3));
            }
        }
    }
}
