using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEditor.Graphing.Util;
using UnityEngine;
using UnityEngine.Graphing;
using UnityEngine.MaterialGraph;
using UnityEngine.Experimental.UIElements;
using Edge = UnityEditor.Experimental.UIElements.GraphView.Edge;
using MouseButton = UnityEngine.Experimental.UIElements.MouseButton;

namespace UnityEditor.MaterialGraph.Drawing
{
    public sealed class MaterialGraphView : GraphView
    {
        public AbstractMaterialGraph graph { get; private set; }

        public override List<NodeAnchor> GetCompatibleAnchors(NodeAnchor startAnchor, NodeAdapter nodeAdapter)
        {
            var compatibleAnchors = new List<NodeAnchor>();
            var startSlot = startAnchor.userData as MaterialSlot;
            if (startSlot == null)
                return compatibleAnchors;

            var startStage = startSlot.shaderStage;
            if (startStage == ShaderStage.Dynamic)
                startStage = NodeUtils.FindEffectiveShaderStage(startSlot.owner, startSlot.isOutputSlot);

            foreach (var candidateAnchor in anchors.ToList())
            {
                var candidateSlot = candidateAnchor.userData as MaterialSlot;
                if (!startSlot.IsCompatibleWith(candidateSlot))
                    continue;

                if (startStage != ShaderStage.Dynamic)
                {
                    var candidateStage = candidateSlot.shaderStage;
                    if (candidateStage == ShaderStage.Dynamic)
                        candidateStage = NodeUtils.FindEffectiveShaderStage(candidateSlot.owner, !startSlot.isOutputSlot);
                    if (candidateStage != ShaderStage.Dynamic && candidateStage != startStage)
                        continue;
                }

                compatibleAnchors.Add(candidateAnchor);
            }
            return compatibleAnchors;
        }

        public delegate void OnSelectionChanged(IEnumerable<INode> nodes);

        public OnSelectionChanged onSelectionChanged;

        public MaterialGraphView(AbstractMaterialGraph graph)
        {
            this.graph = graph;
        }

        void SelectionChanged()
        {
            var selectedNodes = selection.OfType<MaterialNodeView>().Where(x => x.userData is INode);
            if (onSelectionChanged != null)
                onSelectionChanged(selectedNodes.Select(x => x.userData as INode));
        }

        public override void AddToSelection(ISelectable selectable)
        {
            base.AddToSelection(selectable);
            SelectionChanged();
        }

        public override void RemoveFromSelection(ISelectable selectable)
        {
            base.RemoveFromSelection(selectable);
            SelectionChanged();
        }

        public override void ClearSelection()
        {
            base.ClearSelection();
            SelectionChanged();
        }
    }

    public static class GraphViewExtensions
    {
        internal static CopyPasteGraph SelectionAsCopyPasteGraph(this MaterialGraphView graphView)
        {
            return new CopyPasteGraph(graphView.selection.OfType<MaterialNodeView>().Select(x => (INode) x.node), graphView.selection.OfType<Edge>().Select(x => x.userData).OfType<IEdge>());
        }

        internal static void InsertCopyPasteGraph(this MaterialGraphView graphView, CopyPasteGraph copyGraph)
        {
            if (copyGraph == null)
                return;

            using (var remappedNodesDisposable = ListPool<INode>.GetDisposable())
            using (var remappedEdgesDisposable = ListPool<IEdge>.GetDisposable())
            {
                var remappedNodes = remappedNodesDisposable.value;
                var remappedEdges = remappedEdgesDisposable.value;
                copyGraph.InsertInGraph(graphView.graph, remappedNodes, remappedEdges);

                // Add new elements to selection
                graphView.ClearSelection();
                graphView.graphElements.ForEach(element =>
                {
                    var edge = element as Edge;
                    if (edge != null && remappedEdges.Contains(edge.userData as IEdge))
                        graphView.AddToSelection(edge);

                    var nodeView = element as MaterialNodeView;
                    if (nodeView != null && remappedNodes.Contains(nodeView.node))
                        graphView.AddToSelection(nodeView);
                });
            }
        }
    }
}
