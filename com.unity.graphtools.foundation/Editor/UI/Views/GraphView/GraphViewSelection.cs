using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    public class GraphViewSelection : ViewSelection
    {
        /// <inheritdoc />
        public override IEnumerable<IGraphElementModel> SelectableModels
        {
            get => m_GraphModelState.GraphModel.GraphElementModels.Where(t => !(t is IVariableDeclarationModel) && t.IsSelectable());
        }
        /// <inheritdoc />
        public GraphViewSelection(GraphView view, GraphModelStateComponent graphModelState, SelectionStateComponent selectionState)
            : base(view, graphModelState, selectionState) { }

        /// <inheritdoc />
        protected override Vector2 GetPasteDelta(CopyPasteData data)
        {
            var mousePosition = GraphViewStaticBridge.GetMousePosition();
            mousePosition = ((GraphView)m_View).ContentViewContainer.WorldToLocal(mousePosition);
            return mousePosition - data.topLeftNodePosition;
        }

        /// <inheritdoc />
        protected override void OnValidateCommand(ValidateCommandEvent evt)
        {
            if (((GraphView)m_View).DisplayMode == GraphViewDisplayMode.Interactive)
            {
                base.OnValidateCommand(evt);
            }
        }

        /// <inheritdoc />
        protected override void OnExecuteCommand(ExecuteCommandEvent evt)
        {
            if (((GraphView)m_View).DisplayMode == GraphViewDisplayMode.Interactive)
            {
                base.OnExecuteCommand(evt);
            }
        }

        /// <inheritdoc />
        protected override CopyPasteData BuildCopyPasteData(HashSet<IGraphElementModel> elementsToCopySet)
        {
            var copyPaste = CopyPasteData.GatherCopiedElementsData(null, elementsToCopySet.ToList());
            return copyPaste;
        }

        /// <inheritdoc />
        protected override HashSet<IGraphElementModel>  CollectCopyableGraphElements(IEnumerable<IGraphElementModel> elements)
        {
            var elementsToCopySet = new HashSet<IGraphElementModel>();
            var elementList = elements.ToList();
            FilterElements(elementList, elementsToCopySet, IsCopiable);

            var nodesInPlacemat = new HashSet<INodeModel>();
            // Also collect hovering list of nodes
            foreach (var placemat in elementList.OfType<IPlacematModel>())
            {
                var placematUI = placemat.GetView<Placemat>(m_View);
                placematUI?.ActOnGraphElementsOver(
                    el =>
                    {
                        if (el.Model is INodeModel node)
                            nodesInPlacemat.Add(node);
                        FilterElements(new[] { el.GraphElementModel },
                            elementsToCopySet,
                            IsCopiable);
                        return false;
                    },
                    true);
            }

            // copying edges between nodes in placemats
            foreach (var edge in m_GraphModelState.GraphModel.EdgeModels)
            {
                if (nodesInPlacemat.Contains(edge.FromPort?.NodeModel) && nodesInPlacemat.Contains(edge.ToPort?.NodeModel))
                    elementsToCopySet.Add(edge);
            }

            return elementsToCopySet;
        }
    }
}
