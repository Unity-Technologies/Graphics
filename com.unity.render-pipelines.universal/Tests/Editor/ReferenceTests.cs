using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;


namespace UnityEngine.Rendering.Universal.Tests
{
    public class ReferenceTests : MonoBehaviour
    {
        class TestClass
        {
            public int i;
            public TestClass next;
        }

        struct TestStruct_Data
        {
            public int i;
            public TestStruct next;
        }

        struct TestStruct
        {
            Reference<TestStruct_Data> m_Data;

            public void Initialize()
            {
                TestStruct_Data initialValue = new TestStruct_Data();
                Reference<TestStruct_Data>.Create(initialValue, out m_Data);
            }

            public ref int i { get { return ref m_Data.DeRef().i; } }
            public ref TestStruct next { get { return ref m_Data.DeRef().next; } }


            public bool IsCreated { get { return m_Data.IsCreated; } }
            public bool IsNull { get { return m_Data.IsNull; } }
            public bool NotNull { get { return !m_Data.IsNull; } }
            public void SetNull() { m_Data.SetNull(); }
            public bool IsEqual(TestStruct node) { return m_Data.IsEqual(node.m_Data); }
            public static bool operator ==(TestStruct a, TestStruct b) { return a.IsEqual(b); }
            public static bool operator !=(TestStruct a, TestStruct b) { return !a.IsEqual(b); }
            public override int GetHashCode() { return m_Data.GetHashCode(); }
            public override bool Equals(object obj) { return GetHashCode() == obj.GetHashCode(); }
        }

        [Test]
        public void Reference_Ptr()
        {
            TestStruct testStruct = new TestStruct();
            Assert.IsTrue(testStruct.IsNull);
            Assert.IsTrue(!testStruct.IsCreated);

            testStruct.Initialize();
            Assert.IsTrue(!testStruct.IsNull);
            Assert.IsTrue(testStruct.IsCreated);
        }

        [Test]
        public void Reference_Equality()  // Equality should work like class
        {
            // Class comparison
            TestClass testClassA = new TestClass();
            TestClass testClassB = new TestClass();
            TestClass testClassC = new TestClass();
            testClassA.i = 10;
            testClassB.i = 10;
            testClassC.i = 5;

            bool resultClassAA = testClassA == testClassA;
            bool resultClassAB = testClassA == testClassB;
            bool resultClassAC = testClassA == testClassC;

            // Reference comparison
            TestStruct testStructA = new TestStruct();
            TestStruct testStructB = new TestStruct();
            TestStruct testStructC = new TestStruct();
            testStructA.Initialize();
            testStructB.Initialize();
            testStructC.Initialize();
            testStructA.i = 10;
            testStructB.i = 10;
            testStructC.i = 5;

            bool resultStructAA = testStructA == testStructA;
            bool resultStructAB = testStructA == testStructB;
            bool resultStructAC = testStructA == testStructC;

            bool resultFinal = resultClassAA == resultStructAA &&
                               resultClassAB == resultStructAB &&
                               resultClassAC == resultStructAC;

            Assert.IsTrue(resultFinal);
        }

        void CreateList(TestStruct test, int length)
        {
            for (int i = 0; i < length; i++)
            {
                test.next = new TestStruct();
                test.next.Initialize();
                test.i = i;
                test = test.next;
            }
        }

        void CreateList(TestClass test, int length)
        {
            for (int i = 0; i < length; i++)
            {
                test.next = new TestClass();
                test.i = i;
                test = test.next;
            }
        }

        [Test]
        public void Reference_Function()  // Equality should work like class
        {
            TestStruct testStruct = new TestStruct();
            testStruct.Initialize();
            CreateList(testStruct, 5);

            TestClass testClass = new TestClass();
            CreateList(testClass, 5);

            while(testClass != null)
            {
                Assert.True(testClass.i == testStruct.i);
                testStruct = testStruct.next;
                testClass = testClass.next;
            }
        }
    }
}

