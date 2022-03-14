using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class CustomizableNode : CollapsibleInOutNode
    {
        protected override void BuildPartList()
        {
            base.BuildPartList();
            PartList.AppendPart(new AddPortPart("add-port", Model, this, ussClassName));
            PartList.AppendPart(new RemovePortPart("remove-port", Model, this, ussClassName));
        }

        protected override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);
            evt.menu.AppendSeparator();
            evt.menu.AppendAction("Add Float Input Port", action =>
            {
                GraphView.Dispatcher.Dispatch(new AddPortCommand(false, "Input", TypeHandle.Float, new [] { (CustomizableNodeModel)Model }));
            });
            evt.menu.AppendAction("Add Float Output Port", action =>
            {
                GraphView.Dispatcher.Dispatch(new AddPortCommand(true, "Output", TypeHandle.Float, new [] { (CustomizableNodeModel)Model }));
            });
        }
    }
}
