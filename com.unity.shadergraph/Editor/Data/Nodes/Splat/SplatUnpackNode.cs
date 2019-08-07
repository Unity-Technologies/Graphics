using System.Collections.Generic;
using UnityEditor.Graphing;
using UnityEngine;
using UnityEditor.ShaderGraph.Drawing.Controls;

namespace UnityEditor.ShaderGraph
{
    [Title("Splat", "Splat Unpack")]
    class SplatUnpackNode : AbstractMaterialNode, IGeneratesBodyCode, ISplatCountListener, ISplatUnpackNode
    {
        private enum Mode
        {
            [InspectorName("float1")] UnpackFloat1,
            [InspectorName("float2")] UnpackFloat2
        }

        [SerializeField]
        private Mode m_Mode;

        [EnumControl("Unpack to:")]
        private Mode mode
        {
            get => m_Mode;
            set
            {
                if (m_Mode != value)
                {
                    m_Mode = value;
                    RecreateSlots();
                }
            }
        }

        [SerializeField]
        private int m_SplatCount = 4;

        public SplatUnpackNode()
        {
            name = "Splat Unpack";
            UpdateNodeAfterDeserialization();
        }

        const string kOutputSlotName = "Output";

        public const int kOutputSlotId = 0;
        public const int kInput0SlotId = 1;
        public const int kInput1SlotId = 2;
        public const int kInput2SlotId = 3;
        public const int kInput3SlotId = 4;

        public override void UpdateNodeAfterDeserialization()
        {
            RecreateSlots();
        }

        void ISplatCountListener.OnSplatCountChange(int splatCount)
        {
            if (splatCount != m_SplatCount)
            {
                m_SplatCount = splatCount;
                RecreateSlots();
            }
        }

        private void RecreateSlots()
        {
            var validSlots = new List<int>();
            validSlots.Add(kOutputSlotId);
            validSlots.Add(kInput0SlotId);
            if (m_Mode == Mode.UnpackFloat1)
            {
                AddSlot(new Vector1MaterialSlot(kOutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, 0.0f, ShaderStageCapability.Fragment));
                if (m_SplatCount <= 4)
                {
                    AddSlot(new Vector4MaterialSlot(kInput0SlotId, "Input", "Input0", SlotType.Input, Vector4.zero, ShaderStageCapability.Fragment));
                }
                else
                {
                    AddSlot(new Vector4MaterialSlot(kInput0SlotId, "Input 0", "Input0", SlotType.Input, Vector4.zero, ShaderStageCapability.Fragment));
                    AddSlot(new Vector4MaterialSlot(kInput1SlotId, "Input 1", "Input1", SlotType.Input, Vector4.zero, ShaderStageCapability.Fragment));
                    validSlots.Add(kInput1SlotId);
                }
            }
            else
            {
                AddSlot(new Vector2MaterialSlot(kOutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector4.zero, ShaderStageCapability.Fragment));
                if (m_SplatCount <= 4)
                {
                    AddSlot(new Vector4MaterialSlot(kInput0SlotId, "Input 0", "Input0", SlotType.Input, Vector4.zero, ShaderStageCapability.Fragment));
                    AddSlot(new Vector4MaterialSlot(kInput1SlotId, "Input 1", "Input1", SlotType.Input, Vector4.zero, ShaderStageCapability.Fragment));
                    validSlots.Add(kInput1SlotId);
                }
                else
                {
                    AddSlot(new Vector4MaterialSlot(kInput0SlotId, "Input 0", "Input0", SlotType.Input, Vector4.zero, ShaderStageCapability.Fragment));
                    AddSlot(new Vector4MaterialSlot(kInput1SlotId, "Input 1", "Input1", SlotType.Input, Vector4.zero, ShaderStageCapability.Fragment));
                    AddSlot(new Vector4MaterialSlot(kInput2SlotId, "Input 2", "Input2", SlotType.Input, Vector4.zero, ShaderStageCapability.Fragment));
                    AddSlot(new Vector4MaterialSlot(kInput3SlotId, "Input 3", "Input3", SlotType.Input, Vector4.zero, ShaderStageCapability.Fragment));
                    validSlots.Add(kInput1SlotId);
                    validSlots.Add(kInput2SlotId);
                    validSlots.Add(kInput3SlotId);
                }
            }
            RemoveSlotsNameNotMatching(validSlots);
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GraphContext graphContext, GenerationMode generationMode)
        {
            var outputVarName = GetVariableNameForSlot(kOutputSlotId);
            sb.AppendLine($"$precision{(m_Mode == Mode.UnpackFloat2 ? "2" : string.Empty)} {outputVarName}[{m_SplatCount}] =");
            using (var scope = sb.BlockSemicolonScope())
            {
                for (int i = 0; i < m_SplatCount; ++i)
                {
                    var inputChannel = (m_Mode == Mode.UnpackFloat1 ? 1 : 2) * i;
                    var srcSwizzle = "xyzw".Substring(inputChannel % 4, m_Mode == Mode.UnpackFloat2 ? 2 : 1);
                    var delimiter = i == m_SplatCount - 1 ? string.Empty : ",";
                    sb.AppendLine($"{GetSlotValue(kInput0SlotId + inputChannel / 4, generationMode)}.{srcSwizzle}{delimiter}");
                }
            }
        }
    }
}
