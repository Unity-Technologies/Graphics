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


namespace UnityEditor.ShaderGraph.Drawing
{
    [Serializable]
    class FloatingWindowsLayout
    {
        public WindowDockingLayout previewLayout = new WindowDockingLayout();
        public WindowDockingLayout blackboardLayout = new WindowDockingLayout();
        public Vector2 masterPreviewSize = new Vector2(400, 400);
    }

    [Serializable]
    class UserViewSettings
    {
        public bool isBlackboardVisible = true;
        public bool isPreviewVisible = true;
        public string colorProvider = NoColors.Title;
    }

    class GraphEditorView : VisualElement, IDisposable
    {
        MaterialGraphView m_GraphView;
        MasterPreviewView m_MasterPreviewView;

        GraphData m_Graph;
        PreviewManager m_PreviewManager;
        MessageManager m_MessageManager;
        SearchWindowProvider m_SearchWindowProvider;
        EdgeConnectorListener m_EdgeConnectorListener;
        BlackboardProvider m_BlackboardProvider;
        ColorManager m_ColorManager;

        public BlackboardProvider blackboardProvider
        {
            get { return m_BlackboardProvider; }
        }

        const string k_UserViewSettings = "UnityEditor.ShaderGraph.ToggleSettings";
        UserViewSettings m_UserViewSettings;

        const string k_FloatingWindowsLayoutKey = "UnityEditor.ShaderGraph.FloatingWindowsLayout2";
        FloatingWindowsLayout m_FloatingWindowsLayout;

        public Action saveRequested { get; set; }

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

        public GraphEditorView(EditorWindow editorWindow, GraphData graph, MessageManager messageManager)
        {
            m_GraphViewGroupTitleChanged = OnGroupTitleChanged;
            m_GraphViewElementsAddedToGroup = OnElementsAddedToGroup;
            m_GraphViewElementsRemovedFromGroup = OnElementsRemovedFromGroup;

            m_Graph = graph;
            m_MessageManager = messageManager;
            styleSheets.Add(Resources.Load<StyleSheet>("Styles/GraphEditorView"));
            previewManager = new PreviewManager(graph, messageManager);
            previewManager.onPrimaryMasterChanged = OnPrimaryMasterChanged;

            var serializedSettings = EditorUserSettings.GetConfigValue(k_UserViewSettings);
            m_UserViewSettings = JsonUtility.FromJson<UserViewSettings>(serializedSettings) ?? new UserViewSettings();
            m_ColorManager = new ColorManager(m_UserViewSettings.colorProvider);

            string serializedWindowLayout = EditorUserSettings.GetConfigValue(k_FloatingWindowsLayoutKey);
            if (!string.IsNullOrEmpty(serializedWindowLayout))
            {
                m_FloatingWindowsLayout = JsonUtility.FromJson<FloatingWindowsLayout>(serializedWindowLayout);
            }
            else
            {
                m_FloatingWindowsLayout = new FloatingWindowsLayout
                {
                    blackboardLayout =
                    {
                        dockingTop = true,
                        dockingLeft = true,
                        verticalOffset = 16,
                        horizontalOffset = 16,
                        size = new Vector2(200, 400)
                    }
                };
            }

            if (m_FloatingWindowsLayout.masterPreviewSize.x > 0f && m_FloatingWindowsLayout.masterPreviewSize.y > 0f)
            {
                previewManager.ResizeMasterPreview(m_FloatingWindowsLayout.masterPreviewSize);
            }

            previewManager.RenderPreviews();
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
                    if (GUILayout.Button("Show In Project", EditorStyles.toolbarButton))
                    {
                        if (showInProjectRequested != null)
                            showInProjectRequested();
                    }

                    EditorGUI.BeginChangeCheck();
                    GUILayout.Label("Precision");
                    graph.concretePrecision = (ConcretePrecision)EditorGUILayout.EnumPopup(graph.concretePrecision, GUILayout.Width(100f));
                    GUILayout.Space(4);
                    if (EditorGUI.EndChangeCheck())
                    {
                        var nodeList = m_GraphView.Query<MaterialNodeView>().ToList();
                        m_ColorManager.SetNodesDirty(nodeList);
                        graph.ValidateGraph();
                        m_ColorManager.UpdateNodeViews(nodeList);
                        foreach (var node in graph.GetNodes<AbstractMaterialNode>())
                        {
                            node.Dirty(ModificationScope.Graph);
                        }
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
                    var newColorIdx = EditorGUILayout.Popup(m_ColorManager.activeIndex, colorProviders, GUILayout.Width(100f));
                    GUILayout.Space(4);
                    m_UserViewSettings.isBlackboardVisible = GUILayout.Toggle(m_UserViewSettings.isBlackboardVisible, "Blackboard", EditorStyles.toolbarButton);

                    GUILayout.Space(6);

                    m_UserViewSettings.isPreviewVisible = GUILayout.Toggle(m_UserViewSettings.isPreviewVisible, "Main Preview", EditorStyles.toolbarButton);
                    if (EditorGUI.EndChangeCheck())
                    {
                        if(newColorIdx != m_ColorManager.activeIndex)
                        {
                            m_ColorManager.SetActiveProvider(newColorIdx, m_GraphView.Query<MaterialNodeView>().ToList());
                            m_UserViewSettings.colorProvider = m_ColorManager.activeProviderName;
                        }

                        UpdateSubWindowsVisibility();

                        var serializedViewSettings = JsonUtility.ToJson(m_UserViewSettings);
                        EditorUserSettings.SetConfigValue(k_UserViewSettings, serializedViewSettings);
                    }
                    GUILayout.EndHorizontal();
                });
            Add(toolbar);

