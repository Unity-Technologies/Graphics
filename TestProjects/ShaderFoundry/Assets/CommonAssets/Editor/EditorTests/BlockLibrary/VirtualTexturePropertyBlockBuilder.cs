using System.Collections.Generic;
using static UnityEditor.ShaderFoundry.VirtualTextureLayerAttribute;

namespace UnityEditor.ShaderFoundry.UnitTests
{
    internal class VirtualTexturePropertyBlockBuilder : BasePropertyBlockBuilder
    {
        public class LayerData
        {
            internal string UniformName;
            internal string DisplayName;
            internal string TextureName;
            internal LayerTextureType? TextureType;
        }

        public class CachedLayerData
        {
            internal string UniformName;
            internal string DisplayName;
            internal string TextureName;
            internal LayerTextureType TextureType;
        }

        const string VirtualTexturePropertyTypeName = "VTPropertyWithTextureType";
        readonly public int LayerCount;
        public int LayerToSample = 0;

        // The data used to build layers. If a layer is null, then there is no override (no attribute) for the layer.
        public List<LayerData> Layers = new List<LayerData>();
        // The cached data of what each layer's values should be
        public List<CachedLayerData> CachedLayers = new List<CachedLayerData>();

        public VirtualTexturePropertyBlockBuilder(int layerCount)
        {
            LayerCount = layerCount;
            BlockName = "VirtualTextureProperty";
            FieldName = "FieldVirtualTexture";
            PropertyAttribute = new PropertyAttributeData();
            for (var i = 0; i < layerCount; ++i)
                Layers.Add(null);
        }

        public Block Build(ShaderContainer container)
        {
            BuildCache();

            var attributes = BuildAttributes(container);
            return BuildWithAttributeOverrides(container, attributes);
        }

        // Builds with the provided attributes instead of building up the relevant virtual texture attributes.
        // This primarily used for unit testing invalid attribute cases.
        public Block BuildWithAttributeOverrides(ShaderContainer container, List<ShaderAttribute> attributes)
        {
            BuildCache();

            ShaderFunction sampleFunction = ShaderFunction.Invalid;
            var propData = new BlockBuilderUtilities.PropertyDeclarationData
            {
                FieldType = GetVirtualTextureType(container),
                FieldName = FieldName,
                PropertyAttribute = PropertyAttribute,
                ExtraAttributes = attributes,
                ExtraBlockGenerationCallback = (blockBuilder, propData) => { sampleFunction = CreateSampleFunction(container, blockBuilder, LayerToSample); },
                OutputsAssignmentCallback = (builder, propData) =>
                {
                    builder.AddLine($"float2 uv = float2(0, 0);");
                    builder.AddLine($"float4 sampleOut = 0;");
                    builder.CallFunction(sampleFunction, "uv", $"inputs.{FieldName}", "sampleOut");
                    builder.AddLine($"outputs.BaseColor = sampleOut.xyz;");
                    builder.AddLine($"outputs.Alpha = sampleOut.w;");
                }
            };
            var builder = BlockBuilderUtilities.CreateSimplePropertyBlockBuilder(container, BlockName, propData);
            return builder.Build();
        }

        internal List<ShaderAttribute> BuildAttributes(ShaderContainer container)
        {
            var attributes = new List<ShaderAttribute>();
            var vtAttribute = BuildVirtualTextureAttribute(container, LayerCount.ToString());
            attributes.Add(vtAttribute);

            for (var i = 0; i < Layers.Count; ++i)
            {
                var layerData = Layers[i];
                // This layer didn't have an override
                if (layerData == null)
                    continue;

                var layerAttribute = BuildLayerAttribute(container, i, layerData);
                attributes.Add(layerAttribute);
            }
            return attributes;
        }

        static internal ShaderAttribute BuildVirtualTextureAttribute(ShaderContainer container, string layerCount)
        {
            var attributeBuilder = new ShaderAttribute.Builder(container, VirtualTextureAttribute.AttributeName);
            // LayerCount might be null so we can test the error case of it not being specified
            if (!string.IsNullOrEmpty(layerCount))
                attributeBuilder.Param(new ShaderAttributeParam.Builder(container, VirtualTextureAttribute.LayerCountParamName, layerCount).Build());
            return attributeBuilder.Build();
        }

        static internal ShaderAttribute BuildLayerAttribute(ShaderContainer container, int layerIndex, LayerData layerData)
        {
            return BuildLayerAttribute(container, layerIndex.ToString(), layerData.UniformName, layerData.DisplayName, layerData.TextureName, layerData.TextureType.ToString());
        }

