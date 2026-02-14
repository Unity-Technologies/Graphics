
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
    }
}
