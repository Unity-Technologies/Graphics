using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEngine;
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

    /// <summary>
    /// Display modes for the GraphView.
    /// </summary>
    public enum GraphViewDisplayMode
    {
        /// <summary>
        /// The graph view will handle user interactions through events.
        /// </summary>
        Interactive,

        /// <summary>
        /// The graph view will only display the model.
        /// </summary>
        NonInteractive,
    }

    /// <summary>
    /// The <see cref="RootView"/> in which graphs are drawn.
    /// </summary>
    public class GraphView : RootView, IDragSource
    {
        public const int frameBorder = 30;

        /// <summary>
        /// GraphView elements are organized into layers to ensure some type of graph elements
        /// are always drawn on top of others.
        /// </summary>
        public class Layer : VisualElement {}

        static readonly List<ModelView> k_UpdateAllUIs = new List<ModelView>();

        public new static readonly string ussClassName = "ge-graph-view";
        public static readonly string ussNonInteractiveModifierClassName = "ge-graph-view".WithUssModifier("non-interactive");

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

        readonly VisualElement m_GraphViewContainer;
        readonly VisualElement m_BadgesParent;

        SelectionDragger m_SelectionDragger;
        ContentDragger m_ContentDragger;
        Clickable m_Clickable;
        RectangleSelector m_RectangleSelector;
        FreehandSelector m_FreehandSelector;

        protected IDragAndDropHandler m_CurrentDragAndDropHandler;
        protected IDragAndDropHandler m_BlackboardDragAndDropHandler;

        protected bool m_SelectionDraggerWasActive;

        GraphViewStateComponent.GraphLoadedObserver m_GraphViewGraphLoadedObserver;
        GraphModelStateComponent.GraphAssetLoadedObserver m_GraphModelGraphLoadedAssetObserver;
        SelectionStateComponent.GraphLoadedObserver m_SelectionGraphLoadedObserver;
        ModelViewUpdater m_UpdateObserver;
        EdgeOrderObserver m_EdgeOrderObserver;
        DeclarationHighlighter m_DeclarationHighlighter;
        ViewSelection m_ViewSelection;

        /// <summary>
        /// The display mode.
        /// </summary>
        public GraphViewDisplayMode DisplayMode { get; }

        /// <summary>
        /// The VisualElement that contains all the views.
        /// </summary>
        public VisualElement ContentViewContainer { get; }

        /// <summary>
        /// The transform of the ContentViewContainer.
        /// </summary>
        public ITransform ViewTransform => ContentViewContainer.transform;

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

        /// <summary>
        /// Whether a manipulator is currently overriding the pan and zoom of the graph view.
        /// </summary>
        /// <remarks>When this is set to true, pan and zoom of the graph view is not updated from the model.</remarks>
        public bool PanZoomIsOverriddenByManipulator { protected get; set; }

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
        /// The agent responsible for triggering graph processing when the mouse is idle.
        /// </summary>
        public ProcessOnIdleAgent ProcessOnIdleAgent { get; }

        public GraphViewModel GraphViewModel => (GraphViewModel)Model;

        /// <summary>
        /// The graph model displayed by the graph view.
        /// </summary>
        public IGraphModel GraphModel => GraphViewModel.GraphModelState?.GraphModel;

        public ViewSelection ViewSelection
        {
            get => m_ViewSelection;
            set
            {
                if (m_ViewSelection != null)
                {
                    m_ViewSelection.DetachFromView();
                }

                m_ViewSelection = value;
                m_ViewSelection.AttachToView();
            }
        }

        /// <inheritdoc />
        public IReadOnlyList<IGraphElementModel> GetSelection()
        {
            return ViewSelection?.GetSelection();
        }

        // PF TODO remove
        public new GraphViewEditorWindow Window
        {
            get => (GraphViewEditorWindow)base.Window;
        }

        public override VisualElement contentContainer => m_GraphViewContainer; // Contains full content, potentially partially visible

        public PlacematContainer PlacematContainer { get; }

        internal PositionDependenciesManager PositionDependenciesManager { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphView" /> class.
        /// </summary>
        /// <param name="window">The window to which the GraphView belongs.</param>
        /// <param name="graphTool">The tool for this GraphView.</param>
        /// <param name="graphViewName">The name of the GraphView.</param>
        /// <param name="displayMode">The display mode for the graph view.</param>
        public GraphView(GraphViewEditorWindow window, BaseGraphTool graphTool, string graphViewName,
            GraphViewDisplayMode displayMode = GraphViewDisplayMode.Interactive)
        : base(window, graphTool)
        {
            DisplayMode = displayMode;
            graphViewName ??= "GraphView_" + new Random().Next();

            if (GraphTool != null)
            {
                IGraphModel graphModel = GraphTool.ToolState.GraphModel;
                Model = new GraphViewModel(graphViewName, graphModel);

                if (DisplayMode == GraphViewDisplayMode.Interactive)
                {
                    ProcessOnIdleAgent = new ProcessOnIdleAgent(GraphTool.Preferences);
                    GraphTool.State.AddStateComponent(ProcessOnIdleAgent.StateComponent);

                    GraphViewCommandsRegistrar.RegisterCommands(this, GraphTool);
                }

                ViewSelection = new GraphViewSelection(this, GraphViewModel.GraphModelState, GraphViewModel.SelectionState);
            }

            name = graphViewName;

            AddToClassList(ussClassName);
            EnableInClassList(ussNonInteractiveModifierClassName, DisplayMode == GraphViewDisplayMode.NonInteractive);

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

            Insert(0, new GridBackground());

            PlacematContainer = new PlacematContainer(this);
            AddLayer(PlacematContainer, PlacematContainer.PlacematsLayer);

            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale, 1.0f);

            PositionDependenciesManager = new PositionDependenciesManager(this, GraphTool?.Preferences);
            m_AutoAlignmentHelper = new AutoAlignmentHelper(this);
            m_AutoSpacingHelper = new AutoSpacingHelper(this);

            if (DisplayMode == GraphViewDisplayMode.Interactive)
            {
                ContextualMenuManipulator = new ContextualMenuManipulator(BuildContextualMenu);

                Clickable = new Clickable(OnDoubleClick);
                Clickable.activators.Clear();
                Clickable.activators.Add(
                    new ManipulatorActivationFilter { button = MouseButton.LeftMouse, clickCount = 2 });

                ContentDragger = new ContentDragger();
                SelectionDragger = new SelectionDragger(this);
                RectangleSelector = new RectangleSelector();
                FreehandSelector = new FreehandSelector();

                RegisterCallback<ValidateCommandEvent>(OnValidateCommand);
                RegisterCallback<ExecuteCommandEvent>(OnExecuteCommand);

                RegisterCallback<MouseOverEvent>(OnMouseOver);
                RegisterCallback<MouseMoveEvent>(OnMouseMove);

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
            }
            else
            {
                void StopEvent(EventBase e)
                {
                    e.StopImmediatePropagation();
                    e.PreventDefault();
                }

                pickingMode = PickingMode.Ignore;

                RegisterCallback<MouseDownEvent>(StopEvent, TrickleDown.TrickleDown);
                RegisterCallback<MouseMoveEvent>(StopEvent, TrickleDown.TrickleDown);
                RegisterCallback<MouseUpEvent>(StopEvent, TrickleDown.TrickleDown);

                RegisterCallback<PointerDownEvent>(StopEvent, TrickleDown.TrickleDown);
                RegisterCallback<PointerMoveEvent>(StopEvent, TrickleDown.TrickleDown);
                RegisterCallback<PointerUpEvent>(StopEvent, TrickleDown.TrickleDown);

                RegisterCallback<MouseEnterEvent>(StopEvent, TrickleDown.TrickleDown);
                RegisterCallback<MouseLeaveEvent>(StopEvent, TrickleDown.TrickleDown);
                RegisterCallback<MouseOverEvent>(StopEvent, TrickleDown.TrickleDown);
                RegisterCallback<MouseOutEvent>(StopEvent, TrickleDown.TrickleDown);

                RegisterCallback<WheelEvent>(StopEvent, TrickleDown.TrickleDown);

                RegisterCallback<PointerEnterEvent>(StopEvent, TrickleDown.TrickleDown);
                RegisterCallback<PointerLeaveEvent>(StopEvent, TrickleDown.TrickleDown);
                RegisterCallback<PointerOverEvent>(StopEvent, TrickleDown.TrickleDown);
                RegisterCallback<PointerOutEvent>(StopEvent, TrickleDown.TrickleDown);
                RegisterCallback<PointerStationaryEvent>(StopEvent, TrickleDown.TrickleDown);

                RegisterCallback<KeyDownEvent>(StopEvent, TrickleDown.TrickleDown);
                RegisterCallback<KeyUpEvent>(StopEvent, TrickleDown.TrickleDown);

                RegisterCallback<DragEnterEvent>(StopEvent, TrickleDown.TrickleDown);
                RegisterCallback<DragExitedEvent>(StopEvent, TrickleDown.TrickleDown);
                RegisterCallback<DragPerformEvent>(StopEvent, TrickleDown.TrickleDown);
                RegisterCallback<DragUpdatedEvent>(StopEvent, TrickleDown.TrickleDown);
                RegisterCallback<DragLeaveEvent>(StopEvent, TrickleDown.TrickleDown);

                RegisterCallback<ClickEvent>(StopEvent, TrickleDown.TrickleDown);
                RegisterCallback<ContextClickEvent>(StopEvent, TrickleDown.TrickleDown);

                RegisterCallback<ValidateCommandEvent>(StopEvent, TrickleDown.TrickleDown);
                RegisterCallback<ExecuteCommandEvent>(StopEvent, TrickleDown.TrickleDown);
            }
        }

        /// <inheritdoc />
        public override void Dispatch(ICommand command, Diagnostics diagnosticsFlags = Diagnostics.None)
        {
            if (DisplayMode == GraphViewDisplayMode.Interactive)
            {
                base.Dispatch(command, diagnosticsFlags);
            }
        }

        public void ClearGraph()
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


        static List<ModelView> s_OutModelViews = new List<ModelView>();

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

            Vector3 oldScale = ContentViewContainer.transform.scale;

            if (oldScale != zoom)
            {
                ContentViewContainer.transform.scale = zoom;

                if (GraphModel != null)
                {
                    GraphModel.GraphElementModels.GetAllViewsRecursivelyInList(this, _ => true, s_OutModelViews);
                    foreach (var graphElement in s_OutModelViews.OfType<GraphElement>())
                    {
                        graphElement.SetLevelOfDetail(zoom.x);
                    }

                    s_OutModelViews.Clear();
                }
            }
        }

        /// <summary>
        /// Base speed for panning, made internal to disable panning in tests.
        /// </summary>
        internal static float basePanSpeed = 0.4f;
        internal static readonly int panAreaWidth = 100;
        public const int panInterval = 10;
        public const float minSpeedFactor = 0.5f;
        public const float maxSpeedFactor = 2.5f;
        internal static float maxPanSpeed = maxSpeedFactor * basePanSpeed;

        internal Vector2 GetEffectivePanSpeed(Vector2 worldMousePos)
        {
            var localMouse = contentContainer.WorldToLocal(worldMousePos);
            var effectiveSpeed = Vector2.zero;

            if (localMouse.x <= panAreaWidth)
                effectiveSpeed.x = -((panAreaWidth - localMouse.x) / panAreaWidth + 0.5f) * basePanSpeed;
            else if (localMouse.x >= contentContainer.layout.width - panAreaWidth)
                effectiveSpeed.x = ((localMouse.x - (contentContainer.layout.width - panAreaWidth)) / panAreaWidth + 0.5f) * basePanSpeed;

            if (localMouse.y <= panAreaWidth)
                effectiveSpeed.y = -((panAreaWidth - localMouse.y) / panAreaWidth + 0.5f) * basePanSpeed;
            else if (localMouse.y >= contentContainer.layout.height - panAreaWidth)
                effectiveSpeed.y = ((localMouse.y - (contentContainer.layout.height - panAreaWidth)) / panAreaWidth + 0.5f) * basePanSpeed;

            effectiveSpeed = Vector2.ClampMagnitude(effectiveSpeed, maxPanSpeed);

            return effectiveSpeed;
        }

        /// <summary>
        /// Gets a <see cref="IDragAndDropHandler"/> that can handle dragged and dropped objects from the blackboard.
        /// </summary>
        /// <returns>A <see cref="IDragAndDropHandler"/> that can handle dragged and dropped objects from the blackboard.</returns>
        protected virtual IDragAndDropHandler GetBlackboardDragAndDropHandler()
        {
            return m_BlackboardDragAndDropHandler ??= new SelectionDropperDropHandler(this);
        }

        /// <summary>
        /// Find an appropriate drag and drop handler for the current drag and drop operation.
        /// </summary>
        /// <returns>The <see cref="IDragAndDropHandler"/> that can handle the objects being dragged.</returns>
        protected virtual IDragAndDropHandler GetDragAndDropHandler()
        {
            var selectionDropperDropHandler = GetBlackboardDragAndDropHandler();
            if (selectionDropperDropHandler?.CanHandleDrop() ?? false)
                return selectionDropperDropHandler;

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
                    Select(m => m.GetView<GraphElement>(this)).ToList();

                bool hasNodeOnGraph = nodesAndNotes.Any(t => !t.GraphElementModel.NeedsContainer());

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
                    var selectionUI = selection.Select(m => m.GetView<GraphElement>(this));
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
                            var outputPort = edgeModel.FromPort.GetView<Port>(this);
                            var inputPort = edgeModel.ToPort.GetView<Port>(this);
                            var outputNode = edgeModel.FromPort.NodeModel.GetView<Node>(this);
                            var inputNode = edgeModel.ToPort.NodeModel.GetView<Node>(this);

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

            ViewSelection?.BuildContextualMenu(evt);

            if (Unsupported.IsDeveloperBuild())
            {
                evt.menu.AppendSeparator();
                evt.menu.AppendAction("Internal/Refresh All UI", _ =>
                {
                    using (var updater = GraphViewModel.GraphViewState.UpdateScope)
                    {
                        updater.ForceCompleteUpdate();
                    }
                });

                if (selection.Any())
                {
                    evt.menu.AppendAction("Internal/Refresh Selected Element(s)",
                        _ =>
                        {
                            using (var graphUpdater = GraphViewModel.GraphModelState.UpdateScope)
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
            var element = panel.Pick(mousePosition).GetFirstOfType<IModelView>();
            var stencil = (Stencil)GraphModel.Stencil;

            VisualElement current = element as VisualElement;
            while (current != null && current != this)
            {
                if (current is IDisplaySmartSearchUI dssUI)
                    if (dssUI.DisplaySmartSearch(mousePosition))
                        return;

                current = current.parent;
            }

            SearcherService.ShowGraphNodes(stencil, GraphTool?.Name, GetType(), GraphTool?.Preferences, GraphModel, mousePosition, item =>
            {
                Dispatch(CreateNodeCommand.OnGraph(item, graphPosition));
            }, Window);
        }

        /// <inheritdoc />
        protected override void RegisterObservers()
        {
            if (GraphTool?.ObserverManager == null)
                return;

            // PF TODO use a single observer on graph loaded to update all states.

            if (m_GraphViewGraphLoadedObserver == null)
            {
                m_GraphViewGraphLoadedObserver = new GraphViewStateComponent.GraphLoadedObserver(GraphTool.ToolState, GraphViewModel.GraphViewState);
                GraphTool.ObserverManager.RegisterObserver(m_GraphViewGraphLoadedObserver);
            }

            if (m_GraphModelGraphLoadedAssetObserver == null)
            {
                m_GraphModelGraphLoadedAssetObserver = new GraphModelStateComponent.GraphAssetLoadedObserver(GraphTool.ToolState, GraphViewModel.GraphModelState);
                GraphTool.ObserverManager.RegisterObserver(m_GraphModelGraphLoadedAssetObserver);
            }

            if (m_SelectionGraphLoadedObserver == null)
            {
                m_SelectionGraphLoadedObserver = new SelectionStateComponent.GraphLoadedObserver(GraphTool.ToolState, GraphViewModel.SelectionState);
                GraphTool.ObserverManager.RegisterObserver(m_SelectionGraphLoadedObserver);
            }

            if (m_UpdateObserver == null)
            {
                m_UpdateObserver = new ModelViewUpdater(this, GraphViewModel.GraphViewState, GraphViewModel.GraphModelState, GraphViewModel.SelectionState, GraphTool.GraphProcessingState, GraphTool.HighlighterState);
                GraphTool.ObserverManager.RegisterObserver(m_UpdateObserver);
            }

            if (m_EdgeOrderObserver == null)
            {
                m_EdgeOrderObserver = new EdgeOrderObserver(GraphViewModel.SelectionState, GraphViewModel.GraphModelState);
                GraphTool.ObserverManager.RegisterObserver(m_EdgeOrderObserver);
            }

            if (m_DeclarationHighlighter == null)
            {
                m_DeclarationHighlighter = new DeclarationHighlighter(GraphTool.ToolState, GraphViewModel.SelectionState, GraphTool.HighlighterState,
                    model => model is IHasDeclarationModel hasDeclarationModel ? hasDeclarationModel.DeclarationModel : null);
                GraphTool.ObserverManager.RegisterObserver(m_DeclarationHighlighter);
            }
        }

        /// <inheritdoc />
        protected override void UnregisterObservers()
        {
            if (GraphTool?.ObserverManager == null)
                return;

            if (m_GraphViewGraphLoadedObserver != null)
            {
                GraphTool?.ObserverManager?.UnregisterObserver(m_GraphViewGraphLoadedObserver);
                m_GraphViewGraphLoadedObserver = null;
            }

            if (m_GraphModelGraphLoadedAssetObserver != null)
            {
                GraphTool?.ObserverManager?.UnregisterObserver(m_GraphModelGraphLoadedAssetObserver);
                m_GraphModelGraphLoadedAssetObserver = null;
            }

            if (m_SelectionGraphLoadedObserver != null)
            {
                GraphTool?.ObserverManager?.UnregisterObserver(m_SelectionGraphLoadedObserver);
                m_SelectionGraphLoadedObserver = null;
            }

            if (m_UpdateObserver != null)
            {
                GraphTool?.ObserverManager?.UnregisterObserver(m_UpdateObserver);
                m_UpdateObserver = null;
            }

            if (m_EdgeOrderObserver != null)
            {
                GraphTool?.ObserverManager?.UnregisterObserver(m_EdgeOrderObserver);
                m_EdgeOrderObserver = null;
            }

            if (m_DeclarationHighlighter != null)
            {
                GraphTool?.ObserverManager?.UnregisterObserver(m_DeclarationHighlighter);
                m_DeclarationHighlighter = null;
            }
        }

        /// <inheritdoc />
        protected override void OnEnterPanel(AttachToPanelEvent e)
        {
            base.OnEnterPanel(e);
            this.SetUpRender(OnUpdateMaterial);
        }

        /// <inheritdoc />
        protected override void OnLeavePanel(DetachFromPanelEvent e)
        {
            this.TearDownRender(OnUpdateMaterial);
            base.OnLeavePanel(e);
        }

        void OnUpdateMaterial(Material mat)
        {
            // Set global graph view shader properties (used by UIR)
            mat.SetFloat(GraphViewStaticBridge.editorPixelsPerPointId, EditorGUIUtility.pixelsPerPoint);
            mat.SetFloat(GraphViewStaticBridge.graphViewScaleId, ViewTransform.scale.x);
        }

        internal void OnValidateCommand(ValidateCommandEvent evt)
        {
            if (panel.GetCapturingElement(PointerId.mousePointerId) != null)
                return;

            if (evt.commandName == GraphViewStaticBridge.EventCommandNames.FrameSelected)
            {
                evt.StopPropagation();
                evt.imguiEvent?.Use();
            }
        }

        internal void OnExecuteCommand(ExecuteCommandEvent evt)
        {
            if (panel.GetCapturingElement(PointerId.mousePointerId) != null)
                return;

            if (evt.commandName == GraphViewStaticBridge.EventCommandNames.FrameSelected)
            {
                this.DispatchFrameSelectionCommand();
                evt.StopPropagation();
            }

            if (evt.isPropagationStopped)
            {
                evt.imguiEvent?.Use();
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
                graphElement.AddToRootView(this);
            }
            catch (Exception e)
            {
                Debug.LogError(e);
            }

            switch (DisplayMode)
            {
                case GraphViewDisplayMode.NonInteractive:
                    IgnorePickingRecursive(graphElement);
                    break;

                case GraphViewDisplayMode.Interactive:
                    if (graphElement is Node || graphElement is Edge)
                    {
                        graphElement.RegisterCallback<MouseOverEvent>(OnMouseOver);
                    }
                    break;
            }

            if (graphElement.Model is IEdgePortalModel portalModel)
            {
                AddPortalDependency(portalModel);
            }

            void IgnorePickingRecursive(VisualElement e)
            {
                e.pickingMode = PickingMode.Ignore;

                foreach (var child in e.hierarchy.Children())
                {
                    IgnorePickingRecursive(child);
                }
            }

            graphElement.SetLevelOfDetail(ViewTransform.scale.x);
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
            graphElement.RemoveFromRootView();
        }

        static readonly List<ModelView> k_CalculateRectToFitAllAllUIs = new List<ModelView>();

        public Rect CalculateRectToFitAll()
        {
            Rect rectToFit = ContentViewContainer.layout;
            bool reachedFirstChild = false;

            GraphModel?.GraphElementModels.GetAllViewsInList(this, null, k_CalculateRectToFitAllAllUIs);
            foreach (var ge in k_CalculateRectToFitAllAllUIs)
            {
                if (ge is null || ge.Model is IEdgeModel)
                    continue;

                if (!reachedFirstChild)
                {
                    rectToFit = ge.parent.ChangeCoordinatesTo(ContentViewContainer, ge.layout);
                    reachedFirstChild = true;
                }
                else
                {
                    rectToFit = RectUtils.Encompass(rectToFit, ge.parent.ChangeCoordinatesTo(ContentViewContainer, ge.layout));
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
            if (ModelView.IsRenameKey(e))
            {
                if (e.target == this)
                {
                    // Forward event to the last selected element.
                    var renamableSelection = GetSelection().Where(x => x.IsRenamable());
                    var lastSelectedItem = renamableSelection.LastOrDefault();
                    var lastSelectedItemUI = lastSelectedItem?.GetView<GraphElement>(this);

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
            evt.StopPropagation();
        }

        protected void OnDoubleClick()
        {
            // Display graph in inspector when clicking on background
            // TODO: displayed on double click ATM as this method overrides the Token.Select() which does not stop propagation
            Selection.activeObject = GraphViewModel.GraphModelState?.GraphModel.Asset as Object;
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
                var elem = elemModel?.GetView<GraphElement>(this);
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
                        Vector2 pos = ContentViewContainer.ChangeCoordinatesTo(elem.hierarchy.parent, elem.layout.position);
                        PositionDependenciesManager.ProcessMovedNodes(pos);
                    }
                }).Until(() => !m_SelectionDraggerWasActive);
            }
        }

        protected virtual void OnDragEnter(DragEnterEvent evt)
        {
            var dragAndDropHandler = GetDragAndDropHandler();
            if (dragAndDropHandler != null)
            {
                m_CurrentDragAndDropHandler = dragAndDropHandler;
                m_CurrentDragAndDropHandler.OnDragEnter(evt);
            }
        }

        protected virtual void OnDragLeave(DragLeaveEvent evt)
        {
            m_CurrentDragAndDropHandler?.OnDragLeave(evt);
            m_CurrentDragAndDropHandler = null;
        }

        protected virtual void OnDragUpdated(DragUpdatedEvent e)
        {
            if (m_CurrentDragAndDropHandler != null)
            {
                m_CurrentDragAndDropHandler?.OnDragUpdated(e);
            }
            else
            {
                DragAndDrop.visualMode = DragAndDropVisualMode.Rejected;
            }
        }

        protected virtual void OnDragPerform(DragPerformEvent e)
        {
            m_CurrentDragAndDropHandler?.OnDragPerform(e);
            m_CurrentDragAndDropHandler = null;
        }

        protected virtual void OnDragExited(DragExitedEvent e)
        {
            m_CurrentDragAndDropHandler?.OnDragExited(e);
            m_CurrentDragAndDropHandler = null;
        }

        /// <summary>
        /// Updates the graph view to reflect the changes in the state components.
        /// </summary>
        public override void UpdateFromModel()
        {
            if (m_UpdateObserver == null || GraphViewModel == null)
                return;

            using (var graphViewObservation = m_UpdateObserver.ObserveState(GraphViewModel.GraphViewState))
            {
                if (graphViewObservation.UpdateType != UpdateType.None)
                {
                    if (!PanZoomIsOverriddenByManipulator)
                        UpdateViewTransform(GraphViewModel.GraphViewState.Position, GraphViewModel.GraphViewState.Scale);
                }
            }

            UpdateType updateType;

            using (var graphModelObservation = m_UpdateObserver.ObserveState(GraphViewModel.GraphModelState))
            using (var selectionObservation = m_UpdateObserver.ObserveState(GraphViewModel.SelectionState))
            using (var highlighterObservation = m_UpdateObserver.ObserveState(GraphTool.HighlighterState))
            {
                updateType = DoUpdate(graphModelObservation, selectionObservation, highlighterObservation);
            }

            DoUpdateProcessingErrorBadges(updateType);
        }

        protected virtual UpdateType DoUpdate(Observation graphModelObservation, Observation selectionObservation, Observation highlighterObservation)
        {
            var rebuildType = graphModelObservation.UpdateType.Combine(selectionObservation.UpdateType);

            if (rebuildType == UpdateType.Complete)
            {
                // This happens on undo, amongst other cases.
                // Undo replaces the IGraphModel object by a new object.
                // We need to recreate the all the UI so it does not reference the old graph model.

                // Sad. We lose the focused element.
                Focus();

                BuildUI();
            }
            else if (rebuildType == UpdateType.Partial || highlighterObservation.UpdateType != UpdateType.None)
            {
                PartialUpdate(graphModelObservation, selectionObservation, highlighterObservation);
            }

            return rebuildType;
        }

        protected virtual void PartialUpdate(Observation graphModelObservation, Observation selectionObservation, Observation highlighterObservation)
        {
            var focusedElement = panel.focusController.focusedElement as VisualElement;
            while (focusedElement != null && !(focusedElement is IModelView))
            {
                focusedElement = focusedElement.parent;
            }

            var focusedModelView = focusedElement as IModelView;

            var modelChangeSet = GraphViewModel.GraphModelState.GetAggregatedChangeset(graphModelObservation.LastObservedVersion);
            var selectionChangeSet = GraphViewModel.SelectionState.GetAggregatedChangeset(selectionObservation.LastObservedVersion);

            if (GraphTool.Preferences.GetBool(BoolPref.LogUIUpdate))
            {
                Debug.Log($"Partial GraphView Update {modelChangeSet?.NewModels.Count() ?? 0} new {modelChangeSet?.ChangedModels.Count() ?? 0} changed {modelChangeSet?.DeletedModels.Count() ?? 0} deleted");
            }

            var changedModels = new HashSet<IGraphElementModel>();
            var shouldUpdatePlacematContainer = false;
            var newPlacemats = new List<GraphElement>();
            if (modelChangeSet != null)
            {
                DeleteElementsFromChangeSet(modelChangeSet, focusedModelView);

                AddElementsFromChangeSet(modelChangeSet, newPlacemats);

                shouldUpdatePlacematContainer = newPlacemats.Any();

                //Update new and deleted node containers
                foreach (var model in modelChangeSet.NewModels.Concat(modelChangeSet.DeletedModels))
                {
                    if (model.Container is IGraphElementModel container)
                        changedModels.Add(container);
                }
            }

            if (modelChangeSet != null && selectionChangeSet != null)
            {
                var combinedSet = new HashSet<IGraphElementModel>(modelChangeSet.ChangedModels);
                combinedSet.UnionWith(selectionChangeSet.ChangedModels.Except(modelChangeSet.DeletedModels));
                changedModels.UnionWith(combinedSet);
            }
            else if (modelChangeSet != null)
            {
                changedModels.UnionWith(modelChangeSet.ChangedModels);
            }
            else if (selectionChangeSet != null)
            {
                changedModels.UnionWith(selectionChangeSet.ChangedModels);
            }

            if (highlighterObservation.UpdateType == UpdateType.Complete)
            {
                changedModels.UnionWith(GraphModel.NodeModels.OfType<IHasDeclarationModel>());
            }
            else if (highlighterObservation.UpdateType == UpdateType.Partial)
            {
                var changeset = GraphTool.HighlighterState.GetAggregatedChangeset(highlighterObservation.LastObservedVersion);
                changedModels.UnionWith(changeset.ChangedModels.SelectMany(d => GraphModel.FindReferencesInGraph(d)));
            }

            UpdateChangedModels(changedModels, shouldUpdatePlacematContainer, newPlacemats);

            // PF FIXME: node state (enable/disabled, used/unused) should be part of the State.
            if (GraphTool.Preferences.GetBool(BoolPref.ShowUnusedNodes))
                PositionDependenciesManager.UpdateNodeState();

            // PF FIXME use observer or otherwise refactor this
            if (modelChangeSet != null && modelChangeSet.ModelsToAutoAlign.Any())
            {
                // Auto placement relies on UI layout to compute node positions, so we need to
                // schedule it to execute after the next layout pass.
                // Furthermore, it will modify the model position, hence it must be
                // done inside a Store.BeginStateChange block.
                var elementsToAlign = modelChangeSet.ModelsToAutoAlign.ToList();
                schedule.Execute(() =>
                {
                    using (var graphUpdater = GraphViewModel.GraphModelState.UpdateScope)
                    {
                        PositionDependenciesManager.AlignNodes(true, elementsToAlign, graphUpdater);
                    }
                });
            }

            var lastSelectedNode = GetSelection().OfType<INodeModel>().LastOrDefault();
            if (lastSelectedNode != null && lastSelectedNode.IsAscendable())
            {
                var nodeUI = lastSelectedNode.GetView<GraphElement>(this);

                nodeUI?.BringToFront();
            }

            if (modelChangeSet?.RenamedModel != null)
            {
                List<ModelView> modelUis = new List<ModelView>();
                modelChangeSet.RenamedModel.GetAllViews(this, _ => true, modelUis);
                foreach (var ui in modelUis)
                {
                    ui.ActivateRename();
                }
            }
        }

        protected virtual void DeleteElementsFromChangeSet(GraphModelStateComponent.Changeset modelChangeSet, IModelView focusedModelView)
        {
            foreach (var model in modelChangeSet.DeletedModels)
            {
                if (ReferenceEquals(model, focusedModelView?.Model))
                {
                    // Focused element will be deleted. Switch the focus to the graph view to avoid dangling focus.
                    Focus();
                }

                model.GetAllViews(this, null, k_UpdateAllUIs);
                foreach (var ui in k_UpdateAllUIs.OfType<GraphElement>())
                {
                    RemoveElement(ui);
                }

                k_UpdateAllUIs.Clear();

                // GTF-Edit: Commented this out because on deletion it tries to represent our port models and fails
                // ToList is needed to bake the dependencies.
                //foreach (var ui in model.GetDependencies().ToList())
                //{
                //    ui.UpdateFromModel();
                //}
            }
        }

        protected virtual void AddElementsFromChangeSet(GraphModelStateComponent.Changeset modelChangeSet, List<GraphElement> newPlacemats)
        {
            foreach (var model in modelChangeSet.NewModels.Where(m => !(m is IEdgeModel) && !(m is IPlacematModel) && !(m is IBadgeModel) && !(m is IDeclarationModel)))
            {
                if (model.Container != GraphModel)
                    continue;
                var ui = ModelViewFactory.CreateUI<GraphElement>(this, model);
                if (ui != null)
                    AddElement(ui);
            }

            foreach (var model in modelChangeSet.NewModels.OfType<IEdgeModel>())
            {
                CreateEdgeUI(model);
            }

            foreach (var model in modelChangeSet.NewModels.OfType<IPlacematModel>())
            {
                var placemat = ModelViewFactory.CreateUI<GraphElement>(this, model);
                if (placemat != null)
                {
                    newPlacemats.Add(placemat);
                    AddElement(placemat);
                }
            }

            foreach (var model in modelChangeSet.NewModels.OfType<IBadgeModel>())
            {
                if (model.ParentModel == null)
                    continue;

                var badge = ModelViewFactory.CreateUI<Badge>(this, model);
                if (badge != null)
                {
                    AddElement(badge);
                }
            }
        }

        protected virtual void UpdateChangedModels(HashSet<IGraphElementModel> changedModels, bool shouldUpdatePlacematContainer, List<GraphElement> placemats)
        {
            foreach (var model in changedModels)
            {
                model.GetAllViews(this, null, k_UpdateAllUIs);
                foreach (var ui in k_UpdateAllUIs)
                {
                    ui.UpdateFromModel();

                    if (ui.parent == PlacematContainer)
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
        }

        protected virtual void DoUpdateProcessingErrorBadges(UpdateType rebuildType)
        {
            // Update processing error badges.
            using (var processingStateObservation = m_UpdateObserver.ObserveState(GraphTool.GraphProcessingState))
            {
                if (processingStateObservation.UpdateType != UpdateType.None || rebuildType == UpdateType.Partial)
                {
                    ConsoleWindowBridge.RemoveLogEntries();
                    var graphAsset = GraphViewModel.GraphModelState.GraphModel?.Asset;

                    foreach (var rawError in GraphTool.GraphProcessingState.RawErrors ?? Enumerable.Empty<GraphProcessingError>())
                    {
                        if (graphAsset is Object asset)
                        {
                            string graphAssetPath;
                            if (asset != null && asset is ISerializedGraphAsset serializedGraphAsset)
                            {
                                graphAssetPath = serializedGraphAsset.FilePath;
                            }
                            else
                            {
                                graphAssetPath = "<unknown>";
                            }
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
                        foreach (var ui in badge.GraphElementModel.GetDependencies().ToList())
                        {
                            ui.UpdateFromModel();
                        }
                    }

                    foreach (var model in GraphTool.GraphProcessingState.Errors ?? Enumerable.Empty<IGraphProcessingErrorModel>())
                    {
                        if (model.ParentModel == null || !GraphModel.TryGetModelFromGuid(model.ParentModel.Guid, out var _))
                            return;

                        var badge = ModelViewFactory.CreateUI<Badge>(this, model);
                        if (badge != null)
                        {
                            AddElement(badge);
                        }
                    }
                }
            }
        }

        public override void BuildUI()
        {
            if (GraphTool?.Preferences != null)
            {
                if (GraphTool.Preferences.GetBool(BoolPref.LogUIUpdate))
                {
                    Debug.Log($"Complete GraphView Update");
                }

                if (GraphTool.Preferences.GetBool(BoolPref.WarnOnUIFullRebuild))
                {
                    Debug.LogWarning($"Rebuilding all GraphView UI ({(Dispatcher as CommandDispatcher)?.LastDispatchedCommandName ?? "Unknown command"})");
                }
            }

            ClearGraph();

            var graphModel = GraphViewModel?.GraphModelState.GraphModel;
            if (graphModel == null)
                return;

            foreach (var nodeModel in graphModel.NodeModels)
            {
                var node = ModelViewFactory.CreateUI<GraphElement>(this, nodeModel);
                if (node != null)
                    AddElement(node);
            }

            foreach (var stickyNoteModel in graphModel.StickyNoteModels)
            {
                var stickyNote = ModelViewFactory.CreateUI<GraphElement>(this, stickyNoteModel);
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
            foreach (var placematModel in GraphViewModel.GraphModelState.GraphModel.PlacematModels)
            {
                var placemat = ModelViewFactory.CreateUI<GraphElement>(this, placematModel);
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

                var badge = ModelViewFactory.CreateUI<Badge>(this, badgeModel);
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

            UpdateViewTransform(GraphViewModel.GraphViewState.Position, GraphViewModel.GraphViewState.Scale);
        }

        bool CreateEdgeUI(IEdgeModel edge)
        {
            if (edge.ToPort != null && edge.FromPort != null)
            {
                AddEdgeUI(edge);
                return true;
            }

            var (inputResult, outputResult) = edge.AddPlaceHolderPorts(out var inputNode, out var outputNode);

            if (inputResult == PortMigrationResult.PlaceholderPortAdded && inputNode != null)
            {
                var inputNodeUi = inputNode.GetView(this);
                inputNodeUi?.UpdateFromModel();
            }

            if (outputResult == PortMigrationResult.PlaceholderPortAdded && outputNode != null)
            {
                var outputNodeUi = outputNode.GetView(this);
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
            var edge = ModelViewFactory.CreateUI<GraphElement>(this, edgeModel);
            AddElement(edge);
            AddPositionDependency(edgeModel);
        }

        /// <summary>
        /// Populates the option menu.
        /// </summary>
        /// <param name="menu">The menu to populate.</param>
        public virtual void BuildOptionMenu(GenericMenu menu)
        {
            var prefs = GraphTool?.Preferences;

            if (prefs != null)
            {
                if (Unsupported.IsDeveloperMode())
                {
                    menu.AddItem(new GUIContent("Show Searcher in Regular Window"),
                        prefs.GetBool(BoolPref.SearcherInRegularWindow),
                        () =>
                        {
                            prefs.ToggleBool(BoolPref.SearcherInRegularWindow);
                        });

                    menu.AddSeparator("");

                    bool graphLoaded = GraphTool?.ToolState.GraphModel != null;

                    // ReSharper disable once RedundantCast : needed in 2020.3.
                    menu.AddItem(new GUIContent("Reload Graph"), false, !graphLoaded ? (GenericMenu.MenuFunction)null : () =>
                    {
                        if (GraphTool?.ToolState.GraphModel != null)
                        {
                            var openedGraph = GraphTool.ToolState.CurrentGraph;
                            Selection.activeObject = null;
                            Resources.UnloadAsset((Object)openedGraph.GetGraphAsset());
                            GraphTool?.Dispatch(new LoadGraphCommand(openedGraph.GetGraphModel()));
                        }
                    });

                    // ReSharper disable once RedundantCast : needed in 2020.3.
                    menu.AddItem(new GUIContent("Rebuild UI"), false, !graphLoaded ? (GenericMenu.MenuFunction)null : () =>
                    {
                        using (var updater = GraphViewModel.GraphModelState.UpdateScope)
                        {
                            updater.ForceCompleteUpdate();
                        }
                    });

                    menu.AddSeparator("");

                    menu.AddItem(new GUIContent("Evaluate Graph Only When Idle"),
                        prefs.GetBool(BoolPref.OnlyProcessWhenIdle), () =>
                        {
                            prefs.ToggleBool(BoolPref.OnlyProcessWhenIdle);
                        });
                }
            }
        }
    }
}
