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

        private static (string typeName, string arraySpec) SplitTypeName(string typeName)
        {
            int i = typeName.IndexOf('[');
            return i == -1 ? (typeName, "") : (typeName.Substring(0, i - 1), typeName.Substring(i));
        }

        private static string DeclareField(IShaderField a)
        {
            var t = SplitTypeName(a.ShaderType.Name);
            return $"{t.typeName} {a.Name}{t.arraySpec}";
        }

        private static string GenerateHints(string closure, IReadOnlyDictionary<string, string> hints, string name = null)
        {
            ShaderStringBuilder sb = new();

            if (name != null)
                sb.AppendLine($"/// <{closure} name = \"{name}\">");
            else sb.AppendLine($"/// <{closure}>");

            foreach(var hint in hints)
                sb.AppendLine($"///\t <{hint.Key}>{hint.Value}</{hint.Key}>");

            sb.AppendLine($"/// </{closure}>");
            return sb.ToString();
        }

        internal static string GenerateCode(IShaderFunction func)
        {
            ShaderStringBuilder sb = new();
            
            foreach(var name in func.Namespace)
                sb.Append($"namespace {name}{{");

            sb.AppendNewLine();
            sb.IncreaseIndent();

            sb.Append(GenerateHints("funchints", func.Hints));
            foreach (var param in func.Parameters)
                sb.Append(GenerateHints("paramhints", param.Hints, param.Name));

            sb.Append($"UNITY_EXPORT_REFLECTION {func.ReturnType.Name} {func.Name}(");
            bool first = true;
            foreach (var param in func.Parameters)
            {
                if (!first) sb.Append(", ");
                sb.Append(DeclareField(param));
            }
            sb.AppendLine(")");
            sb.AppendLine("{");
            sb.IncreaseIndent();
            sb.AppendLine(func.FunctionBody);
            sb.DecreaseIndent();
            sb.AppendLine("}");
            sb.DecreaseIndent();

            foreach (var name in func.Namespace)
                sb.Append($"}} /*{name}*/");

            return sb.ToString();
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
