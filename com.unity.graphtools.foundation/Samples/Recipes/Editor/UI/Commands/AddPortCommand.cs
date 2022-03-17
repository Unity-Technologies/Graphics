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

        public static void DefaultHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, AddPortCommand command)
        {
            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveSingleState(graphModelState, command);
            }

            using (var graphUpdater = graphModelState.UpdateScope)
            {
                foreach (var nodeModel in command.Models)
                {
                    nodeModel.SetIngredientCount(nodeModel.IngredientCount + 1, out var _, out var __, out var ___);

                    graphUpdater.MarkChanged(nodeModel, ChangeHint.GraphTopology);
                }
            }
        }
    }
}
