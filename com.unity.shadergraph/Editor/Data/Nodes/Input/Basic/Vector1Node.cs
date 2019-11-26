using System.Collections.Generic;
using System.Linq;
using UnityEditor.ShaderGraph.Drawing.Controls;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph.Internal;

namespace UnityEditor.ShaderGraph
{
    [Title("Input", "Basic", "Vector 1")]
    class Vector1Node : AbstractMaterialNode, IGeneratesBodyCode, IPropertyFromNode
    {
        [SerializeField]
        private float m_Value = 0;

        const string kInputSlotXName = "X";
        const string kOutputSlotName = "Out";

        public const int InputSlotXId = 1;
        public const int OutputSlotId = 0;

        public Vector1Node()
        {
            name = "Vector 1";
            UpdateNodeAfterDeserialization();
        }


        public sealed override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new Vector1MaterialSlot(InputSlotXId, kInputSlotXName, kInputSlotXName, SlotType.Input, m_Value));
            AddSlot(new Vector1MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, 0));
            RemoveSlotsNameNotMatching(new[] { OutputSlotId, InputSlotXId });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode)
        {
            var inputValue = GetSlotValue(InputSlotXId, generationMode);
            sb.AppendLine(string.Format("$precision {0} = {1};", GetVariableNameForSlot(OutputSlotId), inputValue));
        }

        public AbstractShaderProperty AsShaderProperty()
        {
            var slot = FindInputSlot<Vector1MaterialSlot>(InputSlotXId);
            return new Vector1ShaderProperty { value = slot.value };
        }

        int IPropertyFromNode.outputSlotId { get { return OutputSlotId; } }
    }
}
