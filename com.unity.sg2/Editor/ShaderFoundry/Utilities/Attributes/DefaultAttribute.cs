using System.Collections.Generic;
using System.Linq;

namespace UnityEditor.ShaderFoundry
{
    internal class DefaultAttribute
    {
        const string AttributeName = "Default";
        internal List<ShaderAttributeParam> Parameters;
        internal static DefaultAttribute Find(IEnumerable<ShaderAttribute> attributes)
        {
            var attribute = attributes.FindFirst(AttributeName);
            return TryParse(attribute);
        }

        internal static DefaultAttribute TryParse(ShaderAttribute attribute)
        {
            if (!attribute.IsValid)
                return null;
            if (attribute.Name != AttributeName && attribute.Parameters != null)
                return null;

            var result = new DefaultAttribute();
            result.Parameters = attribute.Parameters?.ToList();
            return result;
        }

        // Default expression for code. This is typically used in a block's "Constructor"
        internal string BuildDefaultValueExpression(ShaderType type)
        {
            // Deal with static initialization functions
            var initializerParam = Parameters.FindAttributeParam("staticInitializer");
            if (initializerParam.IsValid)
                return $"{initializerParam.Value}()";

            // Otherwise we have to parse the arguments depending on the type
            var parameters = Parameters.ToList();
            if (type.IsScalar)
                return parameters[0].Value;
            else if (type.IsVector)
            {
                var builder = new ShaderBuilder();
                builder.Add(type.Name);
                builder.Add("(");
                builder.Add(string.Join(", ", parameters.Select((v) => v.Value)));
                builder.Add(")");
                return builder.ToString();
            }
            else if (type.IsArray)
            {
                if (!type.ArrayElementType.IsScalar)
                {
                    ErrorHandling.ReportError($"Array defaults currently only support scalar types.");
                    return null;
                }
                if (type.ArrayElements != parameters.Count)
                {
                    ErrorHandling.ReportError($"Array default attribute must have enough arguments for the array size. {parameters.Count} provided when {type.ArrayElements} expected.");
                    return null;
                }

                var builder = new ShaderBuilder();
                builder.Add("{");
                builder.Add(string.Join(", ", parameters.Select((v) => v.Value)));
                builder.Add("}");
                return builder.ToString();
            }
            // TODO @ Shaders: There's no real way to tell if a type is one of our custom opaque types right now...
            else if (type.IsTexture || type.IsSamplerState || type.IsStruct)
                return null;
            else
            {
                ErrorHandling.ReportError($"Default values for type {type.Name} are not yet supported.");
                return null;
            }
        }

        internal string BuildMaterialPropertyDefaultValue(ShaderType type)
        {
            var parameters = Parameters.ToList();
            // Scalar: Value
            if (type.IsScalar)
                return parameters[0].Value;
            // Vector: (x, y, z, w). Note: float2 must be upgrade to a float3 because of SL parser reasons
            else if (type.IsVector)
            {
                var builder = new ShaderBuilder();
                builder.Add("(");
                builder.Add(string.Join(", ", parameters.Select((v) => v.Value)));
                for (var i = parameters.Count; i < 3; ++i)
                    builder.Add(", 0");
                builder.Add(")");
                return builder.ToString();
            }
            // Texture: "Name" {}
            else if (type.IsTexture)
                return $"\"{parameters[0].Value}\" {{}}";
            else
                return null;
        }
    }
}
