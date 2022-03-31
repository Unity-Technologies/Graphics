using System.Collections.Generic;

namespace UnityEditor.ShaderFoundry.UnitTests
{
    internal class SamplerStatePropertyBlockBuilder : BasePropertyBlockBuilder
    {
        public SamplerStateAttribute.FilterModeEnum? FilterMode;
        public List<SamplerStateAttribute.WrapModeParameterStates> WrapModes = new List<SamplerStateAttribute.WrapModeParameterStates>();
        public bool? DepthCompare;
        public int? AnisotropicLevel;

        public SamplerStatePropertyBlockBuilder()
        {
            BlockName = "UnitySamplerState";
            FieldName = "FieldSamplerState";
            PropertyAttribute = new PropertyAttributeData();
        }

        public string GetUniformName() => PropertyAttribute.UniformName ?? FieldName;

        public Block Build(ShaderContainer container)
        {
            var attributes = BuildAttributes(container);
            return BuildWithAttributeOverrides(container, attributes);
        }

        // Builds with the provided attributes.
        // This primarily used for unit testing invalid attribute cases.
        public Block BuildWithAttributeOverrides(ShaderContainer container, List<ShaderAttribute> attributes)
        {
            var propData = new BlockBuilderUtilities.PropertyDeclarationData
            {
                Fields = new List<BlockBuilderUtilities.FieldData>
                {
                    // Build the sampler state property
                    new BlockBuilderUtilities.FieldData
                    {
                        Name = FieldName,
                        Type = container._UnitySamplerState,
                        PropertyAttribute = PropertyAttribute,
                        ExtraAttributes = attributes,
                    },
                    // Build a texture property that we can sample. This would be needed to actually use the sampler in a play mode test
                    new BlockBuilderUtilities.FieldData
                    {
                        Name = "_Texture",
                        Type = container._UnityTexture2D,
                        PropertyAttribute = new PropertyAttributeData(),
                    },
                },
                OutputsAssignmentCallback = (builder, propData) =>
                {
                    builder.AddLine($"float2 uv = float2(0, 0);");
                    builder.AddLine($"float4 sample = inputs._Texture.Sample(inputs.{FieldName}.samplerstate, uv);");
                    builder.AddLine($"outputs.BaseColor = sample.xyz;");
                    builder.AddLine($"outputs.Alpha = sample.w;");
                }
            };
            return BlockBuilderUtilities.CreateSimplePropertyBlock(container, BlockName, propData);
        }

        internal List<ShaderAttribute> BuildAttributes(ShaderContainer container)
        {
            var attributes = new List<ShaderAttribute>();
            var attributeBuilder = new ShaderAttribute.Builder(container, SamplerStateAttribute.AttributeName);
            if (FilterMode is SamplerStateAttribute.FilterModeEnum filterMode)
                attributeBuilder.Param(SamplerStateAttribute.FilterModeParamName, filterMode.ToString());
            var wrapModeString = SamplerStateAttribute.BuildWrapModeParameterValue(WrapModes);
            if (!string.IsNullOrEmpty(wrapModeString))
                attributeBuilder.Param(SamplerStateAttribute.WrapModeParamName, wrapModeString);
            if (DepthCompare is bool depthCompare)
                attributeBuilder.Param(SamplerStateAttribute.DepthCompareParamName, depthCompare.ToString());
            if (AnisotropicLevel is int anisotropicLevel)
                attributeBuilder.Param(SamplerStateAttribute.AnisotropicLevelParamName, anisotropicLevel.ToString());
            attributes.Add(attributeBuilder.Build());
            return attributes;
        }
    }
}
