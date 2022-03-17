using System.Collections.Generic;
using UnityEditor.ShaderFoundry;

namespace UnityEditor.ShaderFoundry.UnitTests
{
    internal class Float3PropertyBlockBuilder : BasePropertyBlockBuilder
    {
        public Float3PropertyBlockBuilder()
        {
            BlockName = "Float3Property";
            PropertyAttribute = new PropertyAttributeData() { DefaultValue = "(1, 1, 1, 0)" };
        }

        public Block Build(ShaderContainer container)
        {
            var propData = new BlockBuilderUtilities.PropertyDeclarationData
            {
                FieldType = container._float3,
                FieldName = FieldName,
                PropertyAttribute = PropertyAttribute,
                OutputsAssignmentCallback = (builder, propData) =>
                {
                    builder.AddLine($"outputs.BaseColor = inputs.{FieldName};");
                    builder.AddLine($"outputs.Alpha = 0;");
                },
            };
            return BlockBuilderUtilities.CreateSimplePropertyBlock(container, BlockName, propData);
        }
    }
}
