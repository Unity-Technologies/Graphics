using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.Graphing.Util;
using UnityEngine;
using UnityEditor.Graphing;
using Object = UnityEngine.Object;
using Data.Interfaces;
using UnityEditor.Experimental.GraphView;
using UnityEditor.ShaderGraph.Drawing.Inspector.PropertyDrawers;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine.UIElements;
using Edge = UnityEditor.Experimental.GraphView.Edge;
using Node = UnityEditor.Experimental.GraphView.Node;

namespace UnityEditor.ShaderGraph.Drawing
{
    sealed class MaterialGraphView : GraphView, IInspectable
    {
        public MaterialGraphView()
        {
            styleSheets.Add(Resources.Load<StyleSheet>("Styles/MaterialGraphView"));
            serializeGraphElements = SerializeGraphElementsImplementation;
            canPasteSerializedData = CanPasteSerializedDataImplementation;
            unserializeAndPaste = UnserializeAndPasteImplementation;
            deleteSelection = DeleteSelectionImplementation;
            elementsInsertedToStackNode = ElementsInsertedToStackNode;
            RegisterCallback<DragUpdatedEvent>(OnDragUpdatedEvent);
            RegisterCallback<DragPerformEvent>(OnDragPerformEvent);
            RegisterCallback<MouseMoveEvent>(OnMouseMoveEvent);
        }

        protected override bool canCutSelection
        {
            get { return selection.OfType<IShaderNodeView>().Any(x => x.node.canCutNode) || selection.OfType<Group>().Any() || selection.OfType<BlackboardField>().Any(); }
        }

        protected override bool canCopySelection
        {
            get { return selection.OfType<IShaderNodeView>().Any(x => x.node.canCopyNode) || selection.OfType<Group>().Any() || selection.OfType<BlackboardField>().Any(); }
        }

        public MaterialGraphView(GraphData graph, Action previewUpdateDelegate) : this()
        {
            this.graph = graph;
            this.m_PreviewManagerUpdateDelegate = previewUpdateDelegate;
        }

        [Inspectable("GraphData", null)]
        public GraphData graph { get; private set; }

        Action m_InspectorUpdateDelegate;
        Action m_PreviewManagerUpdateDelegate;

        public string inspectorTitle => this.graph.path;

        public object GetObjectToInspect()
        {
            return this.graph;
        }

        public PropertyInfo[] GetPropertyInfo()
        {
            return this.GetType().GetProperties();
        }

        public void SupplyDataToPropertyDrawer(IPropertyDrawer propertyDrawer, Action inspectorUpdateDelegate)
        {
            m_InspectorUpdateDelegate = inspectorUpdateDelegate;
            if (propertyDrawer is GraphDataPropertyDrawer graphDataPropertyDrawer)
            {
                graphDataPropertyDrawer.GetPropertyData(this.ChangeTargetSettings, ChangeConcretePrecision);
            }
        }

        void ChangeTargetSettings()
        {
            var activeBlocks = graph.GetActiveBlocksForAllActiveTargets();
            if (ShaderGraphPreferences.autoAddRemoveBlocks)
            {
                graph.AddRemoveBlocksFromActiveList(activeBlocks);
            }

            graph.UpdateActiveBlocks(activeBlocks);
            this.m_PreviewManagerUpdateDelegate();
            this.m_InspectorUpdateDelegate();
        }

        void ChangeConcretePrecision(ConcretePrecision newValue)
        {
            var graphEditorView = this.GetFirstAncestorOfType<GraphEditorView>();
            if(graphEditorView == null)
                return;

            graph.owner.RegisterCompleteObjectUndo("Change Precision");
            if (graph.concretePrecision == newValue)
                return;

            graph.concretePrecision = newValue;
            var nodeList = this.Query<MaterialNodeView>().ToList();
            graphEditorView.colorManager.SetNodesDirty(nodeList);

            graph.ValidateGraph();
            graphEditorView.colorManager.UpdateNodeViews(nodeList);
            foreach (var node in graph.GetNodes<AbstractMaterialNode>())
            {
                node.Dirty(ModificationScope.Graph);
            }
        }

        public Action onConvertToSubgraphClick { get; set; }
        public Vector2 cachedMousePosition { get; private set; }

        // GraphView has UQueryState<Node> nodes built in to query for Nodes
        // We need this for Contexts but we might as well cast it to a list once
        List<ContextView> contexts { get; set; }

        // We have to manually update Contexts
        // Currently only called during GraphEditorView ctor as our Contexts are static
        public void UpdateContextList()
        {
            var contextQuery = contentViewContainer.Query<ContextView>().Build();
            contexts = contextQuery.ToList();
        }

        // We need a way to access specific ContextViews
        public ContextView GetContext(ContextData contextData)
        {
            return contexts.FirstOrDefault(s => s.contextData == contextData);
        }

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
            Vector2 mousePosition = evt.mousePosition;
            base.BuildContextualMenu(evt);
            if(evt.target is GraphView)
            {
                evt.menu.InsertAction(1, "Create Sticky Note", (e) => { AddStickyNote(mousePosition); });

                foreach (AbstractMaterialNode node in graph.GetNodes<AbstractMaterialNode>())
                {
                    if (node.hasPreview && node.previewExpanded == true)
                        evt.menu.InsertAction(2, "Collapse All Previews", CollapsePreviews, (a) => DropdownMenuAction.Status.Normal);
                    if (node.hasPreview && node.previewExpanded == false)
                        evt.menu.InsertAction(2, "Expand All Previews", ExpandPreviews, (a) => DropdownMenuAction.Status.Normal);
                }
                evt.menu.AppendSeparator();
            }

