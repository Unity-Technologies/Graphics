using System.Collections.Generic;
using System.Linq;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Basic", "Vector 2")]
    class Vector2Node : AbstractMaterialNode, IGeneratesBodyCode, IPropertyFromNode
    {
        [SerializeField]
        private Vector2 m_Value = Vector2.zero;

        const string kInputSlotXName = "X";
        const string kInputSlotYName = "Y";
        const string kOutputSlotName = "Out";

        public const int OutputSlotId = 0;
        public const int InputSlotXId = 1;
        public const int InputSlotYId = 2;


        public Vector2Node()
        {
            name = "Vector 2";
            synonyms = new string[] { "2", "v2", "vec2", "float2" };
            UpdateNodeAfterDeserialization();
        }

        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector1MaterialSlot(InputSlotXId, kInputSlotXName, kInputSlotXName, SlotType.Input, m_Value.x));
            AddSlot(new Vector1MaterialSlot(InputSlotYId, kInputSlotYName, kInputSlotYName, SlotType.Input, m_Value.y, label1: "Y"));
            AddSlot(new Vector2MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector4.zero));
            RemoveSlotsNameNotMatching(new[] { OutputSlotId, InputSlotXId, InputSlotYId });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            var inputXValue = GetSlotValue(InputSlotXId, generationMode);
            var inputYValue = GetSlotValue(InputSlotYId, generationMode);
            var outputName = GetVariableNameForSlot(OutputSlotId);

            var s = string.Format("$precision2 {0} = $precision2({1}, {2});",
                outputName,
                inputXValue,
                inputYValue);
            sb.AppendLine(s);
        }

        public AbstractShaderProperty AsShaderProperty()
        {
            var slotX = FindInputSlot<Vector1MaterialSlot>(InputSlotXId);
            var slotY = FindInputSlot<Vector1MaterialSlot>(InputSlotYId);
            return new Vector2ShaderProperty { value = new Vector2(slotX.value, slotY.value) };
        }

        int IPropertyFromNode.outputSlotId { get { return OutputSlotId; } }
    }
}
