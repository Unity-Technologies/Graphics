using System.Collections.Generic;
using UnityEditor.ShaderFoundry;

namespace UnityEditor.ShaderFoundry.UnitTests
{
    internal class UnityTexture3DPropertyBlockBuilder : BasePropertyBlockBuilder
    {
        public UnityTexture3DPropertyBlockBuilder()
        {
            BlockName = "UnityTexture3DProperty";
            FieldName = "FieldTexture";
            PropertyAttribute = new PropertyAttributeData() { DefaultValue = "\"\" {}" };
        }

        public Block Build(ShaderContainer container)
        {
            var propData = new BlockBuilderUtilities.PropertyDeclarationData
            {
                FieldType = container._UnityTexture3D,
                FieldName = FieldName,
                PropertyAttribute = PropertyAttribute,
                OutputsAssignmentCallback = (builder, propData) =>
                {
                    builder.AddLine($"float3 uvw = float3(0, 0, 0);");
                    builder.AddLine($"float4 sample = tex3D(inputs.{FieldName}, uvw);");
                    builder.AddLine($"outputs.BaseColor = sample.xyz;");
                    builder.AddLine($"outputs.Alpha = sample.w;");
                }
            };
            return BlockBuilderUtilities.CreateSimplePropertyBlock(container, BlockName, propData);
        }
    }
}
