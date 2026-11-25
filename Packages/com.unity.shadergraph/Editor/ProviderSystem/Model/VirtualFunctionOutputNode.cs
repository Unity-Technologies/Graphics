using System;
using Unity.GraphAuthoring.Editor.ProviderSystem;

namespace UnityEditor.ShaderGraph.ProviderSystem
{
    // The graph keeps one per virtual function, in its context's stack.
    [Serializable]
    internal class VirtualFunctionOutputNode : AbstractVirtualFunctionNode
    {
        internal override string shortName => "Output";

        private protected override SlotLayout slotLayout => SlotLayout.OutputsAsInputs;

        public override bool canDeleteNode => isOrphaned;

        public override bool canCopyNode => false;

        public override bool canCutNode => false;

        public VirtualFunctionOutputNode()
        {
            name = "Virtual Function Output";
        }

        // Its stack already says what it is.
        internal override string TitleFor(IVirtualFunction definition) => definition.Presentation.DisplayName;
    }
}
