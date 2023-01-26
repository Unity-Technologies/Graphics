using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using UnityObject = UnityEngine.Object;

namespace UnityEngine.Rendering.HighDefinition.Tests
{
    class CopyToTests
    {
        class DummyMonoBehaviour : MonoBehaviour { }

        GameObject m_Root;
        List<UnityObject> m_ToClean;

        [SetUp]
        public void SetUp()
        {
            m_Root = new GameObject("TEST");
            m_Root.SetActive(false);
            m_ToClean = new List<UnityObject> { m_Root };
        }

        [TearDown]
        public void TearDown()
        {
            if (m_ToClean != null)
            {
                foreach (var obj in m_ToClean)
                    CoreUtils.Destroy(obj);
                m_ToClean = null;
            }
        }

        class TestTypeCase : TestCaseData
        {
            public TestTypeCase(Type type) : base(type)
                => SetName($"{type.Name}.CopyTo");
        }

        //Resources for test
        static TestCaseData[] s_TypeDatas =
        {
            // test that this automatic test works as expected
            new TestTypeCase(typeof(VisibilitySelfTest)),
            new TestTypeCase(typeof(FilterSelfTest)),
            new TestTypeCase(typeof(TypeSelfTest)),

            // test object we want to test the CopyTo
            new TestTypeCase(typeof(ScalableSettingValue<int>)),
            new TestTypeCase(typeof(InfluenceVolume)),
            new TestTypeCase(typeof(HDAdditionalCameraData)),
            new TestTypeCase(typeof(HDAdditionalLightData)),
        };

