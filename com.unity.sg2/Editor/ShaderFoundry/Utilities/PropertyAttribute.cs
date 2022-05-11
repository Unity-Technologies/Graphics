using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderFoundry
{
    internal enum UniformDataSource
    {
        Global,
        PerMaterial,
        PerInstance,
        Custom,
        None = -1
    }

    internal class PropertyAttribute
    {
        internal const string AttributeName = CommonShaderAttributes.Property;
        internal const string UniformNameParamName = "uniformName";
        internal const string DisplayNameParamName = "displayName";
        internal const string DefaultValueParamName = "defaultValue";
        internal const string DataSourceParamName = "dataSource";
        internal const string CustomBufferParamName = "customBufferName";
        internal const string ExposedParamName = "exposed";

        internal string UniformName;
        internal string DisplayName;
        internal string DefaultValue;
        internal UniformDataSource DataSource = UniformDataSource.PerMaterial;
        internal string CustomBufferName;
        internal bool Exposed = true;

        internal static PropertyAttribute FindFirst(IEnumerable<ShaderAttribute> attributes)
        {
            var attribute = attributes.FindFirst(AttributeName);
            if (attribute.IsValid)
                return Parse(attribute);
            return null;
        }

        internal ShaderAttribute Build(ShaderContainer container)
        {
            void TryAdd(ref ShaderAttribute.Builder builder, string name, string value)
            {
                if (!string.IsNullOrEmpty(value))
                    builder.Param(name, value);
            }

            var attributeBuilder = new ShaderAttribute.Builder(container, AttributeName);
            TryAdd(ref attributeBuilder, UniformNameParamName, UniformName);
            TryAdd(ref attributeBuilder, DisplayNameParamName, DisplayName);
            TryAdd(ref attributeBuilder, DefaultValueParamName, DefaultValue);
            TryAdd(ref attributeBuilder, DataSourceParamName, DataSource.ToString());
            TryAdd(ref attributeBuilder, CustomBufferParamName, CustomBufferName);
            TryAdd(ref attributeBuilder, ExposedParamName, Exposed.ToString());

            return attributeBuilder.Build();
        }

        static AttributeParsing.SignatureDescription<PropertyAttribute> AttributeSignature = new AttributeParsing.SignatureDescription<PropertyAttribute>()
        {
            ParameterDescriptions = new List<AttributeParsing.ParameterDescription<PropertyAttribute>>
            {
                new AttributeParsing.ParameterDescription<PropertyAttribute>(UniformNameParamName, (param, index, target) => AttributeParsing.ParseString(param, index, ref target.UniformName)),
                new AttributeParsing.ParameterDescription<PropertyAttribute>(DisplayNameParamName, (param, index, target) => AttributeParsing.ParseString(param, index, ref target.DisplayName)),
                new AttributeParsing.ParameterDescription<PropertyAttribute>(DefaultValueParamName, (param, index, target) => AttributeParsing.ParseString(param, index, ref target.DefaultValue)),
                new AttributeParsing.ParameterDescription<PropertyAttribute>(DataSourceParamName, (param, index, target) => AttributeParsing.ParseEnum(param, index, ref target.DataSource)),
                new AttributeParsing.ParameterDescription<PropertyAttribute>(CustomBufferParamName, (param, index, target) => AttributeParsing.ParseString(param, index, ref target.CustomBufferName)),
                new AttributeParsing.ParameterDescription<PropertyAttribute>(ExposedParamName, (param, index, target) => AttributeParsing.ParseBool(param, index, ref target.Exposed)),
            },
        };

        internal static PropertyAttribute Parse(ShaderAttribute attribute)
        {
            if (!attribute.IsValid || attribute.Name != AttributeName)
                return null;

            var result = new PropertyAttribute();
            AttributeParsing.Parse(attribute, AttributeSignature, result);
            // A custom data source without a custom buffer name doesn't make sense.
            if (result.DataSource == UniformDataSource.Custom && string.IsNullOrEmpty(result.CustomBufferName))
            {
                ErrorHandling.ReportError($"Data source of {UniformDataSource.Custom} must specify a valid {CustomBufferParamName}. Will fallback to data source '{UniformDataSource.Global}'");
                result.DataSource = UniformDataSource.Global;
            }
            return result;
        }
    }
}
