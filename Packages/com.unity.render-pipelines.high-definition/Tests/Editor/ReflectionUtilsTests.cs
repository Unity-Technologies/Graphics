using NUnit.Framework;
using System;
using System.Reflection;

namespace UnityEngine.Rendering.HighDefinition.Tests
{
    class ReflectionUtilsTests
    {
        class TestIntegersAllPublic
        {
            public int A;
            public int B;
            public int C;
        }

        class TestIntegersSomePublic
        {
            public int A;
            private int B;
            public int C;
        }

        class TestIntegersStatic
        {
            public static int A;
            private static int B;
        }

        class TestObject
        {

        }
        class TestIntegersWithTestObject
        {
            public TestObject A = new();
            private TestObject B;
            public int C;
        }

        class TestEmpty
        {
        }

        private const BindingFlags k_InstanceNonPublic = BindingFlags.NonPublic | BindingFlags.Instance;
        private const BindingFlags k_InstancePublic = BindingFlags.Public | BindingFlags.Instance;

        private const BindingFlags k_StaticNonPublic = BindingFlags.NonPublic | BindingFlags.Static;
        private const BindingFlags k_StaticPublic = BindingFlags.Public | BindingFlags.Static;

        [Test]
        [TestCase(typeof(TestEmpty), ExpectedResult = 0)]
        [TestCase(typeof(TestIntegersAllPublic), ExpectedResult = 3)]
        [TestCase(typeof(TestIntegersSomePublic), ExpectedResult = 3)]
        [TestCase(typeof(TestIntegersStatic), ExpectedResult = 0)]
        [TestCase(typeof(TestIntegersWithTestObject), ExpectedResult = 1)]
        
        [TestCase(typeof(TestIntegersStatic), k_StaticNonPublic, ExpectedResult = 1)]
        [TestCase(typeof(TestIntegersWithTestObject), k_InstanceNonPublic, ExpectedResult = 0)]
        [TestCase(typeof(TestIntegersAllPublic), k_InstanceNonPublic, ExpectedResult = 0)]
        [TestCase(typeof(TestIntegersSomePublic), k_InstanceNonPublic, ExpectedResult = 1)]

        [TestCase(typeof(TestIntegersStatic), k_StaticPublic, ExpectedResult = 1)]
        [TestCase(typeof(TestIntegersAllPublic), k_InstancePublic, ExpectedResult = 3)]
        [TestCase(typeof(TestIntegersSomePublic), k_InstancePublic, ExpectedResult = 2)]
        [TestCase(typeof(TestIntegersWithTestObject), k_InstancePublic, ExpectedResult = 1)]

        [TestCase(typeof(TestIntegersAllPublic), k_StaticNonPublic, ExpectedResult = 0)]
        [TestCase(typeof(TestIntegersAllPublic), k_StaticPublic, ExpectedResult = 0)]

        public int ForEachFieldOfType(Type type, BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)
        {
            int count = 0;
            Activator.CreateInstance(type).ForEachFieldOfType<int>(value => count++, flags);
            return count;
        }
    }
}
