using System;
using System.Reflection;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Command to set the value of a field on an model.
    /// </summary>
    public class SetInspectedGraphModelFieldCommand: SetInspectedObjectFieldCommand
    {
        public IGraphModel InspectedModel;

        /// <summary>
        /// Initializes a new instance of the <see cref="SetInspectedGraphElementModelFieldCommand"/> class.
        /// </summary>
        /// <param name="value">The value to set.</param>
        /// <param name="inspectedModel">The model being inspected.</param>
        /// <param name="inspectedObject">The object that owns the field. Most of the time, it is the same as <paramref name="inspectedModel"/> but can differ if the model is a proxy onto another object.</param>
        /// <param name="field">The field to set.</param>
        public SetInspectedGraphModelFieldCommand(object value, IGraphModel inspectedModel, object inspectedObject, FieldInfo field)
            : base(value, inspectedObject, field)
        {
            InspectedModel = inspectedModel;
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The state to modify.</param>
        /// <param name="command">The command to apply to the state.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState,  SetInspectedGraphModelFieldCommand command)
        {
            if (command.InspectedModel != null && command.InspectedObject != null && command.Field != null)
            {
                using (var undoStateUpdater = undoState.UpdateScope)
                {
                    undoStateUpdater.SaveSingleState(graphModelState, command);
                }

                SetField(command, out var newModels, out var changedModels, out var deletedModels);

                if (newModels != null || changedModels != null || deletedModels != null)
                {
                    using (var updater = graphModelState.UpdateScope)
                    {
                        updater.MarkNew(newModels);
                        updater.MarkChanged(changedModels);
                        updater.MarkDeleted(deletedModels);
                    }
                }
            }
        }
    }
}
