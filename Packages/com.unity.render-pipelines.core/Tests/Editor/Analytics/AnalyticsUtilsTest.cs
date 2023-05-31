using NUnit.Framework;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Pool;

namespace UnityEditor.Rendering.Tests
{
    public class AnalyticsUtilsTests
    {
        private TestScriptableObject m_SO;
        private TestObject m_Current;

        [SetUp]
        public void SetUp()
        {
            m_Current = new TestObject();
            m_SO = ScriptableObject.CreateInstance(typeof(TestScriptableObject)) as TestScriptableObject;
        }

        [TearDown]
        public void TearDown()
        {
            ScriptableObject.DestroyImmediate(m_SO);
        }

        [Serializable]
        public class TestObject
        {
            public int integer;
            public float floatNumber;
            public double doubleNumber;
            public bool boolean;
            public string test;
            public int[] array;

            public enum TestDiffEnum
            {
                a, b, c
            }

            public TestDiffEnum diffEnum;

            [Serializable]
            public struct InnerTestStruct
            {
                public int myValue;
            }

            public InnerTestStruct innerStruct;
        }

        public class TestScriptableObject : ScriptableObject
        {
            public TestObject myData;

            public TestScriptableObject()
            {
                myData = new TestObject();
            }
        }

        static TestCaseData[] s_TestCaseDatas =
        {
            new TestCaseData("integer", 1)
                .Returns(new string[] { "{\"myData.integer\":\"1\"}" })
                .SetName("Given an int field, the nested column is generated correctly"),
            new TestCaseData("floatNumber", 1.5f)
                .Returns(new string[] { "{\"myData.floatNumber\":\"1.5\"}" })
                .SetName("Given a float field, the nested column is generated correctly"),
            new TestCaseData("doubleNumber", 1.2)
                .Returns(new string[] { "{\"myData.doubleNumber\":\"1.2\"}" })
                .SetName("Given a double field, the nested column is generated correctly"),
            new TestCaseData("boolean", true)
                .Returns(new string[] { "{\"myData.boolean\":\"True\"}" })
                .SetName("Given a bool field, the nested column is generated correctly"),
            new TestCaseData("test", "Hello World")
                .Returns(new string[] { "{\"myData.test\":\"Hello World\"}" })
                .SetName("Given a string field, the nested column is generated correctly"),
            new TestCaseData("array", new int[] { 1,2,3} )
                .Returns(new string[] { "{\"myData.array\":\"[1,2,3]\"}" })
                .SetName("Given an array of ints field, the nested column is generated correctly"),
            new TestCaseData("diffEnum", TestObject.TestDiffEnum.b)
                .Returns(new string[] { "{\"myData.diffEnum\":\"b\"}" })
                .SetName("Given an enum field, the nested column is generated correctly"),
             new TestCaseData("innerStruct", new TestObject.InnerTestStruct{ myValue = 5 } )
                .Returns(new string[] { "{\"myData.innerStruct.myValue\":\"5\"}" })
                .SetName("Given an inner struct field, the nested column is generated correctly"),
        };

        private void SetValue(TestObject current, string fieldName, object value)
        {
            current.GetType().GetField(fieldName).SetValue(current, value);
        }

        [Test, TestCaseSource(nameof(s_TestCaseDatas))]
        public string[] ToNestedColumnUnityObject(string fieldName, object value)
        {
            SetValue(m_Current, fieldName, value);
            m_SO.myData = m_Current;
            return m_SO.ToNestedColumn(true);
        }

        static TestCaseData[] s_TestCaseDatasWithDefault =
        {
            new TestCaseData("integer", 1)
                .Returns(new string[] { "{\"integer\":\"1\"}" })
                .SetName("Given an int field, the nested column is generated correctly"),
            new TestCaseData("floatNumber", 1.5f)
                .Returns(new string[] { "{\"floatNumber\":\"1.5\"}" })
                .SetName("Given a float field, the nested column is generated correctly"),
            new TestCaseData("doubleNumber", 1.2)
                .Returns(new string[] { "{\"doubleNumber\":\"1.2\"}" })
                .SetName("Given a double field, the nested column is generated correctly"),
            new TestCaseData("boolean", true)
                .Returns(new string[] { "{\"boolean\":\"True\"}" })
                .SetName("Given a bool field, the nested column is generated correctly"),
            new TestCaseData("test", "Hello World")
                .Returns(new string[] { "{\"test\":\"Hello World\"}" })
                .SetName("Given a string field, the nested column is generated correctly"),
            new TestCaseData("array", new int[] { 1,2,3} )
                .Returns(new string[] { "{\"array\":\"[1,2,3]\"}" })
                .SetName("Given an array of ints field, the nested column is generated correctly"),
            new TestCaseData("diffEnum", TestObject.TestDiffEnum.b)
                .Returns(new string[] { "{\"diffEnum\":\"b\"}" })
                .SetName("Given an enum field, the nested column is generated correctly"),
             new TestCaseData("innerStruct", new TestObject.InnerTestStruct{ myValue = 5 } )
                .Returns(new string[] { "{\"innerStruct.myValue\":\"5\"}" })
                .SetName("Given an inner struct field, the nested column is generated correctly"),
        };

