using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor.Graphing;
using UnityEditor.Graphing.Util;
using UnityEditor.ShaderGraph.Drawing.Inspector;
using Object = UnityEngine.Object;

using UnityEditor.Experimental.GraphView;
using UnityEditor.ShaderGraph.Drawing.Colors;
using UnityEditor.ShaderGraph.Internal;
using UnityEngine.UIElements;
using Edge = UnityEditor.Experimental.GraphView.Edge;
using UnityEditor.VersionControl;
using UnityEditor.Searcher;

using Unity.Profiling;

namespace UnityEditor.ShaderGraph.Drawing
{
    [Serializable]
    class FloatingWindowsLayout
    {
        public WindowDockingLayout previewLayout = new WindowDockingLayout
        {
            dockingTop = false,
            dockingLeft = false,
            verticalOffset = 8,
            horizontalOffset = 8
        };
        public WindowDockingLayout blackboardLayout = new WindowDockingLayout
        {
            dockingTop = true,
            dockingLeft = true,
            verticalOffset = 8,
            horizontalOffset = 8
        };
    }

    [Serializable]
    class UserViewSettings
    {
        public bool isBlackboardVisible = true;
        public bool isPreviewVisible = true;
        public bool isInspectorVisible = true;
        public string colorProvider = NoColors.Title;
    }

    class GraphEditorView : VisualElement, IDisposable
    {
        MaterialGraphView m_GraphView;
        MasterPreviewView m_MasterPreviewView;
        InspectorView m_InspectorView;

        GraphData m_Graph;
        PreviewManager m_PreviewManager;
        MessageManager m_MessageManager;
        SearchWindowProvider m_SearchWindowProvider;
        EdgeConnectorListener m_EdgeConnectorListener;
        BlackboardProvider m_BlackboardProvider;
        ColorManager m_ColorManager;
        EditorWindow m_EditorWindow;

        public BlackboardProvider blackboardProvider
        {
            get { return m_BlackboardProvider; }
        }

        const string k_UserViewSettings = "UnityEditor.ShaderGraph.ToggleSettings";
        UserViewSettings m_UserViewSettings;

        internal UserViewSettings viewSettings { get => m_UserViewSettings; }

        const string k_FloatingWindowsLayoutKey = "UnityEditor.ShaderGraph.FloatingWindowsLayout2";
        FloatingWindowsLayout m_FloatingWindowsLayout = new FloatingWindowsLayout();

        public Action saveRequested { get; set; }

        public Action saveAsRequested { get; set; }

        public Func<bool> isCheckedOut { get; set; }

        public Action checkOut { get; set; }

        public Action convertToSubgraphRequested
        {
            get { return m_GraphView.onConvertToSubgraphClick; }
            set { m_GraphView.onConvertToSubgraphClick = value; }
        }

        public Action showInProjectRequested { get; set; }

        public MaterialGraphView graphView
        {
            get { return m_GraphView; }
        }

        PreviewManager previewManager
        {
            get { return m_PreviewManager; }
            set { m_PreviewManager = value; }
        }

        public string assetName
        {
            get { return m_BlackboardProvider.assetName; }
            set
            {
                m_BlackboardProvider.assetName = value;
            }
        }

        public ColorManager colorManager
        {
            get => m_ColorManager;
        }

