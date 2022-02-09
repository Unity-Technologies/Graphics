using System;
using System.Collections.Generic;

namespace UnityEditor.ShaderFoundry
{
    internal class FieldPropertyContext
    {
        readonly internal ShaderContainer Container;
        readonly internal ShaderType FieldType;
        readonly internal string FieldName;
        readonly internal IEnumerable<ShaderAttribute> Attributes;
        readonly PropertyAttribute propertyAttribute;

        internal string UniformName;
        internal string DisplayName;
        internal bool Exposed => propertyAttribute.Exposed;
        internal UniformDataSource DataSource => propertyAttribute.DataSource;
        internal string DefaultValue => propertyAttribute.DefaultValue;
        
        
        internal Dictionary<string, ShaderAttribute> ProcessedAttributes = new Dictionary<string, ShaderAttribute>();
        // Any extra attributes being added by a property that don't have backing ShaderAttributes.
        // Used to do things like add the [Toggle] attribute for booleans.
        internal List<string> ExtraAttributes;

        internal FieldPropertyContext()
        {
        }

        internal FieldPropertyContext(ShaderContainer container, ShaderType fieldType, string fieldName, IEnumerable<ShaderAttribute> attributes, PropertyAttribute propertyAttribute)
        {
            Container = container;
            FieldType = fieldType;
            FieldName = fieldName;
            Attributes = attributes;
            this.propertyAttribute = propertyAttribute;
            UniformName = propertyAttribute.UniformName ?? FieldName;
            DisplayName = propertyAttribute.DisplayName ?? FieldName;
        }
    }
}
