using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Contexts.UI
{
    class SampleBlock : BlockNode
    {
        protected override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            ContextSampleUIFactoryExtensions.BuildContextualMenu(GraphView, BlockNodeModel as IVariableNodeModel, evt, "Block ");

            base.BuildContextualMenu(evt);
        }
    }
}
