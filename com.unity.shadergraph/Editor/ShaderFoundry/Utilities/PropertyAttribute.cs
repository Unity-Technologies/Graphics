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
        const string AttributeName = CommonShaderAttributes.Property;
        const string UniformNameParamName = "uniformName";
        const string DisplayNameParamName = "displayName";
        const string DefaultValueParamName = "defaultValue";
        const string DataSourceParamName = "dataSource";
        const string CustomBufferParamName = "customBufferName";
        const string ExposedParamName = "exposed";

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

        internal static PropertyAttribute Parse(ShaderAttribute attribute)
        {
            if (!attribute.IsValid || attribute.Name != AttributeName)
                return null;


            var result = new PropertyAttribute();

            var signature = new AttributeParsing.SignatureDescription();
            signature.ParameterDescriptions = new List<AttributeParsing.ParameterDescription>
            {
                new AttributeParsing.ParameterDescription(UniformNameParamName, (param, index) => AttributeParsing.StringParseCallback(param, index, ref result.UniformName)),
                new AttributeParsing.ParameterDescription(DisplayNameParamName, (param, index) => AttributeParsing.StringParseCallback(param, index, ref result.DisplayName)),
                new AttributeParsing.ParameterDescription(DefaultValueParamName, (param, index) => AttributeParsing.StringParseCallback(param, index, ref result.DefaultValue)),
                new AttributeParsing.ParameterDescription(DataSourceParamName, (param, index) => AttributeParsing.EnumParseCallback(param, index, ref result.DataSource) ),
                new AttributeParsing.ParameterDescription(CustomBufferParamName, (param, index) => AttributeParsing.StringParseCallback(param, index, ref result.CustomBufferName)),
                new AttributeParsing.ParameterDescription(ExposedParamName, (param, index) => AttributeParsing.BoolParseCallback(param, index, ref result.Exposed)),
            };
            AttributeParsing.Parse(attribute, signature);

            return result;
        }
    }
}