            var content = new VisualElement { name = "content" };
            {
                m_GraphView = new MaterialGraphView(graph) { name = "GraphView", viewDataKey = "MaterialGraphView" };
                m_GraphView.SetupZoom(0.05f, ContentZoomer.DefaultMaxScale);
                m_GraphView.AddManipulator(new ContentDragger());
                m_GraphView.AddManipulator(new SelectionDragger());
                m_GraphView.AddManipulator(new RectangleSelector());
                m_GraphView.AddManipulator(new ClickSelector());
                m_GraphView.RegisterCallback<KeyDownEvent>(OnKeyDown);
                RegisterGraphViewCallbacks();
                content.Add(m_GraphView);

                m_BlackboardProvider = new BlackboardProvider(graph);
                m_GraphView.Add(m_BlackboardProvider.blackboard);

                CreateMasterPreview();

                UpdateSubWindowsVisibility();

                m_GraphView.graphViewChanged = GraphViewChanged;

                RegisterCallback<GeometryChangedEvent>(ApplySerializewindowLayouts);
                if (m_Graph.isSubGraph)
                {
                    m_GraphView.AddToClassList("subgraph");
                }
            }

            m_SearchWindowProvider = ScriptableObject.CreateInstance<SearchWindowProvider>();
            m_SearchWindowProvider.Initialize(editorWindow, m_Graph, m_GraphView);
            m_GraphView.nodeCreationRequest = (c) =>
                {
                    m_SearchWindowProvider.connectedPort = null;
                    SearchWindow.Open(new SearchWindowContext(c.screenMousePosition), m_SearchWindowProvider);
                };

            m_EdgeConnectorListener = new EdgeConnectorListener(m_Graph, m_SearchWindowProvider);

            foreach (var graphGroup in graph.groups)
            {
                AddGroup(graphGroup);
            }

            foreach (var stickyNote in graph.stickyNotes)
            {
                AddStickyNote(stickyNote);
            }

            foreach (var node in graph.GetNodes<AbstractMaterialNode>())
                AddNode(node);

            foreach (var edge in graph.edges)
                AddEdge(edge);

            Add(content);
        }