            if (evt.target is GraphView || evt.target is Node)
            {
                if (evt.target is Node node)
                {
                    if (!selection.Contains(node))
                    {
                        selection.Clear();
                        selection.Add(node);
                    }
                }

                evt.menu.AppendAction("Select/Unused Nodes", SelectUnusedNodes);

                InitializeViewSubMenu(evt);
                InitializePrecisionSubMenu(evt);

                evt.menu.AppendAction("Convert To/Sub-graph", ConvertToSubgraph, ConvertToSubgraphStatus);
                evt.menu.AppendAction("Convert To/Inline Node", ConvertToInlineNode, ConvertToInlineNodeStatus);
                evt.menu.AppendAction("Convert To/Property", ConvertToProperty, ConvertToPropertyStatus);
                evt.menu.AppendSeparator();

                var editorView = GetFirstAncestorOfType<GraphEditorView>();
                if (editorView.colorManager.activeSupportsCustom && selection.OfType<MaterialNodeView>().Any())
                {
                    evt.menu.AppendSeparator();
                    evt.menu.AppendAction("Color/Change...", ChangeCustomNodeColor,
                        eventBase => DropdownMenuAction.Status.Normal);

                    evt.menu.AppendAction("Color/Reset", menuAction =>
                    {
                        graph.owner.RegisterCompleteObjectUndo("Reset Node Color");
                        foreach (var selectable in selection)
                        {
                            if (selectable is MaterialNodeView nodeView)
                            {
                                nodeView.node.ResetColor(editorView.colorManager.activeProviderName);
                                editorView.colorManager.UpdateNodeView(nodeView);
                            }
                        }
                    }, eventBase => DropdownMenuAction.Status.Normal);
                }

                if (selection.OfType<IShaderNodeView>().Count() == 1)
                {
                    evt.menu.AppendSeparator();
                    evt.menu.AppendAction("Open Documentation _F1", SeeDocumentation, SeeDocumentationStatus);
                }
                if (selection.OfType<IShaderNodeView>().Count() == 1 && selection.OfType<IShaderNodeView>().First().node is SubGraphNode)
                {
                    evt.menu.AppendSeparator();
                    evt.menu.AppendAction("Open Sub Graph", OpenSubGraph, (a) => DropdownMenuAction.Status.Normal);
                }
            }
            evt.menu.AppendSeparator();
            if (evt.target is StickyNote)
            {
                evt.menu.AppendAction("Select/Unused Nodes", SelectUnusedNodes);
                evt.menu.AppendSeparator();
            }

            // This needs to work on nodes, groups and properties
            if ((evt.target is Node) || (evt.target is StickyNote))
            {
                evt.menu.AppendAction("Group Selection %g", _ => GroupSelection(), (a) =>
                {
                    List<ISelectable> filteredSelection = new List<ISelectable>();

                    foreach (ISelectable selectedObject in selection)
                    {
                        if (selectedObject is Group)
                            return DropdownMenuAction.Status.Disabled;
                        GraphElement ge = selectedObject as GraphElement;
                        if (ge.userData is BlockNode)
                        {
                            return DropdownMenuAction.Status.Disabled;
                        }
                        if (ge.userData is IGroupItem)
                        {
                            filteredSelection.Add(ge);
                        }
                    }

                    if (filteredSelection.Count > 0)
                        return DropdownMenuAction.Status.Normal;

                    return DropdownMenuAction.Status.Disabled;
                });

                evt.menu.AppendAction("Ungroup Selection %u", _ => RemoveFromGroupNode(), (a) =>
                {
                    List<ISelectable> filteredSelection = new List<ISelectable>();

                    foreach (ISelectable selectedObject in selection)
                    {
                        if (selectedObject is Group)
                            return DropdownMenuAction.Status.Disabled;
                        GraphElement ge = selectedObject as GraphElement;
                        if (ge.userData is IGroupItem)
                        {
                            if (ge.GetContainingScope() is Group)
                                filteredSelection.Add(ge);
                        }
                    }

                    if (filteredSelection.Count > 0)
                        return DropdownMenuAction.Status.Normal;

                    return DropdownMenuAction.Status.Disabled;
                });
            }

            if (evt.target is ShaderGroup shaderGroup)
            {
                evt.menu.AppendAction("Select/Unused Nodes", SelectUnusedNodes);
                evt.menu.AppendSeparator();
                if (!selection.Contains(shaderGroup))
                {
                    selection.Add(shaderGroup);
                }

                var data = shaderGroup.userData;
                int count = evt.menu.MenuItems().Count;
                evt.menu.InsertAction(count, "Delete Group and Contents", (e) => RemoveNodesInsideGroup(e, data), DropdownMenuAction.AlwaysEnabled);
            }

