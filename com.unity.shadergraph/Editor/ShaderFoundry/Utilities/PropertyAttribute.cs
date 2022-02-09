using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderFoundry
{
    internal enum UniformDataSource { Global, PerMaterial, PerInstance, Custom, None = -1 }

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
            foreach (var param in attribute.Parameters)
            {
                if (param.Name == UniformNameParamName)
                    result.UniformName = param.Value;
                else if (param.Name == DisplayNameParamName)
                    result.DisplayName = param.Value;
                else if (param.Name == DefaultValueParamName)
                    result.DefaultValue = param.Value;
                else if (param.Name == DataSourceParamName)
                    result.DataSource = ParseDataSource(param.Value);
                else if (param.Name == CustomBufferParamName)
                    result.CustomBufferName = param.Value;
                else if (param.Name == ExposedParamName)
                {
                    if (bool.TryParse(param.Value, out var exposed))
                        result.Exposed = exposed;
                }
            }
            return result;
        }

        static UniformDataSource ParseDataSource(string dataSource)
        {
            UniformDataSource result;
            if (!Enum.TryParse(dataSource, out result))
                result = UniformDataSource.PerMaterial;
            return result;
        }
    }
}
