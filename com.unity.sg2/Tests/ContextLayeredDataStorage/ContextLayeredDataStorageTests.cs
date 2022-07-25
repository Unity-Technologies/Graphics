using System;
using System.Collections.Generic;
using System.Linq;
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

        [Test]
        [TestCase("", "")]
        [TestCase("Foo", "")]
        [TestCase("Foo.Bar", "Foo")]
        [TestCase("Foo.Bar.Baz", "Foo.Bar")]
        public void ParentID(string fullPath, string expectedParentPath)
        {
            ElementID test = fullPath;
            Assert.AreEqual(expectedParentPath, test.ParentPath);
        }

        [Test]
        [TestCase("", new string[] { }, "Foo", "Foo")]
        [TestCase("", new string[] { "Bar" }, "Bar", "Bar_1")]
        [TestCase("", new string[] { "Bar", "Bar_1" }, "Bar", "Bar_2")]
        [TestCase("Foo", new string[] { }, "Bar", "Foo.Bar")]
        [TestCase("Foo", new string[] { "Bar" }, "Bar", "Foo.Bar_1")]
        [TestCase("Foo", new string[] { "Bar", "Bar_1" }, "Bar", "Foo.Bar_2")]
        public void CreateUniqueID(string parentID, IEnumerable<string> existingLocalIDs, string desiredLocalID, string expectedID)
        {
            var test = ElementID.CreateUniqueLocalID(parentID, existingLocalIDs, desiredLocalID);
            Assert.AreEqual(expectedID, test.FullPath);
        }

        [Serializable]
        public class TestClass
        {
            public ElementID id;
        }

        [Test]
        [TestCase("")]
        [TestCase("Foo")]
        [TestCase("Foo.Bar")]
        public void TestSerialization(string testCase)
        {
            ElementID test = testCase;
            var testClass = new TestClass();
            testClass.id = test;
            string serializationString = EditorJsonUtility.ToJson(testClass);
            var roundTrip = new TestClass();
            EditorJsonUtility.FromJsonOverwrite(serializationString, roundTrip);
            Assert.AreEqual(testCase, roundTrip.id.FullPath);
        }
    }

    public class TestReader : DataReader
    {
        public TestReader(Element element) : base(element) { }

        public override IEnumerable<DataReader> GetChildren()
        {
            foreach(var (key, value) in Owner.FlatStructureLookup)
            {
                if(Element.ID.IsSubpathOf(key))
                {
                    yield return new DataReader(value);
                }
            }
        }
    }

    public class TestHeader : DataHeader
    {
        public override DataReader GetReader(Element element)
        {
            return new TestReader(element);
        }
    }

    public class TestStorage : ContextLayeredDataStorage
    {
        protected override void AddDefaultLayers()
        {
            AddLayer(0, "TestRoot", true);
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
                var elem = store.Search("a").Element;
                Assert.NotNull(elem);
                Assert.AreEqual(elem.ID.FullPath, "a");
                Assert.True(elem.Children.Count > 0);
                int data;
                elem = store.Search("a.b").Element;
                Assert.NotNull(elem);
                data = elem.GetData<int>();
                Assert.AreEqual(data, 13);
                Assert.AreEqual(elem.ID.LocalPath, "b");
                Assert.True(elem.Children.Count > 0);
                float data2;
                elem = store.Search("a.b.c.d").Element;
                Assert.NotNull(elem);
                data2 = elem.GetData<float>();
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
                var elem = store.Search("a").Element;
                Assert.NotNull(elem);
                Assert.AreEqual(elem.ID.LocalPath, "a");
                Assert.True(elem.Children.Count > 0);
                elem = store.Search("a.b").Element;
                Assert.NotNull(elem);
                Assert.AreEqual(elem.ID.LocalPath, "b");
                Assert.True(elem.Children.Count > 0);

                int data;

                elem = store.Search("a.b.c.d").Element;
                Assert.NotNull(elem);
                data = elem.GetData<int>();
                Assert.AreEqual(data, 18);
                Assert.AreEqual(elem.ID.LocalPath, "d");
                Assert.False(elem.Children.Count > 0);
                store.RemoveData("a.b");
                Assert.IsNull(store.Search("a.b"));
                elem = store.Search("a.b.c.d").Element;
                Assert.NotNull(elem);
                data = elem.GetData<int>();
                Assert.AreEqual(data, 18);
                Assert.AreEqual(elem.ID.FullPath, "a.b.c.d");
                Assert.False(elem.Children.Count > 0);
            }



            [Test]
            public void CanAddAndGetDataFromStoreLayered()
            {
                TestStorage store = new TestStorage();
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
                var elem = store.Search("a").Element;
                Assert.IsNotNull(elem);
                Assert.IsFalse(elem.Children.Count > 0);
                store.Rebalance();
                elem = store.Search("a").Element;
                Assert.IsNotNull(elem);
                Assert.IsTrue(elem.Children.Count > 0);
                elem = store.Search("a.b.c").Element;
                Assert.IsNotNull(elem);
                Assert.IsNotNull(elem.Parent);
                Assert.AreEqual(elem.Parent.ID.LocalPath, "b");
                elem = store.Search("a.b.c.d.foo.bar.baz").Element;
                Assert.IsNotNull(elem);
                Assert.AreEqual(elem.ID.LocalPath, "baz");

            }

            [Test]
            public void CanOverrideHeaderReader()
            {
                TestStorage store = new TestStorage();
                store.AddData("a");
                store.AddData("a.b");
                store.AddData("a.b.c");
                store.AddData("a.b.c.d");
                store.AddData("a.b.c.d.e");
                store.AddData("a.b.c.d.e.f");
                store.AddData("a.b.c.d.foo");
                store.AddData("a.b.c.d.bar");

                store.SetHeader("a.b", new TestHeader());
                var reader = store.Search("a.b");
                var children = reader.GetChildren().ToList();
                Assert.IsTrue(children.Count == 6);
                Assert.IsTrue(children.Any(dr => dr.Element.ID.Equals(ElementID.FromString("a.b.c.d.e.f"))));
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
            var reader = clds.Search("node");
        }

        [Test]
        public void TestCopyDataBranch()
        {
            ContextLayeredDataStorage clds = new ContextLayeredDataStorage();
            clds.AddData("a");
            clds.AddData("a.b", 13);
            clds.AddData("a.b.c", 35.4f);
            var copy = clds.AddData("a_copy");
            clds.CopyDataBranch(clds.Search("a"), copy);
            var child = clds.Search("a_copy");
            child = child.GetChild("b");
            Assert.IsNotNull(child);
            Assert.AreEqual(13, child.GetData<int>());
            child = child.GetChild("c");
            Assert.IsNotNull(child);
            Assert.AreEqual(35.4f, child.GetData<float>());

        }

        [Test]
        public void TestCopyPaste()
        {
            ContextLayeredDataStorage clds = new ContextLayeredDataStorage();
            clds.AddData("a");
            clds.AddData("a.b", 13);
            clds.AddData("a.b.c", 35.4f);
            clds.AddData("a.b.c.d.e", true);
            clds.SetMetadata("a.b", "foo", new Color(1, 0, 0));

            List<DataReader> elements = new List<DataReader>()
            {
                clds.Search("a"),
                clds.Search("a.b"),
                clds.Search("a.b.c"),
                clds.Search("a.b.c.d.e")
            };

            var ser = clds.CopyElementCollection(elements);

            //Paste into same CLDS
            clds.PasteElementCollection(ser.layer, ser.metadata, "Root", out _);
            var copied = clds.Search("a_1");
            Assert.NotNull(copied);
            Assert.IsTrue(copied.GetChildren().Count() == 1);
            copied = copied.GetChild("b");
            Assert.NotNull(copied);
            Assert.AreEqual(13, copied.GetData<int>());
            Assert.AreEqual(new Color(1, 0, 0), clds.GetMetadata<Color>(copied.Element.ID, "foo"));
            Assert.IsTrue(copied.GetChildren().Count() == 1);
            copied = copied.GetChild("c");
            Assert.NotNull(copied);
            Assert.AreEqual(35.4f, copied.GetData<float>());
            //Okay, so this is a weird one and I want to explain it, since either myself in 3 months or
            //whoever is looking at this code later might think this is a bug. Of course "c" has a child,
            //its "d.e"! But what needs to be remembered is we act on the flat structure, and "GetChildren"
            //will only return the _immediate_ children of a reader (at least in the base DataReader case).
            //That means, if "d" were contained in this structure, it could be returned. But since its not
            //here, and "d.e" wont be seen as an immediate child, its ignored. 
            Assert.AreEqual(1, copied.GetChildren().Count());
            //This part though seems to go against that; "c" has no children, why can you getchild on "d.e"
            //and get a correct value? Currently, getchild just appends the rest of the localID onto c's
            //full path ID, and then just searches the graph for that, which will search for "a.b.c.d.e"
            //and find the correct value. The name could potentially be changed, but thats really up to
            //how the reader is implemented.
            copied = copied.GetChild("d.e");
            Assert.NotNull(copied);
            Assert.AreEqual(true, copied.GetData<bool>());
            Assert.IsTrue(copied.GetChildren().Count() == 0);

            //Paste into new CLDS
            clds = new ContextLayeredDataStorage();
            clds.PasteElementCollection(ser.layer, ser.metadata, "Root", out _);
            copied = clds.Search("a");
            Assert.NotNull(copied);
            Assert.IsTrue(copied.GetChildren().Count() == 1);
            copied = copied.GetChild("b");
            Assert.NotNull(copied);
            Assert.AreEqual(13, copied.GetData<int>());
            Assert.AreEqual(new Color(1, 0, 0), clds.GetMetadata<Color>(copied.Element.ID, "foo"));
            Assert.IsTrue(copied.GetChildren().Count() == 1);
            copied = copied.GetChild("c");
            Assert.NotNull(copied);
            Assert.AreEqual(35.4f, copied.GetData<float>());
            Assert.AreEqual(1, copied.GetChildren().Count());
            copied = copied.GetChild("d.e");
            Assert.NotNull(copied);
            Assert.AreEqual(true, copied.GetData<bool>());
            Assert.IsTrue(copied.GetChildren().Count() == 0);
        }

    }
}
