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

        public static void DefaultHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, RemovePortCommand command)
        {
            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveSingleState(graphModelState, command);
            }

            using (var graphUpdater = graphModelState.UpdateScope)
            {
                foreach (var nodeModel in command.Models)
                {
                    nodeModel.SetIngredientCount(nodeModel.IngredientCount - 1, out var _, out var __, out var deletedModels);

                    graphUpdater.MarkChanged(nodeModel, ChangeHint.GraphTopology);
                    graphUpdater.MarkDeleted(deletedModels);
                }
            }
        }
    }
}
