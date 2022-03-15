using System;
using System.Linq;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public static class BlackboardViewCommandHandlers
    {
        /// <summary>
        /// Command handler for the <see cref="PasteSerializedDataCommand"/>.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph model state component.</param>
        /// <param name="selectionState">The selection state component.</param>
        /// <param name="blackboardState">The blackboard state component.</param>
        /// <param name="command">The command.</param>
        public static void PasteSerializedDataCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState,
            SelectionStateComponent selectionState, BlackboardViewStateComponent blackboardState, PasteSerializedDataCommand command)
        {
            if (!command.Data.IsEmpty())
            {
                var selectionHelper = new GlobalSelectionCommandHelper(selectionState);

                using (var undoStateUpdater = undoState.UpdateScope)
                {
                    var undoableStates = selectionHelper.UndoableSelectionStates.Append(graphModelState);
                    undoStateUpdater.SaveStates(undoableStates.ToArray(), command);
                }

                using (var graphViewUpdater = graphModelState.UpdateScope)
                using (var selectionUpdaters = selectionHelper.UpdateScopes)
                using (var blackboardUpdater = blackboardState?.UpdateScope)
                {
                    foreach (var selectionUpdater in selectionUpdaters)
                    {
                        selectionUpdater.ClearSelection(graphModelState.GraphModel);
                    }

                    CopyPasteData.PasteSerializedData(command.Operation, command.Delta, graphViewUpdater,
                        blackboardUpdater, selectionUpdaters.MainUpdateScope, command.Data, graphModelState.GraphModel, command.SelectedGroup);
                }
            }
        }
    }
}
