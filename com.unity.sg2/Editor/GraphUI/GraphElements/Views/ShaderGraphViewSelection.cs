using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class ShaderGraphViewSelection : GraphViewSelection
    {
        public ShaderGraphViewSelection(GraphView view, GraphModelStateComponent graphModelState, SelectionStateComponent selectionState)
            : base(view, graphModelState, selectionState)
        {

        }

        /// <summary>
        /// Adds items related to the selection to the contextual menu.
        /// </summary>
        /// <param name="evt">The contextual menu event.</param>
        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (evt.menu.MenuItems().Count > 0)
                evt.menu.AppendSeparator();

            if (GetSelection().Any(model => model is INodeModel))
                HandleNodeContextMenus(evt);
            else
                HandleEdgeContextMenu(evt);
        }

        void HandleEdgeContextMenu(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction("Delete", _ =>
            {
                m_View.Dispatch(new DeleteElementsCommand(GetSelection().ToList()));
            }, CanDeleteSelection ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
        }

        void HandleNodeContextMenus(ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction("Cut", _ => { CutSelection(); },
                CanCutSelection ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            evt.menu.AppendAction("Copy", _ => { CopySelection(); },
                CanCopySelection ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            evt.menu.AppendAction("Paste", _ => { Paste(); },
                CanPaste ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            evt.menu.AppendSeparator();

            evt.menu.AppendAction("Duplicate", _ => { DuplicateSelection(); },
                CanDuplicateSelection ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            evt.menu.AppendAction("Delete", _ =>
            {
                m_View.Dispatch(new DeleteElementsCommand(GetSelection().ToList()));
            }, CanDeleteSelection ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            evt.menu.AppendSeparator();

            evt.menu.AppendAction("Select All", _ =>
            {
                m_View.Dispatch(new SelectElementsCommand(SelectElementsCommand.SelectionMode.Add, SelectableModels.ToList()));
            }, _ => DropdownMenuAction.Status.Normal);
        }
    }
}