            if (evt.target is BlackboardField)
            {
                evt.menu.AppendAction("Delete", (e) => DeleteSelectionImplementation("Delete", AskUser.DontAskUser), (e) => canDeleteSelection ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
                evt.menu.AppendAction("Duplicate %d", (e) => DuplicateSelection(), (a) => canDuplicateSelection ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            }

            // Contextual menu
            if (evt.target is Edge)
            {
                var target = evt.target as Edge;
                var pos = evt.mousePosition;

                evt.menu.AppendSeparator();
                evt.menu.AppendAction("Add Redirect Node", e => CreateRedirectNode(pos, target));
            }
        }

        public void CreateRedirectNode(Vector2 position, Edge edgeTarget)
        {
            var outputSlot = edgeTarget.output.GetSlot();
            var inputSlot = edgeTarget.input.GetSlot();
            // Need to check if the Nodes that are connected are in a group or not
            // If they are in the same group we also add in the Redirect Node
            // var groupGuidOutputNode = graph.GetNodeFromGuid(outputSlot.slotReference.nodeGuid).groupGuid;
            // var groupGuidInputNode = graph.GetNodeFromGuid(inputSlot.slotReference.nodeGuid).groupGuid;
            GroupData group = null;
            if (outputSlot.owner.group == inputSlot.owner.group)
            {
                group = inputSlot.owner.group;
            }

            RedirectNodeData.Create(graph, outputSlot.valueType, contentViewContainer.WorldToLocal(position), inputSlot.slotReference,
                outputSlot.slotReference, group);
        }

        void SelectUnusedNodes(DropdownMenuAction action)
        {
            graph.owner.RegisterCompleteObjectUndo("Select Unused Nodes");
            ClearSelection();

            List<AbstractMaterialNode> endNodes = new List<AbstractMaterialNode>();
            if (!graph.isSubGraph)
            {
                var nodeView = graph.GetNodes<BlockNode>();
                foreach (BlockNode blockNode in nodeView)
                {
                    endNodes.Add(blockNode as AbstractMaterialNode);
                }
            }
            else
            {
                var nodes = graph.GetNodes<SubGraphOutputNode>();
                foreach (var node in nodes)
                {
                    endNodes.Add(node);
                }
            }

            var nodesConnectedToAMasterNode = new List<AbstractMaterialNode>();

            // Get the list of nodes from Master nodes or SubGraphOutputNode
            foreach (var abs in endNodes)
            {
                NodeUtils.DepthFirstCollectNodesFromNode(nodesConnectedToAMasterNode, abs);
            }

            selection.Clear();
            // Get all nodes and then compare with the master nodes list
            var nodesConnectedHash = new HashSet<AbstractMaterialNode>(nodesConnectedToAMasterNode);
            var allNodes = nodes.ToList().OfType<IShaderNodeView>();
            foreach (IShaderNodeView materialNodeView in allNodes)
            {
                if (!nodesConnectedHash.Contains(materialNodeView.node))
                {
                    var nd = materialNodeView as GraphElement;
                    AddToSelection(nd);
                }
            }
        }

        public delegate void SelectionChanged(List<ISelectable> selection);
        public SelectionChanged OnSelectionChange;
        public override void AddToSelection(ISelectable selectable)
        {
            base.AddToSelection(selectable);

            if(OnSelectionChange != null)
                OnSelectionChange(selection);
        }

        public override void RemoveFromSelection(ISelectable selectable)
        {
            base.RemoveFromSelection(selectable);

            if(OnSelectionChange != null)
                OnSelectionChange(selection);
        }

        public override void ClearSelection()
        {
            base.ClearSelection();

            if(OnSelectionChange != null)
                OnSelectionChange(selection);
        }


        private void RemoveNodesInsideGroup(DropdownMenuAction action, GroupData data)
        {
            graph.owner.RegisterCompleteObjectUndo("Delete Group and Contents");
            var groupItems = graph.GetItemsInGroup(data);
            graph.RemoveElements(groupItems.OfType<AbstractMaterialNode>().ToArray(), new IEdge[] {}, new [] {data}, groupItems.OfType<StickyNoteData>().ToArray());
        }

        private void InitializePrecisionSubMenu(ContextualMenuPopulateEvent evt)
        {
            // Default the menu buttons to disabled
            DropdownMenuAction.Status inheritPrecisionAction = DropdownMenuAction.Status.Disabled;
            DropdownMenuAction.Status floatPrecisionAction = DropdownMenuAction.Status.Disabled;
            DropdownMenuAction.Status halfPrecisionAction = DropdownMenuAction.Status.Disabled;

            // Check which precisions are available to switch to
            foreach (MaterialNodeView selectedNode in selection.Where(x => x is MaterialNodeView).Select(x => x as MaterialNodeView))
            {
                if (selectedNode.node.precision != Precision.Inherit)
                    inheritPrecisionAction = DropdownMenuAction.Status.Normal;
                if (selectedNode.node.precision != Precision.Float)
                    floatPrecisionAction = DropdownMenuAction.Status.Normal;
                if (selectedNode.node.precision != Precision.Half)
                    halfPrecisionAction = DropdownMenuAction.Status.Normal;
            }

            // Create the menu options
            evt.menu.AppendAction("Precision/Inherit", _ => SetNodePrecisionOnSelection(Precision.Inherit), (a) => inheritPrecisionAction);
            evt.menu.AppendAction("Precision/Float", _ => SetNodePrecisionOnSelection(Precision.Float), (a) => floatPrecisionAction);
            evt.menu.AppendAction("Precision/Half", _ => SetNodePrecisionOnSelection(Precision.Half), (a) => halfPrecisionAction);
        }

        private void InitializeViewSubMenu(ContextualMenuPopulateEvent evt)
        {
            // Default the menu buttons to disabled
            DropdownMenuAction.Status expandPreviewAction = DropdownMenuAction.Status.Disabled;
            DropdownMenuAction.Status collapsePreviewAction = DropdownMenuAction.Status.Disabled;
            DropdownMenuAction.Status minimizeAction = DropdownMenuAction.Status.Disabled;
            DropdownMenuAction.Status maximizeAction = DropdownMenuAction.Status.Disabled;

            // Initialize strings
            string expandPreviewText = "View/Expand Previews";
            string collapsePreviewText = "View/Collapse Previews";
            string expandPortText = "View/Expand Ports";
            string collapsePortText = "View/Collapse Ports";
            if (selection.Count == 1)
            {
                collapsePreviewText = "View/Collapse Preview";
                expandPreviewText = "View/Expand Preview";
            }

            // Check if we can expand or collapse the ports/previews
            foreach (MaterialNodeView selectedNode in selection.Where(x => x is MaterialNodeView).Select(x => x as MaterialNodeView))
            {
                if (selectedNode.node.hasPreview)
                {
                    if (selectedNode.node.previewExpanded)
                        collapsePreviewAction = DropdownMenuAction.Status.Normal;
                    else
                        expandPreviewAction = DropdownMenuAction.Status.Normal;
                }

                if (selectedNode.CanToggleNodeExpanded())
                {
                    if (selectedNode.expanded)
                        minimizeAction = DropdownMenuAction.Status.Normal;
                    else
                        maximizeAction = DropdownMenuAction.Status.Normal;
                }
            }

            // Create the menu options
            evt.menu.AppendAction(collapsePortText, _ => SetNodeExpandedForSelectedNodes(false), (a) => minimizeAction);
            evt.menu.AppendAction(expandPortText, _ => SetNodeExpandedForSelectedNodes(true), (a) => maximizeAction);

            evt.menu.AppendSeparator("View/");

            evt.menu.AppendAction(expandPreviewText, _ => SetPreviewExpandedForSelectedNodes(true), (a) => expandPreviewAction);
            evt.menu.AppendAction(collapsePreviewText, _ => SetPreviewExpandedForSelectedNodes(false), (a) => collapsePreviewAction);
        }

        void ChangeCustomNodeColor(DropdownMenuAction menuAction)
        {
            // Color Picker is internal :(
            var t = typeof(EditorWindow).Assembly.GetTypes().FirstOrDefault(ty => ty.Name == "ColorPicker");
            var m = t?.GetMethod("Show", new[] {typeof(Action<Color>), typeof(Color), typeof(bool), typeof(bool)});
            if (m == null)
            {
                Debug.LogWarning("Could not invoke Color Picker for ShaderGraph.");
                return;
            }

            var editorView = GetFirstAncestorOfType<GraphEditorView>();
            var defaultColor = Color.gray;
            if (selection.FirstOrDefault(sel => sel is MaterialNodeView) is MaterialNodeView selNode1)
            {
                defaultColor = selNode1.GetColor();
                defaultColor.a = 1.0f;
            }

            void ApplyColor(Color pickedColor)
            {
                foreach (var selectable in selection)
                {
                    if(selectable is MaterialNodeView nodeView)
                    {
                        nodeView.node.SetColor(editorView.colorManager.activeProviderName, pickedColor);
                        editorView.colorManager.UpdateNodeView(nodeView);
                    }
                }
            }
            graph.owner.RegisterCompleteObjectUndo("Change Node Color");
            m.Invoke(null, new object[] {(Action<Color>) ApplyColor, defaultColor, true, false});
        }

        protected override bool canDeleteSelection
        {
            get
            {
                return selection.Any(x => !(x is IShaderNodeView nodeView) || nodeView.node.canDeleteNode);
            }
        }

        public void GroupSelection()
        {
            var title = "New Group";
            var groupData = new GroupData(title, new Vector2(10f,10f));

            graph.owner.RegisterCompleteObjectUndo("Create Group Node");
            graph.CreateGroup(groupData);

            foreach (var element in selection.OfType<GraphElement>())
            {
                if (element.userData is IGroupItem groupItem)
                {
                    graph.SetGroup(groupItem, groupData);
                }
            }
        }

        public void AddStickyNote(Vector2 position)
        {
            position = contentViewContainer.WorldToLocal(position);
            string title = "New Note";
            string content = "Write something here";
            var stickyNoteData  = new StickyNoteData(title, content, new Rect(position.x, position.y, 200, 160));
            graph.owner.RegisterCompleteObjectUndo("Create Sticky Note");
            graph.AddStickyNote(stickyNoteData);
        }

        public void RemoveFromGroupNode()
        {
            graph.owner.RegisterCompleteObjectUndo("Ungroup Node(s)");
            foreach (var element in selection.OfType<GraphElement>())
            {
                if (element.userData is IGroupItem)
                {
                    Group group = element.GetContainingScope() as Group;
                    if (group != null)
                    {
                        group.RemoveElement(element);
                    }
                }
            }
        }

        public void SetNodeExpandedForSelectedNodes(bool state, bool recordUndo = true)
        {
            if (recordUndo)
            {
                graph.owner.RegisterCompleteObjectUndo(state ? "Expand Nodes" : "Collapse Nodes");
            }

            foreach (MaterialNodeView selectedNode in selection.Where(x => x is MaterialNodeView).Select(x => x as MaterialNodeView))
            {
                if (selectedNode.CanToggleNodeExpanded() && selectedNode.expanded != state)
                {
                    selectedNode.expanded = state;
                    selectedNode.node.Dirty(ModificationScope.Topological);
                }
            }
        }

        public void SetPreviewExpandedForSelectedNodes(bool state)
        {
            graph.owner.RegisterCompleteObjectUndo(state ? "Expand Nodes" : "Collapse Nodes");

            foreach (MaterialNodeView selectedNode in selection.Where(x => x is MaterialNodeView).Select(x => x as MaterialNodeView))
            {
                selectedNode.node.previewExpanded = state;
            }
        }

        public void SetNodePrecisionOnSelection(Precision inPrecision)
        {
            var editorView = GetFirstAncestorOfType<GraphEditorView>();
            IEnumerable<MaterialNodeView> nodes = selection.Where(x => x is MaterialNodeView node && node.node.canSetPrecision).Select(x => x as MaterialNodeView);

            graph.owner.RegisterCompleteObjectUndo("Set Precisions");
            editorView.colorManager.SetNodesDirty(nodes);

            foreach (MaterialNodeView selectedNode in nodes)
            {
                selectedNode.node.precision = inPrecision;
            }

            // Reflect the data down
            graph.ValidateGraph();
            editorView.colorManager.UpdateNodeViews(nodes);

            // Update the views
            foreach (MaterialNodeView selectedNode in nodes)
                selectedNode.node.Dirty(ModificationScope.Topological);
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

            var path = AssetDatabase.GetAssetPath(subgraphNode.asset);
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
                graph.SanitizeGraphInputName(prop);
                graph.AddGraphInput(prop);

                var propNode = new PropertyNode();
                propNode.drawState = node.drawState;
                propNode.group = node.group;
                graph.AddNode(propNode);
                propNode.property = prop;

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

        void DuplicateSelection()
        {
            graph.owner.RegisterCompleteObjectUndo("Duplicate Blackboard Property");

            List<ShaderInput> selectedProperties = new List<ShaderInput>();
            foreach (var selectable in selection)
            {
                ShaderInput shaderProp = (ShaderInput)((BlackboardField)selectable).userData;
                if (shaderProp != null)
                {
                    selectedProperties.Add(shaderProp);
                }
            }

            // Sort so that the ShaderInputs are in the correct order
            selectedProperties.Sort((x, y) => graph.GetGraphInputIndex(x) > graph.GetGraphInputIndex(y) ? 1 : -1);

            CopyPasteGraph copiedProperties = new CopyPasteGraph(null, null, null, selectedProperties,
                null, null, null);

            GraphViewExtensions.InsertCopyPasteGraph(this, copiedProperties);
        }

        DropdownMenuAction.Status ConvertToSubgraphStatus(DropdownMenuAction action)
        {
            if (onConvertToSubgraphClick == null) return DropdownMenuAction.Status.Hidden;
            return selection.OfType<IShaderNodeView>().Any(v => v.node != null && v.node.allowedInSubGraph && !(v.node is SubGraphOutputNode) ) ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Hidden;
        }

        void ConvertToSubgraph(DropdownMenuAction action)
        {
            onConvertToSubgraphClick();
        }

        string SerializeGraphElementsImplementation(IEnumerable<GraphElement> elements)
        {
            var groups = elements.OfType<ShaderGroup>().Select(x => x.userData);
            var nodes = elements.OfType<IShaderNodeView>().Select(x => x.node).Where(x => x.canCopyNode);
            var edges = elements.OfType<Edge>().Select(x => (Graphing.Edge)x.userData);
            var inputs = selection.OfType<BlackboardField>().Select(x => x.userData as ShaderInput).ToList();
            var notes = elements.OfType<StickyNote>().Select(x => x.userData);

            // Collect the property nodes and get the corresponding properties
            var metaProperties = new HashSet<AbstractShaderProperty>(nodes.OfType<PropertyNode>().Select(x => x.property).Concat(inputs.OfType<AbstractShaderProperty>()));

            // Collect the keyword nodes and get the corresponding keywords
            var metaKeywords = new HashSet<ShaderKeyword>(nodes.OfType<KeywordNode>().Select(x => x.keyword).Concat(inputs.OfType<ShaderKeyword>()));

            // Sort so that the ShaderInputs are in the correct order
            inputs.Sort((x, y) => graph.GetGraphInputIndex(x) > graph.GetGraphInputIndex(y) ? 1 : -1);

            var copyPasteGraph = new CopyPasteGraph(groups, nodes, edges, inputs, metaProperties, metaKeywords, notes);
            return MultiJson.Serialize(copyPasteGraph);
        }

        bool CanPasteSerializedDataImplementation(string serializedData)
        {
            return CopyPasteGraph.FromJson(serializedData, graph) != null;
        }

        void UnserializeAndPasteImplementation(string operationName, string serializedData)
        {
            graph.owner.RegisterCompleteObjectUndo(operationName);

            var pastedGraph = CopyPasteGraph.FromJson(serializedData, graph);
            this.InsertCopyPasteGraph(pastedGraph);
        }

        void DeleteSelectionImplementation(string operationName, GraphView.AskUser askUser)
        {
            bool containsProperty = false;

            // Keywords need to be tested against variant limit based on multiple factors
            bool keywordsDirty = false;

            // Track dependent keyword nodes to remove them
            List<KeywordNode> keywordNodes = new List<KeywordNode>();

            foreach (var selectable in selection)
            {
                var field = selectable as BlackboardField;
                if (field != null && field.userData != null)
                {
                    switch(field.userData)
                    {
                        case AbstractShaderProperty property:
                            containsProperty = true;
                            break;
                        case ShaderKeyword keyword:
                            keywordNodes.AddRange(graph.GetNodes<KeywordNode>().Where(x => x.keyword == keyword));
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }

            if(containsProperty)
            {
                if (graph.isSubGraph)
                {
                    if (!EditorUtility.DisplayDialog("Sub Graph Will Change", "If you remove a property and save the sub graph, you might change other graphs that are using this sub graph.\n\nDo you want to continue?", "Yes", "No"))
                        return;
                }
            }

            // Filter nodes that cannot be deleted
            var nodesToDelete = selection.OfType<IShaderNodeView>().Where(v => !(v.node is SubGraphOutputNode) && v.node.canDeleteNode).Select(x => x.node);

            // Add keyword nodes dependent on deleted keywords
            nodesToDelete = nodesToDelete.Union(keywordNodes);

            // If deleting a Sub Graph node whose asset contains Keywords test variant limit
            foreach(SubGraphNode subGraphNode in nodesToDelete.OfType<SubGraphNode>())
            {
                if (subGraphNode.asset == null)
                {
                    continue;
                }
                if(subGraphNode.asset.keywords.Any())
                {
                    keywordsDirty = true;
                }
            }

            graph.owner.RegisterCompleteObjectUndo(operationName);
            graph.RemoveElements(nodesToDelete.ToArray(),
                selection.OfType<Edge>().Select(x => x.userData).OfType<IEdge>().ToArray(),
                selection.OfType<ShaderGroup>().Select(x => x.userData).ToArray(),
                selection.OfType<StickyNote>().Select(x => x.userData).ToArray());

            foreach (var selectable in selection)
            {
                var field = selectable as BlackboardField;
                if (field != null && field.userData != null)
                {
                    var input = (ShaderInput)field.userData;
                    graph.RemoveGraphInput(input);

                    // If deleting a Keyword test variant limit
                    if(input is ShaderKeyword keyword)
                    {
                        keywordsDirty = true;
                    }
                }
            }

            // Test Keywords against variant limit
            if(keywordsDirty)
            {
                graph.OnKeywordChangedNoValidate();
            }

            selection.Clear();
        }

        // Gets the index after the currently selected shader input per row.
        public static List<int> GetIndicesToInsert(Blackboard blackboard, int numberOfSections = 2)
        {
            List<int> indexPerSection = new List<int>();

            for (int x = 0; x < numberOfSections; x++)
                indexPerSection.Add(-1);

            if (blackboard == null || !blackboard.selection.Any())
                return indexPerSection;

            foreach (ISelectable selection in blackboard.selection)
            {
                BlackboardField selectedBlackboardField = selection as BlackboardField;
                if (selectedBlackboardField != null)
                {
                    BlackboardRow row = selectedBlackboardField.GetFirstAncestorOfType<BlackboardRow>();
                    BlackboardSection section = selectedBlackboardField.GetFirstAncestorOfType<BlackboardSection>();
                    if (row == null || section == null)
                        continue;
                    VisualElement sectionContainer = section.parent;

                    int sectionIndex = sectionContainer.IndexOf(section);
                    if (sectionIndex > numberOfSections)
                        continue;

                    int rowAfterIndex = section.IndexOf(row) + 1;
                    if (rowAfterIndex  > indexPerSection[sectionIndex])
                    {
                        indexPerSection[sectionIndex] = rowAfterIndex;
                    }
                }
            }

            return indexPerSection;
        }

        #region Drag and drop

        bool ValidateObjectForDrop(Object obj)
        {
            return EditorUtility.IsPersistent(obj) && (
                obj is Texture2D ||
                obj is Cubemap ||
                obj is SubGraphAsset asset && !asset.descendents.Contains(graph.assetGuid) && asset.assetGuid != graph.assetGuid ||
                obj is Texture2DArray ||
                obj is Texture3D);
        }

        void OnDragUpdatedEvent(DragUpdatedEvent e)
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

        void OnMouseMoveEvent(MouseMoveEvent evt)
        {
            this.cachedMousePosition = evt.mousePosition;
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
                var drawState = node.drawState;
                drawState.position = new Rect(nodePosition, drawState.position.size);
                node.drawState = drawState;
                graph.AddNode(node);

                if (isNormalMap)
                    node.textureType = TextureType.Normal;

                var inputslot = node.FindInputSlot<Texture2DInputMaterialSlot>(SampleTexture2DNode.TextureInputId);
                if (inputslot != null)
                    inputslot.texture = texture2D;
            }

            var textureArray = obj as Texture2DArray;
            if (textureArray != null)
            {
                graph.owner.RegisterCompleteObjectUndo("Drag Texture Array");

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

                var node = new SampleCubemapNode();
                var drawState = node.drawState;
                drawState.position = new Rect(nodePosition, drawState.position.size);
                node.drawState = drawState;
                graph.AddNode(node);

                var inputslot = node.FindInputSlot<CubemapInputMaterialSlot>(SampleCubemapNode.CubemapInputId);
                if (inputslot != null)
                    inputslot.cubemap = cubemap;
            }

            var subGraphAsset = obj as SubGraphAsset;
            if (subGraphAsset != null)
            {
                graph.owner.RegisterCompleteObjectUndo("Drag Sub-Graph");
                var node = new SubGraphNode();

                var drawState = node.drawState;
                drawState.position = new Rect(nodePosition, drawState.position.size);
                node.drawState = drawState;
                node.asset = subGraphAsset;
                graph.AddNode(node);
            }

            var blackboardField = obj as BlackboardField;
            if (blackboardField != null)
            {
                graph.owner.RegisterCompleteObjectUndo("Drag Graph Input");

                switch(blackboardField.userData)
                {
                    case AbstractShaderProperty property:
                    {
                        // This could be from another graph, in which case we add a copy of the ShaderInput to this graph.
                        if (graph.properties.FirstOrDefault(p => p == property) == null)
                        {
                            var copy = (AbstractShaderProperty)property.Copy();
                            graph.SanitizeGraphInputName(copy);
                            graph.SanitizeGraphInputReferenceName(copy, property.overrideReferenceName); // We do want to copy the overrideReferenceName

                            property = copy;
                            graph.AddGraphInput(property);
                        }

                        var node = new PropertyNode();
                        var drawState = node.drawState;
                        drawState.position =  new Rect(nodePosition, drawState.position.size);
                        node.drawState = drawState;
                        graph.AddNode(node);

                        // Setting the guid requires the graph to be set first.
                        node.property = property;
                        break;
                    }
                    case ShaderKeyword keyword:
                    {
                        // This could be from another graph, in which case we add a copy of the ShaderInput to this graph.
                        if (graph.keywords.FirstOrDefault(k => k == keyword) == null)
                        {
                            var copy = (ShaderKeyword)keyword.Copy();
                            graph.SanitizeGraphInputName(copy);
                            graph.SanitizeGraphInputReferenceName(copy, keyword.overrideReferenceName); // We do want to copy the overrideReferenceName

                            keyword = copy;
                            graph.AddGraphInput(keyword);
                        }

                        var node = new KeywordNode();
                        var drawState = node.drawState;
                        drawState.position =  new Rect(nodePosition, drawState.position.size);
                        node.drawState = drawState;
                        graph.AddNode(node);

                        // Setting the guid requires the graph to be set first.
                        node.keyword = keyword;
                        break;
                    }
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        #endregion

        void ElementsInsertedToStackNode(StackNode stackNode, int insertIndex, IEnumerable<GraphElement> elements)
        {
            var contextView = stackNode as ContextView;
            contextView.InsertElements(insertIndex, elements);
        }
    }

    static class GraphViewExtensions
    {
        // Sorts based on their position on the blackboard
        internal class PropertyOrder : IComparer<ShaderInput>
        {
            GraphData graphData;

            internal PropertyOrder(GraphData data)
            {
                graphData = data;
            }

            public int Compare(ShaderInput x, ShaderInput y)
            {
                if (graphData.GetGraphInputIndex(x) > graphData.GetGraphInputIndex(y)) return 1;
                else return -1;
            }
        }

        internal static void InsertCopyPasteGraph(this MaterialGraphView graphView, CopyPasteGraph copyGraph)
        {
            if (copyGraph == null)
                return;

            // Keywords need to be tested against variant limit based on multiple factors
            bool keywordsDirty = false;

            Blackboard blackboard = graphView.GetFirstAncestorOfType<GraphEditorView>().blackboardProvider.blackboard;

            // Get the position to insert the new shader inputs per section.
            List<int> indicies = MaterialGraphView.GetIndicesToInsert(blackboard);

            // Make new inputs from the copied graph
            foreach (ShaderInput input in copyGraph.inputs)
            {
                ShaderInput copiedInput;

                switch(input)
                {
                    case AbstractShaderProperty property:
                        copiedInput = DuplicateShaderInputs(input, graphView.graph, indicies[BlackboardProvider.k_PropertySectionIndex]);

                        // Increment for next within the same section
                        if (indicies[BlackboardProvider.k_PropertySectionIndex] >= 0)
                            indicies[BlackboardProvider.k_PropertySectionIndex]++;

                        // Update the property nodes that depends on the copied node
                        var dependentPropertyNodes = copyGraph.GetNodes<PropertyNode>().Where(x => x.property == input);
                        foreach (var node in dependentPropertyNodes)
                        {
                            node.owner = graphView.graph;
                            node.property = property;
                        }
                        break;

                    case ShaderKeyword shaderKeyword:
                        // Don't duplicate built-in keywords within the same graph
                        if ((input as ShaderKeyword).isBuiltIn && graphView.graph.keywords.Where(p => p.referenceName == input.referenceName).Any())
                            continue;

                        copiedInput = DuplicateShaderInputs(input, graphView.graph, indicies[BlackboardProvider.k_KeywordSectionIndex]);

                        // Increment for next within the same section
                        if (indicies[BlackboardProvider.k_KeywordSectionIndex] >= 0)
                            indicies[BlackboardProvider.k_KeywordSectionIndex]++;

                        // Update the keyword nodes that depends on the copied node
                        var dependentKeywordNodes = copyGraph.GetNodes<KeywordNode>().Where(x => x.keyword == input);
                        foreach (var node in dependentKeywordNodes)
                        {
                            node.owner = graphView.graph;
                            node.keyword = shaderKeyword;
                        }

                        // Pasting a new Keyword so need to test against variant limit
                        keywordsDirty = true;
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            // Pasting a Sub Graph node that contains Keywords so need to test against variant limit
            foreach(SubGraphNode subGraphNode in copyGraph.GetNodes<SubGraphNode>())
            {
                if(subGraphNode.asset.keywords.Any())
                {
                    keywordsDirty = true;
                }
            }

            // Test Keywords against variant limit
            if(keywordsDirty)
            {
                graphView.graph.OnKeywordChangedNoValidate();
            }

            using (var remappedNodesDisposable = ListPool<AbstractMaterialNode>.GetDisposable())
            {

                using (var remappedEdgesDisposable = ListPool<Graphing.Edge>.GetDisposable())
                {
                    var remappedNodes = remappedNodesDisposable.value;
                    var remappedEdges = remappedEdgesDisposable.value;
                    var nodeList = copyGraph.GetNodes<AbstractMaterialNode>();

                    ClampNodesWithinView(graphView, nodeList);

                    graphView.graph.PasteGraph(copyGraph, remappedNodes, remappedEdges);

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

        static ShaderInput DuplicateShaderInputs(ShaderInput original, GraphData graph, int index)
        {
            ShaderInput copy = original.Copy();
            graph.SanitizeGraphInputName(copy);
            graph.AddGraphInput(copy, index);
            copy.generatePropertyBlock = original.generatePropertyBlock;
            return copy;
        }

        private static void ClampNodesWithinView(MaterialGraphView graphView, IEnumerable<AbstractMaterialNode> nodeList)
        {
            // Compute the centroid of the copied nodes at their original positions
            var nodePositions = nodeList.Select(n => n.drawState.position.position);
            var centroid = UIUtilities.CalculateCentroid(nodePositions);

            /* Ensure nodes get pasted at cursor */
            var graphMousePosition = graphView.contentViewContainer.WorldToLocal(graphView.cachedMousePosition);
            var copiedNodesOrigin = graphMousePosition;
            float xMin = float.MaxValue, xMax = float.MinValue, yMin = float.MaxValue, yMax = float.MinValue;

            // Calculate bounding rectangle min and max coordinates for these nodes, to use in clamping later
            foreach (var node in nodeList)
            {
                var drawState = node.drawState;
                var position = drawState.position;
                xMin = Mathf.Min(xMin, position.x);
                yMin = Mathf.Min(yMin, position.y);
                xMax = Mathf.Max(xMax, position.x);
                yMax = Mathf.Max(yMax, position.y);
            }

            // Get center of the current view
            var center = graphView.contentViewContainer.WorldToLocal(graphView.layout.center);
            // Get offset from center of view to mouse position
            var mouseOffset = center - graphMousePosition;

            var zoomAdjustedViewScale = 1.0f / graphView.scale;
            var graphViewScaledHalfWidth = (graphView.layout.width * zoomAdjustedViewScale) / 2.0f;
            var graphViewScaledHalfHeight = (graphView.layout.height * zoomAdjustedViewScale) / 2.0f;
            const float widthThreshold = 40.0f;
            const float heightThreshold = 20.0f;

            if ((Mathf.Abs(mouseOffset.x) + widthThreshold > graphViewScaledHalfWidth ||
                 (Mathf.Abs(mouseOffset.y) + heightThreshold > graphViewScaledHalfHeight)))
            {
                // Out of bounds - Adjust taking into account the size of the bounding box around nodes and the current graph zoom level
                var adjustedPositionX = (xMax - xMin) + widthThreshold * zoomAdjustedViewScale;
                var adjustedPositionY = (yMax - yMin) + heightThreshold * zoomAdjustedViewScale;
                adjustedPositionY *= -1.0f * Mathf.Sign(copiedNodesOrigin.y);
                adjustedPositionX *= -1.0f * Mathf.Sign(copiedNodesOrigin.x);
                copiedNodesOrigin.x += adjustedPositionX;
                copiedNodesOrigin.y += adjustedPositionY;
            }

            foreach (var node in nodeList)
            {
                var drawState = node.drawState;
                var position = drawState.position;

                // Get the relative offset from the calculated centroid
                var relativeOffsetFromCentroid = position.position - centroid;
                // Reapply that offset to ensure node positions are consistent when multiple nodes are copied
                position.x = copiedNodesOrigin.x + relativeOffsetFromCentroid.x;
                position.y = copiedNodesOrigin.y + relativeOffsetFromCentroid.y;
                drawState.position = position;
                node.drawState = drawState;
            }
        }
    }
}
