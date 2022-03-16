using System;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Command to open or close a section in the inspector.
    /// </summary>
    public class CollapseInspectorSectionCommand : ICommand
    {
        /// <summary>
        /// The inspector section to modify.
        /// </summary>
        public IInspectorSectionModel Model;
        /// <summary>
        /// True if the section needs to be collapsed, false otherwise.
        /// </summary>
        public bool Collapsed;

        /// <summary>
        /// Initializes a new instance of the <see cref="CollapseInspectorSectionCommand"/> class.
        /// </summary>
        /// <param name="model">The inspector section to modify.</param>
        /// <param name="collapsed">True if the section needs to be collapsed, false otherwise.</param>
        public CollapseInspectorSectionCommand(IInspectorSectionModel model, bool collapsed)
        {
            Model = model;
            Collapsed = collapsed;
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="modelInspectorState">The state to modify.</param>
        /// <param name="command">The command to apply to the state.</param>
        public static void DefaultCommandHandler(ModelInspectorStateComponent modelInspectorState, CollapseInspectorSectionCommand command)
        {
            using (var updater = modelInspectorState.UpdateScope)
            {
                updater.SetSectionCollapsed(command.Model, command.Collapsed);
            }
        }
    }
}