        //This test will compute the type given a combination of LightType and HDAdditionalLightdata.
        //It will set the two types on a Light and HDAdditionalLightData components before attemting to compute the type with the two public API accessors.
        [Test, TestCaseSource(nameof(s_TypeDatas))]
        public void TestType(Type type)
        {
            MethodInfo copyToMethodInfo = type.GetMethod("CopyTo", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (copyToMethodInfo == null)
                Assert.Fail($"{type.Name} don't have CopyTo method.");

            object originalObject = GenerateObject(type);
            object modifiedObject = GenerateObjectWithMutation(type);

            copyToMethodInfo.Invoke(originalObject, new[] { modifiedObject });

            AssertAllFieldMatches(originalObject, modifiedObject);
        }

        #region Creation and Mutation
        object GenerateObject(Type type)
        {
            if (typeof(Component).IsAssignableFrom(type))
            {
                if (type == typeof(Transform))
                {
                    //cannot create Transform
                    GameObject go = new GameObject("DummyForTransform");
                    go.SetActive(false);
                    m_ToClean.Add(go);
                    return go.transform;
                }
                else if (type.GetCustomAttributes(typeof(DisallowMultipleComponent), inherit: true).Length > 0
                         || type == typeof(Camera)
                         || type == typeof(Light))
                {
                    //cannot add multiple instance
                    GameObject go = new GameObject("DummyForUnique");
                    go.SetActive(false);
                    m_ToClean.Add(go);
                    return go.AddComponent(type);
                }
                else if (type == typeof(Component)
                         || type == typeof(Behaviour)
                         || type == typeof(MonoBehaviour))
                    //fallback on DummyMonoBehaviour for case that cannot be created directly
                    type = typeof(DummyMonoBehaviour);
                return m_Root.AddComponent(type);
            }
            else
                return Activator.CreateInstance(type);  //currently only default constructor are handled
        }

        object GenerateObjectWithMutation(Type type)
        {
            object createdObject = GenerateObject(type);

            type = createdObject.GetType();
            foreach (FieldInfo fieldInfo in GetAllField(type))
                ChangeValue(createdObject, fieldInfo);

            return createdObject;
        }

        IEnumerable<FieldInfo> GetAllField(Type type)
            => type
            .GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Where(fieldInfo =>
            {
                // remove const and readonly
                if (fieldInfo.IsLiteral)
                    return false;

                // remove events, delegate, Action, Func (all functors)
                if (typeof(Delegate).IsAssignableFrom(fieldInfo.FieldType))
                    return false;

                // remove explicitely excluded field with attribute [CopyFilterAttribute(CopyFilterAttribute.Filter.Exclude)]
                foreach (Attribute attribute in fieldInfo.GetCustomAttributes(typeof(CopyFilterAttribute)))
                    if ((attribute as CopyFilterAttribute).filter == CopyFilterAttribute.Filter.Exclude)
                        return false;

                return true;
            });

        void ChangeValue(object instance, FieldInfo fieldInfo)
        {
            object initialValue = fieldInfo.GetValue(instance);

            if (fieldInfo.FieldType == typeof(string))
                // string: construct and replace by new string by ammending something
                fieldInfo.SetValue(instance, String.IsNullOrEmpty(initialValue as string) ? "str" : ((string)initialValue) + "_1");
            else if (fieldInfo.FieldType == typeof(Texture)
                     || fieldInfo.FieldType == typeof(Texture2D))
            {
                // Texture or Texture2D: change refered object. Need special constructor for those
                Texture generated = new Texture2D(1, 1);
                m_ToClean.Add(generated);
                fieldInfo.SetValue(instance, generated);
            }
            else if (typeof(Texture).IsAssignableFrom(fieldInfo.FieldType))
                Assert.Fail($"Unsuported Type {fieldInfo.FieldType}. Add it in CopyToTests.ChangeValue");
            else if (fieldInfo.FieldType.IsArray)
                // Array: create and use array +1 in size and default values
                fieldInfo.SetValue(instance, Activator.CreateInstance(fieldInfo.FieldType, new object[] { ((instance as Array)?.Length ?? 0) + 1 }));
            else if (fieldInfo.FieldType.IsClass)
                // MonoBehaviour or System.Object: change refered object
                fieldInfo.SetValue(instance, GenerateObject(fieldInfo.FieldType));
            else if (fieldInfo.FieldType == typeof(bool))
                // boolean
                fieldInfo.SetValue(instance, !(bool)initialValue);
            else if (fieldInfo.FieldType == typeof(char)
                     || fieldInfo.FieldType == typeof(byte)
                     || fieldInfo.FieldType == typeof(uint)
                     || fieldInfo.FieldType == typeof(ulong))
                // unsigned numerals
                fieldInfo.SetValue(instance, Convert.ChangeType(((ulong)Convert.ChangeType(initialValue, typeof(ulong)) > 0) ? 0 : 1, fieldInfo.FieldType));
            else if (fieldInfo.FieldType == typeof(int)
                     || fieldInfo.FieldType == typeof(long))
                // signed numerals
                fieldInfo.SetValue(instance, Convert.ChangeType(((long)Convert.ChangeType(initialValue, typeof(long)) > 0) ? 0 : 1, fieldInfo.FieldType));
            else if (fieldInfo.FieldType.IsEnum)
            {
                // enum
                Type underlyingType = Enum.GetUnderlyingType(fieldInfo.FieldType);
                long castedInitValue = (long)Convert.ChangeType(Convert.ChangeType(initialValue, underlyingType), typeof(long));
                object changedValue = Convert.ChangeType(castedInitValue > 0 ? 0 : 1, underlyingType);
                fieldInfo.SetValue(instance, changedValue);
            }
            else if (fieldInfo.FieldType == typeof(float)
                     || fieldInfo.FieldType == typeof(double))
                // floating points
                fieldInfo.SetValue(instance, Convert.ChangeType(((double)Convert.ChangeType(initialValue, typeof(double)) > .5f) ? 0f : 1f, fieldInfo.FieldType));
            else if (fieldInfo.FieldType.IsValueType)
            {
                // struct: recurse on inner field
                object initialStruct = fieldInfo.GetValue(instance);
                foreach (FieldInfo fi in GetAllField(fieldInfo.FieldType))
                    ChangeValue(initialStruct, fi);
                fieldInfo.SetValue(instance, initialStruct);
            }
            else
                Assert.Fail($"Unsuported Type {fieldInfo.FieldType}. Add it in CopyToTests.ChangeValue");

            if (fieldInfo.FieldType.IsArray)
            {
                IList arrayA = initialValue as IList;
                IList arrayB = fieldInfo.GetValue(instance) as IList;

                if (arrayA == null ^ arrayB == null)
                    return;

                if (arrayA != null) // case where both non null
                {
                    if (arrayA.Count == arrayB.Count)
                    {
                        for (int i = 0; i < arrayA.Count; ++i)
                            if (!arrayA[i].Equals(arrayB[i]))
                                return;

                        Assert.Fail($"Value of {fieldInfo.Name} cannot have been modified.");
                    }
                }
                else //both are null
                    Assert.Fail($"Value of {fieldInfo.Name} cannot have been modified.");
            }
            else
                Assert.AreNotEqual(initialValue, fieldInfo.GetValue(instance), $"Value of {fieldInfo.Name} cannot have been modified.");
        }

        void AssertAllFieldMatches(object a, object b)
        {
            Assert.AreEqual(a.GetType(), b.GetType(), "Type mismatch");

            foreach (FieldInfo fieldInfo in GetAllField(a.GetType()))
            {
                if (fieldInfo.GetCustomAttribute(typeof(ObsoleteAttribute), false) != null)
                {
                    // This field is marked as obsolete. Don't check it.
                    continue;
                }
                AssertOneFieldMatch(a, b, fieldInfo);
            }
        }

        void AssertOneFieldMatch(object a, object b, FieldInfo fieldInfo)
        {
            bool CheckContentCopy = fieldInfo
                .GetCustomAttributes(typeof(CopyFilterAttribute))
                .Any(attribute => (attribute as CopyFilterAttribute).filter == CopyFilterAttribute.Filter.CheckContent);

            if (CheckContentCopy)
            {
                if (fieldInfo.FieldType.IsArray) //always check array as if they are in CheckContent
                {
                    IList arrayA = fieldInfo.GetValue(a) as IList;
                    IList arrayB = fieldInfo.GetValue(b) as IList;

                    if (arrayA == null ^ arrayB == null)
                        Assert.Fail($"Only one of the array is null in {fieldInfo.Name}.");

                    if (arrayA != null) // case where both non null
                    {
                        Assert.AreEqual(arrayA.Count, arrayB.Count, $"Arrays in {fieldInfo.Name} have different size.");

                        for (int i = 0; i < arrayA.Count; ++i)
                            Assert.AreEqual(arrayA[i], arrayB[i], $"Value of {fieldInfo.Name}[{i}] is different.");
                    }
                }
                else
                    AssertAllFieldMatches(fieldInfo.GetValue(a), fieldInfo.GetValue(b));
            }
            else
                Assert.AreEqual(fieldInfo.GetValue(a), fieldInfo.GetValue(b), $"Value of {fieldInfo.Name} is different.");
        }

        #endregion

        #region Self Testing
        class TypeSelfTest
        {
            enum CustomEnum { _ }

            struct InnerInnerStruct
            {
                int m_Int;
                double m_Double;
            }

            struct InnerStruct
            {
                int m_Int;
                double m_Double;
                InnerInnerStruct m_InnerInnerStruct;
            }

            struct Struct
            {
                int m_Int;
                double m_Double;
                InnerStruct m_InnerStruct;
            }

            class Class
            {
                int m_Int;
                double m_Double;

                public void CopyTo(Class other)
                {
                    other.m_Int = m_Int;
                    other.m_Double = m_Double;
                }
            }

            class CustomMonoBehaviour : MonoBehaviour
            {
                int m_Int;
                double m_Double;
            }

            string m_String;
            Texture m_Texture;
            Texture2D m_Texture2D;
            Component m_Component; //interesting as we cannot instanciate it directly
            Transform m_Transform; //interesting as we only can have one per GameObject
            BoxCollider m_Collider; //other Component
            Behaviour m_Behaviour; //interesting as we cannot instanciate it directly
            MonoBehaviour m_MonoBehaviour; //interesting as we cannot instanciate it directly
            CustomMonoBehaviour m_CustomMonoBehaviour;
            Class m_ClassRef;
            [ValueCopy]
            Class m_Class = new Class();
            bool m_Bool;
            int m_Int;
            uint m_UInt;
            byte m_Byte;
            long m_Long;
            ulong m_ULong;
            char m_Char;
            CustomEnum m_CustomEnum;
            float m_Float;
            double m_Double;
            Struct m_Struct;

            int[] m_IntRef = new int[0];
            [ValueCopy]
            int[] m_Ints = new int[0];
            [ValueCopy]
            Struct[] m_Structs = new Struct[0];
            [ValueCopy]
            Texture[] m_Textures = new Texture[0];
            [ValueCopy]
            Component[] m_Components = new Component[0];
            [ValueCopy]
            Transform[] m_Transforms = new Transform[0];
            [ValueCopy]
            CustomMonoBehaviour[] m_CustomMonoBehaviours = new CustomMonoBehaviour[0];
            [ValueCopy]
            Class[] m_Classes = new Class[0];

            void CopyTo(TypeSelfTest other)
            {
                other.m_String = m_String;
                other.m_Texture = m_Texture;
                other.m_Texture2D = m_Texture2D;
                other.m_Component = m_Component;
                other.m_Transform = m_Transform;
                other.m_Collider = m_Collider;
                other.m_Behaviour = m_Behaviour;
                other.m_MonoBehaviour = m_MonoBehaviour;
                other.m_CustomMonoBehaviour = m_CustomMonoBehaviour;
                other.m_ClassRef = m_ClassRef;
                other.m_Class = new Class();
                m_Class.CopyTo(other.m_Class);
                other.m_Bool = m_Bool;
                other.m_Int = m_Int;
                other.m_UInt = m_UInt;
                other.m_Byte = m_Byte;
                other.m_Long = m_Long;
                other.m_ULong = m_ULong;
                other.m_Char = m_Char;
                other.m_CustomEnum = m_CustomEnum;
                other.m_Float = m_Float;
                other.m_Double = m_Double;
                other.m_Struct = m_Struct;
                other.m_IntRef = m_IntRef;
                other.m_Ints = new int[m_Ints.Length];
                m_Ints.CopyTo(other.m_Ints, 0);
                other.m_Structs = new Struct[m_Structs.Length];
                m_Structs.CopyTo(other.m_Structs, 0);
                other.m_Textures = new Texture[m_Textures.Length];
                m_Textures.CopyTo(other.m_Textures, 0);
                other.m_Components = new Component[m_Components.Length];
                m_Components.CopyTo(other.m_Components, 0);
                other.m_Transforms = new Transform[m_Transforms.Length];
                m_Transforms.CopyTo(other.m_Transforms, 0);
                other.m_CustomMonoBehaviours = new CustomMonoBehaviour[m_CustomMonoBehaviours.Length];
                m_CustomMonoBehaviours.CopyTo(other.m_CustomMonoBehaviours, 0);
                other.m_Classes = new Class[m_Classes.Length];
                m_Classes.CopyTo(other.m_Classes, 0);
            }
        }

        class FilterSelfTest
        {
            int m_FieldIncluded;

#pragma warning disable 0067 //never used
            [ExcludeCopy]
            int m_FieldExcluded;
#pragma warning restore 0067

            int m_PropertyIncluded { get; set; }

#pragma warning disable 0067 //never used
            [field: ExcludeCopy]
            int m_PropertyExcluded { get; set; }

            event Action m_Event1;
            event Func<int> m_Event2;

            delegate void MyDelegate(int i, string s);
            event MyDelegate m_Event3;

            Action m_Action;
            Func<int> m_Func;
            MyDelegate m_Delegate;


            [ExcludeCopy]
            int m_CustomBackingField;
#pragma warning restore 0067

            int m_PropertyWithoutBackingField
            {
                get => m_CustomBackingField;
                set => m_CustomBackingField = value;
            }

            void CopyTo(FilterSelfTest other)
            {
                other.m_FieldIncluded = m_FieldIncluded;
                other.m_PropertyIncluded = m_PropertyIncluded;
            }
        }

        class VisibilitySelfBaseTest
        {
            private int m_BasePrivate;
            protected int m_BaseProtected;
            internal int m_BaseInternal;
            public int m_BasePublic;
        }

        class VisibilitySelfTest : VisibilitySelfBaseTest
        {
            private int m_ChildPrivate;
            protected int m_ChildProtected;
            internal int m_ChildInternal;
            public int m_ChildPublic;

            void CopyTo(VisibilitySelfTest other)
            {
                other.m_BaseProtected = m_BaseProtected;
                other.m_BaseInternal = m_BaseInternal;
                other.m_BasePublic = m_BasePublic;

                other.m_ChildPrivate = m_ChildPrivate;
                other.m_ChildProtected = m_ChildProtected;
                other.m_ChildInternal = m_ChildInternal;
                other.m_ChildPublic = m_ChildPublic;
            }
        }
        #endregion
    }
}