        private static readonly ProfilerMarker AddGroupsMarker = new ProfilerMarker("AddGroups");
        private static readonly ProfilerMarker AddStickyNotesMarker = new ProfilerMarker("AddStickyNotes");
        public GraphEditorView(EditorWindow editorWindow, GraphData graph, MessageManager messageManager)
        {
            m_GraphViewGroupTitleChanged = OnGroupTitleChanged;
            m_GraphViewElementsAddedToGroup = OnElementsAddedToGroup;
            m_GraphViewElementsRemovedFromGroup = OnElementsRemovedFromGroup;

            m_EditorWindow = editorWindow;
            m_Graph = graph;
            m_MessageManager = messageManager;
            previewManager = new PreviewManager(graph, messageManager);
            previewManager.RenderPreviews(false);

            styleSheets.Add(Resources.Load<StyleSheet>("Styles/GraphEditorView"));

            var serializedSettings = EditorUserSettings.GetConfigValue(k_UserViewSettings);
            m_UserViewSettings = JsonUtility.FromJson<UserViewSettings>(serializedSettings) ?? new UserViewSettings();
            m_ColorManager = new ColorManager(m_UserViewSettings.colorProvider);

            var colorProviders = m_ColorManager.providerNames.ToArray();
            var toolbar = new IMGUIContainer(() =>
                {
                    GUILayout.BeginHorizontal(EditorStyles.toolbar);
                    if (GUILayout.Button("Save Asset", EditorStyles.toolbarButton))
                    {
                        if (saveRequested != null)
                            saveRequested();
                    }
                    GUILayout.Space(6);
                    if (GUILayout.Button("Save As...", EditorStyles.toolbarButton))
                    {
                        saveAsRequested();
                    }
                    GUILayout.Space(6);
                    if (GUILayout.Button("Show In Project", EditorStyles.toolbarButton))
                    {
                        if (showInProjectRequested != null)
                            showInProjectRequested();
                    }

                    if (isCheckedOut != null)
                    {
                        if (!isCheckedOut() && Provider.enabled && Provider.isActive)
                        {
                            if (GUILayout.Button("Check Out", EditorStyles.toolbarButton))
                            {
                                if (checkOut != null)
                                    checkOut();
                            }
                        }
                        else
                        {
                            EditorGUI.BeginDisabledGroup(true);
                            GUILayout.Button("Check Out", EditorStyles.toolbarButton);
                            EditorGUI.EndDisabledGroup();
                        }
                    }

                    GUILayout.FlexibleSpace();

                    EditorGUI.BeginChangeCheck();
                    GUILayout.Label("Color Mode");
                    var newColorIndex = EditorGUILayout.Popup(m_ColorManager.activeIndex, colorProviders, GUILayout.Width(100f));
                    GUILayout.Space(4);
                    m_UserViewSettings.isBlackboardVisible = GUILayout.Toggle(m_UserViewSettings.isBlackboardVisible, "Blackboard", EditorStyles.toolbarButton);

                    GUILayout.Space(6);

                    m_UserViewSettings.isInspectorVisible = GUILayout.Toggle(m_UserViewSettings.isInspectorVisible, "Graph Inspector", EditorStyles.toolbarButton);

                    GUILayout.Space(6);

                    m_UserViewSettings.isPreviewVisible = GUILayout.Toggle(m_UserViewSettings.isPreviewVisible, "Main Preview", EditorStyles.toolbarButton);
                    if (EditorGUI.EndChangeCheck())
                    {
                        UserViewSettingsChangeCheck(newColorIndex);
                    }
                    GUILayout.EndHorizontal();
                });
            Add(toolbar);

            var content = new VisualElement { name = "content" };
            {
                m_GraphView = new MaterialGraphView(graph, () => m_PreviewManager.UpdateMasterPreview(ModificationScope.Topological))
                    { name = "GraphView", viewDataKey = "MaterialGraphView" };
                m_GraphView.SetupZoom(0.05f, 8);
                m_GraphView.AddManipulator(new ContentDragger());
                m_GraphView.AddManipulator(new SelectionDragger());
                m_GraphView.AddManipulator(new RectangleSelector());
                m_GraphView.AddManipulator(new ClickSelector());
                m_GraphView.RegisterCallback<KeyDownEvent>(OnKeyDown);
                m_GraphView.RegisterCallback<MouseUpEvent>(evt => { m_GraphView.ResetSelectedBlockNodes(); } );

                RegisterGraphViewCallbacks();
                content.Add(m_GraphView);

                string serializedWindowLayout = EditorUserSettings.GetConfigValue(k_FloatingWindowsLayoutKey);
                if (!string.IsNullOrEmpty(serializedWindowLayout))
                {
                    m_FloatingWindowsLayout = JsonUtility.FromJson<FloatingWindowsLayout>(serializedWindowLayout);
                }

                CreateBlackboard();
                CreateMasterPreview();
                // When Matt integrates his stacks work, the inspector will need to trigger preview updates
                CreateInspector();

                UpdateSubWindowsVisibility();

                m_GraphView.graphViewChanged = GraphViewChanged;

                RegisterCallback<GeometryChangedEvent>(ApplySerializedWindowLayouts);
                if (m_Graph.isSubGraph)
                {
                    m_GraphView.AddToClassList("subgraph");
                }
            }

            m_SearchWindowProvider = ScriptableObject.CreateInstance<SearcherProvider>();
            m_SearchWindowProvider.Initialize(editorWindow, m_Graph, m_GraphView);
            m_GraphView.nodeCreationRequest = NodeCreationRequest;
            //regenerate entries when graph view is refocused, to propogate subgraph changes
            m_GraphView.RegisterCallback<FocusInEvent>( evt => { m_SearchWindowProvider.regenerateEntries = true; });

            m_EdgeConnectorListener = new EdgeConnectorListener(m_Graph, m_SearchWindowProvider, editorWindow);

            if(!m_Graph.isSubGraph)
            {
                AddContexts();
            }

            using (AddGroupsMarker.Auto())
            {
                foreach (var graphGroup in graph.groups)
                    AddGroup(graphGroup);
            }

            using (AddStickyNotesMarker.Auto())
            {
                foreach (var stickyNote in graph.stickyNotes)
                    AddStickyNote(stickyNote);
            }

            AddNodes(graph.GetNodes<AbstractMaterialNode>());
            AddBlocks(graph.GetNodes<BlockNode>());
            AddEdges(graph.edges);
            Add(content);

            // Active block lists need to be initialized on window start up
            // Do this here to as we cant do this inside GraphData
            // This is due to targets not being deserialized yet
            var context = new TargetSetupContext();
            foreach(var target in m_Graph.activeTargets)
            {
                target.Setup(ref context);
            }
            var activeBlocks = m_Graph.GetActiveBlocksForAllActiveTargets();
            m_Graph.UpdateActiveBlocks(activeBlocks);

            //graph settings need to be initilaized after the target setup
            m_InspectorView.InitializeGraphSettings();
        }

        private void CreateBlackboard()
        {
            m_BlackboardProvider = new BlackboardProvider(m_Graph);
            m_GraphView.Add(m_BlackboardProvider.blackboard);
        }

        void AddContexts()
        {
            ContextView AddContext(string name, ContextData contextData, Direction portDirection)
            {
                //need to eventually remove this reference to editor window in context views
                var contextView = new ContextView(name, contextData, m_EditorWindow);
                contextView.SetPosition(new Rect(contextData.position, Vector2.zero));
                contextView.AddPort(portDirection);
                m_GraphView.AddElement(contextView);
                return contextView;
            }

            // Add Contexts
            // As Contexts are hardcoded and contain a single port we can just give the direction
            var vertexContext = AddContext("Vertex", m_Graph.vertexContext, Direction.Output);
            var fragmentContext = AddContext("Fragment", m_Graph.fragmentContext, Direction.Input);

            // Connect Contexts
            // Vertical Edges have no representation in Model
            // Therefore just draw it and dont allow interaction
            var contextEdge = new Edge()
            {
                output = vertexContext.port,
                input = fragmentContext.port,
                pickingMode = PickingMode.Ignore,
            };
            m_GraphView.AddElement(contextEdge);

            // Update the Context list on MaterialGraphView
            m_GraphView.UpdateContextList();
        }

        internal void UserViewSettingsChangeCheck(int newColorIndex)
        {
            if (newColorIndex != m_ColorManager.activeIndex)
            {
                m_ColorManager.SetActiveProvider(newColorIndex, m_GraphView.Query<MaterialNodeView>().ToList());
                m_UserViewSettings.colorProvider = m_ColorManager.activeProviderName;
            }

            var serializedViewSettings = JsonUtility.ToJson(m_UserViewSettings);
            EditorUserSettings.SetConfigValue(k_UserViewSettings, serializedViewSettings);

            UpdateSubWindowsVisibility();
        }

