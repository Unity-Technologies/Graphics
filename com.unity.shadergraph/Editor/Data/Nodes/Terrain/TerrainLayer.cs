using System.Collections.Generic;
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

        private MaterialSlot m_AlbedoNode;
        private MaterialSlot m_NormalNode;
        private MaterialSlot m_MetallicNode;
        private MaterialSlot m_SmoothnessNode;
        private MaterialSlot m_OcclusionNode;
        private MaterialSlot m_AlphaNode;

        private string m_AlbedoType;
        private string m_NormalType;
        private string m_MetallicType;
        private string m_SmoothnessType;
        private string m_OcclusionType;
        private string m_AlphaType;

        private string m_AlbedoValue;
        private string m_NormalValue;
        private string m_MetallicValue;
        private string m_SmoothnessValue;
        private string m_OcclusionValue;
        private string m_AlphaValue;

        private IEnumerable<IEdge> m_AlbedoEdge;
        private IEnumerable<IEdge> m_NormalEdge;
        private IEnumerable<IEdge> m_MetallicEdge;
        private IEnumerable<IEdge> m_SmoothnessEdge;
        private IEnumerable<IEdge> m_OcclusionEdge;
        private IEnumerable<IEdge> m_AlphaEdge;

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

            // pre-accusitions
            m_AlbedoNode = FindOutputSlot<MaterialSlot>(OutputAlbedoId);
            m_NormalNode = FindOutputSlot<MaterialSlot>(OutputNormalId);
            m_MetallicNode = FindOutputSlot<MaterialSlot>(OutputMetallicId);
            m_SmoothnessNode = FindOutputSlot<MaterialSlot>(OutputSmoothnessId);
            m_OcclusionNode = FindOutputSlot<MaterialSlot>(OutputOcclusionId);
            m_AlphaNode = FindOutputSlot<MaterialSlot>(OutputAlphaId);

            m_AlbedoType = m_AlbedoNode.concreteValueType.ToShaderString();
            m_NormalType = m_NormalNode.concreteValueType.ToShaderString();
            m_MetallicType = m_MetallicNode.concreteValueType.ToShaderString();
            m_SmoothnessType = m_SmoothnessNode.concreteValueType.ToShaderString();
            m_OcclusionType = m_OcclusionNode.concreteValueType.ToShaderString();
            m_AlphaType = m_AlphaNode.concreteValueType.ToShaderString();

            m_AlbedoValue = GetVariableNameForSlot(OutputAlbedoId);
            m_NormalValue = GetVariableNameForSlot(OutputNormalId);
            m_MetallicValue = GetVariableNameForSlot(OutputMetallicId);
            m_SmoothnessValue = GetVariableNameForSlot(OutputSmoothnessId);
            m_OcclusionValue = GetVariableNameForSlot(OutputOcclusionId);
            m_AlphaValue = GetVariableNameForSlot(OutputAlphaId);

            m_AlbedoEdge = owner.GetEdges(m_AlbedoNode.slotReference);
            m_NormalEdge = owner.GetEdges(m_NormalNode.slotReference);
            m_MetallicEdge = owner.GetEdges(m_MetallicNode.slotReference);
            m_SmoothnessEdge = owner.GetEdges(m_SmoothnessNode.slotReference);
            m_OcclusionEdge = owner.GetEdges(m_OcclusionNode.slotReference);
            m_AlphaEdge = owner.GetEdges(m_AlphaNode.slotReference);

            GenerateNodeCodeInUniversalTerrain(sb, inputLayerIndex);
            GenerateNodeCodeInUniversalTerrainBaseMapGen(sb, inputLayerIndex);
            GenerateNodeCodeInHDTerrain(sb, inputLayerIndex);
            GenerateNodeCodeInNullTerrain(sb);
        }

        private void GenerateNodeCodeInUniversalTerrain(ShaderStringBuilder sb, int inputLayerIndex)
        {
            string universalTerrainDef = inputLayerIndex < 4
                ? "#if defined(UNIVERSAL_TERRAIN_ENABLED) && !defined(_TERRAIN_BASEMAP_GEN) && !defined(TERRAIN_SPLAT_ADDPASS)"
                : "#if defined(UNIVERSAL_TERRAIN_ENABLED) && !defined(_TERRAIN_BASEMAP_GEN) &&  defined(TERRAIN_SPLAT_ADDPASS)";
            int layerIndex = inputLayerIndex < 4 ? inputLayerIndex : (inputLayerIndex - 4);

            sb.AppendLine(universalTerrainDef);
            sb.IncreaseIndent();
            sb.AppendLine("#ifndef SPLAT{0}_ATTRIBUTES", layerIndex);
            sb.AppendLine("#define SPLAT{0}_ATTRIBUTES", layerIndex);
            sb.AppendLine("DECLARE_AND_FETCH_SPLAT_ATTRIBUTES({0})", layerIndex);
            sb.AppendLine("#endif // SPLAT{0}_ATTRIBUTES", layerIndex);
            sb.DecreaseIndent();

            if (m_AlbedoEdge.Any()) sb.AppendLine("{0} {1} = FetchLayerAlbedo({2});", m_AlbedoType, m_AlbedoValue, layerIndex);
            if (m_NormalEdge.Any()) sb.AppendLine("{0} {1} = FetchLayerNormal({2});", m_NormalType, m_NormalValue, layerIndex);
            if (m_MetallicEdge.Any()) sb.AppendLine("{0} {1} = FetchLayerMetallic({2});", m_MetallicType, m_MetallicValue, layerIndex);
            if (m_SmoothnessEdge.Any()) sb.AppendLine("{0} {1} = FetchLayerSmoothness({2});", m_SmoothnessType, m_SmoothnessValue, layerIndex);
            if (m_OcclusionEdge.Any()) sb.AppendLine("{0} {1} = FetchLayerOcclusion({2});", m_OcclusionType, m_OcclusionValue, layerIndex);
            if (m_AlphaEdge.Any()) sb.AppendLine("{0} {1} = 1.0;", m_AlphaType, m_AlphaValue);
        }

        private void GenerateNodeCodeInUniversalTerrainBaseMapGen(ShaderStringBuilder sb, int inputLayerIndex)
        {
            string universalTerrainDef = "#elif defined(UNIVERSAL_TERRAIN_ENABLED) && defined(_TERRAIN_BASEMAP_GEN)";
            int layerIndex = inputLayerIndex < 4 ? inputLayerIndex : (inputLayerIndex - 4);

            sb.AppendLine(universalTerrainDef);
            sb.IncreaseIndent();
            sb.AppendLine("#ifndef SPLAT{0}_ATTRIBUTES", layerIndex);
            sb.AppendLine("#define SPLAT{0}_ATTRIBUTES", layerIndex);
            sb.AppendLine("DECLARE_AND_FETCH_SPLAT_ATTRIBUTES({0});", layerIndex);
            sb.AppendLine("#else");
            sb.AppendLine("FETCH_SPLAT_ATTRIBUTES({0});", layerIndex);
            sb.AppendLine("#endif // SPLAT{0}_ATTRIBUTES", layerIndex);
            sb.DecreaseIndent();

            if (inputLayerIndex < 4)
            {
                if (m_AlbedoEdge.Any()) sb.AppendLine("{0} {1} = lerp(FetchLayerAlbedo({2}), 0.0, _DstBlend);", m_AlbedoType, m_AlbedoValue, layerIndex);
                if (m_NormalEdge.Any()) sb.AppendLine("{0} {1} = lerp(FetchLayerNormal({2}), 0.0, _DstBlend);", m_NormalType, m_NormalValue, layerIndex);
                if (m_MetallicEdge.Any()) sb.AppendLine("{0} {1} = lerp(FetchLayerMetallic({2}), 0.0, _DstBlend);", m_MetallicType, m_MetallicValue, layerIndex);
                if (m_SmoothnessEdge.Any()) sb.AppendLine("{0} {1} = lerp(FetchLayerSmoothness({2}), 0.0, _DstBlend);", m_SmoothnessType, m_SmoothnessValue, layerIndex);
                if (m_OcclusionEdge.Any()) sb.AppendLine("{0} {1} = lerp(FetchLayerOcclusion({2}), 0.0, _DstBlend);", m_OcclusionType, m_OcclusionValue, layerIndex);
                if (m_AlphaEdge.Any()) sb.AppendLine("{0} {1} = 1.0;", m_AlphaType, m_AlphaValue);
            }
            else
            {
                if (m_AlbedoEdge.Any()) sb.AppendLine("{0} {1} = lerp(0.0, FetchLayerAlbedo({2}), _DstBlend);", m_AlbedoType, m_AlbedoValue, layerIndex);
                if (m_NormalEdge.Any()) sb.AppendLine("{0} {1} = lerp(0.0, FetchLayerNormal({2}), _DstBlend);", m_NormalType, m_NormalValue, layerIndex);
                if (m_MetallicEdge.Any()) sb.AppendLine("{0} {1} = lerp(0.0, FetchLayerMetallic({2}), _DstBlend);", m_MetallicType, m_MetallicValue, layerIndex);
                if (m_SmoothnessEdge.Any()) sb.AppendLine("{0} {1} = lerp(0.0, FetchLayerSmoothness({2}), _DstBlend);", m_SmoothnessType, m_SmoothnessValue, layerIndex);
                if (m_OcclusionEdge.Any()) sb.AppendLine("{0} {1} = lerp(0.0, FetchLayerOcclusion({2}), _DstBlend);", m_OcclusionType, m_OcclusionValue, layerIndex);
                if (m_AlphaEdge.Any()) sb.AppendLine("{0} {1} = 1.0;", m_AlphaType, m_AlphaValue);
            }
        }

        private void GenerateNodeCodeInHDTerrain(ShaderStringBuilder sb, int inputLayerIndex)
        {
            string hdTerrainDef = inputLayerIndex < 4
                ? "#elif defined(HD_TERRAIN_ENABLED)"
                : "#elif defined(HD_TERRAIN_ENABLED) && defined(_TERRAIN_8_LAYERS)";

            sb.AppendLine(hdTerrainDef);
            sb.IncreaseIndent();
            sb.AppendLine("#ifndef SPLAT_PREREQUISITES");
            sb.AppendLine("#define SPLAT_PREREQUISITES");
            sb.AppendLine("float4 albedo[_LAYER_COUNT];");
            sb.AppendLine("float3 m_Normal[_LAYER_COUNT];");
            sb.AppendLine("float4 masks[_LAYER_COUNT];");
            sb.AppendLine("float2 dxuv = ddx(IN.uv0.xy);");
            sb.AppendLine("float2 dyuv = ddy(IN.uv0.xy);");
            sb.AppendLine("#endif // SPLAT_PREREQUISITES");
            sb.AppendLine("#ifndef SPLAT{0}_ATTRIBUTES", inputLayerIndex);
            sb.AppendLine("#define SPLAT{0}_ATTRIBUTES", inputLayerIndex);
            sb.AppendLine("float2 splat{0}uv = IN.uv0.xy * _Splat{0}_ST.xy + _Splat{0}_ST.zw;", inputLayerIndex);
            sb.AppendLine("float2 splat{0}dxuv = dxuv * _Splat{0}_ST.x;", inputLayerIndex);
            sb.AppendLine("float2 splat{0}dyuv = dyuv * _Splat{0}_ST.x;", inputLayerIndex);
            sb.AppendLine("");
            sb.AppendLine("albedo[{0}] = SampleLayerAlbedo({0});", inputLayerIndex);
            sb.AppendLine("m_Normal[{0}] = SampleLayerNormal({0});", inputLayerIndex);
            sb.AppendLine("masks[{0}] = SampleLayerMasks({0});", inputLayerIndex);
            sb.AppendLine("#endif // SPLAT{0}_ATTRIBUTES", inputLayerIndex);
            sb.DecreaseIndent();

            if (m_AlbedoEdge.Any()) sb.AppendLine("{0} {1} = albedo[{2}].xyz;", m_AlbedoType, m_AlbedoValue, inputLayerIndex);
            if (m_NormalEdge.Any()) sb.AppendLine("{0} {1} = m_Normal[{2}];", m_NormalType, m_NormalValue, inputLayerIndex);
            if (m_MetallicEdge.Any()) sb.AppendLine("{0} {1} = masks[{2}].x;", m_MetallicType, m_MetallicValue, inputLayerIndex);
            if (m_SmoothnessEdge.Any()) sb.AppendLine("{0} {1} = masks[{2}].w;", m_SmoothnessType, m_SmoothnessValue, inputLayerIndex);
            if (m_OcclusionEdge.Any()) sb.AppendLine("{0} {1} = masks[{2}].y;", m_OcclusionType, m_OcclusionValue, inputLayerIndex);
            if (m_AlphaEdge.Any()) sb.AppendLine("{0} {1} = albedo[{2}].w;", m_AlphaType, m_AlphaValue, inputLayerIndex);
        }

        private void GenerateNodeCodeInNullTerrain(ShaderStringBuilder sb)
        {
            sb.AppendLine("#else");

            if (m_AlbedoEdge.Any()) sb.AppendLine("{0} {1} = 0.0;", m_AlbedoType, m_AlbedoValue);
            if (m_NormalEdge.Any()) sb.AppendLine("{0} {1} = 0.0;", m_NormalType, m_NormalValue);
            if (m_MetallicEdge.Any()) sb.AppendLine("{0} {1} = 0.0;", m_MetallicType, m_MetallicValue);
            if (m_SmoothnessEdge.Any()) sb.AppendLine("{0} {1} = 0.0;", m_SmoothnessType, m_SmoothnessValue);
            if (m_OcclusionEdge.Any()) sb.AppendLine("{0} {1} = 0.0;", m_OcclusionType, m_OcclusionValue);
            if (m_AlphaEdge.Any()) sb.AppendLine("{0} {1} = 0.0;", m_AlphaType, m_AlphaValue);
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
