using System;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.Models
{
    public class DummyTestType {}

    class StencilTests
    {
        [Test]
        public void TestCanLoadAllTypesFromAssemblies()
        {
            // If this test fails, failing assemblies must be added to the Stencil.BlackListedAssemblies
            Assert.DoesNotThrow(() =>
            {
                var types = AssemblyCache.CachedAssemblies
                    .SelectMany(AssemblyExtensions.GetTypesSafe, (_, assemblyType) => assemblyType)
                    .Where(t => !t.IsAbstract && !t.IsInterface);
                Assert.IsNotNull(types);
            });

            LogAssert.NoUnexpectedReceived();
        }

        [TestCase(typeof(string), ExpectedResult = true, TestName = "TestValidType")]
        [TestCase(typeof(IDisposable), ExpectedResult = false, TestName = "TestInterface")]
        [TestCase(typeof(Stencil), ExpectedResult = false, TestName = "TestAbstractType")]
        [TestCase(typeof(Transform), ExpectedResult = true, TestName = "TestUnityEngineComponent")]
        [TestCase(typeof(StencilTests), ExpectedResult = false, TestName = "TestPrivateType")]
        [TestCase(typeof(DummyTestType), ExpectedResult = true, TestName = "TestPublicTypeWithNoNamespace")]
        [TestCase(typeof(PublicAPIAttribute), ExpectedResult = false, TestName = "TestTypeWithBlackListedNamespace")]
        public bool TestIsValidType(Type type)
        {
            var methodInfo = typeof(ClassStencil).GetMethod("IsValidType", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(methodInfo);

            return Convert.ToBoolean(methodInfo.Invoke(null, new object[] { type }));
        }
    }
}
