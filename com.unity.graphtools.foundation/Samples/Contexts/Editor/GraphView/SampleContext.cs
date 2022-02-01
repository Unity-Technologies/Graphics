using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Contexts.UI
{
    class SampleContext : ContextNode
    {
        protected override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            ContextSampleUIFactoryExtensions.BuildContextualMenu(GraphView, Model as IVariableNodeModel, evt);

            base.BuildContextualMenu(evt);
        }
    }
}
