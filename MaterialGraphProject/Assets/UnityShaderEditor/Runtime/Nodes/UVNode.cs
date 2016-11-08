using UnityEngine.Graphing;

namespace UnityEngine.MaterialGraph
{
    interface IRequiresMeshUV
    { }

    [Title("Input/UV Node")]
	public class UVNode : AbstractMaterialNode, IGeneratesBodyCode, IRequiresMeshUV
    {
        public const int OutputSlotId = 0;
        private const string kOutputSlotName = "UV";

        public override bool hasPreview { get { return true; } }

        public UVNode()
        {
            name = "UV";
            UpdateNodeAfterDeserialization();
        }

        public override void UpdateNodeAfterDeserialization()
        {
            AddSlot(new MaterialSlot(OutputSlotId, kOutputSlotName, kOutputSlotName, SlotType.Output, SlotValueType.Vector4, Vector4.zero));
            RemoveSlotsNameNotMatching(new[] { OutputSlotId });
        }

        public void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode)
        {
            string uvValue = "IN.meshUV0";
            visitor.AddShaderChunk(precision + "4 " + GetVariableNameForSlot(OutputSlotId) + " = " + uvValue + ";", true);
        }
    }
}
