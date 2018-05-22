using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEditor.Graphing.Util;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEngine.Experimental.UIElements;
using Edge = UnityEditor.Experimental.UIElements.GraphView.Edge;
using Node = UnityEditor.Experimental.UIElements.GraphView.Node;
#if !UNITY_2018_1
using UnityEditor.Graphs;
#endif

namespace UnityEditor.ShaderGraph.Drawing
{
    public sealed class MaterialGraphView : GraphView
    {
        public MaterialGraphView()
        {
            AddStyleSheetPath("Styles/MaterialGraphView");
            serializeGraphElements = SerializeGraphElementsImplementation;
            canPasteSerializedData = CanPasteSerializedDataImplementation;
            unserializeAndPaste = UnserializeAndPasteImplementation;
            deleteSelection = DeleteSelectionImplementation;
            RegisterCallback<DragUpdatedEvent>(OnDragUpdatedEvent);
            RegisterCallback<DragPerformEvent>(OnDragPerformEvent);
        }

        protected override bool canCopySelection
        {
            get { return selection.OfType<Node>().Any() || selection.OfType<GroupNode>().Any() || selection.OfType<BlackboardField>().Any(); }
        }

        public MaterialGraphView(AbstractMaterialGraph graph) : this()
        {
            this.graph = graph;
        }

        public AbstractMaterialGraph graph { get; private set; }
        public Action onConvertToSubgraphClick { get; set; }

        public override List<Port> GetCompatiblePorts(Port startAnchor, NodeAdapter nodeAdapter)
        {
            var compatibleAnchors = new List<Port>();
            var startSlot = startAnchor.GetSlot();
            if (startSlot == null)
                return compatibleAnchors;

            var startStage = startSlot.stageCapability;
            if (startStage == ShaderStageCapability.All)
                startStage = NodeUtils.GetEffectiveShaderStageCapability(startSlot, true) & NodeUtils.GetEffectiveShaderStageCapability(startSlot, false);

            foreach (var candidateAnchor in ports.ToList())
            {
                var candidateSlot = candidateAnchor.GetSlot();
                if (!startSlot.IsCompatibleWith(candidateSlot))
                    continue;

                if (startStage != ShaderStageCapability.All)
                {
                    var candidateStage = candidateSlot.stageCapability;
                    if (candidateStage == ShaderStageCapability.All)
                        candidateStage = NodeUtils.GetEffectiveShaderStageCapability(candidateSlot, true)
                            & NodeUtils.GetEffectiveShaderStageCapability(candidateSlot, false);
                    if (candidateStage != ShaderStageCapability.All && candidateStage != startStage)
                        continue;
                }

                compatibleAnchors.Add(candidateAnchor);
            }
            return compatibleAnchors;
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            base.BuildContextualMenu(evt);
            if (evt.target is GraphView || evt.target is Node)
            {
                evt.menu.AppendAction("Convert To Sub-graph", ConvertToSubgraph, ConvertToSubgraphStatus);
                evt.menu.AppendAction("Convert To Inline Node", ConvertToInlineNode, ConvertToInlineNodeStatus);
                evt.menu.AppendAction("Convert To Property", ConvertToProperty, ConvertToPropertyStatus);
                if (selection.OfType<MaterialNodeView>().Count() == 1)
                {
                    evt.menu.AppendSeparator();
                    evt.menu.AppendAction("Open Documentation", SeeDocumentation, SeeDocumentationStatus);
                }
                if (selection.OfType<MaterialNodeView>().Count() == 1 && selection.OfType<MaterialNodeView>().First().node is SubGraphNode)
                {
                    evt.menu.AppendSeparator();
                    evt.menu.AppendAction("Open Sub Graph", OpenSubGraph, ContextualMenu.MenuAction.StatusFlags.Normal);
                }
            }
            else if (evt.target is BlackboardField)
            {
                evt.menu.AppendAction("Delete", (e) => DeleteSelectionImplementation("Delete", AskUser.DontAskUser), (e) => canDeleteSelection ? ContextualMenu.MenuAction.StatusFlags.Normal : ContextualMenu.MenuAction.StatusFlags.Disabled);
            }
            if (evt.target is MaterialGraphView)
            {
                evt.menu.AppendAction("Collapse Previews", CollapsePreviews, ContextualMenu.MenuAction.StatusFlags.Normal);
                evt.menu.AppendAction("Expand Previews", ExpandPreviews, ContextualMenu.MenuAction.StatusFlags.Normal);
                evt.menu.AppendSeparator();
            }
        }

