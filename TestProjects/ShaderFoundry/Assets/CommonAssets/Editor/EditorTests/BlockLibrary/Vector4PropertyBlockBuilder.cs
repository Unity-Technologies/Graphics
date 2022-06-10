using System.Collections.Generic;

namespace UnityEditor.ShaderFoundry.UnitTests
{
    internal class Vector4PropertyBlockBuilder : BasePropertyBlockBuilder
    {
        public Vector4PropertyBlockBuilder()
        {
            BlockName = "Vector4Property";
            PropertyAttribute = new PropertyAttributeData() { DefaultValue = "(1, 1, 1, 1)" };
        }

        public Block Build(ShaderContainer container, ShaderType fieldType) => BuildWithAttributeOverrides(container, fieldType, null);

        public Block BuildWithAttributeOverrides(ShaderContainer container, ShaderType fieldType, List<ShaderAttribute> attributes)
        {
            var propData = new BlockBuilderUtilities.PropertyDeclarationData
            {
                FieldType = fieldType,
                FieldName = FieldName,
                PropertyAttribute = PropertyAttribute,
                ExtraAttributes = attributes,
                OutputsAssignmentCallback = (builder, propData) =>
                {
                    builder.AddLine($"outputs.BaseColor = inputs.{FieldName}.xyz;");
                    builder.AddLine($"outputs.Alpha = inputs.{FieldName}.w;");
                },
            };
            return BlockBuilderUtilities.CreateSimplePropertyBlock(container, BlockName, propData);
        }
    }
}
