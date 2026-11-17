using System;

namespace UnityEditor.ShaderGraph.ProviderSystem
{
    [Serializable]
    internal class VirtualFunctionInputNode : AbstractVirtualFunctionNode
    {
        internal override string shortName => "Input";

        private protected override SlotLayout slotLayout => SlotLayout.InputsAsOutputs;

        public VirtualFunctionInputNode()
        {
            name = "Virtual Function Input";
        }
    }
}
