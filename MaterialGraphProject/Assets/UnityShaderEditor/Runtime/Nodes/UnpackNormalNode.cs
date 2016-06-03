using System;
using UnityEngine.Graphing;


namespace UnityEngine.MaterialGraph
{
    [Title("Channels/Unpack Normal Node")]
    internal class UnpackNormalNode : Function1Input
    {
        public UnpackNormalNode(IGraph owner) : base(owner)
        {
            name = "UnpackNormalNode";
        }

        public override bool hasPreview
        {
            get { return false; }
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
