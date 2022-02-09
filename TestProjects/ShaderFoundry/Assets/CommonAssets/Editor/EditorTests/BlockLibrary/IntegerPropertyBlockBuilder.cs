using System.Collections.Generic;
using UnityEditor.ShaderFoundry;

namespace UnityEditor.ShaderFoundry.UnitTests
{
    internal class IntegerPropertyBlockBuilder : BasePropertyBlockBuilder
    {
        public IntegerPropertyBlockBuilder()
        {
            BlockName = "IntegerProperty";
            PropertyAttribute = new PropertyAttributeData() { DefaultValue = "1" };
        }

        public Block Build(ShaderContainer container)
        {
            var propData = new BlockBuilderUtilities.PropertyDeclarationData
            {
                FieldType = container._int,
                FieldName = FieldName,
                PropertyAttribute = PropertyAttribute,
                OutputsAssignmentCallback = (builder, propData) => builder.AddLine($"outputs.BaseColor = float3(inputs.{FieldName}.xxx);"),
            };
            return BlockBuilderUtilities.CreateSimplePropertyBlock(container, BlockName, propData);
        }
    }
}
