using System.Collections.Generic;

namespace UnityEditor.ShaderFoundry
{
    class MaterialPropertyDeclarationData
    {
        internal IEnumerable<string> Attributes;
        internal string UniformName;
        internal string DisplayName;
        internal string DisplayType;
        internal string DefaultValueExpression;

        internal static MaterialPropertyDeclarationData BuildSimple(FieldPropertyContext context, string displayType, string defaultValue, FieldPropertyData resultProperty)
        {
            if (context.Exposed == false)
                return null;

            // Build the list of shaderlab attributes for the property
            var attributes = new List<string>();
            if (context.ExtraAttributes != null)
                attributes.AddRange(context.ExtraAttributes);
            foreach (var attribute in context.ProcessedAttributes.Values)
            {
                var builder = new ShaderBuilder();
                builder.DeclareAttribute(attribute);
                attributes.Add(builder.ToString());
            }

            var matProp = new MaterialPropertyDeclarationData
            {
                UniformName = context.UniformName,
                DisplayName = context.DisplayName,
                DisplayType = displayType,
                DefaultValueExpression = context.DefaultValue ?? defaultValue,
                Attributes = attributes,
            };
            resultProperty.MaterialPropertyDeclarations.Add(matProp);
            return matProp;
        }

        internal void Declare(ShaderBuilder builder)
        {
            builder.Indentation();
            if (Attributes != null)
            {
                foreach (var attribute in Attributes)
                    builder.Add(attribute);
            }
            builder.Add($"{UniformName}(\"{DisplayName}\", {DisplayType}) = {DefaultValueExpression}");
            builder.NewLine();
        }
    }
}
