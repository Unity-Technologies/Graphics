using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace GtfPlayground.Commands
{
    /// <summary>
    /// Moves all nodes on the graph randomly. Used to demonstrate updating existing models in an undoable command.
    /// </summary>
    public class ScatterNodesCommand : UndoableCommand
    {
        public ScatterNodesCommand()
        {
            UndoString = "Scatter Nodes";
        }

        public static void DefaultCommandHandler(GraphToolState graphToolState, ScatterNodesCommand command)
        {
            graphToolState.PushUndo(command);

            using (var graphUpdater = graphToolState.GraphViewState.UpdateScope)
            {
                var model = graphToolState.GraphViewState.GraphModel;

                foreach (var nodeModel in model.NodeModels)
                {
                    nodeModel.Position = Random.insideUnitCircle * 800f;
                    graphUpdater.MarkChanged(nodeModel);
                }
            }
        }
    }
}
