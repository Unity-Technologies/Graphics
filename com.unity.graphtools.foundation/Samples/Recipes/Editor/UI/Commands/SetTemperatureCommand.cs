using System;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.Recipes
{
    public class SetTemperatureCommand : ModelCommand<BakeNodeModel, int>
    {
        const string k_UndoStringSingular = "Set Bake Node Temperature";
        const string k_UndoStringPlural = "Set Bake Nodes Temperature";

        public SetTemperatureCommand(int value, params BakeNodeModel[] nodes)
            : base(k_UndoStringSingular, k_UndoStringPlural, value, nodes)
        {
        }

        public static void DefaultHandler(UndoStateComponent undoState, GraphViewStateComponent graphViewState, SetTemperatureCommand command)
        {
            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveSingleState(graphViewState, command);
            }

            using (var graphUpdater = graphViewState.UpdateScope)
            {
                foreach (var nodeModel in command.Models)
                {
                    nodeModel.Temperature = command.Value;
                    graphUpdater.MarkChanged(nodeModel);
                }
            }
        }
    }
}
