using System;
using System.Collections.Generic;
using NUnit.Framework;

namespace UnityEditor.ShaderGraph.ProviderSystem.Tests
{
    internal static class TestUtils
    {
        internal static bool CompareSequence<T>(IEnumerable<T> a, IEnumerable<T> b, Func<T, T, bool> cmp)
        {
            if (a == null && b == null)
                return true;
            else if (a == null || b == null)
                return false;

            var ait = a.GetEnumerator();
            var bit = b.GetEnumerator();

            bool av;
            bool bv;

            do {
                av = ait.MoveNext();
                bv = bit.MoveNext();
                if (av && bv && !cmp(ait.Current, bit.Current))
                    return false;
            } while (av && bv);
            return av == bv;
        }

        internal static bool CompareBase(IShaderObject a, IShaderObject b)
        {
            return a.IsValid && b.IsValid && string.Equals(a.Name, b.Name)
                && CompareSequence(a.Namespace, b.Namespace, string.Equals)
                && CompareSequence(a.Hints, b.Hints, (e, f) => e.Key == f.Key && e.Value == f.Value);
        }

        internal static bool CompareType(IShaderType a, IShaderType b)
        {
            return CompareBase(a, b);
        }

        internal static bool CompareField(IShaderField a, IShaderField b)
        {
            return a.IsInput == b.IsInput && a.IsOutput == b.IsOutput && CompareType(a.ShaderType, b.ShaderType) && CompareBase(a, b);
        }

        internal static bool CompareFunction(IShaderFunction a, IShaderFunction b)
        {
            return CompareBase(a, b)
                && CompareType(a.ReturnType, b.ReturnType)
                && a.FunctionBody == b.FunctionBody
                && CompareSequence(a.Parameters, b.Parameters, CompareField);
        }

        internal static void AssertNodeSetup(IProvider<IShaderFunction> provider)
        {
            Assert.IsNotNull(provider);
            Assert.IsTrue(provider.IsValid);

            var node = new ProviderNode();
            node.InitializeFromProvider(provider);

            List<MaterialSlot> slots = new();
            node.GetSlots(slots);

            int count = 0;

            if (provider.Definition.ReturnType.Name != "void")
                count++;

            foreach (var param in node.Provider.Definition.Parameters)
                count += (param.IsInput ? 1 : 0) + (param.IsOutput ? 1 : 0);

            Assert.AreEqual(slots.Count, count, $"{provider.ProviderKey} has an unexpected number of slots.");
        }
    }
}
