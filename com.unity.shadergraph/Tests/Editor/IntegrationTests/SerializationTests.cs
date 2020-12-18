using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEditor.ShaderGraph;
using UnityEditor.ShaderGraph.Serialization;

namespace UnityEditor.Graphing.IntegrationTests
{
    [TestFixture]
    public class SerializationTests
    {
        private class DummyJsonHolder : JsonObject
        {
            public List<JsonData<MaterialSlot>> testSlots = new List<JsonData<MaterialSlot>>();

            public DummyJsonHolder() : base()
            {
            }

            public DummyJsonHolder(List<MaterialSlot> materialSlots)
            {
                foreach (var slot in materialSlots)
                {
                    testSlots.Add(slot);
                }
            }
        }
        interface ITestInterface
        {}

        [Serializable]
        class SimpleSerializeClass : ITestInterface
        {
            [SerializeField]
            public string stringValue;

            [SerializeField]
            public int intValue;

            [SerializeField]
            public float floatValue;

            [SerializeField]
            public int[] arrayValue;

            public static SimpleSerializeClass instance
            {
                get
                {
                    return new SimpleSerializeClass
                    {
                        stringValue = "ABCD",
                        intValue = 5,
                        floatValue = 7.7f,
                        arrayValue = new[] {1, 2, 3, 4}
                    };
                }
            }

            public virtual void AssertAsReference()
            {
                var reference = instance;
                Assert.AreEqual(reference.stringValue, stringValue);
                Assert.AreEqual(reference.intValue, intValue);
                Assert.AreEqual(reference.floatValue, floatValue);
                Assert.AreEqual(reference.arrayValue.Length, arrayValue.Length);
                Assert.AreEqual(reference.arrayValue, arrayValue);
            }
        }

        [Serializable]
        class ChildClassA : SimpleSerializeClass
        {
            [SerializeField]
            public string childString;

            public new static ChildClassA instance
            {
                get
                {
                    return new ChildClassA
                    {
                        stringValue = "qwee",
                        intValue = 5,
                        floatValue = 6f,
                        arrayValue = new[] {5, 6, 7, 8},
                        childString = "CHILD"
                    };
                }
            }

            public override void AssertAsReference()
            {
                var reference = instance;
                Assert.AreEqual(reference.stringValue, stringValue);
                Assert.AreEqual(reference.intValue, intValue);
                Assert.AreEqual(reference.floatValue, floatValue);
                Assert.AreEqual(reference.arrayValue.Length, arrayValue.Length);
                Assert.AreEqual(reference.arrayValue, arrayValue);
                Assert.AreEqual(reference.childString, childString);
            }
        }

        [Serializable]
        class ChildClassB : SimpleSerializeClass
        {
            [SerializeField]
            public int childInt;

            public new static ChildClassB instance
            {
                get
                {
                    return new ChildClassB
                    {
                        stringValue = "qwee",
                        intValue = 5,
                        floatValue = 6f,
                        arrayValue = new[] {5, 6, 7, 8},
                        childInt = 666
                    };
                }
            }

            public override void AssertAsReference()
            {
                var reference = instance;
                Assert.AreEqual(reference.stringValue, stringValue);
                Assert.AreEqual(reference.intValue, intValue);
                Assert.AreEqual(reference.floatValue, floatValue);
                Assert.AreEqual(reference.arrayValue.Length, arrayValue.Length);
                Assert.AreEqual(reference.arrayValue, arrayValue);
                Assert.AreEqual(reference.childInt, childInt);
            }
        }

        [Serializable]
        class SerializationContainer
        {
            public List<SerializationHelper.JSONSerializedElement> serializedElements;
        }

        [Test]
        public void TestSerializationHelperCanSerializeThenDeserialize()
        {
            var toSerialize = new List<SimpleSerializeClass>()
            {
                SimpleSerializeClass.instance
            };

            var serialized = SerializationHelper.Serialize<SimpleSerializeClass>(toSerialize);
            Assert.AreEqual(1, serialized.Count);

            var loaded = SerializationHelper.Deserialize<SimpleSerializeClass>(serialized, GraphUtil.GetLegacyTypeRemapping());
            Assert.AreEqual(1, loaded.Count);
            Assert.IsInstanceOf<SimpleSerializeClass>(loaded[0]);
            loaded[0].AssertAsReference();
        }

