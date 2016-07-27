using UnityEngine.Graphing;


namespace UnityEngine.MaterialGraph
{
    [Title("Channels/Unpack Normal Node")]
    internal class UnpackNormalNode : Function1Input
    {
        public UnpackNormalNode()
        {
            name = "UnpackNormalNode";
        }

        public override bool hasPreview
        {
            get { return false; }
        }

        protected virtual MaterialSlot GetInputSlot()
        {
            return new MaterialSlot(InputSlotId, GetInputSlotName(), kInputSlotShaderName, SlotType.Input, SlotValueType.Vector4, Vector4.zero);
        }

        protected virtual MaterialSlot GetOutputSlot()
        {
            return new MaterialSlot(OutputSlotId, GetOutputSlotName(), kOutputSlotShaderName, SlotType.Output, SlotValueType.Vector3, Vector4.zero);
        }
       
        protected override string GetInputSlotName() {return "PackedNormal"; }
        protected override string GetOutputSlotName() {return "Normal"; }

        protected override string GetFunctionName()
        {
            return "UnpackNormal";
        }
    }
}
