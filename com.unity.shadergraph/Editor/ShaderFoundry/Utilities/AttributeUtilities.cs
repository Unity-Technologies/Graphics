using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.ShaderFoundry;

namespace UnityEditor.ShaderFoundry
{
    static internal class CommonShaderAttributes
    {
        internal const string Property = "Property";
        internal const string Global = "Global";
        internal const string PerMaterial = "PerMaterial";
        internal const string Hybrid = "Hybrid";

        internal const string MaterialProperty = "MaterialProperty";
        internal const string PropertyVariable = "PropertyVariable";
        internal const string PropertyType = "PropertyType";
        internal const string DefaultValue = "DefaultValue";
        internal const string Varying = "Varying";
    }

    internal static class AttributeExtensions
    {
        internal static bool IsProperty(this ShaderAttribute attribute)
        {
            if (attribute.Name == CommonShaderAttributes.Property ||
                attribute.Name == CommonShaderAttributes.Global ||
                attribute.Name == CommonShaderAttributes.PerMaterial ||
                attribute.Name == CommonShaderAttributes.Hybrid)
                return true;
            return false;
        }

        internal static bool IsProperty(this IEnumerable<ShaderAttribute> attributes)
        {
            foreach (var attribute in attributes)
            {
                if (attribute.IsProperty())
                    return true;
            }
            return false;
        }

        internal static ShaderAttribute Clone(this ShaderAttribute attribute, ShaderContainer container)
        {
            // An attribute doesn't actually have to be cloned to be re-used if the container is the same
            if(attribute.Container == container)
                return attribute;

            // Otherwise we have to copy this into the current container
            var builder = new ShaderAttribute.Builder(attribute.Name);
            foreach (var param in attribute.Parameters)
                builder.Param(param.Name, param.Value);
            return builder.Build(container);
        }

        internal static List<ShaderAttribute> Clone(this IEnumerable<ShaderAttribute> attributes, ShaderContainer container)
        {
            if (attributes == null)
                return null;

            var results = new List<ShaderAttribute>();
            foreach (var attribute in attributes)
                results.Add(attribute.Clone(container));
            return results;
        }

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

        internal static ShaderAttributeParam GetAttributeParam(this IEnumerable<ShaderAttributeParam> parameters, int index)
        {
            var parameterList = parameters.ToList();
            if (index < parameterList.Count)
                return parameterList[index];
            return ShaderAttributeParam.Invalid;
        }

        internal static ShaderAttributeParam FindAttributeParam(this IEnumerable<ShaderAttributeParam> parameters, string name)
        {
            foreach (var parameter in parameters)
            {
                if (parameter.Name == name)
                    return parameter;
            }
            return ShaderAttributeParam.Invalid;
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

        internal static HLSLDeclaration GetDeclaration(this ShaderAttribute attribute)
        {
            var decl = HLSLDeclaration.DoNotDeclare;
            if (attribute.Name == "Property")
                decl = HLSLDeclaration.UnityPerMaterial;
            else if (attribute.Name == "Global")
                decl = HLSLDeclaration.Global;
            else if (attribute.Name == "PerMaterial")
                decl = HLSLDeclaration.UnityPerMaterial;
            else if (attribute.Name == "Hybrid")
                decl = HLSLDeclaration.HybridPerInstance;
            return decl;
        }

        internal static HLSLDeclaration GetDeclaration(this IEnumerable<ShaderAttribute> attributes)
        {
            var result = HLSLDeclaration.DoNotDeclare;
            foreach (var attribute in attributes)
            {
                var decl = attribute.GetDeclaration();
                if (decl != HLSLDeclaration.DoNotDeclare)
                    result = attribute.GetDeclaration();
            }
            return result;
        }
    }
    
    internal class PropertyTypeAttribute
    {
        internal string PropertyType { get; set; }
        internal static PropertyTypeAttribute Find(IEnumerable<ShaderAttribute> attributes)
        {
            var propertyType = attributes.FindFirstAttributeParamValue(CommonShaderAttributes.PropertyType, 0);
            if (propertyType != null)
                return new PropertyTypeAttribute { PropertyType = propertyType };
            return null;
        }
    }

    internal class PropertyVariableAttribute
    {
        internal string FormatString { get; set; }
        internal static PropertyVariableAttribute Find(IEnumerable<ShaderAttribute> attributes)
        {
            var formatString = attributes.FindFirstAttributeParamValue(CommonShaderAttributes.PropertyVariable, 0);
            if (formatString != null)
                return new PropertyVariableAttribute { FormatString = formatString };
            return null;
        }

        internal string BuildDeclarationString(string referenceName)
        {
            return FormatString.Replace("#", referenceName);
        }
    }

    internal class MaterialPropertyAttribute
    {
        internal string FormatString { get; set; }
        internal static MaterialPropertyAttribute Find(IEnumerable<ShaderAttribute> attributes)
        {
            var formatString = attributes.FindFirstAttributeParamValue(CommonShaderAttributes.MaterialProperty, 0);
            if (formatString != null)
                return new MaterialPropertyAttribute { FormatString = formatString };
            return null;
        }

        internal string BuildDeclarationString(string referenceName, string displayName)
        {
            return FormatString.Replace("#", referenceName);
        }
    }

    internal class DefaultValueAttribute
    {
        internal string DefaultValue { get; set; }
        internal static DefaultValueAttribute Find(IEnumerable<ShaderAttribute> attributes)
        {
            var defaultValue = attributes.FindFirstAttributeParamValue(CommonShaderAttributes.DefaultValue, 0);
            if (defaultValue != null)
                return new DefaultValueAttribute { DefaultValue = defaultValue };
            return null;
        }

        internal static DefaultValueAttribute Find(IEnumerable<ShaderAttribute> attributes, string variableName)
        {
            var attribute = attributes.FindFirst(CommonShaderAttributes.DefaultValue, variableName);
            if (attribute.IsValid)
            {
                var param = attribute.Parameters.GetAttributeParam(0);
                if (param.IsValid)
                    return new DefaultValueAttribute { DefaultValue = param.Value };
            }
            return null;
        }
    }
}
