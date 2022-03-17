using System.Collections.Generic;
using UnityEditor.ShaderFoundry;

namespace UnityEditor.ShaderFoundry.UnitTests
{
    internal class Float4PropertyBlockBuilder : BasePropertyBlockBuilder
    {
        public Float4PropertyBlockBuilder()
        {
            BlockName = "Float4Property";
            PropertyAttribute = new PropertyAttributeData() { DefaultValue = "(1, 1, 1, 1)" };
        }

        public Block Build(ShaderContainer container)
        {
            var propData = new BlockBuilderUtilities.PropertyDeclarationData
            {
                FieldType = container._float4,
                FieldName = FieldName,
                PropertyAttribute = PropertyAttribute,
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
