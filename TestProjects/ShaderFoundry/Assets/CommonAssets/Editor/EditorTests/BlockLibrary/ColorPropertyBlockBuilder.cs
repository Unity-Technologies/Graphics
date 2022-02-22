using System.Collections.Generic;
using UnityEditor.ShaderFoundry;

namespace UnityEditor.ShaderFoundry.UnitTests
{
    internal class ColorPropertyBlockBuilder : BasePropertyBlockBuilder
    {
        public ColorPropertyBlockBuilder()
        {
            BlockName = "ColorProperty";
            FieldName = "FieldColor";
            PropertyAttribute = new PropertyAttributeData() { DefaultValue = "1" };
        }

        public Block Build(ShaderContainer container)
        {
            var colorAttribute = new ShaderAttribute.Builder(container, "Color").Build();
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
                ExtraAttributes = new List<ShaderAttribute> { colorAttribute },
            };
            return BlockBuilderUtilities.CreateSimplePropertyBlock(container, BlockName, propData);
        }
    }
}
