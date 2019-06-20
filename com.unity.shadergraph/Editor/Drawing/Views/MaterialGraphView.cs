using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Graphing.Util;
using UnityEngine;
using UnityEditor.Graphing;
using Object = UnityEngine.Object;
using UnityEditor.Graphs;

using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using Edge = UnityEditor.Experimental.GraphView.Edge;
using Node = UnityEditor.Experimental.GraphView.Node;

namespace UnityEditor.ShaderGraph.Drawing
{
    sealed class MaterialGraphView : GraphView
    {
        public MaterialGraphView()
        {
            styleSheets.Add(Resources.Load<StyleSheet>("Styles/MaterialGraphView"));
            serializeGraphElements = SerializeGraphElementsImplementation;
            canPasteSerializedData = CanPasteSerializedDataImplementation;
            unserializeAndPaste = UnserializeAndPasteImplementation;
            deleteSelection = DeleteSelectionImplementation;
            RegisterCallback<DragUpdatedEvent>(OnDragUpdatedEvent);
            RegisterCallback<DragPerformEvent>(OnDragPerformEvent);
        }

        protected override bool canCopySelection
        {
            get { return selection.OfType<Node>().Any() || selection.OfType<Group>().Any() || selection.OfType<BlackboardField>().Any(); }
        }

        public MaterialGraphView(GraphData graph) : this()
        {
            this.graph = graph;
        }

        public GraphData graph { get; private set; }
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

                evt.menu.AppendAction("Group Selection", GroupSelection, (a) =>
                {
                    List<ISelectable> filteredSelection = new List<ISelectable>();

                    foreach (ISelectable selectedObject in selection)
                    {
                        if (selectedObject is Group)
                            return DropdownMenuAction.Status.Disabled;
                        VisualElement ve = selectedObject as VisualElement;
                        if (ve.userData is AbstractMaterialNode)
                        {
                            var selectedNode = selectedObject as Node;
                            if (selectedNode.GetContainingScope() is Group)
                                return DropdownMenuAction.Status.Disabled;

                            filteredSelection.Add(selectedObject);
                        }
                    }

                    if (filteredSelection.Count > 0)
                        return DropdownMenuAction.Status.Normal;
                    else
                        return DropdownMenuAction.Status.Disabled;
                });

                evt.menu.AppendAction("Ungroup Selection", RemoveFromGroupNode, (a) =>
                {
                    List<ISelectable> filteredSelection = new List<ISelectable>();

                    foreach (ISelectable selectedObject in selection)
                    {
                        if (selectedObject is Group)
                            return DropdownMenuAction.Status.Disabled;
                        VisualElement ve = selectedObject as VisualElement;
                        if (ve.userData is AbstractMaterialNode)
                        {
                            var selectedNode = selectedObject as Node;
                            if (selectedNode.GetContainingScope() is Group)
                                filteredSelection.Add(selectedObject);
                        }
                    }

                    if (filteredSelection.Count > 0)
                        return DropdownMenuAction.Status.Normal;
                    else
                        return DropdownMenuAction.Status.Disabled;
                });

                if (selection.OfType<IShaderNodeView>().Count() == 1)
                {
                    evt.menu.AppendSeparator();
                    evt.menu.AppendAction("Open Documentation", SeeDocumentation, SeeDocumentationStatus);
                }
                if (selection.OfType<IShaderNodeView>().Count() == 1 && selection.OfType<IShaderNodeView>().First().node is SubGraphNode)
                {
                    evt.menu.AppendSeparator();

                    evt.menu.AppendAction("Open Sub Graph", OpenSubGraph, (a) => DropdownMenuAction.Status.Normal);
                }
            }
            else if (evt.target is BlackboardField)
            {
                evt.menu.AppendAction("Delete", (e) => DeleteSelectionImplementation("Delete", AskUser.DontAskUser), (e) => canDeleteSelection ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            }
            if (evt.target is MaterialGraphView)
            {
                evt.menu.AppendAction("Collapse Previews", CollapsePreviews, (a) => DropdownMenuAction.Status.Normal);
                evt.menu.AppendAction("Expand Previews", ExpandPreviews, (a) => DropdownMenuAction.Status.Normal);
                evt.menu.AppendSeparator();
            }
        }

        void GroupSelection(DropdownMenuAction action)
        {
            Vector2 pos = action.eventInfo.localMousePosition;

            string title = "New Group";
            GroupData groupData = new GroupData(title, pos);

            graph.owner.RegisterCompleteObjectUndo("Create Group Node");
            graph.AddGroupData(groupData);

            foreach (var shaderNodeView in selection.OfType<IShaderNodeView>())
                {
                graph.SetNodeGroup(shaderNodeView.node, groupData);
            }
        }

