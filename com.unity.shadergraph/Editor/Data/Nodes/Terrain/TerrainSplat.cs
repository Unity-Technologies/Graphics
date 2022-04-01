using System.Collections.Generic;
using System.Linq;
using UnityEditor.Graphing;
using UnityEngine;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    [Title("Terrain", "Terrain Splat")]
    class TerrainSplat : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireMeshUV // IGeneratesFunction
    {
        const int InputUVId = 0;
        const int InputSplatId = 1;
        const int OutputControlRId = 2;
        const int OutputControlGId = 3;
        const int OutputControlBId = 4;
        const int OutputControlAId = 5;

        const string kInputUVSlotName = "UV";
        const string kInputSplatSlotName = "Splat Index";
        const string kOutputControlRSlotName = "Control(r)";
        const string kOutputControlGSlotName = "Control(g)";
        const string kOutputControlBSlotName = "Control(b)";
        const string kOutputControlASlotName = "Control(a)";

        private MaterialSlot m_ControlRNode;
        private MaterialSlot m_ControlGNode;
        private MaterialSlot m_ControlBNode;
        private MaterialSlot m_ControlANode;

        private string m_ControlRType;
        private string m_ControlGType;
        private string m_ControlBType;
        private string m_ControlAType;

        private string m_ControlRValue;
        private string m_ControlGValue;
        private string m_ControlBValue;
        private string m_ControlAValue;

        private IEnumerable<IEdge> m_ControlREdge;
        private IEnumerable<IEdge> m_ControlGEdge;
        private IEnumerable<IEdge> m_ControlBEdge;
        private IEnumerable<IEdge> m_ControlAEdge;

        public TerrainSplat()
        {
            name = "Terrain Splat";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new UVMaterialSlot(InputUVId, kInputUVSlotName, kInputUVSlotName, UVChannel.UV0));
            AddSlot(new Vector1MaterialSlot(InputSplatId, kInputSplatSlotName, kInputSplatSlotName, SlotType.Input, 0));
            AddSlot(new Vector1MaterialSlot(OutputControlRId, kOutputControlRSlotName, kOutputControlRSlotName, SlotType.Output, 0));
            AddSlot(new Vector1MaterialSlot(OutputControlGId, kOutputControlGSlotName, kOutputControlGSlotName, SlotType.Output, 0));
            AddSlot(new Vector1MaterialSlot(OutputControlBId, kOutputControlBSlotName, kOutputControlBSlotName, SlotType.Output, 0));
            AddSlot(new Vector1MaterialSlot(OutputControlAId, kOutputControlASlotName, kOutputControlASlotName, SlotType.Output, 0));

            RemoveSlotsNameNotMatching(new[] { InputUVId, InputSplatId, OutputControlRId, OutputControlGId, OutputControlBId, OutputControlAId, });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            var inputSplatIndexValue = GetSlotValue(InputSplatId, GenerationMode.ForReals);
            var inputSplatIndex = int.Parse(inputSplatIndexValue);

            // pre-accusitions
            m_ControlRNode = FindOutputSlot<MaterialSlot>(OutputControlRId);
            m_ControlGNode = FindOutputSlot<MaterialSlot>(OutputControlGId);
            m_ControlBNode = FindOutputSlot<MaterialSlot>(OutputControlBId);
            m_ControlANode = FindOutputSlot<MaterialSlot>(OutputControlAId);

            m_ControlRType = m_ControlRNode.concreteValueType.ToShaderString();
            m_ControlGType = m_ControlGNode.concreteValueType.ToShaderString();
            m_ControlBType = m_ControlBNode.concreteValueType.ToShaderString();
            m_ControlAType = m_ControlANode.concreteValueType.ToShaderString();

            m_ControlRValue = GetVariableNameForSlot(OutputControlRId);
            m_ControlGValue = GetVariableNameForSlot(OutputControlGId);
            m_ControlBValue = GetVariableNameForSlot(OutputControlBId);
            m_ControlAValue = GetVariableNameForSlot(OutputControlAId);

            m_ControlREdge = owner.GetEdges(m_ControlRNode.slotReference);
            m_ControlGEdge = owner.GetEdges(m_ControlGNode.slotReference);
            m_ControlBEdge = owner.GetEdges(m_ControlBNode.slotReference);
            m_ControlAEdge = owner.GetEdges(m_ControlANode.slotReference);

            GenerateNodeCodeInUniversalTerrain(sb, inputSplatIndex);
            GenerateNodeCodeInUniversalTerrainBaseMapGen(sb, inputSplatIndex);
            GenerateNodeCodeInHDTerrain(sb, inputSplatIndex);
            GenerateNodeCodeInNullTerrain(sb);
        }

        private void GenerateNodeCodeInUniversalTerrain(ShaderStringBuilder sb, int inputSplatIndex)
        {
            string universalTerrainDef = inputSplatIndex == 0
                ? "#if defined(UNIVERSAL_TERRAIN_ENABLED) &&!defined(_TERRAIN_BASEMAP_GEN) && !defined(TERRAIN_SPLAT_ADDPASS)"
                : "#if defined(UNIVERSAL_TERRAIN_ENABLED) &&!defined(_TERRAIN_BASEMAP_GEN) &&  defined(TERRAIN_SPLAT_ADDPASS)";

            sb.AppendLine(universalTerrainDef);
            sb.IncreaseIndent();
            sb.AppendLine("#ifndef SPLAT_CONTROL");
            sb.AppendLine("#define SPLAT_CONTROL");
            sb.AppendLine("float2 controlUV = (IN.uv0.xy * (_Control_TexelSize.zw - 1.0) + 0.5) * _Control_TexelSize.xy;");
            sb.AppendLine("half4 splatControl = SAMPLE_TEXTURE2D(_Control, sampler_Control, controlUV);");
            sb.AppendLine("#endif // SPLAT_CONTROL");
            sb.DecreaseIndent();

            if (m_ControlREdge.Any()) sb.AppendLine("{0} {1} = splatControl.r;", m_ControlRType, m_ControlRValue);
            if (m_ControlGEdge.Any()) sb.AppendLine("{0} {1} = splatControl.g;", m_ControlGType, m_ControlGValue);
            if (m_ControlBEdge.Any()) sb.AppendLine("{0} {1} = splatControl.b;", m_ControlBType, m_ControlBValue);
            if (m_ControlAEdge.Any()) sb.AppendLine("{0} {1} = splatControl.a;", m_ControlAType, m_ControlAValue);
        }

        private void GenerateNodeCodeInUniversalTerrainBaseMapGen(ShaderStringBuilder sb, int inputSplatIndex)
        {
            string universalTerrainDef = "#elif defined(UNIVERSAL_TERRAIN_ENABLED) && defined(_TERRAIN_BASEMAP_GEN)";

            sb.AppendLine(universalTerrainDef);
            sb.IncreaseIndent();
            sb.AppendLine("#ifndef SPLAT_CONTROL");
            sb.AppendLine("#define SPLAT_CONTROL");
            sb.AppendLine("float2 controlUV = (IN.uv0.xy * (_Control_TexelSize.zw - 1.0) + 0.5) * _Control_TexelSize.xy;");
            sb.AppendLine("half4 splatControl = SAMPLE_TEXTURE2D(_Control, sampler_Control, controlUV);");
            sb.AppendLine("#endif // SPLAT_CONTROL");
            sb.DecreaseIndent();

            if (inputSplatIndex == 0)
            {
                if (m_ControlREdge.Any()) sb.AppendLine("{0} {1} = lerp(splatControl.r, 0.0, _DstBlend);", m_ControlRType, m_ControlRValue);
                if (m_ControlGEdge.Any()) sb.AppendLine("{0} {1} = lerp(splatControl.g, 0.0, _DstBlend);", m_ControlGType, m_ControlGValue);
                if (m_ControlBEdge.Any()) sb.AppendLine("{0} {1} = lerp(splatControl.b, 0.0, _DstBlend);", m_ControlBType, m_ControlBValue);
                if (m_ControlAEdge.Any()) sb.AppendLine("{0} {1} = lerp(splatControl.a, 0.0, _DstBlend);", m_ControlAType, m_ControlAValue);
            }
            else
            {
                if (m_ControlREdge.Any()) sb.AppendLine("{0} {1} = lerp(0.0, splatControl.r, _DstBlend);", m_ControlRType, m_ControlRValue);
                if (m_ControlGEdge.Any()) sb.AppendLine("{0} {1} = lerp(0.0, splatControl.g, _DstBlend);", m_ControlGType, m_ControlGValue);
                if (m_ControlBEdge.Any()) sb.AppendLine("{0} {1} = lerp(0.0, splatControl.b, _DstBlend);", m_ControlBType, m_ControlBValue);
                if (m_ControlAEdge.Any()) sb.AppendLine("{0} {1} = lerp(0.0, splatControl.a, _DstBlend);", m_ControlAType, m_ControlAValue);
            }
        }

        private void GenerateNodeCodeInHDTerrain(ShaderStringBuilder sb, int inputSplatIndex)
        {
            string hdTerrainDef = inputSplatIndex == 0
                ? "#elif defined(HD_TERRAIN_ENABLED)"
                : "#elif defined(HD_TERRAIN_ENABLED) && defined(_TERRAIN_8_LAYERS)";

            sb.AppendLine(hdTerrainDef);
            sb.IncreaseIndent();
            sb.AppendLine("#ifndef SPLAT_CONTROL{0}", inputSplatIndex);
            sb.AppendLine("#define SPLAT_CONTROL{0}", inputSplatIndex);
            sb.AppendLine("float2 controlUV{0} = (IN.uv0.xy * (_Control{0}_TexelSize.zw - 1.0) + 0.5) * _Control{0}_TexelSize.xy;", inputSplatIndex);
            sb.AppendLine("float4 splatControl{0} = SAMPLE_TEXTURE2D(_Control{0}, sampler_Control{0}, controlUV{0});", inputSplatIndex);
            sb.AppendLine("#endif // SPLAT_CONTROL{0}", inputSplatIndex);
            sb.DecreaseIndent();

            if (m_ControlREdge.Any()) sb.AppendLine("{0} {1} = splatControl{2}.r;", m_ControlRType, m_ControlRValue, inputSplatIndex);
            if (m_ControlGEdge.Any()) sb.AppendLine("{0} {1} = splatControl{2}.g;", m_ControlGType, m_ControlGValue, inputSplatIndex);
            if (m_ControlBEdge.Any()) sb.AppendLine("{0} {1} = splatControl{2}.b;", m_ControlBType, m_ControlBValue, inputSplatIndex);
            if (m_ControlAEdge.Any()) sb.AppendLine("{0} {1} = splatControl{2}.a;", m_ControlAType, m_ControlAValue, inputSplatIndex);
        }

        private void GenerateNodeCodeInNullTerrain(ShaderStringBuilder sb)
        {
            sb.AppendLine("#else");

            if (m_ControlREdge.Any()) sb.AppendLine("{0} {1} = 0.0;", m_ControlRType, m_ControlRValue);
            if (m_ControlGEdge.Any()) sb.AppendLine("{0} {1} = 0.0;", m_ControlGType, m_ControlGValue);
            if (m_ControlBEdge.Any()) sb.AppendLine("{0} {1} = 0.0;", m_ControlBType, m_ControlBValue);
            if (m_ControlAEdge.Any()) sb.AppendLine("{0} {1} = 0.0;", m_ControlAType, m_ControlAValue);
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
