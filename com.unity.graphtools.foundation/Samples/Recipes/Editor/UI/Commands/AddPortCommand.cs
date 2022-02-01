using System;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Recipes
{
    public class AddPortCommand : ModelCommand<MixNodeModel>
    {
        const string k_UndoStringSingular = "Add Ingredient";

        public AddPortCommand(MixNodeModel[] nodes)
            : base(k_UndoStringSingular, k_UndoStringSingular, nodes)
        {
        }

        public static void DefaultHandler(UndoStateComponent undoState, GraphViewStateComponent graphViewState, AddPortCommand command)
        {
            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveSingleState(graphViewState, command);
            }

            using (var graphUpdater = graphViewState.UpdateScope)
            {
                foreach (var nodeModel in command.Models)
                {
                    nodeModel.AddIngredientPort();
                    graphUpdater.MarkChanged(nodeModel);
                }
            }
        }
    }
}
