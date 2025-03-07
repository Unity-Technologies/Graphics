using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.Graphing.Util;
using UnityEngine;
using UnityEditor.Graphing;
using Object = UnityEngine.Object;
using UnityEditor.Experimental.GraphView;
using UnityEditor.ShaderGraph.Drawing.Inspector.PropertyDrawers;
using UnityEditor.ShaderGraph.Drawing.Views;
using UnityEditor.ShaderGraph.Internal;
using UnityEditor.ShaderGraph.Serialization;
using UnityEngine.UIElements;
using Edge = UnityEditor.Experimental.GraphView.Edge;
using Node = UnityEditor.Experimental.GraphView.Node;
using UnityEngine.Pool;

namespace UnityEditor.ShaderGraph.Drawing
{
    sealed class MaterialGraphView : GraphView, IInspectable, ISelectionProvider
    {
        readonly MethodInfo m_UndoRedoPerformedMethodInfo;

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

            this.viewTransformChanged += OnTransformChanged;

            // Get reference to GraphView assembly
            Assembly graphViewAssembly = null;
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var assemblyName = assembly.GetName().ToString();
                if (assemblyName.Contains("GraphView"))
                {
                    graphViewAssembly = assembly;
                }
            }

            Type graphViewType = graphViewAssembly?.GetType("UnityEditor.Experimental.GraphView.GraphView");
            // Cache the method info for this function to be used through application lifetime
            m_UndoRedoPerformedMethodInfo = graphViewType?.GetMethod("UndoRedoPerformed",
                BindingFlags.FlattenHierarchy | BindingFlags.Instance | BindingFlags.NonPublic,
                null,
                new Type[] { typeof(UndoRedoInfo).MakeByRefType()},
                null);
        }

        // GraphView has a bug where the viewTransform will be reset to default when swapping between two
        // GraphViewEditor windows of the same type. This is a hack to prevent that from happening w/as little
        // halo as possible.
        Vector3 lkgPosition;
        Vector3 lkgScale;
        void OnTransformChanged(GraphView graphView)
        {
#pragma warning disable CS0618 // Type or member is obsolete
            if (!graphView.viewTransform.position.Equals(Vector3.zero))
            {
                lkgPosition = graphView.viewTransform.position;
                lkgScale = graphView.viewTransform.scale;
            }
            else if (!lkgPosition.Equals(Vector3.zero))
            {
                graphView.UpdateViewTransform(lkgPosition, lkgScale);
            }
#pragma warning restore CS0618 // Type or member is obsolete
        }

        protected internal override bool canCutSelection
        {
            get { return selection.OfType<IShaderNodeView>().Any(x => x.node.canCutNode) || selection.OfType<Group>().Any() || selection.OfType<SGBlackboardField>().Any() || selection.OfType<SGBlackboardCategory>().Any() || selection.OfType<StickyNote>().Any(); }
        }

        protected internal override bool canCopySelection
        {
            get { return selection.OfType<IShaderNodeView>().Any(x => x.node.canCopyNode) || selection.OfType<Group>().Any() || selection.OfType<SGBlackboardField>().Any() || selection.OfType<SGBlackboardCategory>().Any() || selection.OfType<StickyNote>().Any(); }
        }

        public MaterialGraphView(GraphData graph, Action previewUpdateDelegate) : this()
        {
            this.graph = graph;
            this.m_PreviewManagerUpdateDelegate = previewUpdateDelegate;
        }

        [Inspectable("GraphData", null)]
        public GraphData graph { get; private set; }

        Action m_BlackboardFieldDropDelegate;
        internal Action blackboardFieldDropDelegate
        {
            get => m_BlackboardFieldDropDelegate;
            set => m_BlackboardFieldDropDelegate = value;
        }

        public List<ISelectable> GetSelection => selection;

        Action m_InspectorUpdateDelegate;
        Action m_PreviewManagerUpdateDelegate;

        public string inspectorTitle => this.graph.path;

        public object GetObjectToInspect()
        {
            return graph;
        }

        public void SupplyDataToPropertyDrawer(IPropertyDrawer propertyDrawer, Action inspectorUpdateDelegate)
        {
            m_InspectorUpdateDelegate = inspectorUpdateDelegate;
            if (propertyDrawer is GraphDataPropertyDrawer graphDataPropertyDrawer)
            {
                graphDataPropertyDrawer.GetPropertyData(this.ChangeTargetSettings, ChangePrecision);
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

        void ChangePrecision(GraphPrecision newGraphDefaultPrecision)
        {
            if (graph.graphDefaultPrecision == newGraphDefaultPrecision)
                return;

            graph.owner.RegisterCompleteObjectUndo("Change Graph Default Precision");

            graph.SetGraphDefaultPrecision(newGraphDefaultPrecision);

            var graphEditorView = this.GetFirstAncestorOfType<GraphEditorView>();
            if (graphEditorView == null)
                return;

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

        public bool wasUndoRedoPerformed { get; set; }

        // GraphView has UQueryState<Node> nodes built in to query for Nodes
        // We need this for Contexts but we might as well cast it to a list once
        public List<ContextView> contexts { get; set; }

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
            // If this is a sub-graph node we always have to check the effective stage as we might have to trace back through the sub-graph
            if (startStage == ShaderStageCapability.All || startSlot.owner is SubGraphNode)
                startStage = NodeUtils.GetEffectiveShaderStageCapability(startSlot, true) & NodeUtils.GetEffectiveShaderStageCapability(startSlot, false);

            foreach (var candidateAnchor in ports.ToList())
            {
                var candidateSlot = candidateAnchor.GetSlot();

                if (!startSlot.IsCompatibleWith(candidateSlot))
                    continue;

                if (startStage != ShaderStageCapability.All)
                {
                    var candidateStage = candidateSlot.stageCapability;
                    if (candidateStage == ShaderStageCapability.All || candidateSlot.owner is SubGraphNode)
                        candidateStage = NodeUtils.GetEffectiveShaderStageCapability(candidateSlot, true)
                            & NodeUtils.GetEffectiveShaderStageCapability(candidateSlot, false);
                    if (candidateStage != ShaderStageCapability.All && candidateStage != startStage)
                        continue;

                    // None stage can only connect to All stage, otherwise you can connect invalid connections
                    if (startStage == ShaderStageCapability.None && candidateStage != ShaderStageCapability.All)
                        continue;
                }

                compatibleAnchors.Add(candidateAnchor);
            }
            return compatibleAnchors;
        }

        internal bool ResetSelectedBlockNodes()
        {
            bool anyNodesWereReset = false;
            var selectedBlocknodes = selection.FindAll(e => e is MaterialNodeView && ((MaterialNodeView)e).node is BlockNode).Cast<MaterialNodeView>().ToArray();
            foreach (var mNode in selectedBlocknodes)
            {
                var bNode = mNode.node as BlockNode;
                var context = GetContext(bNode.contextData);

                // Check if the node is currently floating (it's parent isn't the context view that owns it).
                // If the node's not floating then the block doesn't need to be reset.
                bool isFloating = mNode.parent != context;
                if (!isFloating)
                    continue;

                anyNodesWereReset = true;
                RemoveElement(mNode);
                context.InsertBlock(mNode);

                // TODO: StackNode in GraphView (Trunk) has no interface to reset drop previews. The least intrusive
                // solution is to call its DragLeave until its interface can be improved.
                context.DragLeave(null, null, null, null);
            }
            if (selectedBlocknodes.Length > 0)
                graph.ValidateCustomBlockLimit();
            return anyNodesWereReset;
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            Vector2 mousePosition = evt.mousePosition;

            // If the target wasn't a block node, but there is one selected (and reset) by the time we reach this point,
            // it means a block node was in an invalid configuration and that it may be unsafe to build the context menu.
            bool targetIsBlockNode = evt.target is MaterialNodeView && ((MaterialNodeView)evt.target).node is BlockNode;
            if (ResetSelectedBlockNodes() && !targetIsBlockNode)
            {
                return;
            }

            base.BuildContextualMenu(evt);
            if (evt.target is GraphView)
            {
                evt.menu.InsertAction(1, "Create Sticky Note", (e) => { AddStickyNote(mousePosition); });

                foreach (AbstractMaterialNode node in graph.GetNodes<AbstractMaterialNode>())
                {
                    var keyHint = ShaderGraphShortcuts.GetKeycodeForContextMenu(ShaderGraphShortcuts.nodePreviewShortcutID);
                    if (node.hasPreview && node.previewExpanded == true)
                        evt.menu.InsertAction(2, $"Collapse All Previews {keyHint}", CollapsePreviews, (a) => DropdownMenuAction.Status.Normal);
                    if (node.hasPreview && node.previewExpanded == false)
                        evt.menu.InsertAction(2, $"Expand All Previews {keyHint}", ExpandPreviews, (a) => DropdownMenuAction.Status.Normal);
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
                    var sc = ShaderGraphShortcuts.GetKeycodeForContextMenu(ShaderGraphShortcuts.summonDocumentationShortcutID);
                    evt.menu.AppendAction($"Open Documentation {sc}", SeeDocumentation, SeeDocumentationStatus);
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
                var scg = ShaderGraphShortcuts.GetKeycodeForContextMenu(ShaderGraphShortcuts.nodeGroupShortcutID);
                evt.menu.AppendAction($"Group Selection {scg}", _ => GroupSelection(), (a) =>
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

                var scu = ShaderGraphShortcuts.GetKeycodeForContextMenu(ShaderGraphShortcuts.nodeUnGroupShortcutID);
                evt.menu.AppendAction($"Ungroup Selection {scu}", _ => RemoveFromGroupNode(), (a) =>
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

            if (evt.target is SGBlackboardField || evt.target is SGBlackboardCategory)
            {
                evt.menu.AppendAction("Delete", (e) => DeleteSelectionImplementation("Delete", AskUser.DontAskUser), (e) => canDeleteSelection ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
                evt.menu.AppendAction("Duplicate %d", (e) => DuplicateSelection(), (a) => canDuplicateSelection ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            }

            // Sticky notes aren't given these context menus in GraphView because it checks for specific types.
            // We can manually add them back in here (although the context menu ordering is different).
            if (evt.target is StickyNote)
            {
                evt.menu.AppendAction("Copy %c", (e) => CopySelectionCallback(), (a) => canCopySelection ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
                evt.menu.AppendAction("Cut %x", (e) => CutSelectionCallback(), (a) => canCutSelection ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
                evt.menu.AppendAction("Duplicate %d", (e) => DuplicateSelectionCallback(), (a) => canDuplicateSelection ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
            }

            // Contextual menu
            if (evt.target is Edge)
            {
                var target = evt.target as Edge;
                var pos = evt.mousePosition;

                var keyHint = ShaderGraphShortcuts.GetKeycodeForContextMenu(ShaderGraphShortcuts.createRedirectNodeShortcutID);
                evt.menu.AppendSeparator();
                evt.menu.AppendAction($"Add Redirect Node {keyHint}", e => CreateRedirectNode(pos, target));
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

            RedirectNodeData.Create(graph, outputSlot.concreteValueType, contentViewContainer.WorldToLocal(position), inputSlot.slotReference,
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

            var nodesConnectedToAMasterNode = new HashSet<AbstractMaterialNode>();

            // Get the list of nodes from Master nodes or SubGraphOutputNode
            foreach (var abs in endNodes)
            {
                NodeUtils.DepthFirstCollectNodesFromNode(nodesConnectedToAMasterNode, abs);
            }

            selection.Clear();
            // Get all nodes and then compare with the master nodes list
            var allNodes = nodes.ToList().OfType<IShaderNodeView>();
            foreach (IShaderNodeView materialNodeView in allNodes)
            {
                if (!nodesConnectedToAMasterNode.Contains(materialNodeView.node))
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

            OnSelectionChange?.Invoke(selection);
        }

        // Replicating these private GraphView functions as we need them for our own purposes
        internal void AddToSelectionNoUndoRecord(GraphElement graphElement)
        {
            graphElement.selected = true;
            selection.Add(graphElement);
            graphElement.OnSelected();

            OnSelectionChange?.Invoke(selection);

            // To ensure that the selected GraphElement gets unselected if it is removed from the GraphView.
            graphElement.RegisterCallback<DetachFromPanelEvent>(OnSelectedElementDetachedFromPanel);

            graphElement.MarkDirtyRepaint();
        }

        public override void RemoveFromSelection(ISelectable selectable)
        {
            base.RemoveFromSelection(selectable);

            if (OnSelectionChange != null)
                OnSelectionChange(selection);
        }

        internal void RemoveFromSelectionNoUndoRecord(ISelectable selectable)
        {
            var graphElement = selectable as GraphElement;
            if (graphElement == null)
                return;
            graphElement.selected = false;

            OnSelectionChange?.Invoke(selection);

            selection.Remove(selectable);
            graphElement.OnUnselected();
            graphElement.UnregisterCallback<DetachFromPanelEvent>(OnSelectedElementDetachedFromPanel);
            graphElement.MarkDirtyRepaint();
        }

        private void OnSelectedElementDetachedFromPanel(DetachFromPanelEvent evt)
        {
            RemoveFromSelectionNoUndoRecord(evt.target as ISelectable);
        }

        public override void ClearSelection()
        {
            base.ClearSelection();

            OnSelectionChange?.Invoke(selection);
        }

        internal bool ClearSelectionNoUndoRecord()
        {
            foreach (var graphElement in selection.OfType<GraphElement>())
            {
                graphElement.selected = false;
                graphElement.OnUnselected();
                graphElement.UnregisterCallback<DetachFromPanelEvent>(OnSelectedElementDetachedFromPanel);
                graphElement.MarkDirtyRepaint();
            }

            OnSelectionChange?.Invoke(selection);

            bool selectionWasNotEmpty = selection.Any();
            selection.Clear();

            return selectionWasNotEmpty;
        }

        private void RemoveNodesInsideGroup(DropdownMenuAction action, GroupData data)
        {
            graph.owner.RegisterCompleteObjectUndo("Delete Group and Contents");
            var groupItems = graph.GetItemsInGroup(data);
            graph.RemoveElements(groupItems.OfType<AbstractMaterialNode>().ToArray(), new IEdge[] { }, new[] { data }, groupItems.OfType<StickyNoteData>().ToArray());
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
                if (selectedNode.node.precision != Precision.Single)
                    floatPrecisionAction = DropdownMenuAction.Status.Normal;
                if (selectedNode.node.precision != Precision.Half)
                    halfPrecisionAction = DropdownMenuAction.Status.Normal;
            }

            // Create the menu options
            evt.menu.AppendAction("Precision/Inherit", _ => SetNodePrecisionOnSelection(Precision.Inherit), (a) => inheritPrecisionAction);
            evt.menu.AppendAction("Precision/Single", _ => SetNodePrecisionOnSelection(Precision.Single), (a) => floatPrecisionAction);
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
            var previewKeyHint = ShaderGraphShortcuts.GetKeycodeForContextMenu(ShaderGraphShortcuts.nodePreviewShortcutID);
            var portKeyHint = ShaderGraphShortcuts.GetKeycodeForContextMenu(ShaderGraphShortcuts.nodeCollapsedShortcutID);

            string expandPreviewText = $"View/Expand Previews {previewKeyHint}";
            string collapsePreviewText = $"View/Collapse Previews {previewKeyHint}";
            string expandPortText = $"View/Expand Ports {portKeyHint}";
            string collapsePortText = $"View/Collapse Ports {portKeyHint}";
            if (selection.Count == 1)
            {
                collapsePreviewText = $"View/Collapse Preview {previewKeyHint}";
                expandPreviewText = $"View/Expand Preview {previewKeyHint}";
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
            var m = t?.GetMethod("Show", new[] { typeof(Action<Color>), typeof(Color), typeof(bool), typeof(bool) });
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
                    if (selectable is MaterialNodeView nodeView)
                    {
                        nodeView.node.SetColor(editorView.colorManager.activeProviderName, pickedColor);
                        editorView.colorManager.UpdateNodeView(nodeView);
                    }
                }
            }

            graph.owner.RegisterCompleteObjectUndo("Change Node Color");
            m.Invoke(null, new object[] { (Action<Color>)ApplyColor, defaultColor, true, false });
        }

        protected internal override bool canDeleteSelection
        {
            get
            {
                return selection.Any(x =>
                {
                    if (x is ContextView) return false; //< context view must not be deleted. ( eg, Vertex, Fragment )
                    return !(x is IShaderNodeView nodeView) || nodeView.node.canDeleteNode;
                });
            }
        }
        public void GroupSelection()
        {
            var title = "New Group";
            var groupData = new GroupData(title, new Vector2(10f, 10f));

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
            var stickyNoteData = new StickyNoteData(title, content, new Rect(position.x, position.y, 200, 160));
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
            m_InspectorUpdateDelegate?.Invoke();

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
            var convertToPropertyAction = new ConvertToPropertyAction();

            var selectedNodeViews = selection.OfType<IShaderNodeView>().Select(x => x.node).ToList();
            foreach (var node in selectedNodeViews)
            {
                if (!(node is IPropertyFromNode))
                    continue;

                var converter = node as IPropertyFromNode;
                convertToPropertyAction.inlinePropertiesToConvert.Add(converter);
            }

            graph.owner.graphDataStore.Dispatch(convertToPropertyAction);
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
            var selectedNodeViews = selection.OfType<IShaderNodeView>()
                .Select(x => x.node)
                .OfType<PropertyNode>();

            var convertToInlineAction = new ConvertToInlineAction();
            convertToInlineAction.propertyNodesToConvert = selectedNodeViews;
            graph.owner.graphDataStore.Dispatch(convertToInlineAction);
        }

        // Made internal for purposes of UI testing
        internal void DuplicateSelection()
        {
            graph.owner.RegisterCompleteObjectUndo("Duplicate Blackboard Selection");

            List<ShaderInput> selectedProperties = new List<ShaderInput>();
            List<CategoryData> selectedCategories = new List<CategoryData>();

            for (int index = 0; index < selection.Count; ++index)
            {
                var selectable = selection[index];
                if (selectable is SGBlackboardCategory blackboardCategory)
                {
                    selectedCategories.Add(blackboardCategory.controller.Model);
                    var childBlackboardFields = blackboardCategory.Query<SGBlackboardField>().ToList();
                    // Remove the children that live in this category (if any) from the selection, as they will get copied twice otherwise
                    selection.RemoveAll(childItem => childBlackboardFields.Contains(childItem));
                }
            }

            foreach (var selectable in selection)
            {
                if (selectable is SGBlackboardField blackboardField)
                {
                    selectedProperties.Add(blackboardField.controller.Model);
                }
            }

            // Sort so that the ShaderInputs are in the correct order
            selectedProperties.Sort((x, y) => graph.GetGraphInputIndex(x) > graph.GetGraphInputIndex(y) ? 1 : -1);

            CopyPasteGraph copiedItems = new CopyPasteGraph(null, null, null, selectedProperties, selectedCategories, null, null, null, null, copyPasteGraphSource: CopyPasteGraphSource.Duplicate);
            GraphViewExtensions.InsertCopyPasteGraph(this, copiedItems);
        }

        DropdownMenuAction.Status ConvertToSubgraphStatus(DropdownMenuAction action)
        {
            if (onConvertToSubgraphClick == null) return DropdownMenuAction.Status.Hidden;
            return selection.OfType<IShaderNodeView>().Any(v => v.node != null && v.node.allowedInSubGraph && !(v.node is SubGraphOutputNode)) ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Hidden;
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
            var notes = elements.OfType<StickyNote>().Select(x => x.userData);

            var categories = new List<CategoryData>();
            foreach (var selectable in selection)
            {
                if (selectable is SGBlackboardCategory blackboardCategory)
                {
                    categories.Add(blackboardCategory.userData as CategoryData);
                }
            }

            var inputs = selection.OfType<SGBlackboardField>().Select(x => x.userData as ShaderInput).ToList();

            // Collect the property nodes and get the corresponding properties
            var metaProperties = new HashSet<AbstractShaderProperty>(nodes.OfType<PropertyNode>().Select(x => x.property).Concat(inputs.OfType<AbstractShaderProperty>()));

            // Collect the keyword nodes and get the corresponding keywords
            var metaKeywords = new HashSet<ShaderKeyword>(nodes.OfType<KeywordNode>().Select(x => x.keyword).Concat(inputs.OfType<ShaderKeyword>()));

            // Collect the dropdown nodes and get the corresponding dropdowns
            var metaDropdowns = new HashSet<ShaderDropdown>(nodes.OfType<DropdownNode>().Select(x => x.dropdown).Concat(inputs.OfType<ShaderDropdown>()));

            // Sort so that the ShaderInputs are in the correct order
            inputs.Sort((x, y) => graph.GetGraphInputIndex(x) > graph.GetGraphInputIndex(y) ? 1 : -1);

            var copyPasteGraph = new CopyPasteGraph(groups, nodes, edges, inputs, categories, metaProperties, metaKeywords, metaDropdowns, notes);
            return MultiJson.Serialize(copyPasteGraph);
        }

        bool CanPasteSerializedDataImplementation(string serializedData)
        {
            var json = CopyPasteGraph.FromJson(serializedData, graph);
            return json != null && !json.IsEmpty();
        }

        void UnserializeAndPasteImplementation(string operationName, string serializedData)
        {
            graph.owner.RegisterCompleteObjectUndo(operationName);

            var pastedGraph = CopyPasteGraph.FromJson(serializedData, graph);
            this.InsertCopyPasteGraph(pastedGraph);
        }

        void DeleteSelectionImplementation(string operationName, GraphView.AskUser askUser)
        {
            // Selection state of Graph elements and the Focus state of UIElements are not mutually exclusive.
            // For Hotkeys, askUser should be AskUser mode, which should early out so that the focused Element can win.
            if (this.focusController.focusedElement != null
                && focusController.focusedElement is UIElements.ObjectField
                && askUser == GraphView.AskUser.AskUser)
            {
                return;
            }

            bool containsProperty = false;

            // Keywords need to be tested against variant limit based on multiple factors
            bool keywordsDirty = false;
            bool dropdownsDirty = false;

            // Track dependent keyword nodes to remove them
            List<KeywordNode> keywordNodes = new List<KeywordNode>();
            List<DropdownNode> dropdownNodes = new List<DropdownNode>();

            foreach (var selectable in selection)
            {
                if (selectable is SGBlackboardField propertyView && propertyView.userData != null)
                {
                    switch (propertyView.userData)
                    {
                        case AbstractShaderProperty property:
                            containsProperty = true;
                            break;
                        case ShaderKeyword keyword:
                            keywordNodes.AddRange(graph.GetNodes<KeywordNode>().Where(x => x.keyword == keyword));
                            break;
                        case ShaderDropdown dropdown:
                            dropdownNodes.AddRange(graph.GetNodes<DropdownNode>().Where(x => x.dropdown == dropdown));
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }
            }

            if (containsProperty)
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
            nodesToDelete = nodesToDelete.Union(dropdownNodes);

            // If deleting a Sub Graph node whose asset contains Keywords test variant limit
            foreach (SubGraphNode subGraphNode in nodesToDelete.OfType<SubGraphNode>())
            {
                if (subGraphNode.asset == null)
                {
                    continue;
                }
                if (subGraphNode.asset.keywords.Any())
                {
                    keywordsDirty = true;
                }
                if (subGraphNode.asset.dropdowns.Any())
                {
                    dropdownsDirty = true;
                }
            }

            graph.owner.RegisterCompleteObjectUndo(operationName);
            graph.RemoveElements(nodesToDelete.ToArray(),
                selection.OfType<Edge>().Select(x => x.userData).OfType<IEdge>().ToArray(),
                selection.OfType<ShaderGroup>().Select(x => x.userData).ToArray(),
                selection.OfType<StickyNote>().Select(x => x.userData).ToArray());


            var copiedSelectionList = new List<ISelectable>(selection);
            var deleteShaderInputAction = new DeleteShaderInputAction();
            var deleteCategoriesAction = new DeleteCategoryAction();

            for (int index = 0; index < copiedSelectionList.Count; ++index)
            {
                var selectable = copiedSelectionList[index];
                if (selectable is SGBlackboardField field && field.userData != null)
                {
                    var input = (ShaderInput)field.userData;
                    deleteShaderInputAction.shaderInputsToDelete.Add(input);

                    // If deleting a Keyword test variant limit
                    if (input is ShaderKeyword keyword)
                    {
                        keywordsDirty = true;
                    }
                    if (input is ShaderDropdown dropdown)
                    {
                        dropdownsDirty = true;
                    }
                }
                // Don't allow the default category to be deleted
                else if (selectable is SGBlackboardCategory category && category.controller.Model.IsNamedCategory())
                {
                    deleteCategoriesAction.categoriesToRemoveGuids.Add(category.viewModel.associatedCategoryGuid);
                }
            }

            if (deleteShaderInputAction.shaderInputsToDelete.Count != 0)
                graph.owner.graphDataStore.Dispatch(deleteShaderInputAction);
            if (deleteCategoriesAction.categoriesToRemoveGuids.Count != 0)
                graph.owner.graphDataStore.Dispatch(deleteCategoriesAction);

            // Test Keywords against variant limit
            if (keywordsDirty)
            {
                graph.OnKeywordChangedNoValidate();
            }
            if (dropdownsDirty)
            {
                graph.OnDropdownChangedNoValidate();
            }

            selection.Clear();
            m_InspectorUpdateDelegate?.Invoke();
        }

        // Updates selected graph elements after undo/redo
        internal void RestorePersistentSelectionAfterUndoRedo()
        {
            wasUndoRedoPerformed = true;
            UndoRedoInfo info = new UndoRedoInfo();
            m_UndoRedoPerformedMethodInfo?.Invoke(this, new object[] {info});
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
                var anyCategoriesInSelection = selection.OfType<SGBlackboardCategory>();
                if (!anyCategoriesInSelection.Any())
                {
                    // Blackboard items
                    bool validFields = false;
                    foreach (SGBlackboardField propertyView in selection.OfType<SGBlackboardField>())
                    {
                        if (!(propertyView.userData is MultiJsonInternal.UnknownShaderPropertyType))
                            validFields = true;
                    }

                    dragging = validFields;
                }
                else
                    DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
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

        // Contrary to the name this actually handles when the drop operation is performed
        void OnDragPerformEvent(DragPerformEvent e)
        {
            Vector2 localPos = (e.currentTarget as VisualElement).ChangeCoordinatesTo(contentViewContainer, e.localMousePosition);

            var selection = DragAndDrop.GetGenericData("DragSelection") as List<ISelectable>;
            if (selection != null)
            {
                // Blackboard
                if (selection.OfType<SGBlackboardField>().Any())
                {
                    IEnumerable<SGBlackboardField> fields = selection.OfType<SGBlackboardField>();
                    foreach (SGBlackboardField field in fields)
                    {
                        CreateNode(field, localPos);
                    }

                    // Call this delegate so blackboard can respond to blackboard field being dropped
                    blackboardFieldDropDelegate?.Invoke();
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

            var blackboardPropertyView = obj as SGBlackboardField;
            if (blackboardPropertyView?.userData is ShaderInput inputBeingDraggedIn)
            {
                var dragGraphInputAction = new DragGraphInputAction { nodePosition = nodePosition, graphInputBeingDraggedIn = inputBeingDraggedIn };
                graph.owner.graphDataStore.Dispatch(dragGraphInputAction);
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

            bool dropdownsDirty = false;

            var blackboardController = graphView.GetFirstAncestorOfType<GraphEditorView>().blackboardController;

            // Get the position to insert the new shader inputs per category
            int insertionIndex = blackboardController.GetInsertionIndexForPaste();

            // Any child of the categories need to be removed from selection as well (there's a Graphview issue where these don't get properly added to selection before the duplication sometimes, have to do it manually)
            foreach (var selectable in graphView.selection)
            {
                if (selectable is SGBlackboardCategory blackboardCategory)
                {
                    foreach (var blackboardChild in blackboardCategory.Children())
                    {
                        if (blackboardChild is SGBlackboardRow blackboardRow)
                        {
                            var blackboardField = blackboardRow.Q<SGBlackboardField>();
                            if (blackboardField != null)
                            {
                                blackboardField.selected = false;
                                blackboardField.OnUnselected();
                            }
                        }
                    }
                }
            }

            var cachedSelection = graphView.selection.ToList();

            // Before copy-pasting, clear all current selections so the duplicated items may be selected instead
            graphView.ClearSelectionNoUndoRecord();

            // Make new inputs from the copied graph
            foreach (ShaderInput input in copyGraph.inputs)
            {
                // Disregard any inputs that belong to a category in the CopyPasteGraph already,
                // GraphData handles copying those child inputs over when the category is copied
                if (copyGraph.IsInputCategorized(input))
                    continue;

                string associatedCategoryGuid = String.Empty;

                foreach (var category in graphView.graph.categories)
                {
                    if (copyGraph.IsInputDuplicatedFromCategory(input, category, graphView.graph))
                    {
                        associatedCategoryGuid = category.categoryGuid;
                    }
                }

                // In the specific case of just an input being selected to copy but some other category than the one containing it was selected, we want to copy it to the default category
                if (associatedCategoryGuid != String.Empty)
                {
                    foreach (var selection in cachedSelection)
                    {
                        if (selection is SGBlackboardCategory blackboardCategory && blackboardCategory.viewModel.associatedCategoryGuid != associatedCategoryGuid)
                        {
                            associatedCategoryGuid = String.Empty;
                            // Also ensures it is added to the end of the default category
                            insertionIndex = -1;
                        }
                    }
                }

                // This ensures that if an item is duplicated, it is copied to the same category
                if (copyGraph.copyPasteGraphSource == CopyPasteGraphSource.Duplicate)
                {
                    associatedCategoryGuid = graphView.graph.FindCategoryForInput(input);
                }

                var copyShaderInputAction = new CopyShaderInputAction { shaderInputToCopy = input, containingCategoryGuid = associatedCategoryGuid };
                copyShaderInputAction.insertIndex = insertionIndex;

                if (graphView.graph.IsInputAllowedInGraph(input))
                {
                    switch (input)
                    {
                        case AbstractShaderProperty property:
                            copyShaderInputAction.dependentNodeList = copyGraph.GetNodes<PropertyNode>().Where(x => x.property == input);
                            break;

                        case ShaderKeyword shaderKeyword:
                            copyShaderInputAction.dependentNodeList = copyGraph.GetNodes<KeywordNode>().Where(x => x.keyword == input);
                            // Pasting a new Keyword so need to test against variant limit
                            keywordsDirty = true;
                            break;

                        case ShaderDropdown shaderDropdown:
                            copyShaderInputAction.dependentNodeList = copyGraph.GetNodes<DropdownNode>().Where(x => x.dropdown == input);
                            dropdownsDirty = true;
                            break;

                        default:
                            AssertHelpers.Fail("Tried to paste shader input of unknown type into graph.");
                            break;
                    }

                    graphView.graph.owner.graphDataStore.Dispatch(copyShaderInputAction);

                    // Increment insertion index for next input
                    insertionIndex++;
                }
            }

            // Make new categories from the copied graph
            foreach (var category in copyGraph.categories)
            {
                foreach (var input in category.Children.ToList())
                {
                    // Remove this input from being copied if its not allowed to be copied into the target graph (eg. its a dropdown and the target graph isn't a sub-graph)
                    if (graphView.graph.IsInputAllowedInGraph(input) == false)
                        category.RemoveItemFromCategory(input);
                }
                var copyCategoryAction = new CopyCategoryAction() { categoryToCopyReference = category };
                graphView.graph.owner.graphDataStore.Dispatch(copyCategoryAction);
            }

            // Pasting a Sub Graph node that contains Keywords so need to test against variant limit
            foreach (SubGraphNode subGraphNode in copyGraph.GetNodes<SubGraphNode>())
            {
                if (subGraphNode.asset.keywords.Any())
                {
                    keywordsDirty = true;
                }

                if (subGraphNode.asset.dropdowns.Any())
                {
                    dropdownsDirty = true;
                }
            }

            // Test Keywords against variant limit
            if (keywordsDirty)
            {
                graphView.graph.OnKeywordChangedNoValidate();
            }

            if (dropdownsDirty)
            {
                graphView.graph.OnDropdownChangedNoValidate();
            }

            using (ListPool<AbstractMaterialNode>.Get(out var remappedNodes))
            {
                using (ListPool<Graphing.Edge>.Get(out var remappedEdges))
                {
                    var nodeList = copyGraph.GetNodes<AbstractMaterialNode>();

                    ClampNodesWithinView(graphView,
                        new List<IRectInterface>()
                            .Union(nodeList)
                            .Union(copyGraph.stickyNotes)
                            .Union(copyGraph.groups)
                    );

                    graphView.graph.PasteGraph(copyGraph, remappedNodes, remappedEdges);

                    // Add new elements to selection
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

        private static void ClampNodesWithinView(MaterialGraphView graphView, IEnumerable<IRectInterface> rectList)
        {
            // Compute the centroid of the copied elements at their original positions
            var positions = rectList.Select(n => n.rect.position);
            var centroid = UIUtilities.CalculateCentroid(positions);

            /* Ensure nodes get pasted at cursor */
            var graphMousePosition = graphView.contentViewContainer.WorldToLocal(graphView.cachedMousePosition);
            var copiedNodesOrigin = graphMousePosition;
            float xMin = float.MaxValue, xMax = float.MinValue, yMin = float.MaxValue, yMax = float.MinValue;

            // Calculate bounding rectangle min and max coordinates for these elements, to use in clamping later
            foreach (var element in rectList)
            {
                var position = element.rect.position;
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
                // Out of bounds - Adjust taking into account the size of the bounding box around elements and the current graph zoom level
                var adjustedPositionX = (xMax - xMin) + widthThreshold * zoomAdjustedViewScale;
                var adjustedPositionY = (yMax - yMin) + heightThreshold * zoomAdjustedViewScale;
                adjustedPositionY *= -1.0f * Mathf.Sign(copiedNodesOrigin.y);
                adjustedPositionX *= -1.0f * Mathf.Sign(copiedNodesOrigin.x);
                copiedNodesOrigin.x += adjustedPositionX;
                copiedNodesOrigin.y += adjustedPositionY;
            }

            foreach (var element in rectList)
            {
                var rect = element.rect;

                // Get the relative offset from the calculated centroid
                var relativeOffsetFromCentroid = rect.position - centroid;
                // Reapply that offset to ensure element positions are consistent when multiple elements are copied
                rect.x = copiedNodesOrigin.x + relativeOffsetFromCentroid.x;
                rect.y = copiedNodesOrigin.y + relativeOffsetFromCentroid.y;
                element.rect = rect;
            }
        }
    }
}
