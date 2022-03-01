using System;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Recipes
{
    class VariableIngredientNode : CollapsibleInOutNode
    {
        protected override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);

            if (!(Model is MixNodeModel mixNodeModel))
            {
                return;
            }

            if (evt.menu.MenuItems().Count > 0)
                evt.menu.AppendSeparator();

            evt.menu.AppendAction($"Add Ingredient", action: action =>
            {
                GraphView.Dispatch(new AddPortCommand(new[] { mixNodeModel }));
            });

            evt.menu.AppendAction($"Remove Ingredient", action: action =>
            {
                GraphView.Dispatch(new RemovePortCommand(new[] { mixNodeModel }));
            });
        }
    }
}
