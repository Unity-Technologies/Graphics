
using System.Collections.Generic;
using System.Text;

namespace UnityEditor.ShaderGraph.ProviderSystem
{
    internal static class ShaderObjectUtils
    {
        internal static string QualifyName(IShaderObject obj, string separator = "::", bool includeName = true)
        {
            if (obj == null || !obj.IsValid)
                return null;

            StringBuilder builder = new();

            if (obj.Namespace != null)
                foreach (var name in obj.Namespace)
                    builder.Append($"{name}{separator}");

            builder.Append(obj.Name);
            return builder.ToString();
        }

        internal static string QualifySignature(IShaderFunction obj, bool includeNamespace = true, bool forDisplay = false)
        {
            if (obj == null || !obj.IsValid)
                return null;

            StringBuilder builder = new();
            builder.Append(includeNamespace ? QualifyName(obj) : obj.Name);
            builder.Append(forDisplay ? " (" : "(");

            bool first = true;
            if (obj.Parameters != null)
            {
                foreach (var param in obj.Parameters)
                {
                    if (!first) builder.Append(forDisplay ? ", " : ",");
                    first = false;
                    builder.Append(QualifyName(param.ShaderType));
                }
            }
            builder.Append(")");

            return builder.ToString();
        }

        internal static string EvaluateProviderKey(IShaderFunction obj, string fromHint = Hints.Func.kProviderKey)
        {
            string result = null;
            if (obj?.IsValid ?? false)
            {
                if (obj.Hints == null || !obj.Hints.TryGetValue(fromHint, out result))
                    result = QualifySignature(obj);
            }
            return result;
        }

        private static (string typeName, string arraySpec) SplitTypeName(string typeName)
        {
            int i = typeName.IndexOf('[');
            return i == -1 ? (typeName, "") : (typeName.Substring(0, i - 1), typeName.Substring(i));
        }

        private static string DeclareField(IShaderField a)
        {
            (string typeName, string arraySpec) SplitTypeName(string typeName)
            {
                int i = typeName.IndexOf('[');
                return i == -1 ? (typeName, "") : (typeName.Substring(0, i - 1), typeName.Substring(i));
            }

            var t = SplitTypeName(a.ShaderType.Name);
            return $"{t.typeName} {a.Name}{t.arraySpec}";
        }

        private static string GenerateHints(string closure, IReadOnlyDictionary<string, string> hints, string name = null)
        {
            ShaderStringBuilder sb = new();

            if (name != null)
                sb.AppendLine($"/// <{closure} name = \"{name}\">");
            else sb.AppendLine($"/// <{closure}>");

            foreach (var hint in hints)
                sb.AppendLine($"///\t <{hint.Key}>{hint.Value}</{hint.Key}>");

            sb.AppendLine($"/// </{closure}>");
            return sb.ToString();
        }

        internal static string GenerateCode(IShaderFunction func, bool generateHints = true, bool export = true, bool generateNamespace = true)
        {
            ShaderStringBuilder sb = new();

            if (generateNamespace)
                foreach (var name in func.Namespace)
                    sb.Append($"namespace {name}{{");

            sb.AppendNewLine();
            sb.IncreaseIndent();

            if (generateHints)
            {
                if (func.Hints.Count > 0)
                    sb.Append(GenerateHints("funchints", func.Hints));

                foreach (var param in func.Parameters)
                    if (param.Hints.Count > 0)
                        sb.Append(GenerateHints("paramhints", param.Hints, param.Name));
            }

            sb.Append($"{(export ? "UNITY_EXPORT_REFLECTION" : "")} {func.ReturnType.Name} {func.Name}(");

            bool first = true;

            foreach (var param in func.Parameters)
            {
                if (!first)
                    sb.Append(", ");
                first = false;
                sb.Append(DeclareField(param));
            }
            sb.AppendLine(")");
            sb.AppendLine("{");
            sb.IncreaseIndent();
            sb.AppendLine(func.FunctionBody);
            sb.DecreaseIndent();
            sb.AppendLine("}");
            sb.DecreaseIndent();

            if (generateNamespace)
                foreach (var name in func.Namespace)
                    sb.Append($"}} /*{name}*/");

            return sb.ToString();
        }
    }
}
