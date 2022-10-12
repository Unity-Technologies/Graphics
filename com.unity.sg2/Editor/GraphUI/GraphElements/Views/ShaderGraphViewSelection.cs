using System.Linq;
using Unity.GraphToolsFoundation.Editor;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    class ShaderGraphViewSelection : GraphViewSelection
    {
        /// <summary>
        /// This class overrides the graph views selection and context menu operation handler
        /// We need it in order to handle cut operations with a slightly different logic,
        /// and to change the default context menu options
        /// </summary>
        public ShaderGraphViewSelection(GraphView view, GraphModelStateComponent graphModelState, SelectionStateComponent selectionState)
            : base(view, graphModelState, selectionState) { }

        ShaderGraphModel shaderGraphModel => m_GraphModelState.GraphModel as ShaderGraphModel;

        /// <summary>
        /// Adds items related to the selection to the contextual menu.
        /// </summary>
        /// <param name="evt">The contextual menu event.</param>
        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (evt.menu.MenuItems().Count > 0)
                evt.menu.AppendSeparator();

            if (GetSelection().Any(model => model is AbstractNodeModel) || GetSelection().Count == 0)
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

        // This calls CopySelection()
        protected override void CutSelection()
        {
            shaderGraphModel.isCutOperation = true;
            base.CutSelection();
        }

        void AddInputEdgesToSelection()
        {
            using (var updater = m_SelectionState.UpdateScope)
            {
                var selection = GetSelection();
                for(var index = 0; index < selection.Count; index++)
                {
                    var element = selection[index];
                    if (element is AbstractNodeModel nodeModel)
                    {
                        var edges = nodeModel.GetConnectedWires().ToList();
                        foreach(var edge in edges)
                        {
                            // Skip output edges
                            if(edge.FromPort.NodeModel == nodeModel)
                                continue;
                            updater.SelectElement(edge, true);
                        }
                    }
                }
            }
        }

        protected override void CopySelection()
        {
            AddInputEdgesToSelection();
            base.CopySelection();
        }

        protected override void DuplicateSelection()
        {
            AddInputEdgesToSelection();
            base.DuplicateSelection();
        }

        protected override void Paste()
        {
            base.Paste();
            shaderGraphModel.isCutOperation = false;
        }
    }
}
