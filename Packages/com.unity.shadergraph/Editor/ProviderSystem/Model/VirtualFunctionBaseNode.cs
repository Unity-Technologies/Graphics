using System;
using System.Collections.Generic;
using Unity.GraphAuthoring.Editor.ProviderSystem;

namespace UnityEditor.ShaderGraph.ProviderSystem
{
    [Serializable]
    internal class VirtualFunctionBaseNode : AbstractVirtualFunctionNode
    {
        internal override string shortName => "Base";

        private protected override SlotLayout slotLayout => SlotLayout.AsDeclaredPassthrough;

        public VirtualFunctionBaseNode()
        {
            name = "Virtual Function Base";
        }

        private protected override void AddNodeMessages(IProvider<IVirtualFunction> provider, List<NodeMessage> messages)
        {
            if (provider is { IsValid: true } && !HasBase(provider.Definition))
                messages.Add(new NodeMessage(null, $"'{name}' calls a pure virtual function, which has no base implementation.", MessageType.Error));
        }
    }
}
