using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Terrain", "Terrain Layer")]
    class TerrainLayer : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireMeshUV // IGeneratesFunction
    {
        const int InputUVId = 0;
        const int InputLayerId = 1;
        const int OutputAlbedoId = 2;
        const int OutputNormalId = 3;
        const int OutputMetallicId = 4;
        const int OutputSmoothnessId = 5;
        const int OutputOcclusionId = 6;
        const int OutputAlphaId = 7;

        const string kInputUVSlotName = "UV";
        const string kInputLayerSlotName = "Layer Index";
        const string kOutputAlbedoSlotName = "Albedo";
        const string kOutputNormalSlotName = "Normal";
        const string kOutputMetallicSlotName = "Metallic";
        const string kOutputSmoothnessSlotName = "Smoothness";
        const string kOutputOcclusionSlotName = "Occlusion";
        const string kOutputAlphaSlotName = "Alpha";

        public TerrainLayer()
        {
            name = "Terrain Layer";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new UVMaterialSlot(InputUVId, kInputUVSlotName, kInputUVSlotName, UVChannel.UV0));
            AddSlot(new Vector1MaterialSlot(InputLayerId, kInputLayerSlotName, kInputLayerSlotName, SlotType.Input, 0));
            AddSlot(new Vector3MaterialSlot(OutputAlbedoId, kOutputAlbedoSlotName, kOutputAlbedoSlotName, SlotType.Output, Vector3.zero));
            AddSlot(new Vector3MaterialSlot(OutputNormalId, kOutputNormalSlotName, kOutputNormalSlotName, SlotType.Output, Vector3.zero));
            AddSlot(new Vector1MaterialSlot(OutputMetallicId, kOutputMetallicSlotName, kOutputMetallicSlotName, SlotType.Output, 0));
            AddSlot(new Vector1MaterialSlot(OutputSmoothnessId, kOutputSmoothnessSlotName, kOutputSmoothnessSlotName, SlotType.Output, 0));
            AddSlot(new Vector1MaterialSlot(OutputOcclusionId, kOutputOcclusionSlotName, kOutputOcclusionSlotName, SlotType.Output, 0));
            AddSlot(new Vector1MaterialSlot(OutputAlphaId, kOutputAlphaSlotName, kOutputAlphaSlotName, SlotType.Output, 0));

            RemoveSlotsNameNotMatching(new[] { InputUVId, InputLayerId, OutputAlbedoId, OutputNormalId, OutputMetallicId, OutputSmoothnessId, OutputOcclusionId, OutputAlphaId, });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            var inputLayerIndexValue = GetSlotValue(InputLayerId, GenerationMode.ForReals);
            var inputLayerIndex = int.Parse(inputLayerIndexValue);

            var albedoType = FindOutputSlot<MaterialSlot>(OutputAlbedoId).concreteValueType.ToShaderString();
            var normalType = FindOutputSlot<MaterialSlot>(OutputNormalId).concreteValueType.ToShaderString();
            var metallicType = FindOutputSlot<MaterialSlot>(OutputMetallicId).concreteValueType.ToShaderString();
            var smoothnessType = FindOutputSlot<MaterialSlot>(OutputSmoothnessId).concreteValueType.ToShaderString();
            var occlusionType = FindOutputSlot<MaterialSlot>(OutputOcclusionId).concreteValueType.ToShaderString();
            var alphaType = FindOutputSlot<MaterialSlot>(OutputAlphaId).concreteValueType.ToShaderString();

            var albedoValue = GetVariableNameForSlot(OutputAlbedoId);
            var normalValue = GetVariableNameForSlot(OutputNormalId);
            var metallicValue = GetVariableNameForSlot(OutputMetallicId);
            var smoothnessValue = GetVariableNameForSlot(OutputSmoothnessId);
            var occlusionValue = GetVariableNameForSlot(OutputOcclusionId);
            var alphaValue = GetVariableNameForSlot(OutputAlphaId);

            sb.AppendLine("");
            if (inputLayerIndex < 4)
                sb.AppendLine("#if defined(UNIVERSAL_TERRAIN_ENABLED)");
            else
                sb.AppendLine("#if defined(UNIVERSAL_TERRAIN_ENABLED) && defined(_TERRAIN_8_LAYERS)");
            sb.AppendLine("{0} {1} = 0.0;", albedoType, albedoValue);
            sb.AppendLine("{0} {1} = 0.0;", normalType, normalValue);
            sb.AppendLine("{0} {1} = 0.0;", metallicType, metallicValue);
            sb.AppendLine("{0} {1} = 0.0;", smoothnessType, smoothnessValue);
            sb.AppendLine("{0} {1} = 0.0;", occlusionType, occlusionValue);
            sb.AppendLine("{0} {1} = 0.0;", alphaType, alphaValue);
            if (inputLayerIndex < 4)
                sb.AppendLine("#elif defined(HD_TERRAIN_ENABLED)");
            else
                sb.AppendLine("#elif defined(HD_TERRAIN_ENABLED) && defined(_TERRAIN_8_LAYERS)");
            sb.AppendLine("    #ifndef SPLAT_DXDY");
            sb.AppendLine("    #define SPLAT_DXDY");
            sb.AppendLine("float2 dxuv = ddx(IN.uv0.xy);");
            sb.AppendLine("float2 dyuv = ddy(IN.uv0.xy);");
            sb.AppendLine("    #endif // SPLAT_DXDY");
            sb.AppendLine("");
            sb.AppendLine("float2 splat{0}uv = IN.uv0.xy * _Splat{0}_ST.xy + _Splat{0}_ST.zw;", inputLayerIndexValue);
            sb.AppendLine("float2 splat{0}dxuv = dxuv * _Splat{0}_ST.x;", inputLayerIndexValue);
            sb.AppendLine("float2 splat{0}dyuv = dyuv * _Splat{0}_ST.x;", inputLayerIndexValue);
            sb.AppendLine("");
            sb.AppendLine("    #ifndef LAYER_ELEMENTS");
            sb.AppendLine("    #define LAYER_ELEMENTS");
            sb.AppendLine("float4 albedo[_LAYER_COUNT];");
            sb.AppendLine("float3 normal[_LAYER_COUNT];");
            sb.AppendLine("float4 masks[_LAYER_COUNT];");
            sb.AppendLine("    #endif // LAYER_ELEMENTS");
            sb.AppendLine("");
            sb.AppendLine("albedo[{0}] = SampleAlbedo({0});", inputLayerIndexValue);
            sb.AppendLine("normal[{0}] = SampleNormal({0});", inputLayerIndexValue);
            sb.AppendLine("masks[{0}] = SampleMasks({0});", inputLayerIndexValue);
            sb.AppendLine("{0} {1} = albedo[{2}].xyz;", albedoType, albedoValue, inputLayerIndexValue);
            sb.AppendLine("{0} {1} = normal[{2}];", normalType, normalValue, inputLayerIndexValue);
            sb.AppendLine("{0} {1} = masks[{2}].x;", metallicType, metallicValue, inputLayerIndexValue);
            sb.AppendLine("{0} {1} = masks[{2}].z;", smoothnessType, smoothnessValue, inputLayerIndexValue);
            sb.AppendLine("{0} {1} = masks[{2}].y;", occlusionType, occlusionValue, inputLayerIndexValue);
            sb.AppendLine("{0} {1} = albedo[{2}].w;", alphaType, alphaValue, inputLayerIndexValue);
            sb.AppendLine("#else");
            sb.AppendLine("{0} {1} = 0.0;", albedoType, albedoValue);
            sb.AppendLine("{0} {1} = 0.0;", normalType, normalValue);
            sb.AppendLine("{0} {1} = 0.0;", metallicType, metallicValue);
            sb.AppendLine("{0} {1} = 0.0;", smoothnessType, smoothnessValue);
            sb.AppendLine("{0} {1} = 0.0;", occlusionType, occlusionValue);
            sb.AppendLine("{0} {1} = 0.0;", alphaType, alphaValue);
            sb.AppendLine("#endif // UNIVERSAL_TERRAIN_ENABLED / HD_TERRAIN_ENABLED");
        }

        public bool RequiresMeshUV(UVChannel channel, ShaderStageCapability stageCapability)
        {
            using (var tempSlots = PooledList<MaterialSlot>.Get())
            {
                GetInputSlots(tempSlots);
                foreach (var slot in tempSlots)
                {
                    if (slot.RequiresMeshUV(channel))
                        return true;
                }

                return false;
            }
        }
    }
}
