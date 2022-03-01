using System.Collections.Generic;
using System.Linq;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine.GraphToolsFoundation.Overdrive;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Command to create a new blackboard group
    /// </summary>
    public class BlackboardGroupCreateCommand : UndoableCommand
    {
        const string k_UndoString = "Create Group";

        /// <summary>
        /// The group in which the new group will be added.
        /// </summary>
        public IGroupModel ContainingGroup;

        /// <summary>
        /// The variable in the <see cref="ContainingGroup"/> after which the new group should be inserted.
        /// </summary>
        public readonly IGroupItemModel InsertAfter;

        /// <summary>
        /// The title of the new group.
        /// </summary>
        public string Title;

        /// <summary>
        /// Items that are added in the new group.
        /// </summary>
        public IReadOnlyList<IGroupItemModel> Items;

        internal BlackboardGroupCreateCommand()
        {
            UndoString = k_UndoString;
        }

        /// <summary>
        /// Creates a instance of a <see cref="BlackboardGroupCreateCommand"/>.
        /// </summary>
        /// <param name="containingGroup">The group in which the new group will be added. Must be non null.</param>
        /// <param name="insertAfter">The variable in the <see cref="ContainingGroup"/> after which the new group should be inserted.
        /// If null will add at the beginning of the group.</param>
        /// <param name="title">The title of the new group.</param>
        /// <param name="items">Items that are added in the new group.</param>
        public BlackboardGroupCreateCommand(IGroupModel containingGroup, IGroupItemModel insertAfter = null, string title = null,
                                            IReadOnlyList<IGroupItemModel> items = null) : this()
        {
            ContainingGroup = containingGroup;
            InsertAfter = insertAfter;
            Title = title;
            Items = items;
        }

        /// <summary>
        /// Default command handler.
        /// </summary>
        /// <param name="undoState">The undo state component.</param>
        /// <param name="blackboardViewState">The state of the blackboard.</param>
        /// <param name="graphViewState">The state of the graph view.</param>
        /// <param name="selectionState">The selection state.</param>
        /// <param name="command">The command.</param>
        public static void DefaultCommandHandler(UndoStateComponent undoState, BlackboardViewStateComponent blackboardViewState, GraphViewStateComponent graphViewState, SelectionStateComponent selectionState, BlackboardGroupCreateCommand command)
        {
            using var graphUpdater = graphViewState.UpdateScope;
            using var graphSelection = selectionState.UpdateScope;

            if (command.ContainingGroup == null)
                return;

            using (var undoStateUpdater = undoState.UpdateScope)
            {
                undoStateUpdater.SaveStates(new IUndoableStateComponent[] { graphViewState, selectionState, blackboardViewState }, command);
            }

            string title = command.Title;
            if (string.IsNullOrEmpty(title))
            {
                title = "New Group";

                int cpt = 1;
                while (command.ContainingGroup.Items.Any(t => t.Title == title))
                    title = $"New Group {cpt++}";
            }

            graphSelection.ClearSelection(command.ContainingGroup.GraphModel);

            IGroupModel newGroup = command.ContainingGroup.GraphModel.CreateVariableGroup(title);
            if (command.Items != null)
            {
                foreach (var subItem in command.Items)
                {
                    if (subItem.Group != null)
                        graphUpdater.MarkChanged(subItem.Group);
                    newGroup.InsertItem(subItem);
                }
            }

            int index = command.ContainingGroup.Items.IndexOfInternal(command.InsertAfter);
            command.ContainingGroup.InsertItem(newGroup, index);

            using (var bbUpdater = blackboardViewState.UpdateScope)
            {
                bbUpdater.SetVariableGroupModelExpanded(command.ContainingGroup,true);
            }

            graphUpdater.MarkChanged(command.ContainingGroup);
            graphUpdater.MarkNew(newGroup);

            graphSelection.SelectElement(newGroup, true);
        }
    }
}