        void CollapsePreviews()
        {
            graph.owner.RegisterCompleteObjectUndo("Collapse Previews");
            foreach (AbstractMaterialNode node in graph.GetNodes<AbstractMaterialNode>())
            {
                node.previewExpanded = false;
            }
        }

        void ExpandPreviews()
        {
            graph.owner.RegisterCompleteObjectUndo("Expand Previews");
            foreach (AbstractMaterialNode node in graph.GetNodes<AbstractMaterialNode>())
            {
                node.previewExpanded = true;
            }
        }

        void SeeDocumentation()
        {
            var node = selection.OfType<MaterialNodeView>().First().node;
            if (node.documentationURL != null)
                System.Diagnostics.Process.Start(node.documentationURL);
        }

        void OpenSubGraph()
        {
            SubGraphNode subgraphNode = selection.OfType<MaterialNodeView>().First().node as SubGraphNode;

            var path = AssetDatabase.GetAssetPath(subgraphNode.subGraphAsset);
            ShaderGraphImporterEditor.ShowGraphEditWindow(path);
        }

        ContextualMenu.MenuAction.StatusFlags SeeDocumentationStatus()
        {
            if (selection.OfType<MaterialNodeView>().First().node.documentationURL == null)
                return ContextualMenu.MenuAction.StatusFlags.Disabled;
            return ContextualMenu.MenuAction.StatusFlags.Normal;
        }

        ContextualMenu.MenuAction.StatusFlags ConvertToPropertyStatus()
        {
            if (selection.OfType<MaterialNodeView>().Any(v => v.node != null))
            {
                if (selection.OfType<MaterialNodeView>().Any(v => v.node is IPropertyFromNode))
                    return ContextualMenu.MenuAction.StatusFlags.Normal;
                return ContextualMenu.MenuAction.StatusFlags.Disabled;
            }
            return ContextualMenu.MenuAction.StatusFlags.Hidden;
        }

        void ConvertToProperty()
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

        ContextualMenu.MenuAction.StatusFlags ConvertToInlineNodeStatus()
        {
            if (selection.OfType<MaterialNodeView>().Any(v => v.node != null))
            {
                if (selection.OfType<MaterialNodeView>().Any(v => v.node is PropertyNode))
                    return ContextualMenu.MenuAction.StatusFlags.Normal;
                return ContextualMenu.MenuAction.StatusFlags.Disabled;
            }
            return ContextualMenu.MenuAction.StatusFlags.Hidden;
        }

        void ConvertToInlineNode()
        {
            var selectedNodeViews = selection.OfType<MaterialNodeView>()
                .Select(x => x.node)
                .OfType<PropertyNode>();

            foreach (var propNode in selectedNodeViews)
                ((AbstractMaterialGraph)propNode.owner).ReplacePropertyNodeWithConcreteNode(propNode);
        }

        ContextualMenu.MenuAction.StatusFlags ConvertToSubgraphStatus()
        {
            if (onConvertToSubgraphClick == null) return ContextualMenu.MenuAction.StatusFlags.Hidden;
            return selection.OfType<MaterialNodeView>().Any(v => v.node != null) ? ContextualMenu.MenuAction.StatusFlags.Normal : ContextualMenu.MenuAction.StatusFlags.Hidden;
        }

        void ConvertToSubgraph()
        {
            onConvertToSubgraphClick();
        }