        void UpdateSubWindowsVisibility()
        {
            m_MasterPreviewView.visible = m_UserViewSettings.isPreviewVisible;

            if (m_UserViewSettings.isBlackboardVisible)
                m_BlackboardProvider.blackboard.style.display = DisplayStyle.Flex;
            else
                m_BlackboardProvider.blackboard.style.display = DisplayStyle.None;
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

            if (evt.ctrlKey && evt.keyCode == KeyCode.G)
            {
                if (m_GraphView.selection.OfType<MaterialNodeView>().Any())
                {
                    m_GraphView.GroupSelection();
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
                    }

                    if (element is StickyNote stickyNote)
                    {
                        SetStickyNotePosition(stickyNote);
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
                if (node is MaterialNodeView materialNodeView)
                {
                    materialNodeView.OnModified(ModificationScope.Topological);
                }
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
                    if (element.userData is IGroupItem groupItem && groupItem.groupGuid != groupData.guid)
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
                    if (element.userData is IGroupItem groupItem && groupItem.groupGuid == groupData.guid)
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

            var dependentNodes = new List<AbstractMaterialNode>();
            NodeUtils.CollectNodesNodeFeedsInto(dependentNodes, inNode);
            foreach (var node in dependentNodes)
            {
                var theViews = m_GraphView.nodes.ToList().OfType<IShaderNodeView>();
                var viewsFound = theViews.Where(x => x.node.guid == node.guid).ToList();
                foreach (var drawableNodeData in viewsFound)
                    drawableNodeData.OnModified(scope);
            }
        }

        HashSet<IShaderNodeView> m_NodeViewHashSet = new HashSet<IShaderNodeView>();
        HashSet<ShaderGroup> m_GroupHashSet = new HashSet<ShaderGroup>();

        public void HandleGraphChanges()
        {
            UnregisterGraphViewCallbacks();

            if(previewManager.HandleGraphChanges())
            {
                var nodeList = m_GraphView.Query<MaterialNodeView>().ToList();

                m_ColorManager.SetNodesDirty(nodeList);
                m_ColorManager.UpdateNodeViews(nodeList);
            }

            previewManager.RenderPreviews();
            m_BlackboardProvider.HandleGraphChanges();
            m_GroupHashSet.Clear();

            foreach (var node in m_Graph.removedNodes)
            {
                node.UnregisterCallback(OnNodeChanged);
                var nodeView = m_GraphView.nodes.ToList().OfType<IShaderNodeView>()
                    .FirstOrDefault(p => p.node != null && p.node.guid == node.guid);
                if (nodeView != null)
                {
                    nodeView.Dispose();
                    m_GraphView.RemoveElement((Node)nodeView);

                    if (node.groupGuid != Guid.Empty)
                    {
                        var shaderGroup = m_GraphView.graphElements.ToList().OfType<ShaderGroup>().First(g => g.userData.guid == node.groupGuid);
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
                    graphElement = m_GraphView.GetNodeByGuid(node.guid.ToString());
                }
                else if (groupChange.groupItem is StickyNoteData stickyNote)
                {
                    graphElement = m_GraphView.GetElementByGuid(stickyNote.guid.ToString());
                }
                else
                {
                    throw new InvalidOperationException("Unknown group item type.");
                }

                if (graphElement != null)
                {
                    var groupView = graphElement.GetContainingScope() as ShaderGroup;
                    if (groupView?.userData.guid != groupChange.newGroupGuid)
                    {
                        groupView?.RemoveElement(graphElement);
                        if (groupChange.newGroupGuid != Guid.Empty)
                        {
                            var newGroupView = m_GraphView.graphElements.ToList()
                                .OfType<ShaderGroup>()
                                .First(x => x.userData.guid == groupChange.newGroupGuid);
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
                    .FirstOrDefault(p => p.node != null && p.node.guid == node.guid);
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
                    .FirstOrDefault(p => p.userData is IEdge && Equals((IEdge) p.userData, edge));
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
                if (node is MaterialNodeView materialNodeView)
                {
                    materialNodeView.OnModified(ModificationScope.Topological);
                }
            }

            UpdateEdgeColors(nodesToUpdate);

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
                var node = m_Graph.GetNodeFromTempId(messageData.Key);

                if (!(m_GraphView.GetNodeByGuid(node.guid.ToString()) is MaterialNodeView nodeView))
                    continue;

                if (messageData.Value.Count == 0)
                {
                    var badge = nodeView.Q<IconBadge>();
                    badge?.Detach();
                    badge?.RemoveFromHierarchy();
                }
                else
                {
                    var foundMessage = messageData.Value.First();
                    nodeView.AttachMessage(foundMessage.message, foundMessage.severity);
                }
            }
        }

        List<GraphElement> m_GraphElementsTemp = new List<GraphElement>();

        void AddNode(AbstractMaterialNode node)
        {
            var materialNode = (AbstractMaterialNode)node;
            Node nodeView;
            if (node is PropertyNode propertyNode)
            {
                var tokenNode = new PropertyNodeView(propertyNode, m_EdgeConnectorListener);
                m_GraphView.AddElement(tokenNode);
                nodeView = tokenNode;
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

            if (m_SearchWindowProvider.nodeNeedsRepositioning && m_SearchWindowProvider.targetSlotReference.nodeGuid.Equals(node.guid))
            {
                m_SearchWindowProvider.nodeNeedsRepositioning = false;
                foreach (var element in nodeView.inputContainer.Children().Union(nodeView.outputContainer.Children()))
                {
                    var port = (ShaderPort)element;
                    if (port.slot.slotReference.Equals(m_SearchWindowProvider.targetSlotReference))
                    {
                        port.RegisterCallback<GeometryChangedEvent>(RepositionNode);
                        return;
                    }
                }
            }

            // This should also work for sticky notes
            m_GraphElementsTemp.Clear();
            m_GraphView.graphElements.ToList(m_GraphElementsTemp);

            if (materialNode.groupGuid != Guid.Empty)
            {
                foreach (var element in m_GraphElementsTemp)
                {
                    if (element is ShaderGroup groupView && groupView.userData.guid == materialNode.groupGuid)
                    {
                        groupView.AddElement(nodeView);
                    }
                }
            }
        }

        void AddGroup(GroupData groupData)
        {
            ShaderGroup graphGroup = new ShaderGroup(m_Graph);

            graphGroup.userData = groupData;
            graphGroup.title = groupData.title;
            graphGroup.SetPosition(new Rect(graphGroup.userData.position, Vector2.zero));

            m_GraphView.AddElement(graphGroup);
        }

        void AddStickyNote(StickyNoteData stickyNoteData)
        {
            var stickyNote = new StickyNote(stickyNoteData.position, m_Graph);

            stickyNote.userData = stickyNoteData;
            stickyNote.viewDataKey = stickyNoteData.guid.ToString();
            stickyNote.title = stickyNoteData.title;
            stickyNote.contents = stickyNoteData.content;
            stickyNote.textSize = (StickyNote.TextSize)stickyNoteData.textSize;
            stickyNote.theme = (StickyNote.Theme)stickyNoteData.theme;
            stickyNote.userData.groupGuid = stickyNoteData.groupGuid;
            stickyNote.SetPosition(new Rect(stickyNote.userData.position));

            m_GraphView.AddElement(stickyNote);

            // Add Sticky Note to group
            m_GraphElementsTemp.Clear();
            m_GraphView.graphElements.ToList(m_GraphElementsTemp);

            if (stickyNoteData.groupGuid != Guid.Empty)
            {
                foreach (var element in m_GraphElementsTemp)
                {
                    if (element is ShaderGroup groupView && groupView.userData.guid == stickyNoteData.groupGuid)
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

        Edge AddEdge(IEdge edge)
        {
            var sourceNode = m_Graph.GetNodeFromGuid(edge.outputSlot.nodeGuid);
            if (sourceNode == null)
            {
                Debug.LogWarning("Source node is null");
                return null;
            }
            var sourceSlot = sourceNode.FindOutputSlot<MaterialSlot>(edge.outputSlot.slotId);

            var targetNode = m_Graph.GetNodeFromGuid(edge.inputSlot.nodeGuid);
            if (targetNode == null)
            {
                Debug.LogWarning("Target node is null");
                return null;
            }
            var targetSlot = targetNode.FindInputSlot<MaterialSlot>(edge.inputSlot.slotId);

            var sourceNodeView = m_GraphView.nodes.ToList().OfType<IShaderNodeView>().FirstOrDefault(x => x.node == sourceNode);
            if (sourceNodeView != null)
            {
                var sourceAnchor = sourceNodeView.gvNode.outputContainer.Children().OfType<ShaderPort>().First(x => x.slot.Equals(sourceSlot));

                var targetNodeView = m_GraphView.nodes.ToList().OfType<IShaderNodeView>().First(x => x.node == targetNode);
                var targetAnchor = targetNodeView.gvNode.inputContainer.Children().OfType<ShaderPort>().First(x => x.slot.Equals(targetSlot));

                var edgeView = new Edge
                {
                    userData = edge,
                    output = sourceAnchor,
                    input = targetAnchor
                };
                edgeView.output.Connect(edgeView);
                edgeView.input.Connect(edgeView);
                m_GraphView.AddElement(edgeView);
                sourceNodeView.gvNode.RefreshPorts();
                targetNodeView.gvNode.RefreshPorts();
                sourceNodeView.UpdatePortInputTypes();
                targetNodeView.UpdatePortInputTypes();

                return edgeView;
            }

            return null;
        }

        Stack<Node> m_NodeStack = new Stack<Node>();

        void UpdateEdgeColors(HashSet<IShaderNodeView> nodeViews)
        {
            var nodeStack = m_NodeStack;
            nodeStack.Clear();
            foreach (var nodeView in nodeViews)
                nodeStack.Push((Node)nodeView);
            while (nodeStack.Any())
            {
                var nodeView = nodeStack.Pop();
                if (nodeView is MaterialNodeView materialNodeView)
                {
                    materialNodeView.UpdatePortInputTypes();
                }
                foreach (var anchorView in nodeView.outputContainer.Children().OfType<Port>())
                {
                    foreach (var edgeView in anchorView.connections)
                    {
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
                foreach (var anchorView in nodeView.inputContainer.Children().OfType<Port>())
                {
                    var targetSlot = anchorView.GetSlot();
                    if (targetSlot.valueType != SlotValueType.DynamicVector)
                        continue;
                    foreach (var edgeView in anchorView.connections)
                    {
                        var connectedNodeView = edgeView.output.node;
                        if (connectedNodeView != null && !nodeViews.Contains((IShaderNodeView)connectedNodeView))
                        {
                            nodeStack.Push(connectedNodeView);
                            nodeViews.Add((IShaderNodeView)connectedNodeView);
                        }
                    }
                }
            }
        }

        void OnPrimaryMasterChanged()
        {
            m_MasterPreviewView?.RemoveFromHierarchy();
            CreateMasterPreview();
            ApplyMasterPreviewLayout();
            UpdateSubWindowsVisibility();
        }

        void HandleEditorViewChanged(GeometryChangedEvent evt)
        {
            m_BlackboardProvider.blackboard.SetPosition(m_FloatingWindowsLayout.blackboardLayout.GetLayout(m_GraphView.layout));
        }

        void StoreBlackboardLayoutOnGeometryChanged(GeometryChangedEvent evt)
        {
            UpdateSerializedWindowLayout();
        }

        void ApplySerializewindowLayouts(GeometryChangedEvent evt)
        {
            UnregisterCallback<GeometryChangedEvent>(ApplySerializewindowLayouts);

            ApplyMasterPreviewLayout();

            // Restore blackboard layout, and make sure that it remains in the view.
            Rect blackboardRect = m_FloatingWindowsLayout.blackboardLayout.GetLayout(this.layout);

            // Make sure the dimensions are sufficiently large.
            blackboardRect.width = Mathf.Clamp(blackboardRect.width, 160f, m_GraphView.contentContainer.layout.width);
            blackboardRect.height = Mathf.Clamp(blackboardRect.height, 160f, m_GraphView.contentContainer.layout.height);

            // Make sure that the positionining is on screen.
            blackboardRect.x = Mathf.Clamp(blackboardRect.x, 0f, Mathf.Max(1f, m_GraphView.contentContainer.layout.width - blackboardRect.width - blackboardRect.width));
            blackboardRect.y = Mathf.Clamp(blackboardRect.y, 0f, Mathf.Max(1f, m_GraphView.contentContainer.layout.height - blackboardRect.height - blackboardRect.height));

            // Set the processed blackboard layout.
            m_BlackboardProvider.blackboard.SetPosition(blackboardRect);

            previewManager.ResizeMasterPreview(m_FloatingWindowsLayout.masterPreviewSize);

            // After the layout is restored from the previous session, start tracking layout changes in the blackboard.
            m_BlackboardProvider.blackboard.RegisterCallback<GeometryChangedEvent>(StoreBlackboardLayoutOnGeometryChanged);

            // After the layout is restored, track changes in layout and make the blackboard have the same behavior as the preview w.r.t. docking.
            RegisterCallback<GeometryChangedEvent>(HandleEditorViewChanged);
        }

        void ApplyMasterPreviewLayout()
        {
            m_FloatingWindowsLayout.previewLayout.ApplyPosition(m_MasterPreviewView);
            m_MasterPreviewView.previewTextureView.style.width = m_FloatingWindowsLayout.masterPreviewSize.x;
            m_MasterPreviewView.previewTextureView.style.height = m_FloatingWindowsLayout.masterPreviewSize.y;
        }

        void UpdateSerializedWindowLayout()
        {
            m_FloatingWindowsLayout.previewLayout.CalculateDockingCornerAndOffset(m_MasterPreviewView.layout, m_GraphView.layout);
            m_FloatingWindowsLayout.previewLayout.ClampToParentWindow();

            m_FloatingWindowsLayout.blackboardLayout.CalculateDockingCornerAndOffset(m_BlackboardProvider.blackboard.layout, m_GraphView.layout);
            m_FloatingWindowsLayout.blackboardLayout.ClampToParentWindow();

            if (m_MasterPreviewView.expanded)
            {
                m_FloatingWindowsLayout.masterPreviewSize = m_MasterPreviewView.previewTextureView.layout.size;
            }

            string serializedWindowLayout = JsonUtility.ToJson(m_FloatingWindowsLayout);
            EditorUserSettings.SetConfigValue(k_FloatingWindowsLayoutKey, serializedWindowLayout);
        }

        public void Dispose()
        {
            if (m_GraphView != null)
            {
                saveRequested = null;
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
