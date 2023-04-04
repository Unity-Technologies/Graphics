using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnityEditor.ShaderFoundry
{
    internal static class AttributeExtensions
    {
        static internal ShaderAttribute FindFirst(this IEnumerable<ShaderAttribute> attributes, string attributeName)
        {
            foreach (var attribute in attributes)
            {
                if (attribute.Name == attributeName)
                    return attribute;
            }
            return ShaderAttribute.Invalid;
        }

        static internal ShaderAttribute FindFirst(this IEnumerable<ShaderAttribute> attributes, string attributeName, string paramName)
        {
            foreach (var attribute in attributes)
            {
                if (attribute.Name == attributeName)
                {
                    foreach (var param in attribute.Parameters)
                    {
                        if (param.Name == paramName)
                            return attribute;
                    }
                }
            }
            return ShaderAttribute.Invalid;
        }

        internal static ShaderAttributeParameter GetAttributeParam(this IEnumerable<ShaderAttributeParameter> parameters, int index)
        {
            var parameterList = parameters.ToList();
            if (index < parameterList.Count)
                return parameterList[index];
            return ShaderAttributeParameter.Invalid;
        }

        internal static ShaderAttributeParameter FindAttributeParam(this IEnumerable<ShaderAttributeParameter> parameters, string name)
        {
            foreach (var parameter in parameters)
            {
                if (parameter.Name == name)
                    return parameter;
            }
            return ShaderAttributeParameter.Invalid;
        }

        internal static string FindFirstAttributeParamValue(this IEnumerable<ShaderAttribute> attributes, string attributeName, int index)
        {
            var attribute = attributes.FindFirst(attributeName);
            if (attribute.IsValid)
            {
                var parameter = GetAttributeParam(attribute.Parameters, index);
                if (parameter.IsValid)
                    return parameter.Value;
            }
            return null;
        }

        // Debug
        internal static void Display(this IEnumerable<ShaderAttribute> attributes)
        {
            var attributesList = attributes.ToList();
            foreach (var attribute in attributesList)
            {
                var attParams = attribute.Parameters.ToList();
                var builder = new StringBuilder();
                builder.Append($"[{attribute.Name}(");
                foreach (var param in attParams)
                {
                    builder.Append($"{param.Name} = {param.Value}, ");
                }
                builder.Append(")]");
            }
        }
    }
}
