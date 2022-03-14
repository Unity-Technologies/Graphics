using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook.UI
{
    public class VariableInputNode : CollapsibleInOutNode
    {
        protected override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);

            if (!(Model is MathOperator mathOperatorNodeModel))
            {
                return;
            }

            if (evt.menu.MenuItems().Count > 0)
                evt.menu.AppendSeparator();

            evt.menu.AppendAction($"Add Input", action: action =>
            {
                GraphView.Dispatch(
                    new SetNumberOfInputPortCommand(mathOperatorNodeModel.InputPortCount + 1, new[] { mathOperatorNodeModel }));
            });

            evt.menu.AppendAction("Remove Input", action =>
            {
                GraphView.Dispatch(
                    new SetNumberOfInputPortCommand(mathOperatorNodeModel.InputPortCount - 1, new[] { mathOperatorNodeModel }));
            }, a => mathOperatorNodeModel.InputPortCount > 2 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
        }
    }
}
