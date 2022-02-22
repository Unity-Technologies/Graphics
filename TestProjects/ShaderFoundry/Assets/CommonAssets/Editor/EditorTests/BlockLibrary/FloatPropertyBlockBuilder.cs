using System.Collections.Generic;
using UnityEditor.ShaderFoundry;

namespace UnityEditor.ShaderFoundry.UnitTests
{
    internal class FloatPropertyBlockBuilder : BasePropertyBlockBuilder
    {
        public FloatPropertyBlockBuilder()
        {
            BlockName = "FloatProperty";
            PropertyAttribute = new PropertyAttributeData() { DefaultValue = "1" };
        }

        public Block Build(ShaderContainer container)
        {
            var propData = new BlockBuilderUtilities.PropertyDeclarationData
            {
                FieldType = container._float,
                FieldName = FieldName,
                PropertyAttribute = PropertyAttribute,
                OutputsAssignmentCallback = (builder, propData) => builder.AddLine($"outputs.BaseColor = float3(inputs.{FieldName}, 0, 0);"),
            };
            return BlockBuilderUtilities.CreateSimplePropertyBlock(container, BlockName, propData);
        }
    }
}
