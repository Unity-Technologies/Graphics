using System.Collections.Generic;
using UnityEditor.ShaderFoundry;

namespace UnityEditor.ShaderFoundry.UnitTests
{
    internal class UnityTexture2DArrayPropertyBlockBuilder : BasePropertyBlockBuilder
    {
        
        public int SampleIndex = 0;

        public UnityTexture2DArrayPropertyBlockBuilder()
        {
            BlockName = "UnityTexture2DArrayProperty";
            FieldName = "FieldTexture";
            PropertyAttribute = new PropertyAttributeData() { DefaultValue = "\"\" {}" };
        }


        public Block Build(ShaderContainer container)
        {
            var propData = new BlockBuilderUtilities.PropertyDeclarationData
            {
                FieldType = container._UnityTexture2DArray,
                FieldName = FieldName,
                PropertyAttribute = PropertyAttribute,
                OutputsAssignmentCallback = (builder, propData) =>
                {
                    builder.AddLine($"float3 uvw = float3(0, 0, {SampleIndex});");
                    builder.AddLine($"float4 sample = inputs.{FieldName}.Sample(inputs.{FieldName}.samplerstate, uvw);");
                    builder.AddLine($"outputs.BaseColor = sample.xyz;");
                    builder.AddLine($"outputs.Alpha = sample.w;");
                }
            };
            return BlockBuilderUtilities.CreateSimplePropertyBlock(container, BlockName, propData);
        }
    }
}
