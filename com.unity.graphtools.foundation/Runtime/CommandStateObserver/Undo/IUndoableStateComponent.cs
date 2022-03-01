using System;

namespace UnityEngine.GraphToolsFoundation.CommandStateObserver
{
    /// <summary>
    /// A state component that can be saved on the undo stack and restored from it.
    /// </summary>
    public interface IUndoableStateComponent : IStateComponent
    {
        /// <summary>
        /// A unique id for the state component.
        /// </summary>
        Hash128 Guid { get; }

        /// <summary>
        /// Called before the state component is pushed on the undo stack.
        /// Use this to push additional objects on the stack.
        /// </summary>
        /// <param name="undoString">The name of the undo operation.</param>
        void WillPerformUndoRedo(string undoString);

        /// <summary>
        /// Replaces serialized values from this component by values from <paramref name="undoData"/>.
        /// </summary>
        /// <param name="undoData">The state component from which to take the values.</param>
        void Apply(IStateComponent undoData);

        /// <summary>
        /// Called after an undo/redo operation.
        /// </summary>
        void UndoRedoPerformed();
    }
}
