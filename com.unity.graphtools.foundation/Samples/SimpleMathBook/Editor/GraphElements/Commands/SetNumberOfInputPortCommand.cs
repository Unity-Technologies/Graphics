using System.Linq;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    public class SetNumberOfInputPortCommand : ModelCommand<MathOperator, int>
    {
        const string k_UndoStringSingular = "Change Input Count";

        public SetNumberOfInputPortCommand(int inputCount, params MathOperator[] nodes)
            : base(k_UndoStringSingular, k_UndoStringSingular, inputCount, nodes) {}

        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, SetNumberOfInputPortCommand command)
        {
            if (!command.Models.Any())
                return;

            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveSingleState(graphModelState, command);
            }

            using (var graphUpdater = graphModelState.UpdateScope)
            {
                foreach (var nodeModel in command.Models)
                {
                    nodeModel.SetInputPortCount(command.Value, out var _, out var __, out var deletedEdges);
                    graphUpdater.MarkDeleted(deletedEdges);
                }
                graphUpdater.MarkChanged(command.Models, ChangeHint.GraphTopology);
            }
        }
    }
}
