using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Terrain", "Terrain Layer")]
    class TerrainLayer : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireMeshUV // IGeneratesFunction
    {
        const int InputUVId = 0;
        const int InputControlId = 1;
        const int OutputAlbedoId = 2;
        const int OutputNormalId = 3;
        const int OutputMetallicId = 4;
        const int OutputSmoothnessId = 5;
        const int OutputOcclusionId = 6;

        const string kInputUVSlotName = "UV";
        const string kInputControlName = "Control";
        const string kOutputAlbedoSlotName = "Albedo";
        const string kOutputNormalSlotName = "Normal";
        const string kOutputMetallicSlotName = "Metallic";
        const string kOutputSmoothnessSlotName = "Smoothness";
        const string kOutputOcclusionSlotName = "Occlusion";

        private MaterialSlot m_AlbedoNode;
        private MaterialSlot m_NormalNode;
        private MaterialSlot m_MetallicNode;
        private MaterialSlot m_SmoothnessNode;
        private MaterialSlot m_OcclusionNode;

        private string m_AlbedoType;
        private string m_NormalType;
        private string m_MetallicType;
        private string m_SmoothnessType;
        private string m_OcclusionType;

        private string m_ControlValue;
        private string m_AlbedoValue;
        private string m_NormalValue;
        private string m_MetallicValue;
        private string m_SmoothnessValue;
        private string m_OcclusionValue;

        private IEnumerable<IEdge> m_AlbedoEdge;
        private IEnumerable<IEdge> m_NormalEdge;
        private IEnumerable<IEdge> m_MetallicEdge;
        private IEnumerable<IEdge> m_SmoothnessEdge;
        private IEnumerable<IEdge> m_OcclusionEdge;

        internal enum LayerIndex
        {
            Index0 = 0,
            Index1 = 1,
            Index2 = 2,
            Index3 = 3,
            Index4 = 4,
            Index5 = 5,
            Index6 = 6,
            Index7 = 7,
        }

        [SerializeField]
        private LayerIndex m_LayerIndex;

        [EnumControl("")]
        public LayerIndex layerIndex
        {
            get { return m_LayerIndex; }
            set { m_LayerIndex = value; Dirty(ModificationScope.Graph); }
        }

        public TerrainLayer()
        {
            name = "Terrain Layer";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new UVMaterialSlot(InputUVId, kInputUVSlotName, kInputUVSlotName, UVChannel.UV0));
            AddSlot(new Vector1MaterialSlot(InputControlId, kInputControlName, kInputControlName, SlotType.Input, 1));
            AddSlot(new Vector3MaterialSlot(OutputAlbedoId, kOutputAlbedoSlotName, kOutputAlbedoSlotName, SlotType.Output, Vector3.zero));
            AddSlot(new Vector3MaterialSlot(OutputNormalId, kOutputNormalSlotName, kOutputNormalSlotName, SlotType.Output, Vector3.zero));
            AddSlot(new Vector1MaterialSlot(OutputMetallicId, kOutputMetallicSlotName, kOutputMetallicSlotName, SlotType.Output, 0));
            AddSlot(new Vector1MaterialSlot(OutputSmoothnessId, kOutputSmoothnessSlotName, kOutputSmoothnessSlotName, SlotType.Output, 0));
            AddSlot(new Vector1MaterialSlot(OutputOcclusionId, kOutputOcclusionSlotName, kOutputOcclusionSlotName, SlotType.Output, 0));

            RemoveSlotsNameNotMatching(new[] { InputUVId, InputControlId, OutputAlbedoId, OutputNormalId, OutputMetallicId, OutputSmoothnessId, OutputOcclusionId, });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            var inputLayerIndex = (int)layerIndex;

            // pre-accusitions
            m_AlbedoNode = FindOutputSlot<MaterialSlot>(OutputAlbedoId);
            m_NormalNode = FindOutputSlot<MaterialSlot>(OutputNormalId);
            m_MetallicNode = FindOutputSlot<MaterialSlot>(OutputMetallicId);
            m_SmoothnessNode = FindOutputSlot<MaterialSlot>(OutputSmoothnessId);
            m_OcclusionNode = FindOutputSlot<MaterialSlot>(OutputOcclusionId);

            m_AlbedoType = m_AlbedoNode.concreteValueType.ToShaderString();
            m_NormalType = m_NormalNode.concreteValueType.ToShaderString();
            m_MetallicType = m_MetallicNode.concreteValueType.ToShaderString();
            m_SmoothnessType = m_SmoothnessNode.concreteValueType.ToShaderString();
            m_OcclusionType = m_OcclusionNode.concreteValueType.ToShaderString();

            m_ControlValue = GetSlotValue(InputControlId, generationMode);
            m_AlbedoValue = GetVariableNameForSlot(OutputAlbedoId);
            m_NormalValue = GetVariableNameForSlot(OutputNormalId);
            m_MetallicValue = GetVariableNameForSlot(OutputMetallicId);
            m_SmoothnessValue = GetVariableNameForSlot(OutputSmoothnessId);
            m_OcclusionValue = GetVariableNameForSlot(OutputOcclusionId);

            m_AlbedoEdge = owner.GetEdges(m_AlbedoNode.slotReference);
            m_NormalEdge = owner.GetEdges(m_NormalNode.slotReference);
            m_MetallicEdge = owner.GetEdges(m_MetallicNode.slotReference);
            m_SmoothnessEdge = owner.GetEdges(m_SmoothnessNode.slotReference);
            m_OcclusionEdge = owner.GetEdges(m_OcclusionNode.slotReference);

            GenerateNodeCodeInUniversalTerrain(sb, inputLayerIndex);
            GenerateNodeCodeInHDTerrain(sb, inputLayerIndex);
            GenerateNodeCodeInNullTerrain(sb);
        }

        private void GenerateNodeCodeInUniversalTerrain(ShaderStringBuilder sb, int inputLayerIndex)
        {
            string universalTerrainDef = "#if defined(UNIVERSAL_TERRAIN_ENABLED)";

            sb.AppendLine(universalTerrainDef);
            sb.IncreaseIndent();
            sb.AppendLine("#ifndef SPLAT_PREREQUISITES");
            sb.AppendLine("#define SPLAT_PREREQUISITES");
            sb.AppendLine("DECLARE_SPLAT_PREREQUISITES");
            sb.AppendLine("#endif // SPLAT_PREREQUISITES");
            sb.AppendLine("#ifndef SPLAT{0}_ATTRIBUTES", inputLayerIndex);
            sb.AppendLine("#define SPLAT{0}_ATTRIBUTES", inputLayerIndex);
            if (inputLayerIndex < 4)
                sb.AppendLine("DECLARE_AND_FETCH_SPLAT_ATTRIBUTES({0})", inputLayerIndex);
            else
                sb.AppendLine("DECLARE_AND_FETCH_SPLAT_ATTRIBUTES_8LAYERS({0}, {1})", inputLayerIndex, inputLayerIndex - 4);
            sb.AppendLine("#endif // SPLAT{0}_ATTRIBUTES", inputLayerIndex);
            sb.DecreaseIndent();

            if (m_AlbedoEdge.Any()) sb.AppendLine("{0} {1} = FetchLayerAlbedo({2}, {3});", m_AlbedoType, m_AlbedoValue, inputLayerIndex, m_ControlValue);
            if (m_NormalEdge.Any()) sb.AppendLine("{0} {1} = FetchLayerNormal({2}, {3});", m_NormalType, m_NormalValue, inputLayerIndex, m_ControlValue);
            if (m_MetallicEdge.Any()) sb.AppendLine("{0} {1} = FetchLayerMetallic({2}, {3});", m_MetallicType, m_MetallicValue, inputLayerIndex, m_ControlValue);
            if (m_SmoothnessEdge.Any()) sb.AppendLine("{0} {1} = FetchLayerSmoothness({2}, {3});", m_SmoothnessType, m_SmoothnessValue, inputLayerIndex, m_ControlValue);
            if (m_OcclusionEdge.Any()) sb.AppendLine("{0} {1} = FetchLayerOcclusion({2}, {3});", m_OcclusionType, m_OcclusionValue, inputLayerIndex, m_ControlValue);
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
            sb.AppendLine("DECLARE_SPLAT_PREREQUISITES");
            sb.AppendLine("#endif // SPLAT_PREREQUISITES");
            sb.AppendLine("#ifndef SPLAT{0}_ATTRIBUTES", inputLayerIndex);
            sb.AppendLine("#define SPLAT{0}_ATTRIBUTES", inputLayerIndex);
            sb.AppendLine("DECLARE_AND_FETCH_SPLAT_ATTRIBUTES({0})", inputLayerIndex);
            sb.AppendLine("#endif // SPLAT{0}_ATTRIBUTES", inputLayerIndex);
            sb.DecreaseIndent();

            if (m_AlbedoEdge.Any()) sb.AppendLine("{0} {1} = albedo[{2}].xyz * {3};", m_AlbedoType, m_AlbedoValue, inputLayerIndex, m_ControlValue);
            if (m_NormalEdge.Any()) sb.AppendLine("{0} {1} = normal[{2}] * {3};", m_NormalType, m_NormalValue, inputLayerIndex, m_ControlValue);
            if (m_MetallicEdge.Any()) sb.AppendLine("{0} {1} = masks[{2}].x * {3};", m_MetallicType, m_MetallicValue, inputLayerIndex, m_ControlValue);
            if (m_SmoothnessEdge.Any()) sb.AppendLine("{0} {1} = masks[{2}].w * {3};", m_SmoothnessType, m_SmoothnessValue, inputLayerIndex, m_ControlValue);
            if (m_OcclusionEdge.Any()) sb.AppendLine("{0} {1} = masks[{2}].y * {3};", m_OcclusionType, m_OcclusionValue, inputLayerIndex, m_ControlValue);
        }

        private void GenerateNodeCodeInNullTerrain(ShaderStringBuilder sb)
        {
            sb.AppendLine("#else");

            if (m_AlbedoEdge.Any()) sb.AppendLine("{0} {1} = 0.0;", m_AlbedoType, m_AlbedoValue);
            if (m_NormalEdge.Any()) sb.AppendLine("{0} {1} = 0.0;", m_NormalType, m_NormalValue);
            if (m_MetallicEdge.Any()) sb.AppendLine("{0} {1} = 0.0;", m_MetallicType, m_MetallicValue);
            if (m_SmoothnessEdge.Any()) sb.AppendLine("{0} {1} = 0.0;", m_SmoothnessType, m_SmoothnessValue);
            if (m_OcclusionEdge.Any()) sb.AppendLine("{0} {1} = 0.0;", m_OcclusionType, m_OcclusionValue);
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