        string SerializeGraphElementsImplementation(IEnumerable<GraphElement> elements)
        {
            var nodes = elements.OfType<MaterialNodeView>().Select(x => (INode)x.node);
            var edges = elements.OfType<Edge>().Select(x => x.userData).OfType<IEdge>();
            var properties = selection.OfType<BlackboardField>().Select(x => x.userData as IShaderProperty);

            // Collect the property nodes and get the corresponding properties
            var propertyNodeGuids = nodes.OfType<PropertyNode>().Select(x => x.propertyGuid);
            var metaProperties = this.graph.properties.Where(x => propertyNodeGuids.Contains(x.guid));

            var graph = new CopyPasteGraph(this.graph.guid, nodes, edges, properties, metaProperties);
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
            foreach (var selectable in selection)
            {
                var field = selectable as BlackboardField;
                if (field != null && field.userData != null)
                {
                    if (EditorUtility.DisplayDialog("Sub Graph Will Change", "If you remove a property and save the sub graph, you might change other graphs that are using this sub graph.\n\nDo you want to continue?", "Yes", "No"))
                        break;
                    return;
                }
            }

            graph.owner.RegisterCompleteObjectUndo(operationName);
            graph.RemoveElements(selection.OfType<MaterialNodeView>().Where(v => !(v.node is SubGraphOutputNode)).Select(x => (INode)x.node), selection.OfType<Edge>().Select(x => x.userData).OfType<IEdge>());

            foreach (var selectable in selection)
            {
                var field = selectable as BlackboardField;
                if (field != null && field.userData != null)
                {
                    var property = (IShaderProperty)field.userData;
                    graph.RemoveShaderProperty(property.guid);
                }
            }

            selection.Clear();
        }

        #region Drag and drop

        static void OnDragUpdatedEvent(DragUpdatedEvent e)
        {
            var selection = DragAndDrop.GetGenericData("DragSelection") as List<ISelectable>;

            if (selection != null && (selection.OfType<BlackboardField>().Any() ))
            {
                DragAndDrop.visualMode = e.ctrlKey ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Move;
            }
        }

        void OnDragPerformEvent(DragPerformEvent e)
        {
            var selection = DragAndDrop.GetGenericData("DragSelection") as List<ISelectable>;
            if (selection == null)
                return;

            IEnumerable<BlackboardField> fields = selection.OfType<BlackboardField>();
            if (!fields.Any())
                return;

            Vector2 localPos = (e.currentTarget as VisualElement).ChangeCoordinatesTo(contentViewContainer, e.localMousePosition);

            foreach (BlackboardField field in fields)
            {
                IShaderProperty property = field.userData as IShaderProperty;
                if (property == null)
                    continue;

                var node = new PropertyNode();

                var drawState = node.drawState;
                var position = drawState.position;
                position.x = localPos.x;
                position.y = localPos.y;
                drawState.position = position;
                node.drawState = drawState;

                graph.owner.RegisterCompleteObjectUndo("Added Property");
                graph.AddNode(node);
                node.propertyGuid = property.guid;
            }
        }

        #endregion
    }

    public static class GraphViewExtensions
    {
        internal static void InsertCopyPasteGraph(this MaterialGraphView graphView, CopyPasteGraph copyGraph)
        {
            if (copyGraph == null)
                return;

            // Make new properties from the copied graph
            foreach (IShaderProperty property in copyGraph.properties)
            {
                string propertyName = graphView.graph.SanitizePropertyName(property.displayName);
                IShaderProperty copiedProperty = property.Copy();
                copiedProperty.displayName = propertyName;
                graphView.graph.AddShaderProperty(copiedProperty);

                // Update the property nodes that depends on the copied node
                var dependentPropertyNodes = copyGraph.GetNodes<PropertyNode>().Where(x => x.propertyGuid == property.guid);
                foreach (var node in dependentPropertyNodes)
                {
                    node.owner = graphView.graph;
                    node.propertyGuid = copiedProperty.guid;
                }
            }

            using (var remappedNodesDisposable = ListPool<INode>.GetDisposable())
            {
                using (var remappedEdgesDisposable = ListPool<IEdge>.GetDisposable())
                {
                    var remappedNodes = remappedNodesDisposable.value;
                    var remappedEdges = remappedEdgesDisposable.value;
                    graphView.graph.PasteGraph(copyGraph, remappedNodes, remappedEdges);

                    if (graphView.graph.guid != copyGraph.sourceGraphGuid)
                    {
                        // Compute the mean of the copied nodes.
                        Vector2 centroid = Vector2.zero;
                        var count = 1;
                        foreach (var node in remappedNodes)
                        {
                            var position = node.drawState.position.position;
                            centroid = centroid + (position - centroid) / count;
                            ++count;
                        }

                        // Get the center of the current view
                        var viewCenter = graphView.contentViewContainer.WorldToLocal(graphView.layout.center);

                        foreach (var node in remappedNodes)
                        {
                            var drawState = node.drawState;
                            var positionRect = drawState.position;
                            var position = positionRect.position;
                            position += viewCenter - centroid;
                            positionRect.position = position;
                            drawState.position = positionRect;
                            node.drawState = drawState;
                        }
                    }

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
}
