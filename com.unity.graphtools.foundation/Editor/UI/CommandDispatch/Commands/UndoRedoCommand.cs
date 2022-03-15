using System;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Command sent on undo/redo.
    /// </summary>
    public class UndoRedoCommand : ICommand
    {
        /// <summary>
        /// Default command handler for undo/redo.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, UndoRedoCommand command)
        {
            undoState.ApplyUndoData();
        }
    }
}
