using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Contexts.UI
{
    class SampleNode : CollapsibleInOutNode
    {
        protected override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            ContextSampleUIFactoryExtensions.BuildContextualMenu(GraphView, Model as IVariableNodeModel, evt);
        }
    }
}