        void NodeCreationRequest(NodeCreationContext c)
        {
            if (EditorWindow.focusedWindow == m_EditorWindow) //only display the search window when current graph view is focused 
            {
                m_SearchWindowProvider.connectedPort = null;
                m_SearchWindowProvider.target = c.target;
                SearcherWindow.Show(m_EditorWindow, (m_SearchWindowProvider as SearcherProvider).LoadSearchWindow(),
                    item => (m_SearchWindowProvider as SearcherProvider).OnSearcherSelectEntry(item, c.screenMousePosition - m_EditorWindow.position.position),
                    c.screenMousePosition - m_EditorWindow.position.position, null);
            }
        }


        // Master Preview, Inspector and Blackboard all need to keep their layouts when hidden in order to restore user preferences.
        // Because of their differences we do this is different ways, for now.
        void UpdateSubWindowsVisibility()
        {
            // Blackboard needs to be effectively removed when hidden to avoid bugs.
            if (m_UserViewSettings.isBlackboardVisible)
                 m_GraphView.Insert(m_GraphView.childCount, m_BlackboardProvider.blackboard);
            else
                m_BlackboardProvider.blackboard.RemoveFromHierarchy();

            // Same for the inspector
            if (m_UserViewSettings.isInspectorVisible)
                m_GraphView.Insert(m_GraphView.childCount, m_InspectorView);
            else
                m_InspectorView.RemoveFromHierarchy();

            m_MasterPreviewView.visible = m_UserViewSettings.isPreviewVisible;
        }

        Action<Group, string> m_GraphViewGroupTitleChanged;
        Action<Group, IEnumerable<GraphElement>> m_GraphViewElementsAddedToGroup;
        Action<Group, IEnumerable<GraphElement>> m_GraphViewElementsRemovedFromGroup;

        void RegisterGraphViewCallbacks()
        {
            m_GraphView.groupTitleChanged = m_GraphViewGroupTitleChanged;
            m_GraphView.elementsAddedToGroup = m_GraphViewElementsAddedToGroup;
            m_GraphView.elementsRemovedFromGroup = m_GraphViewElementsRemovedFromGroup;
        }

        void UnregisterGraphViewCallbacks()
        {
            m_GraphView.groupTitleChanged = null;
            m_GraphView.elementsAddedToGroup = null;
            m_GraphView.elementsRemovedFromGroup = null;
        }

        void CreateMasterPreview()
        {
            m_MasterPreviewView = new MasterPreviewView(previewManager, m_Graph) {name = "masterPreview"};

            var masterPreviewViewDraggable = new WindowDraggable(null, this);
            m_MasterPreviewView.AddManipulator(masterPreviewViewDraggable);
            m_GraphView.Add(m_MasterPreviewView);

            masterPreviewViewDraggable.OnDragFinished += UpdateSerializedWindowLayout;
            m_MasterPreviewView.previewResizeBorderFrame.OnResizeFinished += UpdateSerializedWindowLayout;
        }

        void CreateInspector()
        {
            m_InspectorView = new InspectorView(graphView);
            m_GraphView.Add(m_InspectorView);
        }

        void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.F1)
            {
                var selection = m_GraphView.selection.OfType<IShaderNodeView>();
                if (selection.Count() == 1)
                {
                    var nodeView = selection.First();
                    if (nodeView.node.documentationURL != null)
                    {
                        System.Diagnostics.Process.Start(nodeView.node.documentationURL);
                    }
                }
            }

            if (evt.actionKey && evt.keyCode == KeyCode.G)
            {
                if (m_GraphView.selection.OfType<GraphElement>().Any())
                {
                    m_GraphView.GroupSelection();
                }
            }

