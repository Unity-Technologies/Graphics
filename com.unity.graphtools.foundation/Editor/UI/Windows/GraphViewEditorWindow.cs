using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
#if UNITY_2022_2_OR_NEWER
using UnityEditor.Overlays;
#endif
using UnityEngine;
using UnityEngine.GraphToolsFoundation.CommandStateObserver;
using UnityEngine.GraphToolsFoundation.Overdrive;
using UnityEngine.Profiling;
using UnityEngine.UIElements;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Base class for windows to edit graphs.
    /// </summary>
    public abstract class GraphViewEditorWindow : EditorWindow, IHasCustomMenu
#if UNITY_2022_2_OR_NEWER
    , ISupportsOverlays
#endif
    {
        /// <summary>
        /// Finds a graph asset's opened window of type <typeparamref name="TWindow"/>. If no window is found, create a new one.
        /// The window is then opened and focused.
        /// </summary>
        /// <param name="assetPath">The path of the graph asset to open.</param>
        /// <typeparam name="TWindow">The window type, which should derive from <see cref="GraphViewEditorWindow"/>.</typeparam>
        /// <returns>A window.</returns>
        public static TWindow FindOrCreateGraphWindow<TWindow>(string assetPath = null) where TWindow : GraphViewEditorWindow
        {
            TWindow window = null;

            if (assetPath != null)
            {
                window = Resources.FindObjectsOfTypeAll(typeof(TWindow)).OfType<TWindow>().FirstOrDefault(w =>
                        w.GraphTool.ToolState.CurrentGraph.GetGraphAssetPath() == assetPath ||
                        w.GraphTool.ToolState.SubGraphStack.FirstOrDefault().GetGraphAssetPath() == assetPath);
            }

            if (window == null)
                window = CreateWindow<TWindow>(desiredDockNextTo: typeof(TWindow));

            window.Show();
            window.Focus();

            return window;
        }

        /// <summary>
        /// Ways of opening a graph when selection changes.
        /// </summary>
        public enum OpenMode
        {
            /// <summary>
            /// Just open the graph in the window.
            /// </summary>
            Open,

            /// <summary>
            /// Show the graph and focus the window.
            /// </summary>
            OpenAndFocus
        }

        public static readonly string graphProcessingPendingUssClassName = "graph-processing-pending";

        static int s_LastFocusedEditor = -1;

        [SerializeField]
        LockTracker m_LockTracker = new LockTracker();

        bool m_Focused;

        protected GraphView m_GraphView;
        protected VisualElement m_GraphContainer;
        protected BlankPage m_BlankPage;
        protected Label m_GraphProcessingPendingLabel;
#if !UNITY_2022_2_OR_NEWER
        protected ModelInspectorView m_SidePanel;
        protected MainToolbar m_MainToolbar;
        protected ErrorToolbar m_ErrorToolbar;
#endif
        List<string> m_DisplayedOverlays;

        AutomaticGraphProcessingObserver m_AutomaticGraphProcessingObserver;
        GraphProcessingStatusObserver m_GraphProcessingStatusObserver;

        [SerializeField]
        Hash128 m_WindowID;

#if !UNITY_2022_2_OR_NEWER
        public bool WithSidePanel { get; set; } = true;
#endif

        public virtual IEnumerable<GraphView> GraphViews
        {
            get { yield return GraphView; }
        }

        bool Locked => m_LockTracker.IsLocked;


        protected Hash128 WindowID => m_WindowID;

        /// <summary>
        /// The graph tool.
        /// </summary>
        public BaseGraphTool GraphTool { get; private set; }

        public GraphView GraphView => m_GraphView;
#if !UNITY_2022_2_OR_NEWER
        public MainToolbar MainToolbar => m_MainToolbar;
#endif

        static GraphViewEditorWindow()
        {
            SetupLogStickyCallback();
        }

        protected GraphViewEditorWindow()
        {
            s_LastFocusedEditor = GetInstanceID();
            m_WindowID = SerializableGUID.Generate();
        }

        protected virtual BaseGraphTool CreateGraphTool()
        {
            return CsoTool.Create<BaseGraphTool>(WindowID);
        }

        protected virtual BlankPage CreateBlankPage()
        {
            return new BlankPage(GraphTool?.Dispatcher, Enumerable.Empty<OnboardingProvider>());
        }

#if !UNITY_2022_2_OR_NEWER
        protected virtual MainToolbar CreateMainToolbar()
        {
            return new MainToolbar(GraphTool, GraphView);
        }

        protected virtual ErrorToolbar CreateErrorToolbar()
        {
            return new ErrorToolbar(GraphTool, GraphView);
        }
#endif

        protected virtual GraphView CreateGraphView()
        {
            return new GraphView(this, GraphTool, GraphTool.Name);
        }

        /// <summary>
        /// Creates a BlackboardView.
        /// </summary>
        /// <returns>A new BlackboardView.</returns>
        public virtual BlackboardView CreateBlackboardView()
        {
            return GraphView != null ? new BlackboardView(this, GraphView) : null;
        }

        /// <summary>
        /// Creates a MiniMapView.
        /// </summary>
        /// <returns>A new MiniMapView.</returns>
        public virtual MiniMapView CreateMiniMapView()
        {
            return new MiniMapView(this, GraphView);
        }

        /// <summary>
        /// Creates a ModelInspectorView.
        /// </summary>
        /// <returns>A new ModelInspectorView.</returns>
        public virtual ModelInspectorView CreateModelInspectorView()
        {
            return GraphView != null ? new ModelInspectorView(this, GraphView) : null;
        }

        protected virtual void Reset()
        {
            if (GraphTool?.ToolState == null)
                return;

            using var toolStateUpdater = GraphTool.ToolState.UpdateScope;
            toolStateUpdater.ClearHistory();
            toolStateUpdater.LoadGraph(null, null);
            m_WindowID = SerializableGUID.Generate();
        }

        protected virtual void OnEnable()
        {
            GraphTool = CreateGraphTool();

#if !UNITY_2022_2_OR_NEWER
            if (m_MainToolbar != null)
            {
                m_MainToolbar.RemoveFromHierarchy();
                m_MainToolbar = null;
            }
#endif

            if (m_GraphContainer != null)
            {
                m_GraphContainer.RemoveFromHierarchy();
                m_GraphContainer = null;
            }

#if !UNITY_2022_2_OR_NEWER
            if (m_ErrorToolbar != null)
            {
                m_ErrorToolbar.RemoveFromHierarchy();
                m_ErrorToolbar = null;
            }
#endif

            if (rootVisualElement.Contains(m_GraphProcessingPendingLabel))
            {
                rootVisualElement.Remove(m_GraphProcessingPendingLabel);
                m_GraphProcessingPendingLabel = null;
            }

            rootVisualElement.pickingMode = PickingMode.Ignore;

            m_GraphContainer = new VisualElement { name = "graphContainer" };
            m_GraphView = CreateGraphView();
#if !UNITY_2022_2_OR_NEWER
            m_MainToolbar = CreateMainToolbar();
            m_ErrorToolbar = CreateErrorToolbar();
#endif
            m_BlankPage = CreateBlankPage();
            m_BlankPage?.CreateUI();

#if !UNITY_2022_2_OR_NEWER
            if (m_MainToolbar != null)
                rootVisualElement.Add(m_MainToolbar);
#endif

            rootVisualElement.Add(m_GraphContainer);

#if !UNITY_2022_2_OR_NEWER
            if (m_ErrorToolbar != null)
                m_GraphView.Add(m_ErrorToolbar);
#endif

            m_GraphContainer.Add(m_GraphView);

            rootVisualElement.AddStylesheet("GraphViewWindow.uss");
            rootVisualElement.AddToClassList("unity-theme-env-variables");
            rootVisualElement.AddToClassList("gtf-root");

            // PF FIXME: Use EditorApplication.playModeStateChanged / AssemblyReloadEvents ? Make sure it works on all domain reloads.

            // After a domain reload, all loaded objects will get reloaded and their OnEnable() called again
            // It looks like all loaded objects are put in a deserialization/OnEnable() queue
            // the previous graph's nodes/edges/... might be queued AFTER this window's OnEnable
            // so relying on objects to be loaded/initialized is not safe
            // hence, we need to defer the loading command

            rootVisualElement.schedule.Execute(() =>
            {
                try
                {
                    var graphModel = GraphTool?.ToolState.LastOpenedGraph.GetGraphModel();
                    if (graphModel != null)
                    {
                        GraphTool?.Dispatch(new LoadGraphCommand(graphModel,
                            GraphTool.ToolState.LastOpenedGraph.BoundObject,
                            LoadGraphCommand.LoadStrategies.KeepHistory));
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            }).ExecuteLater(0);

            m_GraphProcessingPendingLabel = new Label("Graph Processing Pending") { name = "graph-processing-pending-label" };

#if !UNITY_2022_2_OR_NEWER
            if (WithSidePanel && m_SidePanel == null)
            {
                m_SidePanel = CreateModelInspectorView();
                if (m_SidePanel != null)
                {
                    m_GraphContainer.Add(m_SidePanel);
                }
            }
#endif

            rootVisualElement.RegisterCallback<AttachToPanelEvent>(OnEnterPanel);
            rootVisualElement.RegisterCallback<DetachFromPanelEvent>(OnLeavePanel);
            // that will be true when the window is restored during the editor startup, so OnEnterPanel won't be called later
            if (rootVisualElement.panel != null)
                OnEnterPanel(null);

            UpdateWindowTitle();

            m_LockTracker.lockStateChanged.AddListener(OnLockStateChanged);

            if (GraphView?.DisplayMode == GraphViewDisplayMode.Interactive)
            {
                m_AutomaticGraphProcessingObserver = new AutomaticGraphProcessingObserver(GraphView.GraphViewModel.GraphModelState, GraphView.ProcessOnIdleAgent.StateComponent, GraphTool.GraphProcessingState, GraphTool.Preferences);
                GraphTool?.ObserverManager.RegisterObserver(m_AutomaticGraphProcessingObserver);

                rootVisualElement.RegisterCallback<MouseMoveEvent>(GraphView.ProcessOnIdleAgent.OnMouseMove);
            }

#if UNITY_2022_2_OR_NEWER
            m_GraphProcessingStatusObserver = new GraphProcessingStatusObserver(m_GraphProcessingPendingLabel, null, GraphTool.GraphProcessingState);
#else
            m_GraphProcessingStatusObserver = new GraphProcessingStatusObserver(m_GraphProcessingPendingLabel, m_ErrorToolbar, GraphTool.GraphProcessingState);
#endif

            GraphTool?.ObserverManager.RegisterObserver(m_GraphProcessingStatusObserver);

#if UNITY_2022_2_OR_NEWER
            GraphViewStaticBridge.RebuildOverlays(this);
#endif
        }

        protected virtual void OnDisable()
        {
            UpdateWindowsWithSameCurrentGraph(true);

            GraphView.ProcessOnIdleAgent?.StopTimer();

            if (GraphTool != null)
            {
                GraphTool.ObserverManager.UnregisterObserver(m_AutomaticGraphProcessingObserver);
                GraphTool.ObserverManager.UnregisterObserver(m_GraphProcessingStatusObserver);
                GraphTool.Dispose();
            }

#if !UNITY_2022_2_OR_NEWER
            if (m_ErrorToolbar != null)
            {
                m_GraphView.Remove(m_ErrorToolbar);
                m_ErrorToolbar = null;
            }
#endif

            rootVisualElement.Remove(m_GraphContainer);

#if !UNITY_2022_2_OR_NEWER
            if (m_MainToolbar != null)
            {
                rootVisualElement.Remove(m_MainToolbar);
                m_MainToolbar = null;
            }
#endif

            m_GraphContainer = null;
            m_GraphView = null;
            m_BlankPage = null;
        }

        protected virtual void OnFocus()
        {
            s_LastFocusedEditor = GetInstanceID();

            if (m_Focused)
                return;

            if (rootVisualElement == null)
                return;

            // selection may have changed while Visual Scripting Editor was looking away
            OnGlobalSelectionChange();

            m_Focused = true;

            UpdateWindowsWithSameCurrentGraph(false);
        }

        /// <summary>
        /// Updates the focused windows and disables windows that have the same current graph as the focused window's.
        /// </summary>
        internal void UpdateWindowsWithSameCurrentGraph(bool currentWindowIsClosing)
        {
            var currentGraph = GraphTool?.ToolState?.GraphModel;
            if (currentGraph == null)
                return;

            if (GraphView != null && !GraphView.enabledSelf)
                GraphView.SetEnabled(true);

            var windows = (GraphViewEditorWindow[])Resources.FindObjectsOfTypeAll(GetType());
            var shouldUpdateFocusedWindow = false;

            foreach (var window in windows.Where(w => w.GetInstanceID() != s_LastFocusedEditor))
            {
                var otherGraph = window.GraphTool?.ToolState?.GraphModel;
                if (otherGraph != null && currentGraph == otherGraph)
                {
                    // Unfocused windows with the same graph are disabled
                    window.GraphView?.SetEnabled(false);

                    if (currentWindowIsClosing)
                    {
                        // If the current window is closing with changes, the changes need to be updated in other windows with the same graph to not lose the changes.
                        UpdateGraphModelState(window.GraphTool.State.AllStateComponents.OfType<GraphModelStateComponent>().FirstOrDefault());
                    }

                    shouldUpdateFocusedWindow = !currentWindowIsClosing;
                }
            }

            if (shouldUpdateFocusedWindow)
            {
                UpdateGraphModelState(GraphTool.State.AllStateComponents.OfType<GraphModelStateComponent>().FirstOrDefault());
            }

            static void UpdateGraphModelState(GraphModelStateComponent graphModelState)
            {
                if (graphModelState == null)
                    return;

                // Update the focused window
                using var updater = graphModelState.UpdateScope;
                updater.ForceCompleteUpdate();
            }
        }

        protected virtual void OnLostFocus()
        {
            m_Focused = false;
        }

        protected void OnInspectorUpdate()
        {
            GraphView?.ProcessOnIdleAgent?.Execute();
        }

        protected virtual void Update()
        {
            Profiler.BeginSample("GraphViewEditorWindow.Update");
            var sw = new Stopwatch();
            sw.Start();

            // PF FIXME To StateObserver, eventually
            UpdateGraphContainer();
            UpdateOverlays();

            GraphTool.Update();

            sw.Stop();

            if (GraphTool.Preferences.GetBool(BoolPref.LogUIBuildTime))
            {
                Debug.Log($"UI Update ({(GraphTool?.Dispatcher as CommandDispatcher)?.LastDispatchedCommandName ?? "Unknown command"}) took {sw.ElapsedMilliseconds} ms");
            }

            UpdateWindowTitle();

            Profiler.EndSample();
        }

        public void AdjustWindowMinSize(Vector2 size)
        {
            // Set the window min size from the graphView, adding the menu bar height
#if UNITY_2022_2_OR_NEWER
            minSize = new Vector2(size.x, size.y);
#else
            minSize = new Vector2(size.x, size.y + m_MainToolbar?.layout.height ?? 0);
#endif
        }

        void OnEnterPanel(AttachToPanelEvent e)
        {
            Selection.selectionChanged += OnGlobalSelectionChange;
            OnGlobalSelectionChange();
        }

        void OnLeavePanel(DetachFromPanelEvent e)
        {
            // ReSharper disable once DelegateSubtraction
            Selection.selectionChanged -= OnGlobalSelectionChange;
        }

        public virtual void AddItemsToMenu(GenericMenu menu)
        {
            var disabled = GraphTool.ToolState.GraphModel == null;

            m_LockTracker.AddItemsToMenu(menu, disabled);
        }

        public override IEnumerable<Type> GetExtraPaneTypes()
        {
            return Assembly
                .GetAssembly(typeof(GraphViewToolWindow))
                .GetTypes()
                .Where(myType => myType.IsClass && !myType.IsAbstract && myType.IsSubclassOf(typeof(GraphViewToolWindow)));
        }

        public static void ShowGraphViewWindowWithTools<T>() where T : GraphViewEditorWindow
        {
            var windows = GraphViewStaticBridge.ShowGraphViewWindowWithTools(typeof(GraphViewBlackboardWindow), typeof(GraphViewMinimapWindow), typeof(T));
            var graphView = (windows[0] as T)?.GraphViews.FirstOrDefault();
            if (graphView != null)
            {
                (windows[1] as GraphViewBlackboardWindow)?.SelectGraphViewFromWindow((windows[0] as T), graphView);
                (windows[2] as GraphViewMinimapWindow)?.SelectGraphViewFromWindow((windows[0] as T), graphView);
            }
        }

        protected void UpdateGraphContainer()
        {
            var graphModel = GraphTool?.ToolState.GraphModel;

            if (m_GraphContainer != null)
            {
                if (graphModel != null)
                {
                    if (m_GraphContainer.Contains(m_BlankPage))
                        m_GraphContainer.Remove(m_BlankPage);
                    if (!m_GraphContainer.Contains(m_GraphView))
                        m_GraphContainer.Insert(0, m_GraphView);
#if !UNITY_2022_2_OR_NEWER
                    if (!m_GraphContainer.Contains(m_SidePanel))
                        m_GraphContainer.Add(m_SidePanel);
#endif

                    if (!rootVisualElement.Contains(m_GraphProcessingPendingLabel))
                        rootVisualElement.Add(m_GraphProcessingPendingLabel);
                }
                else
                {
#if !UNITY_2022_2_OR_NEWER
                    if (m_GraphContainer.Contains(m_SidePanel))
                        m_GraphContainer.Remove(m_SidePanel);
#endif
                    if (m_GraphContainer.Contains(m_GraphView))
                        m_GraphContainer.Remove(m_GraphView);
                    if (!m_GraphContainer.Contains(m_BlankPage))
                        m_GraphContainer.Insert(0, m_BlankPage);
                    if (rootVisualElement.Contains(m_GraphProcessingPendingLabel))
                        rootVisualElement.Remove(m_GraphProcessingPendingLabel);
                }
            }
        }

        protected virtual void UpdateOverlays()
        {
#if UNITY_2022_2_OR_NEWER
            var graphModel = GraphTool?.ToolState.GraphModel;
            if (graphModel != null)
            {
                if (m_DisplayedOverlays != null)
                {
                    foreach (var overlayId in m_DisplayedOverlays)
                    {
                        if (TryGetOverlay(overlayId, out var overlay))
                        {
                            overlay.displayed = true;
                        }
                    }

                    m_DisplayedOverlays = null;
                }
            }
            else
            {
                if (m_DisplayedOverlays == null)
                {
                    m_DisplayedOverlays = new List<string>();
                    foreach (var overlay in this.GetAllOverlays())
                    {
                        if (overlay.displayed)
                        {
                            m_DisplayedOverlays.Add(overlay.id);
                            overlay.displayed = false;
                        }
                    }
                }
            }
#endif
        }

        void OnLockStateChanged(bool locked)
        {
            // Make sure that upon unlocking, any selection change is updated
            if (!locked)
                OnGlobalSelectionChange();
        }

        // DO NOT name this one "OnSelectionChange", which is a magical unity function name
        // and would automatically call this method when the selection changes.
        // we want more granular control and register it manually
        void OnGlobalSelectionChange()
        {
            // if we're in Locked mode, keep current selection
            if (Locked)
                return;

            foreach (var onboardingProvider in m_BlankPage?.OnboardingProviders ?? Enumerable.Empty<OnboardingProvider>())
            {
                if (onboardingProvider.GetGraphAndObjectFromSelection(GraphTool.ToolState, Selection.activeObject, out var graphAsset, out var boundObject))
                {
                    SetCurrentSelection(graphAsset, OpenMode.Open, boundObject);
                    return;
                }
            }

            if (GraphTool?.ToolState?.GraphModel == null && Selection.activeObject is IGraphAsset graph && CanHandleAssetType(graph))
            {
                SetCurrentSelection(graph, OpenMode.Open);
            }
        }

        public void SetCurrentSelection(IGraphAsset graphAsset, OpenMode mode, GameObject boundObject = null)
        {
            var windows = (GraphViewEditorWindow[])Resources.FindObjectsOfTypeAll(typeof(GraphViewEditorWindow));

            // Only the last focused editor should try to answer a change to the current selection.
            if (s_LastFocusedEditor != GetInstanceID() && windows.Length > 1)
                return;

            var currentOpenedGraph = GraphTool?.ToolState.CurrentGraph ?? default;
            // don't load if same graph and same bound object
            if (GraphTool?.ToolState.GraphModel != null &&
                graphAsset == currentOpenedGraph.GetGraphAsset() &&
                currentOpenedGraph.BoundObject == boundObject)
                return;

            // If there is no graph asset, unload the current one.
            if (graphAsset.GraphModel == null)
            {
                return;
            }

            // Load this graph asset.
            GraphTool?.Dispatch(new LoadGraphCommand(graphAsset.GraphModel, boundObject));

            UpdateWindowTitle();

            if (mode != OpenMode.OpenAndFocus)
                return;

            // Check if an existing window already has this asset, if yes give it the focus.
            foreach (var window in windows)
            {
                if (window.GraphTool.ToolState.GraphModel == graphAsset.GraphModel)
                {
                    window.Focus();
                    return;
                }
            }
        }

        /// <summary>
        /// Indicates if the graphview window can handle the given <paramref name="asset"/>.
        /// </summary>
        /// <param name="asset">The asset we want to know if hte window handles</param>
        /// <returns>True if the window can handle the givne <paramref name="asset"/>. False otherwise.</returns>
        protected abstract bool CanHandleAssetType(IGraphAsset asset);

        static void SetupLogStickyCallback()
        {
            ConsoleWindowBridge.SetEntryDoubleClickedDelegate((file, _) =>
            {
                var pathAndGuid = file.Split('@');

                var window = Resources.FindObjectsOfTypeAll(typeof(GraphViewEditorWindow)).OfType<GraphViewEditorWindow>().FirstOrDefault(w =>
                    w.GraphTool.ToolState.CurrentGraph.GetGraphAssetPath() == pathAndGuid[0] ||
                    w.GraphTool.ToolState.SubGraphStack.FirstOrDefault().GetGraphAssetPath() == pathAndGuid[0]);

                if (window != null)
                {
                    window.Focus();
                    var guid = new SerializableGUID(pathAndGuid[1]);
                    if (window.GraphView.GraphModel.TryGetModelFromGuid(guid, out var nodeModel))
                    {
                        var graphElement = nodeModel.GetView<GraphElement>(window.GraphView);
                        if (graphElement != null)
                        {
                            window.GraphView.DispatchFrameAndSelectElementsCommand(true, graphElement);
                        }
                    }
                }
            });
        }

        // Internal for tests
        internal void UpdateWindowTitle()
        {
            var initialAsset = GraphTool?.ToolState?.SubGraphStack?.FirstOrDefault().GetGraphAssetWithoutLoading();
            var currentAsset = GraphTool?.ToolState?.CurrentGraph.GetGraphAssetWithoutLoading();

            var initialAssetName = (initialAsset as Object == null) ? "" : initialAsset.Name;
            var currentAssetName = (currentAsset as Object == null) ? "" : currentAsset.Name;
            var initialAssetDirty = (initialAsset as Object == null) ? false : initialAsset.Dirty;
            var currentAssetDirty = (currentAsset as Object == null) ? false : currentAsset.Dirty;

            var formattedTitle = FormatWindowTitle(initialAssetName, currentAssetName, initialAssetDirty, currentAssetDirty, out var toolTip);
            titleContent = new GUIContent(formattedTitle, GraphTool?.Icon, toolTip);
        }

        string FormatWindowTitle(string initialAssetName, string currentAssetName, bool initialAssetIsDirty, bool currentAssetIsDirty, out string completeTitle)
        {
            if (string.IsNullOrEmpty(initialAssetName) && string.IsNullOrEmpty(currentAssetName))
            {
                completeTitle = GraphTool?.Name ?? "";
                return completeTitle;
            }

            const int maxLength = 20; // Maximum limit of characters in a window primary tab
            const string ellipsis = "...";
            var currentAssetDirtyStr = currentAssetIsDirty ? "*" : "";
            if (string.IsNullOrEmpty(initialAssetName))
            {
                var expectedLength = maxLength - ellipsis.Length - (currentAssetIsDirty ? 1 : 0); // The max length for the window title without the ellipsis and the dirty flag
                currentAssetName = currentAssetName.Length > maxLength ? currentAssetName.Substring(0, expectedLength) + ellipsis : currentAssetName;
                completeTitle = currentAssetName + currentAssetDirtyStr;
                return completeTitle;
            }

            var initialAssetDirtyStr = initialAssetIsDirty ? "*" : "";

            // In the case the current graph is a subgraph, the window primary tab's naming should follow this format: (InitialAssetName...*) CurrentAssetName...*
            completeTitle = $"({initialAssetName}{initialAssetDirtyStr}) {currentAssetName}{currentAssetDirtyStr}";
            if (completeTitle.Length <= maxLength)
                return completeTitle;

            var dirtyCount = currentAssetIsDirty ? 1 : 0;
            if (initialAssetIsDirty)
                dirtyCount++;

            var otherCharactersLength = 9 + dirtyCount; // Other characters that are not letters in the naming format: parenthesis, dirty flag, ellipsis in (InitialAssetName...*) CurrentAssetName...*
            var actualLength = (initialAssetName + currentAssetName).Length + otherCharactersLength;

            const int minCurrentAssetNameLength = 5;
            var excessLength = actualLength - maxLength;
            if (currentAssetName.Length - excessLength >= minCurrentAssetNameLength)
            {
                currentAssetName = currentAssetName.Substring(0, currentAssetName.Length - excessLength) + ellipsis;
            }
            else
            {
                var availableLength = maxLength - otherCharactersLength;
                var expectedInitialAssetNameLength = availableLength - currentAssetName.Length;
                if (currentAssetName.Length > minCurrentAssetNameLength)
                {
                    currentAssetName = currentAssetName.Substring(0, minCurrentAssetNameLength);
                    expectedInitialAssetNameLength = availableLength - currentAssetName.Length;
                    currentAssetName += ellipsis;
                }

                initialAssetName = initialAssetName.Length > expectedInitialAssetNameLength ? initialAssetName.Substring(0, expectedInitialAssetNameLength) + ellipsis : initialAssetName;
            }

            return $"({initialAssetName}{initialAssetDirtyStr}) {currentAssetName}{currentAssetDirtyStr}";
        }
    }
}
