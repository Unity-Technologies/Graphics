using System.Collections.Generic;
using UnityEditor.ShaderFoundry;

namespace UnityEditor.ShaderFoundry.UnitTests
{
    internal class UnityTextureCubePropertyBlockBuilder : BasePropertyBlockBuilder
    {
        public string SampleDirection = "float3(1, 0, 0)";

        public UnityTextureCubePropertyBlockBuilder()
        {
            BlockName = "UnityTextureCubeProperty";
            FieldName = "FieldTexture";
            PropertyAttribute = new PropertyAttributeData() { DefaultValue = "\"\" {}" };
        }

        public Block Build(ShaderContainer container)
        {
            var propData = new BlockBuilderUtilities.PropertyDeclarationData
            {
                FieldType = container._UnityTextureCube,
                FieldName = FieldName,
                PropertyAttribute = PropertyAttribute,
                OutputsAssignmentCallback = (builder, propData) =>
                {
                    builder.AddLine($"float3 uvw = {SampleDirection};");
                    builder.AddLine($"float4 sample = texCUBE(inputs.{FieldName}, uvw);");
                    builder.AddLine($"outputs.BaseColor = sample.xyz;");
                    builder.AddLine($"outputs.Alpha = sample.w;");
                }
            };
            return BlockBuilderUtilities.CreateSimplePropertyBlock(container, BlockName, propData);
        }
    }
}
