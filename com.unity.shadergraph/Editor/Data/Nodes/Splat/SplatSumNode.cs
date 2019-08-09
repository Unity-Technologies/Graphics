using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph
{
    [Title("Splat", "Splat Sum")]
    class SplatSumNode : AbstractMaterialNode, IGeneratesBodyCode, ISplatLoopNode
    {
        public SplatSumNode()
        {
            name = "Splat Sum";
            UpdateNodeAfterDeserialization();
        }

        public override bool hasPreview => true;

        const string kInputSlotName = "Input";
        const string kOutputSlotName = "Output";

        public const int kInputSlotId = 0;
        public const int kOutputSlotId = 1;

        public override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new DynamicVectorMaterialSlot(kInputSlotId, kInputSlotName, kInputSlotName, SlotType.Input, Vector4.zero, ShaderStageCapability.Fragment));
            AddSlot(new DynamicVectorMaterialSlot(kOutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, Vector4.zero, ShaderStageCapability.Fragment));
            RemoveSlotsNameNotMatching(new[] { kInputSlotId, kOutputSlotId });
        }

        public void GenerateNodeCode(ShaderStringBuilder sb, GraphContext graphContext, GenerationMode generationMode)
        {
            sb.AppendLine($"{GetVariableNameForSlot(kOutputSlotId)} += {GetSlotValue(kInputSlotId, generationMode)};");
        }

        void ISplatLoopNode.GenerateSetupCode(ShaderStringBuilder sb, GraphContext graphContext, GenerationMode generationMode)
        {
            var outputSlot = FindOutputSlot<MaterialSlot>(kOutputSlotId);
            sb.AppendLine($"{outputSlot.concreteValueType.ToShaderString()} {GetVariableNameForSlot(kOutputSlotId)} = 0;");
        }
    }
}
