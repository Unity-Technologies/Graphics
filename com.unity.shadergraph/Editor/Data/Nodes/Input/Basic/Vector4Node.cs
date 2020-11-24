using System.Collections.Generic;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Basic", "Vector 4")]
    class Vector4Node : AbstractMaterialNode, IGeneratesBodyCode, IPropertyFromNode
    {
        [SerializeField]
        private Vector4 m_Value = Vector4.zero;

        const string kInputSlotXName = "X";
        const string kInputSlotYName = "Y";
        const string kInputSlotZName = "Z";
        const string kInputSlotWName = "W";
        const string kOutputSlotName = "Out";

        public const int OutputSlotId = 0;
        public const int InputSlotXId = 1;
        public const int InputSlotYId = 2;
        public const int InputSlotZId = 3;
        public const int InputSlotWId = 4;


        public Vector4Node()
        {
            name = "Vector 4";
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector1MaterialSlot(InputSlotXId, kInputSlotXName, kInputSlotXName, SlotType.Input, m_Value.x));
            AddSlot(new Vector1MaterialSlot(InputSlotYId, kInputSlotYName, kInputSlotYName, SlotType.Input, m_Value.y, label1: "Y"));
            AddSlot(new Vector1MaterialSlot(InputSlotZId, kInputSlotZName, kInputSlotZName, SlotType.Input, m_Value.z, label1: "Z"));
            AddSlot(new Vector1MaterialSlot(InputSlotWId, kInputSlotWName, kInputSlotWName, SlotType.Input, m_Value.w, label1: "W"));
            AddSlot(new Vector4MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector4.zero));
            RemoveSlotsNameNotMatching(new[] { OutputSlotId, InputSlotXId, InputSlotYId, InputSlotZId, InputSlotWId });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            var inputXValue = GetSlotValue(InputSlotXId, generationMode);
            var inputYValue = GetSlotValue(InputSlotYId, generationMode);
            var inputZValue = GetSlotValue(InputSlotZId, generationMode);
            var inputWValue = GetSlotValue(InputSlotWId, generationMode);
            var outputName = GetVariableNameForSlot(outputSlotId);

            var s = string.Format("$precision4 {0} = $precision4({1}, {2}, {3}, {4});",
                outputName,
                inputXValue,
                inputYValue,
                inputZValue,
                inputWValue);
            sb.AppendLine(s);
        }

        public AbstractShaderProperty AsShaderProperty()
        {
            var slotX = FindInputSlot<Vector1MaterialSlot>(InputSlotXId);
            var slotY = FindInputSlot<Vector1MaterialSlot>(InputSlotYId);
            var slotZ = FindInputSlot<Vector1MaterialSlot>(InputSlotZId);
            var slotW = FindInputSlot<Vector1MaterialSlot>(InputSlotWId);
            return new Vector4ShaderProperty { value = new Vector4(slotX.value, slotY.value, slotZ.value, slotW.value) };
        }

        public int outputSlotId { get { return OutputSlotId; } }
    }
}
