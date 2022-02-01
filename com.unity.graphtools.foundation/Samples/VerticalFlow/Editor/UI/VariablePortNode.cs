using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Vertical
{
    class VerticalNode : CollapsibleInOutNode
    {
        protected override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);

            if (!(Model is VerticalNodeModel verticalNodeModel))
                return;

            if (evt.menu.MenuItems().Count > 0)
                evt.menu.AppendSeparator();

            evt.menu.AppendAction("Input/Add Port", action =>
            {
                GraphView.Dispatch(new AddPortCommand(PortDirection.Input, PortOrientation.Horizontal, verticalNodeModel));
            });

            evt.menu.AppendAction("Input/Add Vertical Port", action =>
            {
                GraphView.Dispatch(new AddPortCommand(PortDirection.Input, PortOrientation.Vertical, verticalNodeModel));
            });

            evt.menu.AppendAction("Input/Remove Port", action =>
            {
                GraphView.Dispatch(new RemovePortCommand(PortDirection.Input, PortOrientation.Horizontal, verticalNodeModel));
            }, a => verticalNodeModel.InputCount > 0 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            evt.menu.AppendAction("Input/Remove Vertical Port", action =>
            {
                GraphView.Dispatch(new RemovePortCommand(PortDirection.Input, PortOrientation.Vertical, verticalNodeModel));
            }, a => verticalNodeModel.VerticalInputCount > 0 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            evt.menu.AppendAction("Output/Add Port", action =>
            {
                GraphView.Dispatch(new AddPortCommand(PortDirection.Output, PortOrientation.Horizontal, verticalNodeModel));
            });

            evt.menu.AppendAction("Output/Add Vertical Port", action =>
            {
                GraphView.Dispatch(new AddPortCommand(PortDirection.Output, PortOrientation.Vertical, verticalNodeModel));
            });

            evt.menu.AppendAction("Output/Remove Port", action =>
            {
                GraphView.Dispatch(new RemovePortCommand(PortDirection.Output, PortOrientation.Horizontal, verticalNodeModel));
            }, a => verticalNodeModel.OutputCount > 0 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            evt.menu.AppendAction("Output/Remove Vertical Port", action =>
            {
                GraphView.Dispatch(new RemovePortCommand(PortDirection.Output, PortOrientation.Vertical, verticalNodeModel));
            }, a => verticalNodeModel.VerticalOutputCount > 0 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
        }
    }
}
