using System;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using NUnit.Framework;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Tests.Types
{
    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    sealed class FakeBinaryOverload
    {
        public static FakeBinaryOverload operator+(FakeBinaryOverload a, FakeBinaryOverload b)
        {
            return a;
        }
    }

    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    sealed class FakeUnaryOverload
    {
        public static bool operator!(FakeUnaryOverload a)
        {
            return true;
        }
    }

    sealed class FakeNoOverload {}

    [UsedImplicitly(ImplicitUseTargetFlags.WithMembers)]
    static class FakeNoOverloadExtension
    {
        public static void Ext1(this FakeNoOverload o) {}
        public static void Ext2(this FakeNoOverload[] o) {}
    }

    sealed class AssemblyCacheTests
    {
        [Test]
        public void TestGetExtensionMethods()
        {
            Assembly assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a =>
                a.FullName.StartsWith("Unity.GraphTools.Foundation.Editor.Tests", StringComparison.Ordinal));
            var extensions = AssemblyCache.GetExtensionMethods<ExtensionAttribute>(new[] { assembly });

            Assert.IsTrue(extensions.TryGetValue(typeof(FakeNoOverload), out var methods));
            Assert.AreEqual(2, methods.Count);
            Assert.AreEqual("Ext1", methods[0].Name);
            Assert.AreEqual("Ext2", methods[1].Name);
        }

        [Test]
        public void TestGetExtensionMethods_NoResult()
        {
            Assembly assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a =>
                a.FullName.StartsWith("nunit", StringComparison.Ordinal));
            var extensions = AssemblyCache.GetExtensionMethods<ExtensionAttribute>(new[] { assembly });

            Assert.IsFalse(extensions.TryGetValue(typeof(FakeNoOverload), out _));
        }
    }
}