        void RemoveFromGroupNode(DropdownMenuAction action)
        {
            graph.owner.RegisterCompleteObjectUndo("Ungroup Node(s)");
            foreach (ISelectable selectable in selection)
            {
                var node = selectable as Node;
                if(node == null)
                    continue;

                Group group = node.GetContainingScope() as Group;
                if (group != null)
                {
                    group.RemoveElement(node);
                }
            }
        }

        void CollapsePreviews(DropdownMenuAction action)
        {
            graph.owner.RegisterCompleteObjectUndo("Collapse Previews");
            foreach (AbstractMaterialNode node in graph.GetNodes<AbstractMaterialNode>())
            {
                node.previewExpanded = false;
            }
        }

        void ExpandPreviews(DropdownMenuAction action)
        {
            graph.owner.RegisterCompleteObjectUndo("Expand Previews");
            foreach (AbstractMaterialNode node in graph.GetNodes<AbstractMaterialNode>())
            {
                node.previewExpanded = true;
            }
        }

        void SeeDocumentation(DropdownMenuAction action)
        {
            var node = selection.OfType<IShaderNodeView>().First().node;
            if (node.documentationURL != null)
                System.Diagnostics.Process.Start(node.documentationURL);
        }

        void OpenSubGraph(DropdownMenuAction action)
        {
            SubGraphNode subgraphNode = selection.OfType<IShaderNodeView>().First().node as SubGraphNode;

            var path = AssetDatabase.GetAssetPath(subgraphNode.subGraphAsset);
            ShaderGraphImporterEditor.ShowGraphEditWindow(path);
        }

        DropdownMenuAction.Status SeeDocumentationStatus(DropdownMenuAction action)
        {
            if (selection.OfType<IShaderNodeView>().First().node.documentationURL == null)
                return DropdownMenuAction.Status.Disabled;
            return DropdownMenuAction.Status.Normal;
        }

        DropdownMenuAction.Status ConvertToPropertyStatus(DropdownMenuAction action)
        {
            if (selection.OfType<IShaderNodeView>().Any(v => v.node != null))
            {
                if (selection.OfType<IShaderNodeView>().Any(v => v.node is IPropertyFromNode))
                    return DropdownMenuAction.Status.Normal;
                return DropdownMenuAction.Status.Disabled;
            }
            return DropdownMenuAction.Status.Hidden;
        }

        void ConvertToProperty(DropdownMenuAction action)
        {
            graph.owner.RegisterCompleteObjectUndo("Convert to Property");
            var selectedNodeViews = selection.OfType<IShaderNodeView>().Select(x => x.node).ToList();
            foreach (var node in selectedNodeViews)
            {
                if (!(node is IPropertyFromNode))
                    continue;

                var converter = node as IPropertyFromNode;
                var prop = converter.AsShaderProperty();
                prop.displayName = graph.SanitizePropertyName(prop.displayName, prop.guid);
                graph.AddShaderProperty(prop);

                var propNode = new PropertyNode();
                propNode.drawState = node.drawState;
                propNode.groupGuid = node.groupGuid;
                graph.AddNode(propNode);
                propNode.propertyGuid = prop.guid;

                var oldSlot = node.FindSlot<MaterialSlot>(converter.outputSlotId);
                var newSlot = propNode.FindSlot<MaterialSlot>(PropertyNode.OutputSlotId);

                foreach (var edge in graph.GetEdges(oldSlot.slotReference))
                    graph.Connect(newSlot.slotReference, edge.inputSlot);

                graph.RemoveNode(node);
            }
        }

        DropdownMenuAction.Status ConvertToInlineNodeStatus(DropdownMenuAction action)
        {
            if (selection.OfType<IShaderNodeView>().Any(v => v.node != null))
            {
                if (selection.OfType<IShaderNodeView>().Any(v => v.node is PropertyNode))
                    return DropdownMenuAction.Status.Normal;
                return DropdownMenuAction.Status.Disabled;
            }
            return DropdownMenuAction.Status.Hidden;
        }

        void ConvertToInlineNode(DropdownMenuAction action)
        {
            graph.owner.RegisterCompleteObjectUndo("Convert to Inline Node");
            var selectedNodeViews = selection.OfType<IShaderNodeView>()
                .Select(x => x.node)
                .OfType<PropertyNode>();

            foreach (var propNode in selectedNodeViews)
                ((GraphData)propNode.owner).ReplacePropertyNodeWithConcreteNode(propNode);
        }

