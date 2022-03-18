using System;
using System.Reflection;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Command to set the value of a field on an model.
    /// </summary>
    public class SetInspectedGraphElementModelFieldCommand: SetInspectedObjectFieldCommand
    {
        public IGraphElementModel InspectedModel;

        /// <summary>
        /// Initializes a new instance of the <see cref="SetInspectedGraphElementModelFieldCommand"/> class.
        /// </summary>
        /// <param name="value">The value to set.</param>
        /// <param name="inspectedModel">The model being inspected.</param>
        /// <param name="inspectedObject">The object that owns the field. Most of the time, it is the same as <paramref name="inspectedModel"/> but can differ if the model is a proxy onto another object.</param>
        /// <param name="field">The field to set.</param>
        public SetInspectedGraphElementModelFieldCommand(object value, IGraphElementModel inspectedModel, object inspectedObject, FieldInfo field)
        : base(value, inspectedObject, field)
        {
            InspectedModel = inspectedModel;
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="graphModelState">The graph view state component.</param>
        /// <param name="command">The command to apply to the state.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, GraphModelStateComponent graphModelState, SetInspectedGraphElementModelFieldCommand command)
        {
            if (command.InspectedModel != null && command.InspectedObject != null && command.Field != null)
            {
                using (var undoStateUpdater = undoState.UpdateScope)
                {
                    undoStateUpdater.SaveSingleState(graphModelState, command);
                }

                SetField(command, out var newModels, out var changedModels, out var deletedModels);

                using (var updater = graphModelState.UpdateScope)
                {
                    updater.MarkChanged(command.InspectedModel, ChangeHint.Data);

                    updater.MarkNew(newModels);
                    updater.MarkChanged(changedModels);
                    updater.MarkDeleted(deletedModels);
                }
            }
        }
    }
}