        [Test]
        public void TestPolymorphicSerializationPreservesTypesViaBaseClass()
        {
            var toSerialize = new List<SimpleSerializeClass>()
            {
                SimpleSerializeClass.instance,
                ChildClassA.instance,
                ChildClassB.instance
            };

            var serialized = SerializationHelper.Serialize<SimpleSerializeClass>(toSerialize);
            Assert.AreEqual(3, serialized.Count);

            var loaded = SerializationHelper.Deserialize<SimpleSerializeClass>(serialized, GraphUtil.GetLegacyTypeRemapping());
            Assert.AreEqual(3, loaded.Count);
            Assert.IsInstanceOf<SimpleSerializeClass>(loaded[0]);
            Assert.IsInstanceOf<ChildClassA>(loaded[1]);
            Assert.IsInstanceOf<ChildClassB>(loaded[2]);
            loaded[0].AssertAsReference();
            loaded[1].AssertAsReference();
            loaded[2].AssertAsReference();
        }

        [Test]
        public void TestPolymorphicSerializationPreservesTypesViaInterface()
        {
            var toSerialize = new List<ITestInterface>()
            {
                SimpleSerializeClass.instance,
                ChildClassA.instance,
                ChildClassB.instance
            };

            var serialized = SerializationHelper.Serialize<ITestInterface>(toSerialize);
            Assert.AreEqual(3, serialized.Count);

            var loaded = SerializationHelper.Deserialize<SimpleSerializeClass>(serialized, GraphUtil.GetLegacyTypeRemapping());
            Assert.AreEqual(3, loaded.Count);
            Assert.IsInstanceOf<SimpleSerializeClass>(loaded[0]);
            Assert.IsInstanceOf<ChildClassA>(loaded[1]);
            Assert.IsInstanceOf<ChildClassB>(loaded[2]);
            loaded[0].AssertAsReference();
            loaded[1].AssertAsReference();
            loaded[2].AssertAsReference();
        }

        [Test]
        public void TestSerializationHelperElementCanSerialize()
        {
            var toSerialize = new List<SimpleSerializeClass>()
            {
                SimpleSerializeClass.instance
            };

            var serialized = SerializationHelper.Serialize<SimpleSerializeClass>(toSerialize);
            Assert.AreEqual(1, serialized.Count);

            var container = new SerializationContainer
            {
                serializedElements = serialized
            };

            var serializedContainer = JsonUtility.ToJson(container, true);

            var deserializedContainer = JsonUtility.FromJson<SerializationContainer>(serializedContainer);
            var loaded = SerializationHelper.Deserialize<SimpleSerializeClass>(deserializedContainer.serializedElements, GraphUtil.GetLegacyTypeRemapping());
            Assert.AreEqual(1, loaded.Count);
            Assert.IsInstanceOf<SimpleSerializeClass>(loaded[0]);
            loaded[0].AssertAsReference();
        }

        [Test]
        public void TestSerializableSlotCanSerialize()
        {
            var toSerialize = new List<MaterialSlot>()
            {
                new TestSlot(0, "InSlot", SlotType.Input),
                new TestSlot(1, "OutSlot", SlotType.Output),
            };

            DummyJsonHolder dummyJsonHolder = new DummyJsonHolder(toSerialize);

            var serialized = MultiJson.Serialize(dummyJsonHolder);
            DummyJsonHolder dummyJsonHolder1 = new DummyJsonHolder();
            MultiJson.Deserialize(dummyJsonHolder1, serialized);
            Assert.AreEqual(2, dummyJsonHolder1.testSlots.Count);
            var loaded = new List<MaterialSlot>(dummyJsonHolder1.testSlots.SelectValue());

            Assert.IsInstanceOf<MaterialSlot>(loaded[0]);
            Assert.IsInstanceOf<MaterialSlot>(loaded[1]);

            Assert.AreEqual(0, loaded[0].id);
            Assert.AreEqual("InSlot(4)", loaded[0].displayName);
            Assert.IsTrue(loaded[0].isInputSlot);

            Assert.AreEqual(1, loaded[1].id);
            Assert.AreEqual("OutSlot(4)", loaded[1].displayName);
            Assert.IsTrue(loaded[1].isOutputSlot);
        }
    }
}