        DropdownMenuAction.Status ConvertToSubgraphStatus(DropdownMenuAction action)
        {
            if (onConvertToSubgraphClick == null) return DropdownMenuAction.Status.Hidden;
            if (graph.isSubGraph) return DropdownMenuAction.Status.Hidden;
            return selection.OfType<IShaderNodeView>().Any(v => v.node != null) ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Hidden;
        }

        void ConvertToSubgraph(DropdownMenuAction action)
        {
            onConvertToSubgraphClick();
        }

        string SerializeGraphElementsImplementation(IEnumerable<GraphElement> elements)
        {
            var groups = elements.OfType<ShaderGroup>().Select(x => x.userData);
            var nodes = elements.OfType<IShaderNodeView>().Select(x => (AbstractMaterialNode)x.node);
            var edges = elements.OfType<Edge>().Select(x => x.userData).OfType<IEdge>();
            var properties = selection.OfType<BlackboardField>().Select(x => x.userData as AbstractShaderProperty);

            // Collect the property nodes and get the corresponding properties
            var propertyNodeGuids = nodes.OfType<PropertyNode>().Select(x => x.propertyGuid);
            var metaProperties = this.graph.properties.Where(x => propertyNodeGuids.Contains(x.guid));

            var graph = new CopyPasteGraph(this.graph.guid, groups, nodes, edges, properties, metaProperties);
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
                    if (graph.isSubGraph)
                    {
                        if (EditorUtility.DisplayDialog("Sub Graph Will Change", "If you remove a property and save the sub graph, you might change other graphs that are using this sub graph.\n\nDo you want to continue?", "Yes", "No"))
                            break;
                        return;
                    }
                }
            }

            graph.owner.RegisterCompleteObjectUndo(operationName);
            graph.RemoveElements(selection.OfType<IShaderNodeView>().Where(v => !(v.node is SubGraphOutputNode)).Select(x => (AbstractMaterialNode)x.node),
                selection.OfType<Edge>().Select(x => x.userData).OfType<IEdge>(),
                selection.OfType<ShaderGroup>().Select(x => x.userData));

            foreach (var selectable in selection)
            {
                var field = selectable as BlackboardField;
                if (field != null && field.userData != null)
                {
                    var property = (AbstractShaderProperty)field.userData;
                    graph.RemoveShaderProperty(property.guid);
                }
            }

            selection.Clear();
        }

        #region Drag and drop

        static bool ValidateObjectForDrop(Object obj)
        {
            return EditorUtility.IsPersistent(obj) && (obj is Texture2D || obj is Cubemap || obj is MaterialSubGraphAsset || obj is Texture2DArray || obj is Texture3D);
        }

        static void OnDragUpdatedEvent(DragUpdatedEvent e)
        {
            var selection = DragAndDrop.GetGenericData("DragSelection") as List<ISelectable>;
            bool dragging = false;
            if (selection != null)
            {
                // Blackboard
                if (selection.OfType<BlackboardField>().Any())
                    dragging = true;
            }
            else
            {
                // Handle unity objects
                var objects = DragAndDrop.objectReferences;
                foreach (Object obj in objects)
                {
                    if (ValidateObjectForDrop(obj))
                    {
                        dragging = true;
                        break;
                    }
                }
            }

            if (dragging)
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Generic;
            }
        }

        void OnDragPerformEvent(DragPerformEvent e)
        {
            Vector2 localPos = (e.currentTarget as VisualElement).ChangeCoordinatesTo(contentViewContainer, e.localMousePosition);

            var selection = DragAndDrop.GetGenericData("DragSelection") as List<ISelectable>;
            if (selection != null)
            {
                // Blackboard
                if (selection.OfType<BlackboardField>().Any())
                {
                    IEnumerable<BlackboardField> fields = selection.OfType<BlackboardField>();
                    foreach (BlackboardField field in fields)
                    {
                        CreateNode(field, localPos);
                    }
                }
            }
            else
            {
                // Handle unity objects
                var objects = DragAndDrop.objectReferences;
                foreach (Object obj in objects)
                {
                    if (ValidateObjectForDrop(obj))
                    {
                        CreateNode(obj, localPos);
                    }
                }
            }
        }

        void CreateNode(object obj, Vector2 nodePosition)
        {
            var texture2D = obj as Texture2D;
            if (texture2D != null)
            {
                graph.owner.RegisterCompleteObjectUndo("Drag Texture");

                bool isNormalMap = false;
                if (EditorUtility.IsPersistent(texture2D) && !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(texture2D)))
                {
                    var importer = AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(texture2D)) as TextureImporter;
                    if (importer != null)
                        isNormalMap = importer.textureType == TextureImporterType.NormalMap;
                }

                var node = new SampleTexture2DNode();
                if (isNormalMap)
                    node.textureType = TextureType.Normal;

                var drawState = node.drawState;
                drawState.position = new Rect(nodePosition, drawState.position.size);
                node.drawState = drawState;
                graph.AddNode(node);

                var inputslot = node.FindInputSlot<Texture2DInputMaterialSlot>(SampleTexture2DNode.TextureInputId);
                if (inputslot != null)
                    inputslot.texture = texture2D;
            }

            var textureArray = obj as Texture2DArray;
            if (textureArray != null)
            {
                graph.owner.RegisterCompleteObjectUndo("Drag Texture Array");
                var property = new Texture2DArrayShaderProperty { displayName = textureArray.name, value = { textureArray = textureArray } };
                graph.AddShaderProperty(property);
                var node = new SampleTexture2DArrayNode();
                var drawState = node.drawState;
                drawState.position = new Rect(nodePosition, drawState.position.size);
                node.drawState = drawState;
                graph.AddNode(node);
                var inputslot = node.FindSlot<Texture2DArrayInputMaterialSlot>(SampleTexture2DArrayNode.TextureInputId);
                if (inputslot != null)
                    inputslot.textureArray = textureArray;
            }

            var texture3D = obj as Texture3D;
            if (texture3D != null)
            {
                graph.owner.RegisterCompleteObjectUndo("Drag Texture 3D");
                var property = new Texture3DShaderProperty { displayName = texture3D.name, value = { texture = texture3D } };
                graph.AddShaderProperty(property);
                var node = new SampleTexture3DNode();
                var drawState = node.drawState;
                drawState.position = new Rect(nodePosition, drawState.position.size);
                node.drawState = drawState;
                graph.AddNode(node);
                var inputslot = node.FindSlot<Texture3DInputMaterialSlot>(SampleTexture3DNode.TextureInputId);
                if (inputslot != null)
                    inputslot.texture = texture3D;
            }

            var cubemap = obj as Cubemap;
            if (cubemap != null)
            {
                graph.owner.RegisterCompleteObjectUndo("Drag Cubemap");
                var property = new CubemapShaderProperty { displayName = cubemap.name, value = { cubemap = cubemap } };
                graph.AddShaderProperty(property);
                var node = new SampleCubemapNode();

                var drawState = node.drawState;
                drawState.position = new Rect(nodePosition, drawState.position.size);
                node.drawState = drawState;
                graph.AddNode(node);

                var inputslot = node.FindInputSlot<CubemapInputMaterialSlot>(SampleCubemapNode.CubemapInputId);
                if (inputslot != null)
                    inputslot.cubemap = cubemap;
            }

            var subGraphAsset = obj as MaterialSubGraphAsset;
            if (subGraphAsset != null)
            {
                graph.owner.RegisterCompleteObjectUndo("Drag Sub-Graph");
                var node = new SubGraphNode();

                var drawState = node.drawState;
                drawState.position = new Rect(nodePosition, drawState.position.size);
                node.drawState = drawState;
                node.subGraphAsset = subGraphAsset;
                graph.AddNode(node);
            }

            var blackboardField = obj as BlackboardField;
            if (blackboardField != null)
            {
                AbstractShaderProperty property = blackboardField.userData as AbstractShaderProperty;
                if (property != null)
                {
                    graph.owner.RegisterCompleteObjectUndo("Drag Property");
                    var node = new PropertyNode();

                    var drawState = node.drawState;
                    drawState.position =  new Rect(nodePosition, drawState.position.size);
                    node.drawState = drawState;
                    graph.AddNode(node);

                    // Setting the guid requires the graph to be set first.
                    node.propertyGuid = property.guid;
                }
            }
        }

        #endregion
    }

    static class GraphViewExtensions
    {
        internal static void InsertCopyPasteGraph(this MaterialGraphView graphView, CopyPasteGraph copyGraph)
        {
            if (copyGraph == null)
                return;

            // Make new properties from the copied graph
            foreach (AbstractShaderProperty property in copyGraph.properties)
            {
                string propertyName = graphView.graph.SanitizePropertyName(property.displayName);
                AbstractShaderProperty copiedProperty = property.Copy();
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

            using (var remappedNodesDisposable = ListPool<AbstractMaterialNode>.GetDisposable())
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
                            if (element is Edge edge && remappedEdges.Contains(edge.userData as IEdge))
                                graphView.AddToSelection(edge);

                            if (element is IShaderNodeView nodeView && remappedNodes.Contains(nodeView.node))
                                graphView.AddToSelection((Node)nodeView);
                        });
                }
            }
        }
    }
}
