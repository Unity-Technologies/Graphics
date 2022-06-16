using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.GraphToolsFoundation.Overdrive.BasicModel;
using UnityEditor.ShaderGraph.GraphDelta;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    public class ShaderGraphViewSelection : GraphViewSelection
    {
        public ShaderGraphViewSelection(GraphView view, GraphModelStateComponent graphModelState, SelectionStateComponent selectionState)
            : base(view, graphModelState, selectionState)
        {
            m_GraphModelStateComponent = graphModelState;
        }

        GraphModelStateComponent m_GraphModelStateComponent;

        ShaderGraphModel shaderGraphModel => m_GraphModelStateComponent.GraphModel as ShaderGraphModel;

        /// <summary>
        /// Adds items related to the selection to the contextual menu.
        /// </summary>
        /// <param name="evt">The contextual menu event.</param>
        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (evt.menu.MenuItems().Count > 0)
                evt.menu.AppendSeparator();

            if (GetSelection().Any(model => model is INodeModel) || GetSelection().Count == 0)
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

        void BuildClipboard()
        {
            var currentSelection = GetSelection();
            var nodeToModelClipboard = new Dictionary<string, IGraphElementModel>();
            var nodeToEdgesClipboard = new Dictionary<string, IEnumerable<IEdgeModel>>();

            foreach (var selectedModel in currentSelection)
            {
                switch (selectedModel)
                {
                    case GraphDataNodeModel graphDataNodeModel:
                        nodeToModelClipboard.Add(graphDataNodeModel.graphDataName, graphDataNodeModel);
                        nodeToEdgesClipboard.Add(graphDataNodeModel.graphDataName, graphDataNodeModel.GetConnectedEdges().ToList());
                        break;
                    case GraphDataVariableNodeModel graphDataVariableNodeModel:
                        nodeToModelClipboard.Add(graphDataVariableNodeModel.graphDataName, graphDataVariableNodeModel);
                        nodeToEdgesClipboard.Add(graphDataVariableNodeModel.graphDataName, graphDataVariableNodeModel.GetConnectedEdges().ToList());
                        break;
                }
            }

            shaderGraphModel.SetClipboard(nodeToModelClipboard, nodeToEdgesClipboard);
        }

        protected override void CutSelection()
        {
            shaderGraphModel.isCutOperation = true;
            base.CutSelection();
        }

        protected override void CopySelection()
        {
            BuildClipboard();
            base.CopySelection();
        }

        protected override void DuplicateSelection()
        {
            BuildClipboard();
            base.DuplicateSelection();
        }

        protected override void Paste()
        {
            base.Paste();
            shaderGraphModel.isCutOperation = false;
        }
    }
}