            if (evt.actionKey && evt.keyCode == KeyCode.U)
            {
                if (m_GraphView.selection.OfType<GraphElement>().Any())
                {
                    m_GraphView.RemoveFromGroupNode();
                }
            }
        }

        GraphViewChange GraphViewChanged(GraphViewChange graphViewChange)
        {
            if (graphViewChange.edgesToCreate != null)
            {
                foreach (var edge in graphViewChange.edgesToCreate)
                {
                    var leftSlot = edge.output.GetSlot();
                    var rightSlot = edge.input.GetSlot();
                    if (leftSlot != null && rightSlot != null)
                    {
                        m_Graph.owner.RegisterCompleteObjectUndo("Connect Edge");
                        m_Graph.Connect(leftSlot.slotReference, rightSlot.slotReference);
                    }
                }
                graphViewChange.edgesToCreate.Clear();
            }

            if (graphViewChange.movedElements != null)
            {
                m_Graph.owner.RegisterCompleteObjectUndo("Move Elements");

                List<GraphElement> nodesInsideGroup = new List<GraphElement>();
                foreach (var element in graphViewChange.movedElements)
                {
                    var groupNode = element as ShaderGroup;
                    if (groupNode == null)
                        continue;

                    foreach (GraphElement graphElement in groupNode.containedElements)
                    {
                        nodesInsideGroup.Add(graphElement);
                    }

                    SetGroupPosition(groupNode);
                }

                if(nodesInsideGroup.Any())
                    graphViewChange.movedElements.AddRange(nodesInsideGroup);

                foreach (var element in graphViewChange.movedElements)
                {
                    if (element.userData is AbstractMaterialNode node)
                    {
                        var drawState = node.drawState;
                        drawState.position = element.parent.ChangeCoordinatesTo(m_GraphView.contentViewContainer, element.GetPosition());
                        node.drawState = drawState;

                        // BlockNode moved outside a Context
                        // This isnt allowed but there is no way to disallow it on the GraphView
                        if(node is BlockNode blockNode &&
                            element.GetFirstAncestorOfType<ContextView>() == null)
                        {
                            var context = graphView.GetContext(blockNode.contextData);

                            // isDragging ensures we arent calling this when moving
                            // the BlockNode into the GraphView during dragging
                            if(context.isDragging)
                                continue;

                            // Remove from GraphView and add back to Context
                            m_GraphView.RemoveElement(element);
                            context.InsertBlock(element as MaterialNodeView);
                        }
                    }

                    if (element is StickyNote stickyNote)
                    {
                        SetStickyNotePosition(stickyNote);
                    }

                    if (element is ContextView contextView)
                    {
                        var rect = element.parent.ChangeCoordinatesTo(m_GraphView.contentViewContainer, element.GetPosition());
                        contextView.contextData.position = rect.position;
                    }
                }
            }

            var nodesToUpdate = m_NodeViewHashSet;
            nodesToUpdate.Clear();

            if (graphViewChange.elementsToRemove != null)
            {
                m_Graph.owner.RegisterCompleteObjectUndo("Remove Elements");
                m_Graph.RemoveElements(graphViewChange.elementsToRemove.OfType<IShaderNodeView>().Select(v => v.node).ToArray(),
                    graphViewChange.elementsToRemove.OfType<Edge>().Select(e => (IEdge)e.userData).ToArray(),
                    graphViewChange.elementsToRemove.OfType<ShaderGroup>().Select(g => g.userData).ToArray(),
                    graphViewChange.elementsToRemove.OfType<StickyNote>().Select(n => n.userData).ToArray());
                foreach (var edge in graphViewChange.elementsToRemove.OfType<Edge>())
                {
                    if (edge.input != null)
                    {
                        if (edge.input.node is IShaderNodeView materialNodeView)
                            nodesToUpdate.Add(materialNodeView);
                    }
                    if (edge.output != null)
                    {
                        if (edge.output.node is IShaderNodeView materialNodeView)
                            nodesToUpdate.Add(materialNodeView);
                    }
                }
            }

            foreach (var node in nodesToUpdate)
            {
                node.OnModified(ModificationScope.Topological);
            }

            UpdateEdgeColors(nodesToUpdate);
            return graphViewChange;
        }

        void SetGroupPosition(ShaderGroup groupNode)
        {
            var pos = groupNode.GetPosition();
            groupNode.userData.position = new Vector2(pos.x, pos.y);
        }

        void SetStickyNotePosition(StickyNote stickyNote)
        {
            var pos = stickyNote.GetPosition();
            stickyNote.userData.position = new Rect(pos);
        }

        void OnGroupTitleChanged(Group graphGroup, string title)
        {
            var groupData = graphGroup.userData as GroupData;
            if (groupData != null)
            {
                groupData.title = graphGroup.title;
            }
        }

        void OnElementsAddedToGroup(Group graphGroup, IEnumerable<GraphElement> elements)
        {
            if (graphGroup.userData is GroupData groupData)
            {
                var anyChanged = false;
                foreach (var element in elements)
                {
                    if (element.userData is IGroupItem groupItem && groupItem.group != groupData)
                    {
                        anyChanged = true;
                        break;
                    }
                }

                if (!anyChanged)
                    return;

                m_Graph.owner.RegisterCompleteObjectUndo(groupData.title);

                foreach (var element in elements)
                {
                    if (element.userData is IGroupItem groupItem)
                    {
                        m_Graph.SetGroup(groupItem, groupData);
                    }
                }
            }
        }

        void OnElementsRemovedFromGroup(Group graphGroup, IEnumerable<GraphElement> elements)
        {
            if (graphGroup.userData is GroupData groupData)
            {
                var anyChanged = false;
                foreach (var element in elements)
                {
                    if (element.userData is IGroupItem groupItem && groupItem.group == groupData)
                    {
                        anyChanged = true;
                        break;
                    }
                }

                if (!anyChanged)
                    return;

                m_Graph.owner.RegisterCompleteObjectUndo("Ungroup Node(s)");

                foreach (var element in elements)
                {
                    if (element.userData is IGroupItem groupItem)
                    {
                        m_Graph.SetGroup(groupItem, null);
                        SetGroupPosition((ShaderGroup)graphGroup); //, (GraphElement)nodeView);
                    }
                }
            }
        }

        void OnNodeChanged(AbstractMaterialNode inNode, ModificationScope scope)
        {
            if (m_GraphView == null)
                return;

            IEnumerable<IShaderNodeView> theViews = m_GraphView.nodes.ToList().OfType<IShaderNodeView>();

            var dependentNodes = new List<AbstractMaterialNode>();
            NodeUtils.CollectNodesNodeFeedsInto(dependentNodes, inNode);
            foreach (var node in dependentNodes)
            {
                var nodeView = theViews.FirstOrDefault(x => x.node.objectId == node.objectId);
                if (nodeView != null)
                    nodeView.OnModified(scope);
            }
        }

        HashSet<IShaderNodeView> m_NodeViewHashSet = new HashSet<IShaderNodeView>();
        HashSet<ShaderGroup> m_GroupHashSet = new HashSet<ShaderGroup>();

        public void HandleGraphChanges(bool wasUndoRedoPerformed)
        {
            UnregisterGraphViewCallbacks();

            previewManager.HandleGraphChanges();

            if (m_Graph.addedEdges.Any() || m_Graph.removedEdges.Any())
            {
                var nodeList = m_GraphView.Query<MaterialNodeView>().ToList();
                m_ColorManager.SetNodesDirty(nodeList);
                m_ColorManager.UpdateNodeViews(nodeList);
            }

            previewManager.RenderPreviews();
            m_BlackboardProvider.HandleGraphChanges(wasUndoRedoPerformed);
            if(wasUndoRedoPerformed || m_InspectorView.DoesInspectorNeedUpdate())
                m_InspectorView.Update();
            m_GroupHashSet.Clear();

            foreach (var node in m_Graph.removedNodes)
            {
                node.UnregisterCallback(OnNodeChanged);
                var nodeView = m_GraphView.nodes.ToList().OfType<IShaderNodeView>()
                    .FirstOrDefault(p => p.node != null && p.node == node);
                if (nodeView != null)
                {
                    nodeView.Dispose();

                    if(node is BlockNode blockNode)
                    {
                        var context = m_GraphView.GetContext(blockNode.contextData);
                        // blocknode may be floating and not actually in the stacknode's visual hierarchy.
                        if (context.Contains(nodeView as Node))
                        {
                            context.RemoveElement(nodeView as Node);
                        }
                        else
                        {
                            m_GraphView.RemoveElement((Node)nodeView);
                        }
                    }
                    else
                    {
                        m_GraphView.RemoveElement((Node)nodeView);
                    }

                    if (node.group != null)
                    {
                        var shaderGroup = m_GraphView.graphElements.ToList().OfType<ShaderGroup>().First(g => g.userData == node.group);
                        m_GroupHashSet.Add(shaderGroup);
                    }
                }
            }

            foreach (var noteData in m_Graph.removedNotes)
            {
                var note = m_GraphView.graphElements.ToList().OfType<StickyNote>().First(n => n.userData == noteData);
                m_GraphView.RemoveElement(note);
            }

            foreach (GroupData groupData in m_Graph.removedGroups)
            {
                var group = m_GraphView.graphElements.ToList().OfType<ShaderGroup>().First(g => g.userData == groupData);
                m_GraphView.RemoveElement(group);
            }

            foreach (var groupData in m_Graph.addedGroups)
            {
                AddGroup(groupData);
            }

            foreach (var stickyNote in m_Graph.addedStickyNotes)
            {
                AddStickyNote(stickyNote);
            }

            foreach (var node in m_Graph.addedNodes)
            {
                AddNode(node);
            }

            foreach (var groupChange in m_Graph.parentGroupChanges)
            {
                GraphElement graphElement = null;
                if (groupChange.groupItem is AbstractMaterialNode node)
                {
                    graphElement = m_GraphView.GetNodeByGuid(node.objectId);
                }
                else if (groupChange.groupItem is StickyNoteData stickyNote)
                {
                    graphElement = m_GraphView.GetElementByGuid(stickyNote.objectId);
                }
                else
                {
                    throw new InvalidOperationException("Unknown group item type.");
                }

                if (graphElement != null)
                {
                    var groupView = graphElement.GetContainingScope() as ShaderGroup;
                    if (groupView?.userData != groupChange.newGroup)
                    {
                        groupView?.RemoveElement(graphElement);
                        if (groupChange.newGroup != null)
                        {
                            var newGroupView = m_GraphView.graphElements.ToList()
                                .OfType<ShaderGroup>()
                                .First(x => x.userData == groupChange.newGroup);
                            newGroupView.AddElement(graphElement);
                        }
                    }
                }
            }

            foreach (var groupData in m_Graph.pastedGroups)
            {
                var group = m_GraphView.graphElements.ToList().OfType<ShaderGroup>().ToList().First(g => g.userData == groupData);
                m_GraphView.AddToSelection(group);
            }

            foreach (var stickyNoteData in m_Graph.pastedStickyNotes)
            {
                var stickyNote = m_GraphView.graphElements.ToList().OfType<StickyNote>().First(s => s.userData == stickyNoteData);
                m_GraphView.AddToSelection(stickyNote);
            }

            foreach (var node in m_Graph.pastedNodes)
            {
                var nodeView = m_GraphView.nodes.ToList().OfType<IShaderNodeView>()
                    .FirstOrDefault(p => p.node != null && p.node == node);
                m_GraphView.AddToSelection((Node)nodeView);
            }

            foreach (var shaderGroup in m_GroupHashSet)
            {
                SetGroupPosition(shaderGroup);
            }

            var nodesToUpdate = m_NodeViewHashSet;
            nodesToUpdate.Clear();

            foreach (var edge in m_Graph.removedEdges)
            {
                var edgeView = m_GraphView.graphElements.ToList().OfType<Edge>()
                    .FirstOrDefault(p => p.userData is IEdge && Equals((IEdge)p.userData, edge));
                if (edgeView != null)
                {
                    var nodeView = (IShaderNodeView)edgeView.input.node;
                    if (nodeView?.node != null)
                    {
                        nodesToUpdate.Add(nodeView);
                    }

                    edgeView.output.Disconnect(edgeView);
                    edgeView.input.Disconnect(edgeView);

                    edgeView.output = null;
                    edgeView.input = null;

                    m_GraphView.RemoveElement(edgeView);
                }
            }

            foreach (var edge in m_Graph.addedEdges)
            {
                var edgeView = AddEdge(edge);
                if (edgeView != null)
                    nodesToUpdate.Add((IShaderNodeView)edgeView.input.node);
            }

            foreach (var node in nodesToUpdate)
            {
                node.OnModified(ModificationScope.Topological);
            }

            UpdateEdgeColors(nodesToUpdate);

            if (m_Graph.movedContexts)
            {
                foreach (var context in m_GraphView.contexts)
                {
                    context.SetPosition(new Rect(context.contextData.position, Vector2.zero));
                }
            }

            // Checking if any new Group Nodes just got added
            if (m_Graph.mostRecentlyCreatedGroup != null)
            {
                var groups = m_GraphView.graphElements.ToList().OfType<ShaderGroup>();
                foreach (ShaderGroup shaderGroup in groups)
                {
                    if (shaderGroup.userData == m_Graph.mostRecentlyCreatedGroup)
                    {
                        shaderGroup.FocusTitleTextField();
                        break;
                    }
                }
            }

            UpdateBadges();

            RegisterGraphViewCallbacks();
        }

        void UpdateBadges()
        {
            if (!m_MessageManager.nodeMessagesChanged)
                return;

            foreach (var messageData in m_MessageManager.GetNodeMessages())
            {
                var node = m_Graph.GetNodeFromId(messageData.Key);

                if (node == null || !(m_GraphView.GetNodeByGuid(node.objectId) is IShaderNodeView nodeView))
                    continue;

                if (messageData.Value.Count == 0)
                {
                    nodeView.ClearMessage();
                }
                else
                {
                    var foundMessage = messageData.Value.First();
                    nodeView.AttachMessage(foundMessage.message, foundMessage.severity);
                }
            }
        }

        List<GraphElement> m_GraphElementsTemp = new List<GraphElement>();

        void AddNode(AbstractMaterialNode node, bool usePrebuiltVisualGroupMap = false)
        {
            var materialNode = node;
            Node nodeView;
            if (node is PropertyNode propertyNode)
            {
                var tokenNode = new PropertyNodeView(propertyNode, m_EdgeConnectorListener);
                m_GraphView.AddElement(tokenNode);
                nodeView = tokenNode;
            }
            else if(node is BlockNode blockNode)
            {
                var blockNodeView = new MaterialNodeView { userData = blockNode };
                blockNodeView.Initialize(blockNode, m_PreviewManager, m_EdgeConnectorListener, graphView);
                blockNodeView.MarkDirtyRepaint();
                nodeView = blockNodeView;

                var context = m_GraphView.GetContext(blockNode.contextData);
                context.InsertBlock(blockNodeView);
            }
            else if (node is RedirectNodeData redirectNodeData)
            {
                var redirectNodeView = new RedirectNodeView {userData = redirectNodeData};
                m_GraphView.AddElement(redirectNodeView);
                redirectNodeView.ConnectToData(materialNode, m_EdgeConnectorListener);
                nodeView = redirectNodeView;
            }
            else
            {
                var materialNodeView = new MaterialNodeView {userData = materialNode};
                m_GraphView.AddElement(materialNodeView);
                materialNodeView.Initialize(materialNode, m_PreviewManager, m_EdgeConnectorListener, graphView);
                m_ColorManager.UpdateNodeView(materialNodeView);
                nodeView = materialNodeView;
            }

            node.RegisterCallback(OnNodeChanged);
            nodeView.MarkDirtyRepaint();

            if (m_SearchWindowProvider.nodeNeedsRepositioning &&
                m_SearchWindowProvider.targetSlotReference.node == node)
            {
                m_SearchWindowProvider.nodeNeedsRepositioning = false;
                if (nodeView is IShaderNodeView shaderView &&
                    shaderView.FindPort(m_SearchWindowProvider.targetSlotReference, out var port))
                {
                    port.RegisterCallback<GeometryChangedEvent>(RepositionNode);
                    return;
                }
            }

            if (materialNode.group != null)
            {
                if (usePrebuiltVisualGroupMap)
                {
                    // cheaper way to add the node to groups it is in
                    ShaderGroup groupView;
                    visualGroupMap.TryGetValue(materialNode.group, out groupView);
                    if (groupView != null)
                        groupView.AddElement(nodeView);
                }
                else
                {
                    // This should also work for sticky notes
                    m_GraphElementsTemp.Clear();
                    m_GraphView.graphElements.ToList(m_GraphElementsTemp);

                    foreach (var element in m_GraphElementsTemp)
                    {
                        if (element is ShaderGroup groupView && groupView.userData == materialNode.group)
                        {
                            groupView.AddElement(nodeView);
                        }
                    }
                }
            }
        }

        private static Dictionary<GroupData, ShaderGroup> visualGroupMap = new Dictionary<GroupData, ShaderGroup>();
        private static void AddToVisualGroupMap(GraphElement e)
        {
            if (e is ShaderGroup sg)
            {
                visualGroupMap.Add(sg.userData, sg);
            }
        }
        private static Action<GraphElement> AddToVisualGroupMapAction = AddToVisualGroupMap;
        void BuildVisualGroupMap()
        {
            visualGroupMap.Clear();
            m_GraphView.graphElements.ForEach(AddToVisualGroupMapAction);
        }

        private static readonly ProfilerMarker AddNodesMarker = new ProfilerMarker("AddNodes");
        void AddNodes(IEnumerable<AbstractMaterialNode> nodes)
        {
            using (AddNodesMarker.Auto())
            {
                BuildVisualGroupMap();
                foreach (var node in nodes)
                {
                    // Skip BlockNodes as we need to order them
                    if(node is BlockNode)
                        continue;

                    AddNode(node, true);
                }
                visualGroupMap.Clear();
            }
        }

        private static readonly ProfilerMarker AddBlocksMarker = new ProfilerMarker("AddBlocks");
        void AddBlocks(IEnumerable<BlockNode> blocks)
        {
            using (AddBlocksMarker.Auto())
            {
                // As they can be reordered, we cannot be sure BlockNodes are deserialized in the same order as their stack position
                // To handle this we reorder the BlockNodes here to avoid having to reorder them on the fly as they are added
                foreach (var node in blocks.OrderBy(s => s.index))
                {
                    AddNode(node);
                }
            }
        }

        void AddGroup(GroupData groupData)
        {
            ShaderGroup graphGroup = new ShaderGroup();

            graphGroup.userData = groupData;
            graphGroup.title = groupData.title;
            graphGroup.SetPosition(new Rect(graphGroup.userData.position, Vector2.zero));

            m_GraphView.AddElement(graphGroup);
        }

        void AddStickyNote(StickyNoteData stickyNoteData)
        {
            var stickyNote = new StickyNote(stickyNoteData.position, m_Graph);

            stickyNote.userData = stickyNoteData;
            stickyNote.viewDataKey = stickyNoteData.objectId;
            stickyNote.title = stickyNoteData.title;
            stickyNote.contents = stickyNoteData.content;
            stickyNote.textSize = (StickyNote.TextSize)stickyNoteData.textSize;
            stickyNote.theme = (StickyNote.Theme)stickyNoteData.theme;
            stickyNote.userData.group = stickyNoteData.group;
            stickyNote.SetPosition(new Rect(stickyNote.userData.position));

            m_GraphView.AddElement(stickyNote);

            // Add Sticky Note to group
            m_GraphElementsTemp.Clear();
            m_GraphView.graphElements.ToList(m_GraphElementsTemp);

            if (stickyNoteData.group != null)
            {
                foreach (var element in m_GraphElementsTemp)
                {
                    if (element is ShaderGroup groupView && groupView.userData == stickyNoteData.group)
                    {
                        groupView.AddElement(stickyNote);
                    }
                }
            }
        }

        static void RepositionNode(GeometryChangedEvent evt)
        {
            var port = evt.target as ShaderPort;
            if (port == null)
                return;
            port.UnregisterCallback<GeometryChangedEvent>(RepositionNode);
            var nodeView = port.node as IShaderNodeView;
            if (nodeView == null)
                return;
            var offset = nodeView.gvNode.mainContainer.WorldToLocal(port.GetGlobalCenter() + new Vector3(3f, 3f, 0f));
            var position = nodeView.gvNode.GetPosition();
            position.position -= offset;
            nodeView.gvNode.SetPosition(position);
            var drawState = nodeView.node.drawState;
            drawState.position = position;
            nodeView.node.drawState = drawState;
            nodeView.gvNode.MarkDirtyRepaint();
            port.MarkDirtyRepaint();
        }

        private static Dictionary<AbstractMaterialNode, IShaderNodeView> visualNodeMap = new Dictionary<AbstractMaterialNode, IShaderNodeView>();
        private static void AddToVisualNodeMap(Node n)
        {
            IShaderNodeView snv = n as IShaderNodeView;
            if (snv != null)
                visualNodeMap.Add(snv.node, snv);
        }
        private static Action<Node> AddToVisualNodeMapAction = AddToVisualNodeMap;
        void BuildVisualNodeMap()
        {
            visualNodeMap.Clear();
            m_GraphView.nodes.ForEach(AddToVisualNodeMapAction);
        }

        private static readonly ProfilerMarker AddEdgesMarker = new ProfilerMarker("AddEdges");
        void AddEdges(IEnumerable<IEdge> edges)
        {
            using (AddEdgesMarker.Auto())
            {
                // fast way
                BuildVisualNodeMap();
                foreach (IEdge edge in edges)
                {
                    AddEdge(edge, true, false);
                }

                // apply the port update on every node
                foreach (IShaderNodeView nodeView in visualNodeMap.Values)
                {
                    nodeView.gvNode.RefreshPorts();
                    nodeView.UpdatePortInputTypes();
                }

                // cleanup temp data
                visualNodeMap.Clear();
            }
        }

        Edge AddEdge(IEdge edge, bool useVisualNodeMap = false, bool updateNodePorts = true)
        {
            var sourceNode = edge.outputSlot.node;
            if (sourceNode == null)
            {
                Debug.LogWarning("Source node is null");
                return null;
            }
            var sourceSlot = sourceNode.FindOutputSlot<MaterialSlot>(edge.outputSlot.slotId);

            var targetNode = edge.inputSlot.node;
            if (targetNode == null)
            {
                Debug.LogWarning("Target node is null");
                return null;
            }
            var targetSlot = targetNode.FindInputSlot<MaterialSlot>(edge.inputSlot.slotId);

            IShaderNodeView sourceNodeView;
            if (useVisualNodeMap)
                visualNodeMap.TryGetValue(sourceNode, out sourceNodeView);
            else
                sourceNodeView = m_GraphView.nodes.ToList().OfType<IShaderNodeView>().FirstOrDefault(x => x.node == sourceNode);

            if (sourceNodeView != null)
            {
                sourceNodeView.FindPort(sourceSlot.slotReference, out var sourceAnchor);

                IShaderNodeView targetNodeView;
                if (useVisualNodeMap)
                    visualNodeMap.TryGetValue(targetNode, out targetNodeView);
                else
                    targetNodeView = m_GraphView.nodes.ToList().OfType<IShaderNodeView>().First(x => x.node == targetNode);

                targetNodeView.FindPort(targetSlot.slotReference, out var targetAnchor);

                var edgeView = new Edge
                {
                    userData = edge,
                    output = sourceAnchor,
                    input = targetAnchor
                };

                edgeView.RegisterCallback<MouseDownEvent>(OnMouseDown);
                edgeView.output.Connect(edgeView);
                edgeView.input.Connect(edgeView);
                m_GraphView.AddElement(edgeView);

                if (updateNodePorts)
                {
                    sourceNodeView.gvNode.RefreshPorts();
                    targetNodeView.gvNode.RefreshPorts();
                    sourceNodeView.UpdatePortInputTypes();
                    targetNodeView.UpdatePortInputTypes();
                }

                return edgeView;
            }

            return null;
        }

        void OnMouseDown(MouseDownEvent evt)
        {
            if (evt.button == (int)MouseButton.LeftMouse && evt.clickCount == 2)
            {
                if (evt.target is Edge edgeTarget)
                {
                    Vector2 pos = evt.mousePosition;
                    m_GraphView.CreateRedirectNode(pos, edgeTarget);
                }
            }
        }

        Stack<Node> m_NodeStack = new Stack<Node>();

        void UpdateEdgeColors(HashSet<IShaderNodeView> nodeViews)
        {
            var nodeStack = m_NodeStack;
            nodeStack.Clear();
            foreach (var nodeView in nodeViews)
                nodeStack.Push((Node)nodeView);
            PooledList<Edge> edgesToUpdate = PooledList<Edge>.Get();
            while (nodeStack.Any())
            {
                var nodeView = nodeStack.Pop();
                if (nodeView is IShaderNodeView shaderNodeView)
                {
                    shaderNodeView.UpdatePortInputTypes();
                }

                foreach (var anchorView in nodeView.outputContainer.Children().OfType<Port>())
                {
                    foreach (var edgeView in anchorView.connections)
                    {
                        //update edges based on the active state of any modified nodes
                        if(edgeView.input.node is MaterialNodeView inputNode && edgeView.output.node is MaterialNodeView outputNode)
                        {
                            //force redraw on update to prevent visual lag in the graph
                            //Now has to be delayed a frame because setting port styles wont update colors till next frame
                            edgesToUpdate.Add(edgeView);
                        }
                        //update edges based on dynamic vector length of any modified nodes
                        var targetSlot = edgeView.input.GetSlot();
                        if (targetSlot.valueType == SlotValueType.DynamicVector || targetSlot.valueType == SlotValueType.DynamicMatrix || targetSlot.valueType == SlotValueType.Dynamic)
                        {
                            var connectedNodeView = edgeView.input.node;
                            if (connectedNodeView != null && !nodeViews.Contains((IShaderNodeView)connectedNodeView))
                            {
                                nodeStack.Push(connectedNodeView);
                                nodeViews.Add((IShaderNodeView)connectedNodeView);
                            }
                        }
                    }
                }

                foreach (var anchorView in nodeView.inputContainer.Query<Port>().ToList())
                {
                    var targetSlot = anchorView.GetSlot();
                    if (targetSlot.valueType != SlotValueType.DynamicVector)
                        continue;
                    foreach (var edgeView in anchorView.connections)
                    {
                        //update edges based on the active state of any modified nodes
                        if(edgeView.input.node is MaterialNodeView inputNode && edgeView.output.node is MaterialNodeView outputNode)
                        {
                            //force redraw on update to prevent visual lag in the graph
                            //Now has to be delayed a frame because setting port styles wont update colors till next frame
                            edgesToUpdate.Add(edgeView);
                        }
                        //update edge color for upstream dynamic vector types
                        var connectedNodeView = edgeView.output.node;
                        if (connectedNodeView != null && !nodeViews.Contains((IShaderNodeView)connectedNodeView))
                        {
                            nodeStack.Push(connectedNodeView);
                            nodeViews.Add((IShaderNodeView)connectedNodeView);
                        }
                    }
                }
            }
            schedule.Execute(() =>
            {
                foreach (Edge e in edgesToUpdate)
                {
                    e.UpdateEdgeControl();
                }
                edgesToUpdate.Dispose();
            }).StartingIn(0);
        }

        void ApplySerializedWindowLayouts(GeometryChangedEvent evt)
        {
            UnregisterCallback<GeometryChangedEvent>(ApplySerializedWindowLayouts);

            ApplyMasterPreviewLayout();

            ApplyBlackboardLayout();

            m_InspectorView.DeserializeLayout();
        }

        void ApplyMasterPreviewLayout()
        {
            // If a preview size was loaded in from saved user settings use that
            if (m_FloatingWindowsLayout.previewLayout.size.x > 0f && m_FloatingWindowsLayout.previewLayout.size.y > 0f)
            {
                previewManager.ResizeMasterPreview(m_FloatingWindowsLayout.previewLayout.size);
            }
            else // Use default specified in the stylesheet for master preview
            {
                m_FloatingWindowsLayout.previewLayout.size = m_MasterPreviewView.layout.size;
            }

            m_FloatingWindowsLayout.previewLayout.ApplyPosition(m_MasterPreviewView);

            m_MasterPreviewView.style.width = m_FloatingWindowsLayout.previewLayout.size.x;
            m_MasterPreviewView.style.height = m_FloatingWindowsLayout.previewLayout.size.y;
            m_MasterPreviewView.RegisterCallback<GeometryChangedEvent>(SerializeMasterPreviewLayout);
        }

        void SerializeMasterPreviewLayout(GeometryChangedEvent evt)
        {
            UpdateSerializedWindowLayout();
        }

        void ApplyBlackboardLayout()
        {
            // If a blackboard size was loaded in from saved user settings use that
            if (m_FloatingWindowsLayout.blackboardLayout.size.x > 0f && m_FloatingWindowsLayout.blackboardLayout.size.y > 0f)
            {
                blackboardProvider.blackboard.style.width = m_FloatingWindowsLayout.blackboardLayout.size.x;
                blackboardProvider.blackboard.style.height = m_FloatingWindowsLayout.blackboardLayout.size.y;
            }
            else // Use default specified in the stylesheet for blackboard
            {
                m_FloatingWindowsLayout.blackboardLayout.size = blackboardProvider.blackboard.layout.size;
            }

            // Restore blackboard layout, and make sure that it remains in the view.
            Rect blackboardRect = m_FloatingWindowsLayout.blackboardLayout.GetLayout(this.layout);

            // Make sure the dimensions are sufficiently large.
            blackboardRect.width = Mathf.Clamp(blackboardRect.width, 160f, m_GraphView.contentContainer.layout.width);
            blackboardRect.height = Mathf.Clamp(blackboardRect.height, 160f, m_GraphView.contentContainer.layout.height);

            // Make sure that the positioning is on screen.
            blackboardRect.x = Mathf.Clamp(blackboardRect.x, 0f,
                Mathf.Max(0f, m_GraphView.contentContainer.layout.width - blackboardRect.width));
            blackboardRect.y = Mathf.Clamp(blackboardRect.y, 0f,
                Mathf.Max(0f, m_GraphView.contentContainer.layout.height - blackboardRect.height));

            // Set the processed blackboard layout.
            m_BlackboardProvider.blackboard.SetPosition(blackboardRect);

            // After the layout is restored from the previous session, start tracking layout changes in the blackboard.
            m_BlackboardProvider.blackboard.RegisterCallback<GeometryChangedEvent>(SerializeBlackboardLayout);
        }

        void SerializeBlackboardLayout(GeometryChangedEvent evt)
        {
            UpdateSerializedWindowLayout();
        }

        void UpdateSerializedWindowLayout()
        {
            m_FloatingWindowsLayout.previewLayout.CalculateDockingCornerAndOffset(m_MasterPreviewView.layout, m_GraphView.layout);
            m_FloatingWindowsLayout.previewLayout.ClampToParentWindow();

            m_FloatingWindowsLayout.blackboardLayout.CalculateDockingCornerAndOffset(m_BlackboardProvider.blackboard.layout, m_GraphView.layout);
            m_FloatingWindowsLayout.blackboardLayout.ClampToParentWindow();

            m_InspectorView.ClampToParentLayout(m_GraphView.layout);

            if (m_MasterPreviewView.visible)
            {
                m_FloatingWindowsLayout.previewLayout.size = m_MasterPreviewView.layout.size;
            }

            string serializedWindowLayout = JsonUtility.ToJson(m_FloatingWindowsLayout);
            EditorUserSettings.SetConfigValue(k_FloatingWindowsLayoutKey, serializedWindowLayout);
        }

        public void Dispose()
        {
            if (m_GraphView != null)
            {
                saveRequested = null;
                saveAsRequested = null;
                convertToSubgraphRequested = null;
                showInProjectRequested = null;
                isCheckedOut = null;
                checkOut = null;
                foreach (var node in m_GraphView.Children().OfType<IShaderNodeView>())
                    node.Dispose();
                m_GraphView.nodeCreationRequest = null;
                m_GraphView = null;
            }
            if (previewManager != null)
            {
                previewManager.Dispose();
                previewManager = null;
            }
            if (m_SearchWindowProvider != null)
            {
                Object.DestroyImmediate(m_SearchWindowProvider);
                m_SearchWindowProvider = null;
            }
        }
    }
}
