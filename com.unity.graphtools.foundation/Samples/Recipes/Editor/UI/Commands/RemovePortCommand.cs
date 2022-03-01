using System;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Recipes
{
    public class RemovePortCommand : ModelCommand<MixNodeModel>
    {
        const string k_UndoStringSingular = "Remove Ingredient";

        public RemovePortCommand(MixNodeModel[] nodes)
            : base(k_UndoStringSingular, k_UndoStringSingular, nodes)
        {
        }

        public static void DefaultHandler(UndoStateComponent undoState, GraphViewStateComponent graphViewState, RemovePortCommand command)
        {
            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveSingleState(graphViewState, command);
            }

            using (var graphUpdater = graphViewState.UpdateScope)
            {
                foreach (var nodeModel in command.Models)
                {
                    nodeModel.RemoveIngredientPort();
                    graphUpdater.MarkChanged(nodeModel);
                }
            }
        }
    }
}
