using System.Collections.Generic;
using UnityEditor.ShaderFoundry;

namespace UnityEditor.ShaderFoundry.UnitTests
{
    internal class UnityTexture2DPropertyBlockBuilder : BasePropertyBlockBuilder
    {
        public UnityTexture2DPropertyBlockBuilder()
        {
            BlockName = "UnityTexture2DProperty";
            FieldName = "FieldTexture";
            PropertyAttribute = new PropertyAttributeData() { DefaultValue = "\"\" {}" };
        }


        public Block Build(ShaderContainer container)
        {
            var propData = new BlockBuilderUtilities.PropertyDeclarationData
            {
                FieldType = container._UnityTexture2D,
                FieldName = FieldName,
                PropertyAttribute = PropertyAttribute,
                OutputsAssignmentCallback = (builder, propData) =>
                {
                    builder.AddLine($"float2 uv = float2(0, 0);");
                    builder.AddLine($"float4 sample = tex2D(inputs.{FieldName}, uv);");
                    builder.AddLine($"outputs.BaseColor = sample.xyz;");
                    builder.AddLine($"outputs.Alpha = sample.w;");
                }
            };
            return BlockBuilderUtilities.CreateSimplePropertyBlock(container, BlockName, propData);
        }
    }
}
