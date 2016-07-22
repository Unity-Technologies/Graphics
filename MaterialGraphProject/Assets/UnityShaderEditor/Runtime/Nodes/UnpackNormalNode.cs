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
            return new MaterialSlot(GetInputSlotName(), GetInputSlotName(), SlotType.Input, 0, SlotValueType.Vector4, Vector4.zero);
        }

        protected virtual MaterialSlot GetOutputSlot()
        {
            return new MaterialSlot(GetOutputSlotName(), GetOutputSlotName(), SlotType.Output, 0, SlotValueType.Vector3, Vector4.zero);
        }

        protected override string GetInputSlotName() {return "PackedNormal"; }
        protected override string GetOutputSlotName() {return "Normal"; }

        protected override string GetFunctionName()
        {
            return "UnpackNormal";
        }
    }
}
