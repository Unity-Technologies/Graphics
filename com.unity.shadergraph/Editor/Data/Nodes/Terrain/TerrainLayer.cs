using System.Linq;
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

            var albedoNode = FindOutputSlot<MaterialSlot>(OutputAlbedoId);
            var normalNode = FindOutputSlot<MaterialSlot>(OutputNormalId);
            var metallicNode = FindOutputSlot<MaterialSlot>(OutputMetallicId);
            var smoothnessNode = FindOutputSlot<MaterialSlot>(OutputSmoothnessId);
            var occlusionNode = FindOutputSlot<MaterialSlot>(OutputOcclusionId);
            var alphaNode = FindOutputSlot<MaterialSlot>(OutputAlphaId);

            var albedoType = albedoNode.concreteValueType.ToShaderString();
            var normalType = normalNode.concreteValueType.ToShaderString();
            var metallicType = metallicNode.concreteValueType.ToShaderString();
            var smoothnessType = smoothnessNode.concreteValueType.ToShaderString();
            var occlusionType = occlusionNode.concreteValueType.ToShaderString();
            var alphaType = occlusionNode.concreteValueType.ToShaderString();

            var albedoValue = GetVariableNameForSlot(OutputAlbedoId);
            var normalValue = GetVariableNameForSlot(OutputNormalId);
            var metallicValue = GetVariableNameForSlot(OutputMetallicId);
            var smoothnessValue = GetVariableNameForSlot(OutputSmoothnessId);
            var occlusionValue = GetVariableNameForSlot(OutputOcclusionId);
            var alphaValue = GetVariableNameForSlot(OutputAlphaId);

            var albedoEdge = owner.GetEdges(albedoNode.slotReference);
            var normalEdge = owner.GetEdges(normalNode.slotReference);
            var metallicEdge = owner.GetEdges(metallicNode.slotReference);
            var smoothnessEdge = owner.GetEdges(smoothnessNode.slotReference);
            var occlusionEdge = owner.GetEdges(occlusionNode.slotReference);
            var alphaEdge = owner.GetEdges(alphaNode.slotReference);

            if (inputLayerIndex < 4)
            {
                sb.AppendLine("#if defined(UNIVERSAL_TERRAIN_ENABLED)");
                sb.IncreaseIndent();
                sb.AppendLine("#ifndef SPLAT{0}_ATTRIBUTES", inputLayerIndexValue);
                sb.AppendLine("half2 splat{0}uv = GetSplat{0}UV(IN);", inputLayerIndex);
                sb.AppendLine("half4 albedoSmoothness{0} = SampleLayerAlbedo({0});", inputLayerIndexValue);
                sb.AppendLine("half3 normal{0} = SampleLayerNormal({0});", inputLayerIndexValue);
                sb.AppendLine("half4 mask{0} = SampleLayerMasks({0});", inputLayerIndexValue);
                sb.AppendLine("half defaultSmoothness{0} = albedoSmoothness{0}.a * _Smoothness{0};", inputLayerIndexValue);
                sb.AppendLine("half defaultMetallic{0} = _Metallic{0};", inputLayerIndexValue);
                sb.AppendLine("half defaultOcclusion{0} = _MaskMapRemapScale{0}.g * _MaskMapRemapOffset{0}.g;", inputLayerIndexValue);
                sb.AppendLine("#endif // SPLAT{0}_ATTRIBUTES", inputLayerIndexValue);
                sb.DecreaseIndent();
                if (albedoEdge.Any())
                    sb.AppendLine("{0} {1} = albedoSmoothness{2}.rgb;", albedoType, albedoValue, inputLayerIndexValue);
                if (normalEdge.Any())
                    sb.AppendLine("{0} {1} = normal{2};", normalType, normalValue, inputLayerIndexValue);
                if (metallicEdge.Any())
                    sb.AppendLine("{0} {1} = lerp(defaultMetallic{2}, mask{2}.r, _LayerHasMask{2});", metallicType, metallicValue, inputLayerIndexValue);
                if (smoothnessEdge.Any())
                    sb.AppendLine("{0} {1} = lerp(defaultSmoothness{2}, mask{2}.a, _LayerHasMask{2});", smoothnessType, smoothnessValue, inputLayerIndexValue);
                if (occlusionEdge.Any())
                    sb.AppendLine("{0} {1} = lerp(defaultOcclusion{2}, mask{2}.g, _LayerHasMask{2});", occlusionType, occlusionValue, inputLayerIndexValue);
                if (alphaEdge.Any())
                    sb.AppendLine("{0} {1} = 1.0;", alphaType, alphaValue);
                sb.AppendLine("#elif defined(HD_TERRAIN_ENABLED)");
            }
            else
            {
                sb.AppendLine("#if defined(HD_TERRAIN_ENABLED) && defined(_TERRAIN_8_LAYERS)");
            }
            sb.IncreaseIndent();
            sb.AppendLine("#ifndef LAYER_ELEMENTS");
            sb.AppendLine("#define LAYER_ELEMENTS");
            sb.AppendLine("float4 albedo[_LAYER_COUNT];");
            sb.AppendLine("float3 normal[_LAYER_COUNT];");
            sb.AppendLine("float4 masks[_LAYER_COUNT];");
            sb.AppendLine("#endif // LAYER_ELEMENTS");
            sb.AppendLine("#ifndef SPLAT_DXDY");
            sb.AppendLine("#define SPLAT_DXDY");
            sb.AppendLine("float2 dxuv = ddx(IN.uv0.xy);");
            sb.AppendLine("float2 dyuv = ddy(IN.uv0.xy);");
            sb.AppendLine("#endif // SPLAT_DXDY");
            sb.AppendLine("#ifndef SPLAT{0}_ATTRIBUTES", inputLayerIndexValue);
            sb.AppendLine("float2 splat{0}uv = IN.uv0.xy * _Splat{0}_ST.xy + _Splat{0}_ST.zw;", inputLayerIndexValue);
            sb.AppendLine("float2 splat{0}dxuv = dxuv * _Splat{0}_ST.x;", inputLayerIndexValue);
            sb.AppendLine("float2 splat{0}dyuv = dyuv * _Splat{0}_ST.x;", inputLayerIndexValue);
            sb.AppendLine("");
            sb.AppendLine("albedo[{0}] = SampleLayerAlbedo({0});", inputLayerIndexValue);
            sb.AppendLine("normal[{0}] = SampleLayerNormal({0});", inputLayerIndexValue);
            sb.AppendLine("masks[{0}] = SampleLayerMasks({0});", inputLayerIndexValue);
            sb.AppendLine("#endif // SPLAT{0}_ATTRIBUTES", inputLayerIndexValue);
            sb.DecreaseIndent();
            if (albedoEdge.Any())
                sb.AppendLine("{0} {1} = albedo[{2}].xyz;", albedoType, albedoValue, inputLayerIndexValue);
            if (normalEdge.Any())
                sb.AppendLine("{0} {1} = normal[{2}];", normalType, normalValue, inputLayerIndexValue);
            if (metallicEdge.Any())
                sb.AppendLine("{0} {1} = masks[{2}].x;", metallicType, metallicValue, inputLayerIndexValue);
            if (smoothnessEdge.Any())
                sb.AppendLine("{0} {1} = masks[{2}].w;", smoothnessType, smoothnessValue, inputLayerIndexValue);
            if (occlusionEdge.Any())
                sb.AppendLine("{0} {1} = masks[{2}].y;", occlusionType, occlusionValue, inputLayerIndexValue);
            if (alphaEdge.Any())
                sb.AppendLine("{0} {1} = albedo[{2}].w;", alphaType, alphaValue, inputLayerIndexValue);
            sb.AppendLine("#else");
            if (albedoEdge.Any())
                sb.AppendLine("{0} {1} = 0.0;", albedoType, albedoValue);
            if (normalEdge.Any())
                sb.AppendLine("{0} {1} = 0.0;", normalType, normalValue);
            if (metallicEdge.Any())
                sb.AppendLine("{0} {1} = 0.0;", metallicType, metallicValue);
            if (smoothnessEdge.Any())
                sb.AppendLine("{0} {1} = 0.0;", smoothnessType, smoothnessValue);
            if (occlusionEdge.Any())
                sb.AppendLine("{0} {1} = 0.0;", occlusionType, occlusionValue);
            if (alphaEdge.Any())
                sb.AppendLine("{0} {1} = 0.0;", alphaType, alphaValue);
            if (inputLayerIndex < 4)
            {
                sb.AppendLine("#endif // UNIVERSAL_TERRAIN_ENABLED / HD_TERRAIN_ENABLED");
            }
            else
            {
                sb.AppendLine("#endif // HD_TERRAIN_ENABLED");
            }
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
