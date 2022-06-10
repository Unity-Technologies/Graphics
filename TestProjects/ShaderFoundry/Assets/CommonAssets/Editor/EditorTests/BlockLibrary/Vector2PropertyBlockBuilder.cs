using System.Collections.Generic;

namespace UnityEditor.ShaderFoundry.UnitTests
{
    internal class Vector2PropertyBlockBuilder : BasePropertyBlockBuilder
    {
        public Vector2PropertyBlockBuilder()
        {
            BlockName = "Vector2Property";
            PropertyAttribute = new PropertyAttributeData() { DefaultValue = "(1, 1, 0, 0)" };
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
                    builder.AddLine($"outputs.BaseColor = float3(inputs.{FieldName}, 0);");
                    builder.AddLine($"outputs.Alpha = 0;");
                },
            };
            return BlockBuilderUtilities.CreateSimplePropertyBlock(container, BlockName, propData);
        }
    }
}
