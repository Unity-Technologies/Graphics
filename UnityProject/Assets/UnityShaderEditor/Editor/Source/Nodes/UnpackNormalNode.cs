using System;

namespace UnityEditor.Graphs.Material
{
    [Title("Channels/Unpack Normal Node")]
    internal class UnpackNormalNode : Function1Input
    {
        public override bool hasPreview
        {
            get { return false; }
        }

        public override void Init()
        {
            base.Init();
            name = "UnpackNormalNode";
        }

        protected override string GetInputSlotName() {return "PackedNormal"; }
        protected override string GetOutputSlotName() {return "Normal"; }

        protected override string GetFunctionName()
        {
            return "UnpackNormal";
        }

        protected override string GetFunctionCallBody(string inputValue)
        {
            return precision + "4(" + GetFunctionName() + " (" + inputValue + ")" + ", 0)";
        }
    }
}
