using System.Collections.Generic;
using UnityEditor.ShaderFoundry;

namespace UnityEditor.ShaderFoundry.UnitTests
{
    internal class Float2PropertyBlockBuilder : BasePropertyBlockBuilder
    {
        public Float2PropertyBlockBuilder()
        {
            BlockName = "Float2Property";
            PropertyAttribute = new PropertyAttributeData() { DefaultValue = "(1, 1, 0, 0)" };
        }

        public Block Build(ShaderContainer container)
        {
            var propData = new BlockBuilderUtilities.PropertyDeclarationData
            {
                FieldType = container._float2,
                FieldName = FieldName,
                PropertyAttribute = PropertyAttribute,
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
