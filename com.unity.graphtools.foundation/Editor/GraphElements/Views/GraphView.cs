using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEngine;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    class ContentViewContainer : VisualElement
    {
        public override bool Overlaps(Rect r)
        {
            return true;
        }
    }

    public enum PasteOperation
    {
        Duplicate,
        Paste
    }

    /// <summary>
    /// The <see cref="VisualElement"/> in which graphs are drawn.
    /// </summary>
    public class GraphView : BaseView, IDragAndDropHandler, IDragSource
    {
        /// <summary>
        /// GraphView elements are organized into layers to ensure some type of graph elements
        /// are always drawn on top of others.
        /// </summary>
        public class Layer : VisualElement {}

        class UpdateObserver : StateObserver
        {
            GraphView m_GraphView;
            GraphProcessingStateComponent m_GraphProcessingState;

            public UpdateObserver(GraphView graphView, GraphProcessingStateComponent graphProcessingState)
                : base(graphView.GraphViewState, graphView.SelectionState, graphProcessingState)
            {
                m_GraphView = graphView;
                m_GraphProcessingState = graphProcessingState;
            }

            public override void Observe()
            {
                if (m_GraphView?.panel != null)
                    m_GraphView.Update(this, m_GraphProcessingState);
            }
        }

        internal delegate string SerializeGraphElementsDelegate(IEnumerable<IGraphElementModel> elements);
        public delegate bool CanPasteSerializedDataDelegate(string data);
        public delegate void UnserializeAndPasteDelegate(PasteOperation operation, string operationName, string data);

        public static readonly string ussClassName = "ge-graph-view";

        internal const int k_FrameBorder = 30;
        const string k_SerializedDataMimeType = "application/vnd.unity.graphview.elements";

        static IReadOnlyList<IGraphElementModel> s_EmptyList = new List<IGraphElementModel>();

        protected Hash128 m_Guid;

        readonly Dictionary<int, Layer> m_ContainerLayers = new Dictionary<int, Layer>();

        ContextualMenuManipulator m_ContextualMenuManipulator;
        ContentZoomer m_Zoomer;

        AutoSpacingHelper m_AutoSpacingHelper;
        AutoAlignmentHelper m_AutoAlignmentHelper;

        float m_MinScale = ContentZoomer.DefaultMinScale;
        float m_MaxScale = ContentZoomer.DefaultMaxScale;
        float m_MaxScaleOnFrame = 1.0f;
        float m_ScaleStep = ContentZoomer.DefaultScaleStep;
        float m_ReferenceScale = ContentZoomer.DefaultReferenceScale;

        Blackboard m_Blackboard;
        readonly VisualElement m_GraphViewContainer;
        readonly VisualElement m_BadgesParent;

        SelectionDragger m_SelectionDragger;
        ContentDragger m_ContentDragger;
        Clickable m_Clickable;
        RectangleSelector m_RectangleSelector;
        FreehandSelector m_FreehandSelector;

        bool m_BuildingBlackboard;

        string m_Clipboard = string.Empty;

        IDragAndDropHandler m_CurrentDragAndDropHandler;
        BlackboardDragAndDropHandler m_BlackboardDragAndDropHandler;

        protected bool m_SelectionDraggerWasActive;
        protected Vector2 m_LastMousePosition;

        GraphViewStateComponent.GraphAssetLoadedObserver m_GraphViewGraphLoadedAssetObserver;
        SelectionStateComponent.GraphAssetLoadedObserver m_SelectionGraphLoadedAssetObserver;
        UpdateObserver m_UpdateObserver;
        EdgeOrderObserver m_EdgeOrderObserver;

        /// <summary>
        /// The VisualElement that contains all the views.
        /// </summary>
        public VisualElement ContentViewContainer { get; }

        /// <summary>
        /// The transform of the ContentViewContainer.
        /// </summary>
        public ITransform ViewTransform => ContentViewContainer.transform;

        // BE AWARE: This is just a stopgap measure to get the minimap notified and should not be used outside of it.
        // This should also get ripped once the minimap is re-written.
        internal Action Redrawn { get; set; }

        protected SelectionDragger SelectionDragger
        {
            get => m_SelectionDragger;
            set => this.ReplaceManipulator(ref m_SelectionDragger, value);
        }

        protected ContentDragger ContentDragger
        {
            get => m_ContentDragger;
            set => this.ReplaceManipulator(ref m_ContentDragger, value);
        }

        protected Clickable Clickable
        {
            get => m_Clickable;
            set => this.ReplaceManipulator(ref m_Clickable, value);
        }

        protected RectangleSelector RectangleSelector
        {
            get => m_RectangleSelector;
            set => this.ReplaceManipulator(ref m_RectangleSelector, value);
        }

        public FreehandSelector FreehandSelector
        {
            get => m_FreehandSelector;
            set => this.ReplaceManipulator(ref m_FreehandSelector, value);
        }

        protected ContextualMenuManipulator ContextualMenuManipulator
        {
            get => m_ContextualMenuManipulator;
            set => this.ReplaceManipulator(ref m_ContextualMenuManipulator, value);
        }

        protected ContentZoomer ContentZoomer
        {
            get => m_Zoomer;
            set => this.ReplaceManipulator(ref m_Zoomer, value);
        }

        /// <summary>
        /// The graph view state component.
        /// </summary>
        public GraphViewStateComponent GraphViewState { get; }

        /// <summary>
        /// The selection state component.
        /// </summary>
        public SelectionStateComponent SelectionState { get; }

        /// <summary>
        /// The blackboard state component.
        /// </summary>
        // PF FIXME move to Blackboard when it becomes a view.
        public BlackboardViewStateComponent BlackboardViewState { get; private set; }

        /// <summary>
        /// The graph model displayed by the graph view.
        /// </summary>
        public IGraphModel GraphModel => GraphViewState.GraphModel;

        /// <inheritdoc />
        public IReadOnlyList<IGraphElementModel> GetSelection()
        {
            return SelectionState?.GetSelection(GraphModel) ?? s_EmptyList;
        }

        public GraphViewEditorWindow Window { get; }

        public virtual bool SupportsWindowedBlackboard => true;

        public override VisualElement contentContainer => m_GraphViewContainer; // Contains full content, potentially partially visible

        public PlacematContainer PlacematContainer { get; }

        public virtual bool CanCopySelection => GetSelection().Any(ge => ge.IsCopiable());

        public virtual bool CanCutSelection => GetSelection().Any(ge => ge.IsCopiable() && ge.IsDeletable());

        public virtual bool CanPaste => CanPasteSerializedData(Clipboard);

        public virtual bool CanDuplicateSelection => CanCopySelection;

        public virtual bool CanDeleteSelection => GetSelection().Any(e => e.IsDeletable());

        internal SerializeGraphElementsDelegate SerializeGraphElementsCallback { get; set; }

        public CanPasteSerializedDataDelegate CanPasteSerializedDataCallback { get; set; }

        public UnserializeAndPasteDelegate UnserializeAndPasteCallback { get; set; }

        // For tests only
        internal bool UseInternalClipboard { get; set; }

        // Internal access for tests.
        internal string Clipboard
        {
            get => UseInternalClipboard ? m_Clipboard : EditorGUIUtility.systemCopyBuffer;

            set
            {
                if (UseInternalClipboard)
                {
                    m_Clipboard = value;
                }
                else
                {
                    EditorGUIUtility.systemCopyBuffer = value;
                }
            }
        }

        /// <summary>
        /// Gets the blackboard for this graph view.
        /// </summary>
        /// <returns>The blackboard element.</returns>
        public Blackboard GetBlackboard()
        {
            var blackboardModel = GraphTool?.ToolState.BlackboardGraphModel;
            if (blackboardModel == null)
            {
                m_Blackboard?.RemoveFromView();
                m_Blackboard = null;
            }
            else if (!m_BuildingBlackboard && (m_Blackboard == null || !ReferenceEquals(m_Blackboard.Model, blackboardModel)))
            {
                CreateBlackboardStateComponent();
                m_BuildingBlackboard = true;
                m_Blackboard = GraphElementFactory.CreateUI<Blackboard>(this, blackboardModel);
                m_Blackboard?.AddToView(this);
                m_BuildingBlackboard = false;
            }

            return m_Blackboard;
        }

        void CreateBlackboardStateComponent()
        {
            if (BlackboardViewState == null && GraphTool != null)
            {
                var viewGuid = new Hash128();
                viewGuid.Append(name);
                viewGuid.Append("Blackboard");
                var assetKey = PersistedState.MakeAssetKey(GraphTool.ToolState.AssetModel);

                BlackboardViewState = PersistedState.GetOrCreateAssetViewStateComponent<BlackboardViewStateComponent>(default, viewGuid, assetKey);
                GraphTool.State.AddStateComponent(BlackboardViewState);

                BlackboardCommandsRegistrar.RegisterCommands(this, GraphTool);
            }
        }

        internal PositionDependenciesManager PositionDependenciesManager { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphView" /> class.
        /// </summary>
        /// <param name="window">The window to which the GraphView belongs.</param>
        /// <param name="graphTool">The tool for this GraphView.</param>
        /// <param name="graphViewName">The name of the GraphView.</param>
        public GraphView(GraphViewEditorWindow window, BaseGraphTool graphTool, string graphViewName)
        : base(graphTool)
        {
            focusable = true;

            Window = window;

            graphViewName ??= "GraphView_" + new Random().Next();

            m_Guid = new Hash128();
            m_Guid.Append(graphViewName);

            if (GraphTool != null)
            {
                var assetKey = PersistedState.MakeAssetKey(GraphTool.ToolState.AssetModel);

                GraphViewState = PersistedState.GetOrCreateAssetViewStateComponent<GraphViewStateComponent>(default, m_Guid, assetKey);
                GraphTool.State.AddStateComponent(GraphViewState);

                SelectionState = PersistedState.GetOrCreateAssetViewStateComponent<SelectionStateComponent>(default, m_Guid, assetKey);
                GraphTool.State.AddStateComponent(SelectionState);

                GraphViewCommandsRegistrar.RegisterCommands(this, GraphTool);
            }

            name = graphViewName;

            AddToClassList(ussClassName);

            this.SetRenderHintsForGraphView();

            m_GraphViewContainer = new VisualElement() { name = "graph-view-container" };
            m_GraphViewContainer.pickingMode = PickingMode.Ignore;
            hierarchy.Add(m_GraphViewContainer);

            ContentViewContainer = new ContentViewContainer
            {
                name = "content-view-container",
                pickingMode = PickingMode.Ignore,
                usageHints = UsageHints.GroupTransform
            };
#if UNITY_2021_2_OR_NEWER
            ContentViewContainer.style.transformOrigin = new TransformOrigin(0, 0, 0);
#endif
            // make it absolute and 0 sized so it acts as a transform to move children to and fro
            m_GraphViewContainer.Add(ContentViewContainer);

            m_BadgesParent = new VisualElement { name = "badge-container" };

            this.AddStylesheet("GraphView.uss");

            PositionDependenciesManager = new PositionDependenciesManager(this, GraphTool?.Preferences);
            m_AutoAlignmentHelper = new AutoAlignmentHelper(this);
            m_AutoSpacingHelper = new AutoSpacingHelper(this);

            ContextualMenuManipulator = new ContextualMenuManipulator(BuildContextualMenu);

            Clickable = new Clickable(OnDoubleClick);
            Clickable.activators.Clear();
            Clickable.activators.Add(
                new ManipulatorActivationFilter { button = MouseButton.LeftMouse, clickCount = 2 });

            ContentDragger = new ContentDragger();
            SelectionDragger = new SelectionDragger(this);
            RectangleSelector = new RectangleSelector();
            FreehandSelector = new FreehandSelector();

            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale, 1.0f);

            RegisterCallback<AttachToPanelEvent>(OnEnterPanel);
            RegisterCallback<DetachFromPanelEvent>(OnLeavePanel);

            RegisterCallback<ValidateCommandEvent>(OnValidateCommand);
            RegisterCallback<ExecuteCommandEvent>(OnExecuteCommand);

            RegisterCallback<MouseOverEvent>(OnMouseOver);
            RegisterCallback<MouseMoveEvent>(OnMouseMove);

            // TODO: Until GraphView.SelectionDragger is used widely in VS, we must register to drag events ON TOP of
            // using the VisualScripting.Editor.SelectionDropper, just to deal with drags from the Blackboard
            RegisterCallback<DragEnterEvent>(OnDragEnter);
            RegisterCallback<DragLeaveEvent>(OnDragLeave);
            RegisterCallback<DragUpdatedEvent>(OnDragUpdated);
            RegisterCallback<DragExitedEvent>(OnDragExited);
            RegisterCallback<DragPerformEvent>(OnDragPerform);
            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);

            RegisterCallback<ShortcutFrameAllEvent>(OnShortcutFrameAllEvent);
            RegisterCallback<ShortcutFrameOriginEvent>(OnShortcutFrameOriginEvent);
            RegisterCallback<ShortcutFramePreviousEvent>(OnShortcutFramePreviousEvent);
            RegisterCallback<ShortcutFrameNextEvent>(OnShortcutFrameNextEvent);
            RegisterCallback<ShortcutDeleteEvent>(OnShortcutDeleteEvent);
            RegisterCallback<ShortcutDisplaySmartSearchEvent>(OnShortcutDisplaySmartSearchEvent);
            RegisterCallback<ShortcutConvertConstantAndVariableEvent>(OnShortcutConvertVariableAndConstantEvent);
            RegisterCallback<ShortcutAlignNodesEvent>(OnShortcutAlignNodesEvent);
            RegisterCallback<ShortcutAlignNodeHierarchiesEvent>(OnShortcutAlignNodeHierarchyEvent);
            RegisterCallback<ShortcutCreateStickyNoteEvent>(OnShortcutCreateStickyNoteEvent);
            RegisterCallback<KeyDownEvent>(OnRenameKeyDown);

            Insert(0, new GridBackground());

            PlacematContainer = new PlacematContainer(this);
            AddLayer(PlacematContainer, PlacematContainer.PlacematsLayer);

            SerializeGraphElementsCallback = OnSerializeGraphElements;
            UnserializeAndPasteCallback = UnserializeAndPaste;
        }

        internal void UnloadGraph()
        {
            GetBlackboard()?.Clear();
            ClearGraph();
        }

        internal void ClearGraph()
        {
            // Directly query the UI here - slow, but the usual path of going through GraphModel.GraphElements
            // won't work because it's often not initialized at this point
            var elements = ContentViewContainer.Query<GraphElement>().ToList();

            PositionDependenciesManager.Clear();
            foreach (var element in elements)
            {
                RemoveElement(element);
            }
        }

        /// <summary>
        /// Updates the graph view pan and zoom.
        /// </summary>
        /// <remarks>This method only updates the view pan and zoom and does not save the
        /// new values in the state. To make the change persistent, dispatch a
        /// <see cref="ReframeGraphViewCommand"/>.</remarks>
        /// <param name="pan">The new coordinate at the top left corner of the graph view.</param>
        /// <param name="zoom">The new zoom factor of the graph view.</param>
        /// <seealso cref="ReframeGraphViewCommand"/>
        public void UpdateViewTransform(Vector3 pan, Vector3 zoom)
        {
            float validateFloat = pan.x + pan.y + pan.z + zoom.x + zoom.y + zoom.z;
            if (float.IsInfinity(validateFloat) || float.IsNaN(validateFloat))
                return;

            pan.x = GraphViewStaticBridge.RoundToPixelGrid(pan.x);
            pan.y = GraphViewStaticBridge.RoundToPixelGrid(pan.y);

            ContentViewContainer.transform.position = pan;
            ContentViewContainer.transform.scale = zoom;
        }

        protected virtual BlackboardDragAndDropHandler GetBlackboardDragAndDropHandler()
        {
            return m_BlackboardDragAndDropHandler ??
                (m_BlackboardDragAndDropHandler = new BlackboardDragAndDropHandler(this));
        }

        /// <summary>
        /// Find an appropriate Drag-and-drop handler for the DragEnter event
        /// </summary>
        /// <param name="evt">current DragEnter event.</param>
        /// <returns>handler or null if nothing appropriate was found.</returns>
        protected virtual IDragAndDropHandler GetExternalDragNDropHandler(DragEnterEvent evt)
        {
            var blackboardDragAndDropHandler = GetBlackboardDragAndDropHandler();
            if (DragAndDrop.objectReferences.Length == 0 && GetSelection().OfType<IVariableDeclarationModel>().Any())
            {
                return blackboardDragAndDropHandler;
            }

            return null;
        }

        public bool GetPortCenterOverride(Port port, out Vector2 overriddenPosition)
        {
            if (PlacematContainer.GetPortCenterOverride(port, out overriddenPosition))
                return true;

            overriddenPosition = Vector3.zero;
            return false;
        }

        void AddLayer(Layer layer, int index)
        {
            m_ContainerLayers.Add(index, layer);

            int indexOfLayer = m_ContainerLayers.OrderBy(t => t.Key).Select(t => t.Value).ToList().IndexOf(layer);

            ContentViewContainer.Insert(indexOfLayer, layer);
        }

        void AddLayer(int index)
        {
            Layer layer = new Layer { pickingMode = PickingMode.Ignore };
            AddLayer(layer, index);
        }

        VisualElement GetLayer(int index)
        {
            return m_ContainerLayers[index];
        }

        internal void ChangeLayer(GraphElement element)
        {
            if (!m_ContainerLayers.ContainsKey(element.Layer))
                AddLayer(element.Layer);

            GetLayer(element.Layer).Add(element);
        }

        public void SetupZoom(float minScaleSetup, float maxScaleSetup, float maxScaleOnFrame)
        {
            SetupZoom(minScaleSetup, maxScaleSetup, maxScaleOnFrame, m_ScaleStep, m_ReferenceScale);
        }

        public void SetupZoom(float minScaleSetup, float maxScaleSetup, float maxScaleOnFrame, float scaleStepSetup, float referenceScaleSetup)
        {
            m_MinScale = minScaleSetup;
            m_MaxScale = maxScaleSetup;
            m_MaxScaleOnFrame = maxScaleOnFrame;
            m_ScaleStep = scaleStepSetup;
            m_ReferenceScale = referenceScaleSetup;
            UpdateContentZoomer();
        }

        void UpdateContentZoomer()
        {
            if (Math.Abs(m_MinScale - m_MaxScale) > float.Epsilon)
            {
                ContentZoomer = new ContentZoomer
                {
                    minScale = m_MinScale,
                    maxScale = m_MaxScale,
                    scaleStep = m_ScaleStep,
                    referenceScale = m_ReferenceScale
                };
            }
            else
            {
                ContentZoomer = null;
            }

            ValidateTransform();
        }

        void ValidateTransform()
        {
            if (ContentViewContainer == null)
                return;
            Vector3 transformScale = ViewTransform.scale;

            transformScale.x = Mathf.Clamp(transformScale.x, m_MinScale, m_MaxScale);
            transformScale.y = Mathf.Clamp(transformScale.y, m_MinScale, m_MaxScale);

            ViewTransform.scale = transformScale;
        }

        protected virtual void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (evt.menu.MenuItems().Count > 0)
                evt.menu.AppendSeparator();

            evt.menu.AppendAction("Create Node", menuAction =>
            {
                Vector2 mousePosition = menuAction?.eventInfo?.mousePosition ?? Event.current.mousePosition;
                DisplaySmartSearch(mousePosition);
            });

            evt.menu.AppendAction("Create Placemat", menuAction =>
            {
                Vector2 mousePosition = menuAction?.eventInfo?.mousePosition ?? Event.current.mousePosition;
                Vector2 graphPosition = ContentViewContainer.WorldToLocal(mousePosition);

                Dispatch(new CreatePlacematCommand(new Rect(graphPosition.x, graphPosition.y, 200, 200)));
            });

            var selection = GetSelection().ToList();
            if (selection.Any())
            {
                var nodesAndNotes = selection.
                    Where(e => e is INodeModel || e is IStickyNoteModel).
                    Select(m => m.GetUI<GraphElement>(this)).ToList();

                bool hasNodeOnGraph = nodesAndNotes.Any(t => !t.Model.NeedsContainer());

                evt.menu.AppendAction("Create Placemat Under Selection", _ =>
                {
                    Rect bounds = new Rect();
                    if (Placemat.ComputeElementBounds(ref bounds, nodesAndNotes))
                    {
                        Dispatch(new CreatePlacematCommand(bounds));
                    }
                }, hasNodeOnGraph ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

                /* Actions on selection */

                evt.menu.AppendSeparator();

                if (hasNodeOnGraph)
                {
                var itemName = ShortcutHelper.CreateShortcutMenuItemEntry("Align Elements/Align Items", GraphTool.Name, ShortcutAlignNodesEvent.id);
                    evt.menu.AppendAction(itemName, _ =>
                    {
                        Dispatch(new AlignNodesCommand(this, false, GetSelection()));
                    });

                itemName = ShortcutHelper.CreateShortcutMenuItemEntry("Align Elements/Align Hierarchy", GraphTool.Name, ShortcutAlignNodeHierarchiesEvent.id);
                    evt.menu.AppendAction(itemName, _ =>
                    {
                        Dispatch(new AlignNodesCommand(this, true, GetSelection()));
                    });
                    var selectionUI = selection.Select(m => m.GetUI<GraphElement>(this));
                    if (selectionUI.Count(elem => elem != null && !(elem.Model is IEdgeModel) && elem.visible) > 1)
                    {
                        evt.menu.AppendAction("Align Elements/Top",
                            _ => m_AutoAlignmentHelper.SendAlignCommand(AutoAlignmentHelper.AlignmentReference.Top));

                        evt.menu.AppendAction("Align Elements/Bottom",
                            _ => m_AutoAlignmentHelper.SendAlignCommand(AutoAlignmentHelper.AlignmentReference.Bottom));

                        evt.menu.AppendAction("Align Elements/Left",
                            _ => m_AutoAlignmentHelper.SendAlignCommand(AutoAlignmentHelper.AlignmentReference.Left));

                        evt.menu.AppendAction("Align Elements/Right",
                            _ => m_AutoAlignmentHelper.SendAlignCommand(AutoAlignmentHelper.AlignmentReference.Right));

                        evt.menu.AppendAction("Align Elements/Horizontal Center",
                            _ => m_AutoAlignmentHelper.SendAlignCommand(AutoAlignmentHelper.AlignmentReference
                                .HorizontalCenter));

                        evt.menu.AppendAction("Align Elements/Vertical Center",
                            _ => m_AutoAlignmentHelper.SendAlignCommand(AutoAlignmentHelper.AlignmentReference
                                .VerticalCenter));

                        evt.menu.AppendAction("Space Elements/Horizontal",
                            _ => m_AutoSpacingHelper.SendSpacingCommand(PortOrientation.Horizontal));

                        evt.menu.AppendAction("Space Elements/Vertical",
                            _ => m_AutoSpacingHelper.SendSpacingCommand(PortOrientation.Vertical));
                    }
                }

                var nodes = selection.OfType<INodeModel>().ToList();
                if (nodes.Count > 0)
                {
                    var connectedNodes = nodes
                        .Where(m => m.GetConnectedEdges().Any())
                        .ToList();

                    evt.menu.AppendAction("Disconnect Nodes", _ =>
                    {
                        Dispatch(new DisconnectNodeCommand(connectedNodes));
                    }, connectedNodes.Count == 0 ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal);

                    var ioConnectedNodes = connectedNodes
                        .OfType<IInputOutputPortsNodeModel>()
                        .Where(x => x.InputsByDisplayOrder.Any(y => y.IsConnected()) &&
                            x.OutputsByDisplayOrder.Any(y => y.IsConnected())).ToList();

                    evt.menu.AppendAction("Bypass Nodes", _ =>
                    {
                        Dispatch(new BypassNodesCommand(ioConnectedNodes, nodes));
                    }, ioConnectedNodes.Count == 0 ? DropdownMenuAction.Status.Disabled : DropdownMenuAction.Status.Normal);

                    var willDisable = nodes.Any(n => n.State == ModelState.Enabled);
                    evt.menu.AppendAction(willDisable ? "Disable Nodes" : "Enable Nodes", _ =>
                    {
                        Dispatch(new ChangeNodeStateCommand(willDisable ? ModelState.Disabled : ModelState.Enabled, nodes));
                    });
                }

                if (selection.Count == 2)
                {
                    // PF: FIXME check conditions correctly for this actions (exclude single port nodes, check if already connected).
                    if (selection.FirstOrDefault(x => x is IEdgeModel) is IEdgeModel edgeModel &&
                        selection.FirstOrDefault(x => x is IInputOutputPortsNodeModel) is IInputOutputPortsNodeModel nodeModel)
                    {
                        evt.menu.AppendAction("Insert Node on Edge", _ => Dispatch(new SplitEdgeAndInsertExistingNodeCommand(edgeModel, nodeModel)),
                            _ => DropdownMenuAction.Status.Normal);
                    }
                }

                var variableNodes = nodes.OfType<IVariableNodeModel>().ToList();
                var constants = nodes.OfType<IConstantNodeModel>().ToList();
                if (variableNodes.Count > 0)
                {
                    // TODO JOCE We might want to bring the concept of Get/Set variable from VS down to GTF
                    var itemName = ShortcutHelper.CreateShortcutMenuItemEntry("Variable/Convert", GraphTool.Name, ShortcutConvertConstantAndVariableEvent.id);
                    evt.menu.AppendAction(itemName,
                        _ => Dispatch(new ConvertConstantNodesAndVariableNodesCommand(null, variableNodes)),
                        variableNodes.Any(v => v.OutputsByDisplayOrder.Any(o => o.PortType == PortType.Data)) ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

                    evt.menu.AppendAction("Variable/Itemize",
                        _ => Dispatch(new ItemizeNodeCommand(variableNodes.OfType<ISingleOutputPortNodeModel>().ToList())),
                        variableNodes.Any(v => v.OutputsByDisplayOrder.Any(o => o.PortType == PortType.Data && o.GetConnectedPorts().Count() > 1)) ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
                }

                if (constants.Count > 0)
                {
                    var itemName = ShortcutHelper.CreateShortcutMenuItemEntry("Constant/Convert", GraphTool.Name, ShortcutConvertConstantAndVariableEvent.id);
                    evt.menu.AppendAction(itemName,
                        _ => Dispatch(new ConvertConstantNodesAndVariableNodesCommand(constants, null)), _ => DropdownMenuAction.Status.Normal);

                    evt.menu.AppendAction("Constant/Itemize",
                        _ => Dispatch(new ItemizeNodeCommand(constants.OfType<ISingleOutputPortNodeModel>().ToList())),
                        constants.Any(v => v.OutputsByDisplayOrder.Any(o => o.PortType == PortType.Data && o.GetConnectedPorts().Count() > 1)) ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

                    evt.menu.AppendAction("Constant/Lock",
                        _ => Dispatch(new LockConstantNodeCommand(constants, true)),
                        _ =>
                            constants.Any(e => !e.IsLocked) ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled
                    );

                    evt.menu.AppendAction("Constant/Unlock",
                        _ => Dispatch(new LockConstantNodeCommand(constants, false)),
                        _ =>
                            constants.Any(e => e.IsLocked) ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled
                    );
                }

                var portals = nodes.OfType<IEdgePortalModel>().ToList();
                if (portals.Count > 0)
                {
                    var canCreate = portals.Where(p => p.CanCreateOppositePortal()).ToList();
                    evt.menu.AppendAction("Create Opposite Portal",
                        _ =>
                        {
                            Dispatch(new CreateOppositePortalCommand(canCreate));
                        }, canCreate.Count > 0 ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
                }

                var colorables = selection.Where(s => s.IsColorable()).ToList();
                if (colorables.Any())
                {
                    evt.menu.AppendAction("Color/Change...", _ =>
                    {
                        void ChangeNodesColor(Color pickedColor)
                        {
                            Dispatch(new ChangeElementColorCommand(pickedColor, colorables));
                        }

                        var defaultColor = new Color(0.5f, 0.5f, 0.5f);
                        if (colorables.Count == 1)
                        {
                            defaultColor = colorables[0].Color;
                        }

                        GraphViewStaticBridge.ShowColorPicker(ChangeNodesColor, defaultColor, true);
                    });

                    evt.menu.AppendAction("Color/Reset", _ =>
                    {
                        Dispatch(new ResetElementColorCommand(colorables));
                    });
                }
                else
                {
                    evt.menu.AppendAction("Color", _ => {}, _ => DropdownMenuAction.Status.Disabled);
                }

                var edges = selection.OfType<IEdgeModel>().ToList();
                if (edges.Count > 0)
                {
                    evt.menu.AppendSeparator();

                    var edgeData = edges.Select(
                        edgeModel =>
                        {
                            var outputPort = edgeModel.FromPort.GetUI<Port>(this);
                            var inputPort = edgeModel.ToPort.GetUI<Port>(this);
                            var outputNode = edgeModel.FromPort.NodeModel.GetUI<Node>(this);
                            var inputNode = edgeModel.ToPort.NodeModel.GetUI<Node>(this);

                            if (outputNode == null || inputNode == null || outputPort == null || inputPort == null)
                                return (null, Vector2.zero, Vector2.zero);

                            return (edgeModel,
                                outputPort.ChangeCoordinatesTo(contentContainer, outputPort.layout.center),
                                inputPort.ChangeCoordinatesTo(contentContainer, inputPort.layout.center));
                        }
                        ).Where(tuple => tuple.Item1 != null).ToList();

                    evt.menu.AppendAction("Create Portals", _ =>
                    {
                        Dispatch(new ConvertEdgesToPortalsCommand(edgeData));
                    });
                }

                var stickyNotes = selection.OfType<IStickyNoteModel>().ToList();

                if (stickyNotes.Count > 0)
                {
                    evt.menu.AppendSeparator();

                    DropdownMenuAction.Status GetThemeStatus(DropdownMenuAction a)
                    {
                        if (stickyNotes.Any(noteModel => noteModel.Theme != stickyNotes.First().Theme))
                        {
                            // Values are not all the same.
                            return DropdownMenuAction.Status.Normal;
                        }

                        return stickyNotes.First().Theme == (a.userData as string) ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal;
                    }

                    DropdownMenuAction.Status GetSizeStatus(DropdownMenuAction a)
                    {
                        if (stickyNotes.Any(noteModel => noteModel.TextSize != stickyNotes.First().TextSize))
                        {
                            // Values are not all the same.
                            return DropdownMenuAction.Status.Normal;
                        }

                        return stickyNotes.First().TextSize == (a.userData as string) ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal;
                    }

                    foreach (var value in StickyNote.GetThemes())
                    {
                        evt.menu.AppendAction("Sticky Note Theme/" + value,
                            menuAction => Dispatch(new UpdateStickyNoteThemeCommand(menuAction.userData as string, stickyNotes)),
                            GetThemeStatus, value);
                    }

                    foreach (var value in StickyNote.GetSizes())
                    {
                        evt.menu.AppendAction("Sticky Note Text Size/" + value,
                            menuAction => Dispatch(new UpdateStickyNoteTextSizeCommand(menuAction.userData as string, stickyNotes)),
                            GetSizeStatus, value);
                    }
                }
            }

            evt.menu.AppendSeparator();

            evt.menu.AppendAction("Cut", _ => { CutSelectionCallback(); },
                CanCutSelection ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            evt.menu.AppendAction("Copy", _ => { CopySelectionCallback(); },
                CanCopySelection ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            evt.menu.AppendAction("Paste", _ => { PasteCallback(); },
                CanPaste ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            evt.menu.AppendSeparator();

            evt.menu.AppendAction("Duplicate", _ => { DuplicateSelectionCallback(); },
                CanDuplicateSelection ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            evt.menu.AppendAction("Delete", _ =>
            {
                Dispatch(new DeleteElementsCommand(selection.ToList()));
            }, CanDeleteSelection ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);

            if (Unsupported.IsDeveloperBuild())
            {
                evt.menu.AppendSeparator();
                evt.menu.AppendAction("Internal/Refresh All UI", _ =>
                {
                    using (var updater = GraphViewState.UpdateScope)
                    {
                        updater.ForceCompleteUpdate();
                    }
                });

                if (selection.Any())
                {
                    evt.menu.AppendAction("Internal/Refresh Selected Element(s)",
                        _ =>
                        {
                            using (var graphUpdater = GraphViewState.UpdateScope)
                            {
                                graphUpdater.MarkChanged(selection);
                            }
                        });
                }
            }
        }

        public virtual void DisplaySmartSearch(Vector2 mousePosition)
        {
            var graphPosition = ContentViewContainer.WorldToLocal(mousePosition);
            var element = panel.Pick(mousePosition).GetFirstOfType<IModelUI>();
            var stencil = (Stencil)GraphModel.Stencil;

            VisualElement current = element as VisualElement;
            while (current != null && current != this)
            {
                if (current is IDisplaySmartSearchUI dssUI)
                    if (dssUI.DisplaySmartSearch(mousePosition))
                        return;

                current = current.parent;
            }

            SearcherService.ShowGraphNodes(stencil, GraphTool?.Name, GraphTool?.Preferences, GraphModel, mousePosition, item =>
            {
                Dispatch(CreateNodeCommand.OnGraph(item, graphPosition));
            });
        }

        void RegisterObservers()
        {
            if (GraphTool == null)
                return;

            if (m_GraphViewGraphLoadedAssetObserver == null)
                m_GraphViewGraphLoadedAssetObserver = new GraphViewStateComponent.GraphAssetLoadedObserver(GraphTool.ToolState, GraphViewState);

            if (m_SelectionGraphLoadedAssetObserver == null)
                m_SelectionGraphLoadedAssetObserver = new SelectionStateComponent.GraphAssetLoadedObserver(GraphTool.ToolState, SelectionState);

            if (m_UpdateObserver == null)
                m_UpdateObserver = new UpdateObserver(this, GraphTool.GraphProcessingState);

            if (m_EdgeOrderObserver == null)
                m_EdgeOrderObserver = new EdgeOrderObserver(SelectionState, GraphViewState);

            GraphTool.ObserverManager.RegisterObserver(m_GraphViewGraphLoadedAssetObserver);
            GraphTool.ObserverManager.RegisterObserver(m_SelectionGraphLoadedAssetObserver);
            GraphTool.ObserverManager.RegisterObserver(m_UpdateObserver);
            GraphTool.ObserverManager.RegisterObserver(m_EdgeOrderObserver);
        }

        void UnregisterObservers()
        {
            if (GraphTool == null)
                return;

            GraphTool.ObserverManager.UnregisterObserver(m_GraphViewGraphLoadedAssetObserver);
            GraphTool.ObserverManager.UnregisterObserver(m_SelectionGraphLoadedAssetObserver);
            GraphTool.ObserverManager.UnregisterObserver(m_UpdateObserver);
            GraphTool.ObserverManager.UnregisterObserver(m_EdgeOrderObserver);
        }

        protected void OnEnterPanel(AttachToPanelEvent e)
        {
            this.SetUpRender(OnUpdateMaterial, OnBeforeUpdate);
            RegisterObservers();
        }

        protected void OnLeavePanel(DetachFromPanelEvent e)
        {
            UnregisterObservers();
            this.TearDownRender(OnUpdateMaterial, OnBeforeUpdate);
        }

        void OnBeforeUpdate(IPanel p)
        {
            Redrawn?.Invoke();
        }

        void OnUpdateMaterial(Material mat)
        {
            // Set global graph view shader properties (used by UIR)
            mat.SetFloat(GraphViewStaticBridge.s_EditorPixelsPerPointId, EditorGUIUtility.pixelsPerPoint);
            mat.SetFloat(GraphViewStaticBridge.s_GraphViewScaleId, ViewTransform.scale.x);
        }

        protected void OnValidateCommand(ValidateCommandEvent evt)
        {
            if (panel.GetCapturingElement(PointerId.mousePointerId) != null)
                return;

            if ((evt.commandName == GraphViewStaticBridge.EventCommandNames.Copy && CanCopySelection)
                || (evt.commandName == GraphViewStaticBridge.EventCommandNames.Paste && CanPaste)
                || (evt.commandName == GraphViewStaticBridge.EventCommandNames.Duplicate && CanDuplicateSelection)
                || (evt.commandName == GraphViewStaticBridge.EventCommandNames.Cut && CanCutSelection)
                || ((evt.commandName == GraphViewStaticBridge.EventCommandNames.Delete || evt.commandName == GraphViewStaticBridge.EventCommandNames.SoftDelete) && CanDeleteSelection))
            {
                evt.StopPropagation();
                evt.imguiEvent?.Use();
            }
            else if (evt.commandName == GraphViewStaticBridge.EventCommandNames.FrameSelected)
            {
                evt.StopPropagation();
                evt.imguiEvent?.Use();
            }
        }

        protected void OnExecuteCommand(ExecuteCommandEvent evt)
        {
            if (panel.GetCapturingElement(PointerId.mousePointerId) != null)
                return;

            if (evt.commandName == GraphViewStaticBridge.EventCommandNames.Copy)
            {
                CopySelectionCallback();
                evt.StopPropagation();
            }
            else if (evt.commandName == GraphViewStaticBridge.EventCommandNames.Paste)
            {
                PasteCallback();
                evt.StopPropagation();
            }
            else if (evt.commandName == GraphViewStaticBridge.EventCommandNames.Duplicate)
            {
                DuplicateSelectionCallback();
                evt.StopPropagation();
            }
            else if (evt.commandName == GraphViewStaticBridge.EventCommandNames.Cut)
            {
                CutSelectionCallback();
                evt.StopPropagation();
            }
            else if (evt.commandName == GraphViewStaticBridge.EventCommandNames.Delete)
            {
                this.DispatchDeleteSelectionCommand();
                evt.StopPropagation();
            }
            else if (evt.commandName == GraphViewStaticBridge.EventCommandNames.SoftDelete)
            {
                this.DispatchDeleteSelectionCommand();
                evt.StopPropagation();
            }
            else if (evt.commandName == GraphViewStaticBridge.EventCommandNames.FrameSelected)
            {
                this.DispatchFrameSelectionCommand();
                evt.StopPropagation();
            }

            if (evt.isPropagationStopped)
            {
                evt.imguiEvent?.Use();
            }
        }

        static void CollectElements(IEnumerable<IGraphElementModel> elements, HashSet<IGraphElementModel> collectedElementSet, Func<IGraphElementModel, bool> conditionFunc)
        {
            foreach (var element in elements.Where(e => e != null && !collectedElementSet.Contains(e) && conditionFunc(e)))
            {
                collectedElementSet.Add(element);
            }
        }

        protected virtual void CollectCopyableGraphElements(IEnumerable<IGraphElementModel> elements, HashSet<IGraphElementModel> elementsToCopySet)
        {
            var elementList = elements.ToList();
            CollectElements(elementList, elementsToCopySet, e => e.IsCopiable());

            // Also collect hovering list of nodes
            foreach (var placemat in elementList.OfType<IPlacematModel>())
            {
                var placematUI = placemat.GetUI<Placemat>(this);
                placematUI?.ActOnGraphElementsOver(
                    el =>
                    {
                        CollectElements(new[] { el.Model },
                            elementsToCopySet,
                            e => e.IsCopiable());
                        return false;
                    },
                    true);
            }
        }

        protected void CopySelectionCallback()
        {
            var elementsToCopySet = new HashSet<IGraphElementModel>();

            CollectCopyableGraphElements(GetSelection(), elementsToCopySet);

            string data = SerializeGraphElements(elementsToCopySet);

            if (!string.IsNullOrEmpty(data))
            {
                Clipboard = data;
            }
        }

        protected void CutSelectionCallback()
        {
            CopySelectionCallback();
            this.DispatchDeleteSelectionCommand("Cut");
        }

        protected void PasteCallback()
        {
            UnserializeAndPasteOperation(PasteOperation.Paste, "Paste", Clipboard);
        }

        protected void DuplicateSelectionCallback()
        {
            var elementsToCopySet = new HashSet<IGraphElementModel>();

            CollectCopyableGraphElements(GetSelection(), elementsToCopySet);

            string serializedData = SerializeGraphElements(elementsToCopySet);

            UnserializeAndPasteOperation(PasteOperation.Duplicate, "Duplicate", serializedData);
        }

        protected string SerializeGraphElements(IEnumerable<IGraphElementModel> elements)
        {
            if (SerializeGraphElementsCallback != null)
            {
                string data = SerializeGraphElementsCallback(elements);
                if (!string.IsNullOrEmpty(data))
                {
                    data = k_SerializedDataMimeType + " " + data;
                }
                return data;
            }
            else
            {
                return string.Empty;
            }
        }

        protected bool CanPasteSerializedData(string data)
        {
            if (CanPasteSerializedDataCallback != null)
            {
                if (data.StartsWith(k_SerializedDataMimeType))
                {
                    return CanPasteSerializedDataCallback(data.Substring(k_SerializedDataMimeType.Length + 1));
                }
                else
                {
                    return CanPasteSerializedDataCallback(data);
                }
            }
            if (data.StartsWith(k_SerializedDataMimeType))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// Paste the content of data into the graph.
        /// </summary>
        /// <param name="operation">The kind of operation.</param>
        /// <param name="operationName">The name of the operation.</param>
        /// <param name="data">The serialized data.</param>
        protected void UnserializeAndPasteOperation(PasteOperation operation, string operationName, string data)
        {
            if (UnserializeAndPasteCallback != null)
            {
                if (data.StartsWith(k_SerializedDataMimeType))
                {
                    UnserializeAndPasteCallback(operation, operationName, data.Substring(k_SerializedDataMimeType.Length + 1));
                }
                else
                {
                    UnserializeAndPasteCallback(operation, operationName, data);
                }
            }
        }

        public virtual void AddElement(GraphElement graphElement)
        {
            if (graphElement is Badge)
            {
                m_BadgesParent.Add(graphElement);
            }
            else if (graphElement is Placemat placemat)
            {
                PlacematContainer.Add(placemat);
            }
            else
            {
                int newLayer = graphElement.Layer;
                if (!m_ContainerLayers.ContainsKey(newLayer))
                {
                    AddLayer(newLayer);
                }

                GetLayer(newLayer).Add(graphElement);
            }

            try
            {
                graphElement.AddToView(this);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

            if (graphElement is Node || graphElement is Edge)
                graphElement.RegisterCallback<MouseOverEvent>(OnMouseOver);

            if (graphElement.Model is IEdgePortalModel portalModel)
            {
                AddPortalDependency(portalModel);
            }
        }

        public virtual void RemoveElement(GraphElement graphElement)
        {
            if (graphElement == null)
                return;

            var graphElementModel = graphElement.Model;
            switch (graphElementModel)
            {
                case IEdgeModel e:
                    RemovePositionDependency(e);
                    break;
                case IEdgePortalModel portalModel:
                    RemovePortalDependency(portalModel);
                    break;
            }

            if (graphElement is Node || graphElement is Edge)
                graphElement.UnregisterCallback<MouseOverEvent>(OnMouseOver);

            graphElement.RemoveFromHierarchy();
            graphElement.RemoveFromView();
        }

        static readonly List<ModelUI> k_CalculateRectToFitAllAllUIs = new List<ModelUI>();
        public Rect CalculateRectToFitAll(VisualElement container)
        {
            Rect rectToFit = container.layout;
            bool reachedFirstChild = false;

            GraphModel.GraphElementModels.GetAllUIsInList(this, null, k_CalculateRectToFitAllAllUIs);
            foreach(var ge in k_CalculateRectToFitAllAllUIs)
            {
                if (ge is null || ge.Model is IEdgeModel)
                    continue;

                if (!reachedFirstChild)
                {
                    rectToFit = ge.ChangeCoordinatesTo(ContentViewContainer, ge.GetRect());
                    reachedFirstChild = true;
                }
                else
                {
                    rectToFit = RectUtils.Encompass(rectToFit, ge.ChangeCoordinatesTo(ContentViewContainer, ge.GetRect()));
                }
            }

            k_CalculateRectToFitAllAllUIs.Clear();

            return rectToFit;
        }

        public void CalculateFrameTransform(Rect rectToFit, Rect clientRect, int border, out Vector3 frameTranslation, out Vector3 frameScaling)
        {
            // bring slightly smaller screen rect into GUI space
            var screenRect = new Rect
            {
                xMin = border,
                xMax = clientRect.width - border,
                yMin = border,
                yMax = clientRect.height - border
            };

            Matrix4x4 m = GUI.matrix;
            GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one);
            Rect identity = GUIUtility.ScreenToGUIRect(screenRect);

            // measure zoom level necessary to fit the canvas rect into the screen rect
            float zoomLevel = Math.Min(identity.width / rectToFit.width, identity.height / rectToFit.height);

            // clamp
            zoomLevel = Mathf.Clamp(zoomLevel, m_MinScale, Math.Min(m_MaxScale, m_MaxScaleOnFrame));

            var trs = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(zoomLevel, zoomLevel, 1.0f));

            var edge = new Vector2(clientRect.width, clientRect.height);
            var origin = new Vector2(0, 0);

            var r = new Rect
            {
                min = origin,
                max = edge
            };

            var parentScale = new Vector3(trs.GetColumn(0).magnitude,
                trs.GetColumn(1).magnitude,
                trs.GetColumn(2).magnitude);
            Vector2 offset = r.center - (rectToFit.center * parentScale.x);

            // Update output values before leaving
            frameTranslation = new Vector3(offset.x, offset.y, 0.0f);
            frameScaling = parentScale;

            GUI.matrix = m;
        }

        protected void AddPositionDependency(IEdgeModel model)
        {
            PositionDependenciesManager.AddPositionDependency(model);
        }

        protected void RemovePositionDependency(IEdgeModel edgeModel)
        {
            PositionDependenciesManager.Remove(edgeModel.FromNodeGuid, edgeModel.ToNodeGuid);
            PositionDependenciesManager.LogDependencies();
        }

        protected void AddPortalDependency(IEdgePortalModel model)
        {
            PositionDependenciesManager.AddPortalDependency(model);
        }

        protected void RemovePortalDependency(IEdgePortalModel model)
        {
            PositionDependenciesManager.RemovePortalDependency(model);
            PositionDependenciesManager.LogDependencies();
        }

        public virtual void StopSelectionDragger()
        {
            // cancellation is handled in the MoveMove callback
            m_SelectionDraggerWasActive = false;
        }

        /// <summary>
        /// Callback for the ShortcutFrameAllEvent.
        /// </summary>
        /// <param name="e">The event.</param>
        protected void OnShortcutFrameAllEvent(ShortcutFrameAllEvent e)
        {
            if (panel.GetCapturingElement(PointerId.mousePointerId) == null)
            {
                this.DispatchFrameAllCommand();
                e.StopPropagation();
            }
        }

        /// <summary>
        /// Callback for the ShortcutFrameOriginEvent.
        /// </summary>
        /// <param name="e">The event.</param>
        protected void OnShortcutFrameOriginEvent(ShortcutFrameOriginEvent e)
        {
            if (panel.GetCapturingElement(PointerId.mousePointerId) == null)
            {
                Vector3 frameTranslation = Vector3.zero;
                Vector3 frameScaling = Vector3.one;
                Dispatch(new ReframeGraphViewCommand(frameTranslation, frameScaling));
                e.StopPropagation();
            }
        }

        /// <summary>
        /// Callback for the ShortcutFramePreviousEvent.
        /// </summary>
        /// <param name="e">The event.</param>
        protected void OnShortcutFramePreviousEvent(ShortcutFramePreviousEvent e)
        {
            if (panel.GetCapturingElement(PointerId.mousePointerId) == null)
            {
                this.DispatchFramePrevCommand(_ => true);
                e.StopPropagation();
            }
        }

        /// <summary>
        /// Callback for the ShortcutFrameNextEvent.
        /// </summary>
        /// <param name="e">The event.</param>
        protected void OnShortcutFrameNextEvent(ShortcutFrameNextEvent e)
        {
            if (panel.GetCapturingElement(PointerId.mousePointerId) == null)
            {
                this.DispatchFrameNextCommand(_ => true);
                e.StopPropagation();
            }
        }

        /// <summary>
        /// Callback for the ShortcutDeleteEvent.
        /// </summary>
        /// <param name="e">The event.</param>
        protected void OnShortcutDeleteEvent(ShortcutDeleteEvent e)
        {
            var selectedNodes = GetSelection().OfType<INodeModel>().ToList();

            if (selectedNodes.Count == 0)
                return;

            var connectedNodes = selectedNodes
                .OfType<IInputOutputPortsNodeModel>()
                .Where(x => x.InputsById.Values
                    .Any(y => y.IsConnected()) && x.OutputsById.Values.Any(y => y.IsConnected()))
                .ToList();

            var canSelectionBeBypassed = connectedNodes.Any();
            if (canSelectionBeBypassed)
                Dispatch(new BypassNodesCommand(connectedNodes, selectedNodes));
            else
                Dispatch(new DeleteElementsCommand(selectedNodes));

            e.StopPropagation();
        }

        /// <summary>
        /// Callback for the ShortcutDisplaySmartSearchEvent.
        /// </summary>
        /// <param name="e">The event.</param>
        protected void OnShortcutDisplaySmartSearchEvent(ShortcutDisplaySmartSearchEvent e)
        {
            DisplaySmartSearch(e.MousePosition);
            e.StopPropagation();
        }

        /// <summary>
        /// Callback for the ShortcutConvertConstantToVariableEvent.
        /// </summary>
        /// <param name="e">The event.</param>
        protected void OnShortcutConvertVariableAndConstantEvent(ShortcutConvertConstantAndVariableEvent e)
        {
            var constantModels = GetSelection().OfType<IConstantNodeModel>().ToList();
            var variableModels = GetSelection().OfType<IVariableNodeModel>().ToList();

            if (constantModels.Any() || variableModels.Any())
            {
                Dispatch(new ConvertConstantNodesAndVariableNodesCommand(constantModels, variableModels));
                e.StopPropagation();
            }
        }

        /// <summary>
        /// Callback for the ShortcutAlignNodesEvent.
        /// </summary>
        /// <param name="e">The event.</param>
        protected void OnShortcutAlignNodesEvent(ShortcutAlignNodesEvent e)
        {
            Dispatch(new AlignNodesCommand(this, false, GetSelection()));
            e.StopPropagation();
        }

        /// <summary>
        /// Callback for the ShortcutAlignNodeHierarchyEvent.
        /// </summary>
        /// <param name="e">The event.</param>
        protected void OnShortcutAlignNodeHierarchyEvent(ShortcutAlignNodeHierarchiesEvent e)
        {
            Dispatch(new AlignNodesCommand(this, true, GetSelection()));
            e.StopPropagation();
        }

        /// <summary>
        /// Callback for the ShortcutCreateStickyNoteEvent.
        /// </summary>
        /// <param name="e">The event.</param>
        protected void OnShortcutCreateStickyNoteEvent(ShortcutCreateStickyNoteEvent e)
        {
            var atPosition = new Rect(this.ChangeCoordinatesTo(ContentViewContainer, this.WorldToLocal(e.MousePosition)), StickyNote.defaultSize);
            Dispatch(new CreateStickyNoteCommand(atPosition));
            e.StopPropagation();
        }

        /// <summary>
        /// Callback for the KeyDownEvent to handle renames.
        /// </summary>
        /// <param name="e">The event.</param>
        protected void OnRenameKeyDown(KeyDownEvent e)
        {
            if (GraphElement.IsRenameKey(e))
            {
                if (e.target == this)
                {
                    // Forward event to the last selected element.
                    var renamableSelection = GetSelection().Where(x => x.IsRenamable());
                    var lastSelectedItem = renamableSelection.LastOrDefault();
                    var lastSelectedItemUI = lastSelectedItem?.GetUI<GraphElement>(this);

                    lastSelectedItemUI?.OnRenameKeyDown(e);
                }
            }
        }

        protected void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            if (Window != null)
            {
                // Set the window min size from the graphView
                Window.AdjustWindowMinSize(new Vector2(resolvedStyle.minWidth.value, resolvedStyle.minHeight.value));
            }
        }

        protected void OnMouseOver(MouseOverEvent evt)
        {
            // Disregard the event if we're moving the mouse over the SmartSearch window.
            if (Children().Any(x =>
            {
                var fullName = x.GetType().FullName;
                return fullName?.Contains("SmartSearch") == true;
            }))
            {
                return;
            }

            evt.StopPropagation();
        }

        protected void OnDoubleClick()
        {
            // Display graph in inspector when clicking on background
            // TODO: displayed on double click ATM as this method overrides the Token.Select() which does not stop propagation
            Selection.activeObject = GraphViewState?.AssetModel as Object;
        }

        protected void OnMouseMove(MouseMoveEvent evt)
        {
            if (m_SelectionDraggerWasActive && !SelectionDragger.IsActive) // cancelled
            {
                m_SelectionDraggerWasActive = false;
                PositionDependenciesManager.CancelMove();
            }
            else if (!m_SelectionDraggerWasActive && SelectionDragger.IsActive) // started
            {
                m_SelectionDraggerWasActive = true;

                var elemModel = GetSelection().OfType<INodeModel>().FirstOrDefault();
                var elem = elemModel?.GetUI<GraphElement>(this);
                if (elem == null)
                    return;

                Vector2 elemPos = elemModel.Position;
                Vector2 startPos = ContentViewContainer.ChangeCoordinatesTo(elem.hierarchy.parent, elemPos);

                bool requireShiftToMoveDependencies = !(((Stencil)elemModel.GraphModel?.Stencil)?.MoveNodeDependenciesByDefault).GetValueOrDefault();
                bool hasShift = evt.modifiers.HasFlag(EventModifiers.Shift);
                bool moveNodeDependencies = requireShiftToMoveDependencies == hasShift;

                if (moveNodeDependencies)
                    PositionDependenciesManager.StartNotifyMove(GetSelection(), startPos);

                // schedule execute because the mouse won't be moving when the graph view is panning
                schedule.Execute(() =>
                {
                    if (SelectionDragger.IsActive && moveNodeDependencies) // processed
                    {
                        Vector2 pos = ContentViewContainer.ChangeCoordinatesTo(elem.hierarchy.parent, elem.GetPosition().position);
                        PositionDependenciesManager.ProcessMovedNodes(pos);
                    }
                }).Until(() => !m_SelectionDraggerWasActive);
            }

            m_LastMousePosition = this.ChangeCoordinatesTo(ContentViewContainer, evt.localMousePosition);
        }

        public virtual void OnDragEnter(DragEnterEvent evt)
        {
            var e = GetExternalDragNDropHandler(evt);
            if (e != null)
            {
                m_CurrentDragAndDropHandler = e;
                e.OnDragEnter(evt);
            }
        }

        public virtual void OnDragLeave(DragLeaveEvent evt)
        {
            m_CurrentDragAndDropHandler?.OnDragLeave(evt);
            m_CurrentDragAndDropHandler = null;
        }

        public virtual void OnDragUpdated(DragUpdatedEvent e)
        {
            m_CurrentDragAndDropHandler?.OnDragUpdated(e);
            e.StopPropagation();
        }

        public virtual void OnDragPerform(DragPerformEvent e)
        {
            m_CurrentDragAndDropHandler?.OnDragPerform(e);
            m_CurrentDragAndDropHandler = null;
            e.StopPropagation();
        }

        public virtual void OnDragExited(DragExitedEvent e)
        {
            m_CurrentDragAndDropHandler?.OnDragExited(e);
            m_CurrentDragAndDropHandler = null;
        }

        protected static string OnSerializeGraphElements(IEnumerable<IGraphElementModel> elements)
        {
            CopyPasteData.s_LastCopiedData = CopyPasteData.GatherCopiedElementsData(elements.ToList());
            return CopyPasteData.s_LastCopiedData.IsEmpty() ? string.Empty : "data";
        }

        void UnserializeAndPaste(PasteOperation operation, string operationName, string data)
        {
            if (CopyPasteData.s_LastCopiedData == null || CopyPasteData.s_LastCopiedData.IsEmpty())//string.IsNullOrEmpty(data))
                return;

            var delta = m_LastMousePosition - CopyPasteData.s_LastCopiedData.topLeftNodePosition;

            var selection = SelectionState.GetSelection(GraphModel);

            foreach (var selected in selection)
            {
                var ui = selected.GetUI(this);
                if (ui != null && ui.PasteIn(operation, operationName, delta, CopyPasteData.s_LastCopiedData))
                    return;
            }

            Dispatch(new PasteSerializedDataCommand(operationName, delta, CopyPasteData.s_LastCopiedData));
        }

        static readonly List<ModelUI> k_UpdateAllUIs = new List<ModelUI>();

        /// <summary>
        /// Updates the graph view to reflect the changes in the state components.
        /// </summary>
        /// <param name="observer">The observer requesting the update.</param>
        /// <param name="graphProcessingState">The graph processing state.</param>
        protected virtual void Update(IStateObserver observer, GraphProcessingStateComponent graphProcessingState)
        {
            using (var gvObservation = observer.ObserveState(GraphViewState))
            using (var selObservation = observer.ObserveState(SelectionState))
            {
                var rebuildType = gvObservation.UpdateType.Combine(selObservation.UpdateType);

                if (rebuildType == UpdateType.Complete)
                {
                    // Sad. We lose the focused element.
                    Focus();

                    RebuildAll();
                }
                else if (rebuildType == UpdateType.Partial)
                {
                    var focusedElement = panel.focusController.focusedElement as VisualElement;
                    while (focusedElement != null && !(focusedElement is IModelUI))
                    {
                        focusedElement = focusedElement.parent;
                    }

                    var focusedModelUI = focusedElement as IModelUI;

                    UpdateViewTransform(
                        GraphViewState.Position,
                        GraphViewState.Scale);

                    var gvChangeSet = GraphViewState.GetAggregatedChangeset(gvObservation.LastObservedVersion);
                    var selChangeSet = SelectionState.GetAggregatedChangeset(selObservation.LastObservedVersion);

                    if (GraphTool.Preferences.GetBool(BoolPref.LogUIUpdate))
                    {
                        Debug.Log($"Partial GraphView Update {gvChangeSet?.NewModels.Count ?? 0} new {gvChangeSet?.ChangedModels.Count ?? 0} changed {gvChangeSet?.DeletedModels.Count ?? 0} deleted");
                    }

                    var changedModels = new HashSet<IGraphElementModel>();
                    var shouldUpdatePlacematContainer = false;
                    var placemats = new List<GraphElement>();
                    if (gvChangeSet != null)
                    {
                        foreach (var model in gvChangeSet.DeletedModels)
                        {
                            if (ReferenceEquals(model, focusedModelUI?.Model))
                            {
                                // Focused element will be deleted. Switch the focus to the graph view to avoid dangling focus.
                                Focus();
                            }

                            model.GetAllUIs(this, null, k_UpdateAllUIs);
                            foreach (var ui in k_UpdateAllUIs.OfType<GraphElement>())
                            {
                                RemoveElement(ui);
                            }
                            k_UpdateAllUIs.Clear();

                            // ToList is needed to bake the dependencies.
                            foreach (var ui in model.GetDependencies().ToList())
                            {
                                ui.UpdateFromModel();
                            }
                        }

                        foreach (var model in gvChangeSet.NewModels.Where(m => !(m is IEdgeModel) && !(m is IPlacematModel) && !(m is IBadgeModel) && !(m is IDeclarationModel)))
                        {
                            if (model.Container != GraphModel)
                                continue;
                            var ui = GraphElementFactory.CreateUI<GraphElement>(this, model);
                            if (ui != null)
                                AddElement(ui);
                        }

                        //Update new and deleted node containers
                        foreach (var model in gvChangeSet.NewModels.Concat(gvChangeSet.DeletedModels))
                        {
                            if (model.Container is IGraphElementModel container)
                                changedModels.Add(container);
                        }

                        foreach (var model in gvChangeSet.NewModels.OfType<IEdgeModel>())
                        {
                            CreateEdgeUI(model);
                        }

                        foreach (var model in gvChangeSet.NewModels.OfType<IPlacematModel>())
                        {
                            var placemat = GraphElementFactory.CreateUI<GraphElement>(this, model);
                            if (placemat != null)
                            {
                                shouldUpdatePlacematContainer = true;
                                placemats.Add(placemat);
                                AddElement(placemat);
                            }
                        }

                        foreach (var model in gvChangeSet.NewModels.OfType<IBadgeModel>())
                        {
                            if (model.ParentModel == null)
                                return;

                            var badge = GraphElementFactory.CreateUI<Badge>(this, model);
                            if (badge != null)
                            {
                                AddElement(badge);
                            }
                        }
                    }

                    if (gvChangeSet != null && selChangeSet != null)
                    {
                        var combinedSet = new HashSet<IGraphElementModel>(gvChangeSet.ChangedModels);
                        combinedSet.AddRangeInternal(selChangeSet.ChangedModels.Except(gvChangeSet.DeletedModels));
                        changedModels.AddRangeInternal(combinedSet);
                    }
                    else if (gvChangeSet != null)
                    {
                        changedModels.AddRangeInternal(gvChangeSet.ChangedModels);
                    }
                    else if (selChangeSet != null)
                    {
                        changedModels.AddRangeInternal(selChangeSet.ChangedModels);
                    }

                    if (selChangeSet != null)
                    {
                        foreach (var changedModel in selChangeSet.ChangedModels)
                        {
                            IDeclarationModel declarationModel = null;
                            if (changedModel is IHasDeclarationModel hasDeclarationModel)
                            {
                                declarationModel = hasDeclarationModel.DeclarationModel;
                            }
                            else if (changedModel is IVariableDeclarationModel variableDeclarationModel)
                            {
                                declarationModel = variableDeclarationModel;
                            }

                            if (declarationModel != null)
                            {
                                foreach (var reference in GraphModel.FindReferencesInGraph(declarationModel))
                                {
                                    changedModels.Add(reference);
                                }
                            }
                        }
                    }

                    foreach (var model in changedModels)
                    {
                        model.GetAllUIs(this, null, k_UpdateAllUIs);
                        foreach (var ui in k_UpdateAllUIs)
                        {
                            ui.UpdateFromModel();

                            if (ui?.parent == PlacematContainer)
                                shouldUpdatePlacematContainer = true;
                        }
                        k_UpdateAllUIs.Clear();

                        // ToList is needed to bake the dependencies.
                        foreach (var ui in model.GetDependencies().ToList())
                        {
                            ui.UpdateFromModel();
                        }
                    }

                    if (shouldUpdatePlacematContainer)
                        PlacematContainer?.UpdateElementsOrder();

                    foreach (var placemat in placemats)
                    {
                        placemat.UpdateFromModel();
                    }

                    // PF FIXME: node state (enable/disabled, used/unused) should be part of the State.
                    if (GraphTool.Preferences.GetBool(BoolPref.ShowUnusedNodes))
                        PositionDependenciesManager.UpdateNodeState();

                    // PF FIXME use observer or otherwise refactor this
                    if (gvChangeSet != null && gvChangeSet.ModelsToAutoAlign.Any())
                    {
                        // Auto placement relies on UI layout to compute node positions, so we need to
                        // schedule it to execute after the next layout pass.
                        // Furthermore, it will modify the model position, hence it must be
                        // done inside a Store.BeginStateChange block.
                        var elementsToAlign = gvChangeSet.ModelsToAutoAlign.ToList();
                        schedule.Execute(() =>
                        {
                            using (var graphUpdater = GraphViewState.UpdateScope)
                            {
                                PositionDependenciesManager.AlignNodes(true, elementsToAlign, graphUpdater);
                            }
                        });
                    }

                    var lastSelectedNode = GetSelection().OfType<INodeModel>().LastOrDefault();
                    if (lastSelectedNode != null && lastSelectedNode.IsAscendable())
                    {
                        var nodeUI = lastSelectedNode.GetUI<GraphElement>(this);

                        nodeUI?.BringToFront();
                    }
                }
            }

            // Update processing error badges.
            using (var procErrObservation = observer.ObserveState(graphProcessingState))
            {
                if (procErrObservation.UpdateType != UpdateType.None)
                {
                    ConsoleWindowBridge.RemoveLogEntries();
                    var graphAsset = GraphViewState.AssetModel;

                    foreach (var rawError in graphProcessingState.RawResults?.Errors ?? Enumerable.Empty<GraphProcessingError>())
                    {
                        if (graphAsset is Object asset)
                        {
                            var graphAssetPath = asset ? AssetDatabase.GetAssetPath(asset) : "<unknown>";
                            ConsoleWindowBridge.LogSticky(
                                $"{graphAssetPath}: {rawError.Description}",
                                $"{graphAssetPath}@{rawError.SourceNodeGuid}",
                                rawError.IsWarning ? LogType.Warning : LogType.Error,
                                LogOption.None,
                                asset.GetInstanceID());
                        }
                    }

                    var badgesToRemove = m_BadgesParent.Children().OfType<Badge>().Where(b => b.Model is IGraphProcessingErrorModel).ToList();

                    foreach (var badge in badgesToRemove)
                    {
                        RemoveElement(badge);

                        // ToList is needed to bake the dependencies.
                        foreach (var ui in badge.Model.GetDependencies().ToList())
                        {
                            ui.UpdateFromModel();
                        }
                    }

                    foreach (var model in graphProcessingState.Errors ?? Enumerable.Empty<IGraphProcessingErrorModel>())
                    {
                        if (model.ParentModel == null)
                            return;

                        var badge = GraphElementFactory.CreateUI<Badge>(this, model);
                        if (badge != null)
                        {
                            AddElement(badge);
                        }
                    }
                }
            }
        }

        void RebuildAll()
        {
            if (GraphTool.Preferences.GetBool(BoolPref.LogUIUpdate))
            {
                Debug.Log($"Complete GraphView Update");
            }

            if (GraphTool.Preferences.GetBool(BoolPref.WarnOnUIFullRebuild))
            {
                Debug.LogWarning($"Rebuilding all GraphView UI ({(Dispatcher as CommandDispatcher)?.LastDispatchedCommandName ?? "Unknown command"})");
            }

            ClearGraph();

            var graphModel = GraphViewState.GraphModel;
            if (graphModel == null)
                return;

            foreach (var nodeModel in graphModel.NodeModels)
            {
                var node = GraphElementFactory.CreateUI<GraphElement>(this, nodeModel);
                if (node != null)
                    AddElement(node);
            }

            foreach (var stickyNoteModel in graphModel.StickyNoteModels)
            {
                var stickyNote = GraphElementFactory.CreateUI<GraphElement>(this, stickyNoteModel);
                if (stickyNote != null)
                    AddElement(stickyNote);
            }

            int index = 0;
            foreach (var edge in graphModel.EdgeModels)
            {
                if (!CreateEdgeUI(edge))
                {
                    Debug.LogWarning($"Edge {index} cannot be restored: {edge}");
                }
                index++;
            }

            var placemats = new List<GraphElement>();
            foreach (var placematModel in GraphViewState.GraphModel.PlacematModels)
            {
                var placemat = GraphElementFactory.CreateUI<GraphElement>(this, placematModel);
                if (placemat != null)
                {
                    placemats.Add(placemat);
                    AddElement(placemat);
                }
            }

            ContentViewContainer.Add(m_BadgesParent);

            m_BadgesParent.Clear();
            foreach (var badgeModel in graphModel.BadgeModels)
            {
                if (badgeModel.ParentModel == null)
                    continue;

                var badge = GraphElementFactory.CreateUI<Badge>(this, badgeModel);
                if (badge != null)
                {
                    AddElement(badge);
                }
            }

            // We need to do this after all graph elements are created.
            foreach (var placemat in placemats)
            {
                placemat.UpdateFromModel();
            }

            UpdateViewTransform(GraphViewState.Position, GraphViewState.Scale);
        }

        bool CreateEdgeUI(IEdgeModel edge)
        {
            if (edge.ToPort != null && edge.FromPort != null)
            {
                AddEdgeUI(edge);
                return true;
            }

            var(inputResult, outputResult) = edge.AddPlaceHolderPorts(out var inputNode, out var outputNode);

            if (inputResult == PortMigrationResult.PlaceholderPortAdded && inputNode != null)
            {
                var inputNodeUi = inputNode.GetUI(this);
                inputNodeUi?.UpdateFromModel();
            }

            if (outputResult == PortMigrationResult.PlaceholderPortAdded && outputNode != null)
            {
                var outputNodeUi = outputNode.GetUI(this);
                outputNodeUi?.UpdateFromModel();
            }

            if (inputResult != PortMigrationResult.PlaceholderPortFailure &&
                outputResult != PortMigrationResult.PlaceholderPortFailure)
            {
                AddEdgeUI(edge);
                return true;
            }

            return false;
        }

        void AddEdgeUI(IEdgeModel edgeModel)
        {
            var edge = GraphElementFactory.CreateUI<GraphElement>(this, edgeModel);
            AddElement(edge);
            AddPositionDependency(edgeModel);
        }
    }
}
