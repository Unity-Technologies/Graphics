using System.Collections.Generic;
using System.Linq;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    [Title("Terrain", "Terrain Splat")]
    class TerrainSplat : AbstractMaterialNode, IGeneratesBodyCode, IMayRequireMeshUV // IGeneratesFunction
    {
        const int InputUVId = 0;
        const int OutputControlRId = 1;
        const int OutputControlGId = 2;
        const int OutputControlBId = 3;
        const int OutputControlAId = 4;

        const string kInputUVSlotName = "UV";
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

        internal enum SplatIndex
        {
            Index0 = 0,
            Index1 = 1,
        }

        [SerializeField]
        private SplatIndex m_SplatIndex;

        [EnumControl("")]
        public SplatIndex splatIndex
        {
            get { return m_SplatIndex; }
            set { m_SplatIndex = value; Dirty(ModificationScope.Graph); }
        }

        public TerrainSplat()
        {
            name = "Terrain Splat";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new UVMaterialSlot(InputUVId, kInputUVSlotName, kInputUVSlotName, UVChannel.UV0, hidden: true));
            AddSlot(new Vector1MaterialSlot(OutputControlRId, kOutputControlRSlotName, kOutputControlRSlotName, SlotType.Output, 0));
            AddSlot(new Vector1MaterialSlot(OutputControlGId, kOutputControlGSlotName, kOutputControlGSlotName, SlotType.Output, 0));
            AddSlot(new Vector1MaterialSlot(OutputControlBId, kOutputControlBSlotName, kOutputControlBSlotName, SlotType.Output, 0));
            AddSlot(new Vector1MaterialSlot(OutputControlAId, kOutputControlASlotName, kOutputControlASlotName, SlotType.Output, 0));

            RemoveSlotsNameNotMatching(new[] { InputUVId, OutputControlRId, OutputControlGId, OutputControlBId, OutputControlAId, });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            var inputSplatIndex = (int) splatIndex;

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

            //sb.AppendLine("");
            //sb.AppendLine("#if !defined(UNIVERSAL_TERRAIN_ENABLED) && !defined(HD_TERRAIN_ENABLED)");
            //sb.AppendLine("#error TerrainSplat Node is working under 'TerrainLit' MaterialType");
            //sb.AppendLine("#endif");
            //sb.AppendLine("");

            sb.AppendLine("#ifndef SPLAT_CONTROL{0}", inputSplatIndex);
            sb.AppendLine("#define SPLAT_CONTROL{0}", inputSplatIndex);
            sb.AppendLine("FETCH_SPLAT_CONTROL{0}", inputSplatIndex);
            sb.AppendLine("#endif // SPLAT_CONTROL{0}", inputSplatIndex);

            if (m_ControlREdge.Any()) sb.AppendLine("{0} {1} = FetchControl({2}).r;", m_ControlRType, m_ControlRValue, inputSplatIndex);
            if (m_ControlGEdge.Any()) sb.AppendLine("{0} {1} = FetchControl({2}).g;", m_ControlGType, m_ControlGValue, inputSplatIndex);
            if (m_ControlBEdge.Any()) sb.AppendLine("{0} {1} = FetchControl({2}).b;", m_ControlBType, m_ControlBValue, inputSplatIndex);
            if (m_ControlAEdge.Any()) sb.AppendLine("{0} {1} = FetchControl({2}).a;", m_ControlAType, m_ControlAValue, inputSplatIndex);
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
