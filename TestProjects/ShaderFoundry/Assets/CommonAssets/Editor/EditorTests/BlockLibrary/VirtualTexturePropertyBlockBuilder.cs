using System.Collections.Generic;
using UnityEditor.ShaderFoundry;
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
            internal LayerTextureType? TextureType = null;
        }

        const string VirtualTexturePropertyTypeName = "VTPropertyWithTextureType";
        public int LayerCount = 2;
        public int LayerToSample = 0;
        
        public List<LayerData> Layers = new List<LayerData>
        {
            new LayerData(),
            new LayerData(),
            new LayerData(),
            new LayerData(),
        };

        public static string GetName(int layerIndex, string layerValue, string propertyValue, string fieldName)
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

        public string GetUniformName(int layerIndex)
        {
            return GetName(layerIndex, Layers[layerIndex]?.UniformName, PropertyAttribute?.UniformName, FieldName);
        }

        public string GetDisplayName(int layerIndex)
        {
            return GetName(layerIndex, Layers[layerIndex]?.DisplayName, PropertyAttribute?.DisplayName, FieldName);
        }

        public string GetTextureDefault(int layerIndex)
        {
            return Layers[layerIndex]?.TextureName ?? PropertyAttribute?.DefaultValue ?? "\"\" {}";
        }

        public VirtualTexturePropertyBlockBuilder()
        {
            BlockName = "VirtualTextureProperty";
            FieldName = "FieldVirtualTexture";
            //PropertyAttribute = new PropertyAttributeData() { DefaultValue = "\"\" {}" };
        }

        public Block Build(ShaderContainer container)
        {
            var attributeBuilder = new VirtualTextureAttribute { LayerCount = LayerCount };
            var attribute = attributeBuilder.Build(container);
            var attributes = new List<ShaderAttribute>();
            attributes.Add(attribute);
            for(var i = 0; i < Layers.Count; ++i)
            {
                var layerData = Layers[i];
                var layerAttribute = new VirtualTextureLayerAttribute
                {
                    Index = i,
                    UniformName = layerData.UniformName,
                    DisplayName = layerData.DisplayName,
                    TextureName = layerData.TextureName,
                    TextureType = layerData?.TextureType ?? LayerTextureType.Default,
                };

                attributes.Add(layerAttribute.Build(container));
            }

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

        static ShaderType GetVirtualTextureType(ShaderContainer container)
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
            functionBuilder.AddLine("vtParams.levelMode = VtLevel_Lod;");
            functionBuilder.AddLine("vtParams.lodOrOffset = 0.0f;");
            functionBuilder.AddLine("}");
            functionBuilder.AddLine("#endif");
            functionBuilder.AddLine($"StackInfo info = PrepareVT({propName}.vtProperty, vtParams);");
            functionBuilder.AddLine($"{layerOutName} = SampleVTLayerWithTextureType({propName}, vtParams, info, {layer});");
            functionBuilder.AddLine("return GetResolveOutput(info);");
            
            var result = functionBuilder.Build();
            blockBuilder.AddFunction(result);
            return result;
        }
    }
}
