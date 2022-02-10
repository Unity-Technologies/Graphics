using System.Linq;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive.Samples.MathBook
{
    public class SetNumberOfInputPortCommand : ModelCommand<MathOperator, int>
    {
        const string k_UndoStringSingular = "Change Input Count";

        public SetNumberOfInputPortCommand(int inputCount, params MathOperator[] nodes)
            : base(k_UndoStringSingular, k_UndoStringSingular, inputCount, nodes) {}

        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphViewStateComponent graphViewState, SetNumberOfInputPortCommand command)
        {
            if (!command.Models.Any())
                return;

            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveSingleState(graphViewState, command);
            }

            using (var graphUpdater = graphViewState.UpdateScope)
            {
                foreach (var nodeModel in command.Models)
                {
                    nodeModel.InputPortCount = command.Value;
                    nodeModel.DefineNode();
                }
                graphUpdater.MarkChanged(command.Models);
            }
        }
    }
}
