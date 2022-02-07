using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace UnityEditor.ContextLayeredDataStorage
{

        [TestFixture]
        class ElementIDTests
        {
            [Test]
            public void EmptyID()
            {
            ElementID test = new ElementID("");
                Assert.AreEqual(test.FullPath, "");
                Assert.AreEqual(test.LocalPath, "");
            }

            [Test]
            [TestCase("foo")]
            [TestCase("$.^")]
            public void SimpleID(string path)
            {
            ElementID test = new ElementID(path);
                Assert.AreEqual(test.FullPath, path);
                Assert.AreEqual(test.LocalPath, path);
            }

            [Test]
            [TestCase(new string[] { }, "", "")]
            [TestCase(new string[] {""}, "", "")]
            [TestCase(new string[] {"foo"}, "foo", "foo")]
            [TestCase(new string[] {"foo", "bar"}, "foo.bar", "bar")]
            [TestCase(new string[] {"foo", "bar", "baz"}, "foo.bar.baz", "baz")]
            public void SimpleIDMulti(string[] path, string expectedFullPath, string expectedLocalPath)
            {
            ElementID test = new ElementID(path);
                Assert.AreEqual(test.FullPath, expectedFullPath);
                Assert.AreEqual(test.LocalPath, expectedLocalPath);
            }
        }


    public class TestStorage : ContextLayeredDataStorage
    {
        protected override void AddDefaultLayers()
        {
            m_layerList.AddLayer(0, "TestRoot", true);
        }

        [TestFixture]
        class CLDSInternalTests
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
                var elem = (store.Search("a") as ElementReader).Element;
                Assert.NotNull(elem);
                Assert.AreEqual(elem.ID.FullPath, "a");
                Assert.True(elem.Children.Count > 0);
                int data;
                elem = (store.Search("a.b") as ElementReader).Element;
                Assert.NotNull(elem);
                Assert.True(elem.TryGetData(out data));
                Assert.AreEqual(data, 13);
                Assert.AreEqual(elem.id.LocalPath, "b");
                Assert.True(elem.Children.Count > 0);
                float data2;
                elem = (store.Search("a.b.c.d") as ElementReader).Element;
                Assert.NotNull(elem);
                Assert.True(elem.TryGetData(out data2));
                Assert.AreEqual(data2, 35.4f);
                Assert.AreEqual(elem.ID.LocalPath, "d");
                Assert.AreEqual(elem.ID.FullPath, "a.b.c.d");
                Assert.False(elem.Children.Count > 0);
            }

            [Test]
            public void CanAddRemoveGetDataFromStoreContextual()
            {
                TestStorage store = new TestStorage();
                store.AddData("a");
                store.AddData("a.b");
                store.AddData("a.b.c.d", 18);
                var elem = (store.Search("a") as ElementReader).Element;
                Assert.NotNull(elem);
                Assert.AreEqual(elem.ID.LocalPath, "a");
                Assert.True(elem.Children.Count > 0);
                elem = (store.Search("a.b") as ElementReader).Element;
                Assert.NotNull(elem);
                Assert.AreEqual(elem.ID.LocalPath, "b");
                Assert.True(elem.Children.Count > 0);

                int data;

                elem = (store.Search("a.b.c.d") as ElementReader).Element;
                Assert.NotNull(elem);
                Assert.True(elem.TryGetData(out data));
                Assert.AreEqual(data, 18);
                Assert.AreEqual(elem.ID.LocalPath, "d");
                Assert.False(elem.Children.Count > 0);
                store.RemoveData("a.b");
                Assert.IsNull(store.Search("a.b"));
                elem = (store.Search("a.b.c.d") as ElementReader).Element;
                Assert.NotNull(elem);
                Assert.True(elem.TryGetData(out data));
                Assert.AreEqual(data, 18);
                Assert.AreEqual(elem.ID.FullPath, "a.b.c.d");
                Assert.False(elem.Children.Count > 0);
            }



            [Test]
            public void CanAddAndGetDataFromStoreLayered()
            {
                TestStorage sidtore = new TestStorage();
                store.AddData("foo", 10);
                int data;
                var elemA = store.Search("foo");
                Assert.NotNull(elemA);
                data = elemA.GetData<int>();
                Assert.AreEqual(data, 10);
                store.AddNewTopLayer("a");
                store.AddData("foo", 15);
                var elemB = store.Search("foo");
                Assert.NotNull(elemB);
                data = elemB.GetData<int>();
                Assert.AreEqual(data, 15);
                store.AddData("TestRoot", "foo.bar", 12);
                var search = store.Search("foo.bar");
                Assert.NotNull(search);
                data = search.GetData<int>();
                Assert.AreEqual(data, 12);
                store.AddData("foo.bar", 20);
                search = store.Search("foo.bar");
                Assert.NotNull(search);
                data = search.GetData<int>();
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
                var elem = (store.Search("a") as ElementReader).Element;
                Assert.IsNotNull(elem);
                Assert.IsFalse(elem.Children.Count > 0);
                store.Rebalance();
                elem = (store.Search("a") as ElementReader).Element;
                Assert.IsNotNull(elem);
                Assert.IsTrue(elem.Children.Count > 0);
                elem = (store.Search("a.b.c") as ElementReader).Element;
                Assert.IsNotNull(elem);
                Assert.IsNotNull(elem.Parent);
                Assert.AreEqual(elem.Parent.id.LocalPath, "b");
                elem = (store.Search("a.b.c.d.foo.bar.baz") as ElementReader).Element;
                Assert.IsNotNull(elem);
                Assert.AreEqual(elem.ID.LocalPath, "baz");

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
                int data = elem.GetData<int>();
                Assert.AreEqual(data, 13);
                elem = storeDeserialized.Search("a.b.c.f"); 
                Assert.IsNotNull(elem);
                Vector3Int data2 = elem.GetData<Vector3Int>();
                Assert.AreEqual(data2, new Vector3Int(1,2,3));
            }
        }
    }

    [TestFixture]
    public class CLDSExternalTests
    {
        [Test]
        public void CanConstruct()
        {
            ContextLayeredDataStorage clds = new ContextLayeredDataStorage();
        }

        [Test]
        public void CanAddData()
        {
            ContextLayeredDataStorage clds = new ContextLayeredDataStorage();
            clds.AddData("foo").AddChild("bar", 39);
        }

        [Test]
        public void CanAddAndReadData()
        {
            ContextLayeredDataStorage clds = new ContextLayeredDataStorage();
            clds.AddData("foo.bar", 30);
            var data = clds.Search("foo.bar");
            Assert.IsNotNull(data);
            Assert.AreEqual(data.GetData<int>(), 30);
            clds.AddData("foo", 12f);
            data = clds.Search("foo");
            Assert.IsNotNull(data);
            Assert.AreEqual(data.GetData<float>(), 12f);
            data = clds.Search("foo.bar");
            Assert.IsNotNull(data);
            Assert.AreEqual(data.GetData<int>(), 30);
        }

        [Test]
        public void CanAddAndReadDataContextual()
        {
            ContextLayeredDataStorage clds = new ContextLayeredDataStorage();
            var node = clds.AddData("node");
            var port = node.AddChild("port");
            port.AddChild("value", new Vector2(10f, 20f));
            var reader = clds.Search("node.port");
            Assert.AreEqual(reader.GetChild("value").GetData<Vector2>(), new Vector2(10f, 20f));
        }

        [Test]
        public void CanAddReadRemoveDataContextual()
        {
            ContextLayeredDataStorage clds = new ContextLayeredDataStorage();
            var node = clds.AddData("node");
            node.AddChild("in1");
            node.AddChild("in2").AddChild("value", 3f);
            node.RemoveChild("in1");
            Assert.IsNull(clds.Search("node.in1"));
            bool failed = false;
            var reader = clds.Search("node");
            try { reader.GetChild("in1"); }
            catch { failed = true; }
            Assert.IsTrue(failed);
        }


    }
}