        static internal ShaderAttribute BuildLayerAttribute(ShaderContainer container, string layerIndex, string uniformName, string displayName, string textureName, string textureType)
        {
            var attributeBuilder = new ShaderAttribute.Builder(container, AttributeName);
            if (!string.IsNullOrEmpty(layerIndex))
                attributeBuilder.Param(new ShaderAttributeParam.Builder(container, IndexParamName, layerIndex).Build());
            if (!string.IsNullOrEmpty(uniformName))
                attributeBuilder.Param(new ShaderAttributeParam.Builder(container, UniformNameParamName, uniformName).Build());
            if (!string.IsNullOrEmpty(displayName))
                attributeBuilder.Param(new ShaderAttributeParam.Builder(container, DisplayNameParamName, displayName).Build());
            if (!string.IsNullOrEmpty(textureName))
                attributeBuilder.Param(new ShaderAttributeParam.Builder(container, TextureNameParamName, textureName).Build());
            if (!string.IsNullOrEmpty(textureType))
                attributeBuilder.Param(new ShaderAttributeParam.Builder(container, TextureTypeParamName, textureType).Build());
            return attributeBuilder.Build();
        }

        public static ShaderType GetVirtualTextureType(ShaderContainer container)
        {
            return container.GetType(VirtualTexturePropertyTypeName);
        }

        ShaderFunction CreateSampleFunction(ShaderContainer container, Block.Builder blockBuilder, int layer)
        {
            var uvName = "uv";
            var propName = "vtProperty";
            var layerOutName = "layerOut";
            var virtualTexturePropertyType = GetVirtualTextureType(container);
            var functionBuilder = new ShaderFunction.Builder(blockBuilder, $"SamplerLayer{layer}", container._float4);
            functionBuilder.AddParameter(new FunctionParameter.Builder(container, uvName, container._float2, true, false).Build());
            functionBuilder.AddParameter(new FunctionParameter.Builder(container, propName, virtualTexturePropertyType, true, false).Build());
            functionBuilder.AddParameter(new FunctionParameter.Builder(container, layerOutName, container._float4, false, true).Build());

            functionBuilder.AddLine("VtInputParameters vtParams;");
            functionBuilder.AddLine($"vtParams.uv = {uvName};");
            functionBuilder.AddLine("vtParams.lodOrOffset = 0.0f;");
            functionBuilder.AddLine("vtParams.dx = 0.0f;");
            functionBuilder.AddLine("vtParams.dy = 0.0f;");
            functionBuilder.AddLine("vtParams.addressMode = VtAddressMode_Wrap;");
            functionBuilder.AddLine("vtParams.filterMode = VtFilter_Anisotropic;");
            functionBuilder.AddLine("vtParams.levelMode = VtLevel_Automatic;");
            functionBuilder.AddLine("vtParams.uvMode = VtUvSpace_Regular;");
            functionBuilder.AddLine("vtParams.sampleQuality = VtSampleQuality_High;");
            functionBuilder.AddLine("vtParams.enableGlobalMipBias = 1;");
            functionBuilder.AddLine("#if defined(SHADER_STAGE_RAY_TRACING)");
            functionBuilder.AddLine("if (vtParams.levelMode == VtLevel_Automatic || vtParams.levelMode == VtLevel_Bias)");
            functionBuilder.AddLine("{");
            functionBuilder.Indent();
            functionBuilder.AddLine("vtParams.levelMode = VtLevel_Lod;");
            functionBuilder.AddLine("vtParams.lodOrOffset = 0.0f;");
            functionBuilder.Deindent();
            functionBuilder.AddLine("}");
            functionBuilder.AddLine("#endif");
            functionBuilder.AddLine($"StackInfo info = PrepareVT({propName}.vtProperty, vtParams);");
            functionBuilder.AddLine($"{layerOutName} = SampleVTLayerWithTextureType({propName}, vtParams, info, {layer});");
            functionBuilder.AddLine("return GetResolveOutput(info);");

            var result = functionBuilder.Build();
            return result;
        }

        void BuildCache()
        {
            for (var i = 0; i < LayerCount; ++i)
            {
                var cachedLayerData = new CachedLayerData();
                var layerData = Layers[i];

                cachedLayerData.UniformName = GetName(i, layerData?.UniformName, PropertyAttribute?.UniformName, FieldName);
                cachedLayerData.DisplayName = GetName(i, layerData?.DisplayName, PropertyAttribute?.DisplayName, FieldName);
                cachedLayerData.TextureName = layerData?.TextureName ?? PropertyAttribute?.DefaultValue ?? "\"\" {}";
                cachedLayerData.TextureType = layerData?.TextureType ?? LayerTextureType.Default;
                CachedLayers.Add(cachedLayerData);
            }
        }

        static string GetName(int layerIndex, string layerValue, string propertyValue, string fieldName)
        {
            // Uniform and Display names follow a fallback scheme. It will use in order:
            // - The layer specific value
            // - The property attribute value
            // - A default generated from the field name
            var name = layerValue;
            if (string.IsNullOrEmpty(name))
            {
                var backupName = propertyValue;
                if (string.IsNullOrEmpty(backupName))
                    backupName = fieldName;
                name = $"{backupName}_Layer{layerIndex}";
            }
            return name;
        }
    }
}
