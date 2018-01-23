using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEditor.Graphing.Util;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.ShaderGraph;
using UnityEngine.Experimental.UIElements;
using Edge = UnityEditor.Experimental.UIElements.GraphView.Edge;
using MouseButton = UnityEngine.Experimental.UIElements.MouseButton;

namespace UnityEditor.ShaderGraph.Drawing
{
    public sealed class MaterialGraphView : GraphView
    {
        public AbstractMaterialGraph graph { get; private set; }
        public Action onConvertToSubgraphClick { get; set; }

        public override List<Port> GetCompatiblePorts(Port startAnchor, NodeAdapter nodeAdapter)
        {
            var compatibleAnchors = new List<Port>();
            var startSlot = startAnchor.GetSlot();
            if (startSlot == null)
                return compatibleAnchors;

            var startStage = startSlot.shaderStage;
            if (startStage == ShaderStage.Dynamic)
                startStage = NodeUtils.FindEffectiveShaderStage(startSlot.owner, startSlot.isOutputSlot);

            foreach (var candidateAnchor in ports.ToList())
            {
                var candidateSlot = candidateAnchor.GetSlot();
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

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);
            evt.menu.AppendAction("Convert To Sub-graph", ConvertToSubgraph, ConvertToSubgraphStatus);
            evt.menu.AppendAction("Convert To Inline Node", ConvertToInlineNode, ConvertToInlineNodeStatus);
            evt.menu.AppendAction("Convert To Property", ConvertToProperty, ConvertToPropertyStatus);
        }

        ContextualMenu.MenuAction.StatusFlags ConvertToPropertyStatus(EventBase eventBase)
        {
            if (selection.OfType<MaterialNodeView>().Any(v => v.node != null))
            {
                if (selection.OfType<MaterialNodeView>().Any(v => v.node is IPropertyFromNode))
                    return ContextualMenu.MenuAction.StatusFlags.Normal;
                return ContextualMenu.MenuAction.StatusFlags.Disabled;
            }
            return ContextualMenu.MenuAction.StatusFlags.Hidden;
        }

        void ConvertToProperty(EventBase eventBase)
        {
            var selectedNodeViews = selection.OfType<MaterialNodeView>().Select(x => x.node).ToList();
            foreach (var node in selectedNodeViews)
            {
                if (!(node is IPropertyFromNode))
                    continue;

                var converter = node as IPropertyFromNode;
                var prop = converter.AsShaderProperty();
                graph.AddShaderProperty(prop);

                var propNode = new PropertyNode();
                propNode.drawState = node.drawState;
                graph.AddNode(propNode);
                propNode.propertyGuid = prop.guid;

                var oldSlot = node.FindSlot<MaterialSlot>(converter.outputSlotId);
                var newSlot = propNode.FindSlot<MaterialSlot>(PropertyNode.OutputSlotId);

                foreach (var edge in graph.GetEdges(oldSlot.slotReference))
                    graph.Connect(newSlot.slotReference, edge.inputSlot);

                graph.RemoveNode(node);
            }
        }

        ContextualMenu.MenuAction.StatusFlags ConvertToInlineNodeStatus(EventBase eventBase)
        {
            if (selection.OfType<MaterialNodeView>().Any(v => v.node != null))
            {
                if (selection.OfType<MaterialNodeView>().Any(v => v.node is PropertyNode))
                    return ContextualMenu.MenuAction.StatusFlags.Normal;
                return ContextualMenu.MenuAction.StatusFlags.Disabled;
            }
            return ContextualMenu.MenuAction.StatusFlags.Hidden;
        }

        void ConvertToInlineNode(EventBase eventBase)
        {
            var selectedNodeViews = selection.OfType<MaterialNodeView>()
                .Select(x => x.node)
                .OfType<PropertyNode>();

            foreach (var propNode in selectedNodeViews)
                ((AbstractMaterialGraph)propNode.owner).ReplacePropertyNodeWithConcreteNode(propNode);
        }

        ContextualMenu.MenuAction.StatusFlags ConvertToSubgraphStatus(EventBase eventBase)
        {
            if (onConvertToSubgraphClick == null) return ContextualMenu.MenuAction.StatusFlags.Hidden;
            return selection.OfType<MaterialNodeView>().Any(v => v.node != null) ? ContextualMenu.MenuAction.StatusFlags.Normal : ContextualMenu.MenuAction.StatusFlags.Hidden;
        }

        void ConvertToSubgraph(EventBase eventBase)
        {
            onConvertToSubgraphClick();
        }

        public delegate void OnSelectionChanged(IEnumerable<INode> nodes);

        public OnSelectionChanged onSelectionChanged;

        public MaterialGraphView()
        {
            serializeGraphElements = SerializeGraphElementsImplementation;
            canPasteSerializedData = CanPasteSerializedDataImplementation;
            unserializeAndPaste = UnserializeAndPasteImplementation;
            deleteSelection = DeleteSelectionImplementation;
        }

        public MaterialGraphView(AbstractMaterialGraph graph) : this()
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

        string SerializeGraphElementsImplementation(IEnumerable<GraphElement> elements)
        {
            var graph = new CopyPasteGraph(elements.OfType<MaterialNodeView>().Select(x => (INode)x.node), elements.OfType<Edge>().Select(x => x.userData).OfType<IEdge>());
            return JsonUtility.ToJson(graph, true);
        }

        bool CanPasteSerializedDataImplementation(string serializedData)
        {
            return CopyPasteGraph.FromJson(serializedData) != null;
        }

        void UnserializeAndPasteImplementation(string operationName, string serializedData)
        {
            graph.owner.RegisterCompleteObjectUndo(operationName);
            var pastedGraph = CopyPasteGraph.FromJson(serializedData);
            this.InsertCopyPasteGraph(pastedGraph);
        }

        void DeleteSelectionImplementation(string operationName, GraphView.AskUser askUser)
        {
            graph.owner.RegisterCompleteObjectUndo(operationName);
            graph.RemoveElements(selection.OfType<MaterialNodeView>().Select(x => (INode)x.node), selection.OfType<Edge>().Select(x => x.userData).OfType<IEdge>());
        }
    }

    public static class GraphViewExtensions
    {
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
