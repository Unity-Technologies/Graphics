using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UnityEditor.ShaderFoundry
{
    static internal class CommonShaderAttributes
    {
        internal const string Property = "Property";
        internal const string Global = "Global";
        internal const string PerMaterial = "PerMaterial";
        internal const string Hybrid = "Hybrid";

        internal const string DefaultValue = "DefaultValue";
        internal const string Default = "Default";
        internal const string Alias = "Alias";
    }

    internal static class AttributeExtensionsOld
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
            if (attribute.Container == container)
                return attribute;

            // Otherwise we have to copy this into the current container
            var builder = new ShaderAttribute.Builder(container, attribute.Name);
            foreach (var param in attribute.Parameters)
                builder.Param(param.Name, param.Value);
            return builder.Build();
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

    internal class AliasAttribute
    {
        internal string AliasName { get; set; }
        internal static IEnumerable<AliasAttribute> ForEach(IEnumerable<ShaderAttribute> attributes)
        {
            foreach (var attribute in attributes)
            {
                var aliasAttribute = Parse(attribute);
                if (aliasAttribute != null)
                    yield return aliasAttribute;
            }
        }

        internal static AliasAttribute Parse(ShaderAttribute attribute)
        {
            if (attribute.Name != CommonShaderAttributes.Alias)
                return null;

            var param = attribute.Parameters.GetAttributeParam(0);
            if (!param.IsValid)
                return null;
            return new AliasAttribute { AliasName = param.Value };
        }
    }
}