        [Test, TestCaseSource(nameof(s_TestCaseDatasWithDefault))]
        public string[] ToNestedColumnWithDefault(string fieldName, object value)
        {
            SetValue(m_Current, fieldName, value);
            m_SO.myData = m_Current;
            return m_Current.ToNestedColumnWithDefault<TestObject>(new TestObject(), true);
        }

        [Test]
        public void GivenAnObjectWithSerializableFields_WhenAllFieldsAreRequested_AllOfThemAreReturnedCorrectly()
        {
            using (ListPool<string>.Get(out var tmp))
            {
                foreach (var testCase in s_TestCaseDatas)
                {
                    SetValue(m_Current, (string)testCase.Arguments[0], testCase.Arguments[1]);
                    tmp.Add(((string[])testCase.ExpectedResult)[0]);
                }

                m_SO.myData = m_Current;
                Assert.AreEqual(m_SO.ToNestedColumn(false), tmp.ToArray());
            }
        }

        [Test]
        public void CheckEditorAnalyticsAreDisabledOnCI()
        {
            var ci = Environment.GetEnvironmentVariable("CI");
            if (!string.IsNullOrWhiteSpace(ci) && bool.TryParse(ci, out var result) && result)
            {
                Assert.IsFalse(EditorAnalytics.enabled, "Analytics must be only enabled for user projects not on CI");
                Debug.Log("Assert for CheckEditorAnalyticsAreDisabledOnCI was executed.");
            }
        }


        [Serializable]
        class ClassWithPublicInt
        {
            public int myPublicInt;
        }

        [Serializable]
        class ClassWithPrivateBool
        {
            private bool myPrivateBoolNotSerializable;
        }

        [Serializable]
        class ClassWithPrivateIntWithSerializableFieldAttribute
        {
            [SerializeField]
            private int myPrivateIntSerializable;
        }

        [Serializable]
        class ClassWithPublicIntWithNonSerializableFieldAttribute
        {
            [NonSerialized]
            public int myPublicNonSerializedInt;
        }

        [Serializable]
        class ClassWithPublicPropertytWithSerializableFieldAttributeForTheBackingField
        {
            [field: SerializeField]
            public int myProperty { get; private set; }
        }

        [Serializable]
        class ClassWithObsoletePublicField
        {
            [Obsolete("this is obsolete")]
            public int myPublicInt;
        }


        [Serializable]
        class BaseClassWithField
        {
            public int myPublicInt;
        }

        [Serializable]
        class ChildClass : BaseClassWithField
        {
            public bool myPublicBool;
        }

        static TestCaseData[] s_ListTestsCaseDatasSerializableFields =
        {
            new TestCaseData(typeof(ClassWithPublicInt), false)
                .SetName("Given a class with a public field, the field is returned")
                .Returns(new string[] { "myPublicInt" }),
            new TestCaseData(typeof(ClassWithPrivateBool), false)
                .SetName("Given a class with a private field, nothing is returned")
                .Returns(new string[] { }),
            new TestCaseData(typeof(ClassWithPrivateIntWithSerializableFieldAttribute), false)
                .SetName("Given a class with a private field with [SerializeField] attribute, the field is returned")
                .Returns(new string[] {"myPrivateIntSerializable" }),
            new TestCaseData(typeof(ClassWithPublicIntWithNonSerializableFieldAttribute), false)
                .SetName("Given a class with public field with [NonSerialized] attribute, nothing is returned")
                .Returns(new string[] { }),
            new TestCaseData(typeof(ClassWithPublicPropertytWithSerializableFieldAttributeForTheBackingField), false)
                .SetName("Given a class with public property with [field: SerializeField] attribute, the backing field is returned")
                .Returns(new string[] {"<myProperty>k__BackingField" }),
            new TestCaseData(typeof(ClassWithObsoletePublicField), false)
                .SetName("Given a class with public field with [Obsolete] attribute and not removing obsolete fields, the obsolete is returned")
                .Returns(new string[] {"myPublicInt" }),
             new TestCaseData(typeof(ClassWithObsoletePublicField), true)
                .SetName("Given a class with public field with [Obsolete] attribute, nothing is returned when obsolete fields must be removed")
                .Returns(new string[] { }),
             new TestCaseData(typeof(ChildClass), false)
                .SetName("Given a class with inheriting from another with a public field, the parent field is returned")
                .Returns(new string[] { "myPublicInt", "myPublicBool" }),
        };


        [Test, TestCaseSource(nameof(s_ListTestsCaseDatasSerializableFields))]
        public string[] GetSerializableFields(Type type, bool removeObsolete)
        {
            return type
                .GetSerializableFields(removeObsolete)
                .Select(f => f.Name)
                .ToArray();
        }
    }
}
