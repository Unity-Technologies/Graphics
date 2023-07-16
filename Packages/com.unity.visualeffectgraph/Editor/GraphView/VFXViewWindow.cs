#define USE_EXIT_WORKAROUND_FOGBUGZ_1062258
using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using UnityEditor.Experimental.GraphView;

using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.UIElements;


namespace UnityEditor.VFX.UI
{
    [Serializable]
    class VFXViewWindow : EditorWindow
    {
        private static Dictionary<Tuple<Type, bool>, string> vfxIconMap = new()
        {
            { new Tuple<Type, bool>(typeof(VisualEffectSubgraphOperator), true), "d_subgraph-operator.png" },
            { new Tuple<Type, bool>(typeof(VisualEffectSubgraphBlock), true), "d_subgraph-block.png" },
            { new Tuple<Type, bool>(typeof(VFXGraph), true), "vfx_graph_icon_gray_dark.png" },
            { new Tuple<Type, bool>(typeof(VisualEffectSubgraphOperator), false), "subgraph-operator.png" },
            { new Tuple<Type, bool>(typeof(VisualEffectSubgraphBlock), false), "subgraph-block.png" },
            { new Tuple<Type, bool>(typeof(VFXGraph), false), "vfx_graph_icon_gray_light.png" },
        };

        static List<VFXViewWindow> s_VFXWindows = new();

        ShortcutHandler m_ShortcutHandler;
        VisualEffect m_pendingAttachment;

        void OnEnable()
        {
            s_VFXWindows.Add(this);
        }

        protected void SetupFramingShortcutHandler(VFXView view)
        {
            m_ShortcutHandler = new ShortcutHandler(
                new Dictionary<Event, ShortcutDelegate>
                {
                    { Event.KeyboardEvent("a"), view.FrameAll },
                    { Event.KeyboardEvent("f"), view.FrameSelection },
                    { Event.KeyboardEvent("o"), view.FrameOrigin },
                    { Event.KeyboardEvent("^#>"), view.FramePrev },
                    { Event.KeyboardEvent("^>"), view.FrameNext },
                    { Event.KeyboardEvent("F7"), view.OnCompile },
                    { Event.KeyboardEvent("#d"), view.OutputToDot },
                    { Event.KeyboardEvent("^&d"), view.DuplicateSelectionWithEdges },
                    { Event.KeyboardEvent("^#d"), view.OutputToDotReduced },
                    { Event.KeyboardEvent("#c"), view.OutputToDotConstantFolding },
                    { Event.KeyboardEvent("^r"), view.ReinitComponents },
                    { Event.KeyboardEvent("F5"), view.ReinitComponents },
                    { Event.KeyboardEvent("#^r"), view.ReinitAndPlayComponents },
                    { Event.KeyboardEvent("#F5"), view.ReinitAndPlayComponents },
                });
        }

        [MenuItem("Window/Visual Effects/Visual Effect Graph", false, 3011)]
        public static void ShowWindow()
        {
            VFXLibrary.LogUnsupportedSRP();

            GetWindow((VisualEffectResource)null, true);
        }

        public static VFXViewWindow GetWindow(VisualEffectAsset vfxAsset, bool createIfNeeded = false)
        {
            return GetWindowLambda(x => x.displayedResource?.asset == vfxAsset, createIfNeeded, true);
        }

        public static VFXViewWindow GetWindow(VFXGraph vfxGraph, bool createIfNeeded = false, bool show = true)
        {
            return GetWindowLambda(
                x => x.displayedResource == vfxGraph?.visualEffectResource,
                createIfNeeded,
                show);
        }

        public static VFXViewWindow GetWindow(VisualEffectResource resource, bool createIfNeeded = false, bool show = true)
        {
            return GetWindowLambda(x => x.graphView?.controller?.graph.visualEffectResource == resource, createIfNeeded, show);
        }

        public static VFXViewWindow GetWindow(VFXParameter vfxParameter, bool createIfNeeded = false)
        {
            return GetWindowLambda(
                x => x.graphView?.controller?.parameterControllers.Any(y => y.model == vfxParameter) == true,
                createIfNeeded,
                true);
        }

        public static void RefreshErrors(VFXModel model)
        {
            if (model != null &&
                model.GetGraph() is { } graph &&
                GetWindow(graph, false, false) is { } window &&
                window.graphView != null &&
                window.graphView.controller != null)
            {
                window.graphView.RefreshErrors(model);
            }
        }

        static VFXViewWindow GetWindowLambda(Func<VFXViewWindow, bool> func, bool createIfNeeded, bool show)
        {
            var windows = GetAllWindows();
            var window = windows.SingleOrDefault(func);
            if (window == null)
            {
                // Get the empty VFX window if it's opened
                window = windows.SingleOrDefault(x => x.m_DisplayedResource == null);
            }

            if (window == null && createIfNeeded)
            {
                window = CreateWindow();
            }

            if (window != null && show)
            {
                window.Show(true);
                window.Focus();
            }

            return window;
        }

        public static VFXViewWindow GetWindowNoShow(VFXView vfxView) => GetWindow(vfxView.controller?.graph, false, false);
        public static VFXViewWindow GetWindow(VFXView vfxView) => GetWindow(vfxView.controller?.graph);
        public static ReadOnlyCollection<VFXViewWindow> GetAllWindows() => s_VFXWindows.AsReadOnly();

        public static bool CloseIfNotLast(VFXView vfxView)
        {
            var noAssetWindows = s_VFXWindows.Where(x => x.graphView?.controller?.graph == null).ToArray();
            if (noAssetWindows.Length > 1)
            {
                var window = noAssetWindows.Single(x => x.graphView == vfxView);
                window.Close();
                return true;
            }

            return false;
        }

        public VFXView graphView { get; private set; }
        public VisualEffectResource displayedResource => m_DisplayedResource;

        public void UpdateTitle(string assetPath)
        {
            titleContent.text = Path.GetFileNameWithoutExtension(assetPath);
        }

        public void UpdateHistory()
        {
            m_ResourceHistory.RemoveAll(x => x == null);
            if (graphView != null)
                graphView.UpdateIsSubgraph();
        }

        public void LoadAsset(VisualEffectAsset asset, VisualEffect effectToAttach)
        {
            VFXLibrary.LogUnsupportedSRP();

            string assetPath = AssetDatabase.GetAssetPath(asset);

            VisualEffectResource resource = VisualEffectResource.GetResourceAtPath(assetPath);

            //Transitionning code
            if (resource == null)
            {
                resource = new VisualEffectResource();
                resource.SetAssetPath(AssetDatabase.GetAssetPath(asset));
            }

            LoadResource(resource, effectToAttach);
        }

        public void LoadResource(VisualEffectResource resource, VisualEffect effectToAttach = null)
        {
            if (graphView?.controller == null || graphView.controller.model != resource)
            {
                InternalLoadResource(resource);
            }

            var asset = effectToAttach == null ? m_pendingAttachment : effectToAttach;
            graphView?.TryAttachTo(asset, true);

            titleContent.text = resource.name;

            UpdateIcon(resource);
        }

        VisualEffect GetVisualEffectFromID(int id) => EditorUtility.InstanceIDToObject(id) as VisualEffect;

        internal void AttachTo(VisualEffect visualEffect)
        {
            if (graphView != null && !graphView.locked)
            {
                graphView.TryAttachTo(visualEffect, true);
                SaveChanges();
            }
            else if (visualEffect.visualEffectAsset == m_DisplayedResource.asset)
            {
                m_pendingAttachment = visualEffect;
            }
        }

        internal void DetachIfDeleted()
        {
            if (graphView != null && graphView.attachedComponent == null)
            {
                graphView.Detach();
                SaveChanges();
            }
        }

        List<VisualEffectResource> m_ResourceHistory = new();

        public IEnumerable<VisualEffectResource> resourceHistory
        {
            get { return m_ResourceHistory; }
        }

        public void PushResource(VisualEffectResource resource)
        {
            if (graphView.controller == null || graphView.controller.model != resource)
            {
                m_ResourceHistory.Add(m_DisplayedResource);
                InternalLoadResource(resource);
            }
        }

        void InternalLoadResource(VisualEffectResource resource)
        {
            m_DisplayedResource = resource;
            graphView.controller = VFXViewController.GetController(resource, true);
            graphView.UpdateGlobalSelection();
            graphView.FrameNewController();
            graphView.UpdateIsSubgraph();
            UpdateIcon(resource);
        }

        void UpdateIcon(VisualEffectResource resource)
        {
            var iconFilePath = vfxIconMap[new Tuple<Type, bool>(resource.isSubgraph ? resource.subgraph.GetType() : resource.graph.GetType(), EditorGUIUtility.isProSkin)];
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>($"{VisualEffectAssetEditorUtility.editorResourcesPath}/VFX/{iconFilePath}");
            titleContent.image = icon;
        }

        public bool CanPopResource()
        {
            return m_ResourceHistory.Any();
        }

        public void PopResource()
        {
            if (CanPopResource())
            {
                var resource = m_ResourceHistory.Last();
                if (resource != null)
                {
                    var window = VFXViewWindow.GetWindow(resource);
                    if (window != null)
                    {
                        window.Focus();
                    }
                    else
                    {
                        LoadResource(resource);
                        m_ResourceHistory.Remove(resource);
                        graphView.UpdateIsSubgraph();
                    }
                }
            }
        }

        protected void CreateGUI()
        {
            VFXManagerEditor.CheckVFXManager();

            graphView = new VFXView();
            graphView.StretchToParentSize();
            SetupFramingShortcutHandler(graphView);

            rootVisualElement.Add(graphView);

            autoCompile = true;
            autoReinit = true;

            graphView.RegisterCallback<AttachToPanelEvent>(OnEnterPanel);
            graphView.RegisterCallback<DetachFromPanelEvent>(OnLeavePanel);

            if (rootVisualElement.panel != null)
            {
                rootVisualElement.AddManipulator(m_ShortcutHandler);
            }

#if USE_EXIT_WORKAROUND_FOGBUGZ_1062258
            EditorApplication.wantsToQuit += Quitting_Workaround;
#endif

            if (graphView?.controller == null && m_DisplayedResource != null)
            {
                LoadResource(m_DisplayedResource);
            }

            if (titleContent.image == null)
            {
                var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(VisualEffectAssetEditorUtility.editorResourcesPath + "/VFX/"
                    + (EditorGUIUtility.isProSkin ? "vfx_graph_icon_gray_dark.png" : "vfx_graph_icon_gray_light.png"));
                titleContent.image = icon;
            }
            graphView?.OnFocus();
        }

#if USE_EXIT_WORKAROUND_FOGBUGZ_1062258
        private bool Quitting_Workaround()
        {
            if (graphView != null)
                graphView.controller = null;
            return true;
        }

#endif

        protected void OnDestroy()
        {
            s_VFXWindows.Remove(this);
#if USE_EXIT_WORKAROUND_FOGBUGZ_1062258
            EditorApplication.wantsToQuit -= Quitting_Workaround;
#endif

            if (graphView != null)
            {
                graphView.UnregisterCallback<AttachToPanelEvent>(OnEnterPanel);
                graphView.UnregisterCallback<DetachFromPanelEvent>(OnLeavePanel);
                graphView.Dispose();
                graphView = null;
            }
        }

        static VFXViewWindow CreateWindow()
        {
            var lastVFXWindow = GetAllWindows().LastOrDefault();

            var window = CreateInstance<VFXViewWindow>();

            if (!TryToTabNextTo(lastVFXWindow, window))
            {
                TryToTabNextTo(GetWindowDontShow<SceneView>(), window);
            }

            return window;
        }

        static bool TryToTabNextTo(EditorWindow nextToWindow, EditorWindow window)
        {
            if (nextToWindow?.m_Parent is DockArea dockArea)
            {
                var index = dockArea.m_Panes.IndexOf(nextToWindow);
                dockArea.AddTab(index + 1, window);

                return true;
            }

            return false;
        }

        void OnEnterPanel(AttachToPanelEvent e)
        {
            rootVisualElement.AddManipulator(m_ShortcutHandler);
        }

        void OnLeavePanel(DetachFromPanelEvent e)
        {
            rootVisualElement.RemoveManipulator(m_ShortcutHandler);
        }

        void OnFocus()
        {
            if (graphView != null) // OnFocus can be somehow called before OnEnable
                graphView.OnFocus();
        }

        public void OnVisualEffectComponentChanged(IEnumerable<VisualEffect> componentChanged)
        {
            if (graphView != null)
                graphView.OnVisualEffectComponentChanged(componentChanged);
        }

        public bool autoCompile { get; set; }
        public bool autoReinit { get; set; }
        public float autoReinitPrewarmTime { get; set; }

        void Update()
        {
            if (graphView == null && m_DisplayedResource == null)
                return;

            VFXViewController controller = graphView.controller;
            var filename = "No Asset";
            if (controller != null)
            {
                controller.NotifyUpdate();
                if (controller.model != null)
                {
                    var graph = controller.graph;
                    if (graph != null)
                    {
                        filename = controller.name;

                        if (EditorUtility.IsDirty(graph))
                        {
                            filename += "*";
                        }

                        if (autoCompile && graph.IsExpressionGraphDirty() && !graph.GetResource().isSubgraph)
                        {
                            VFXGraph.explicitCompile = true;
                            graphView.errorManager.ClearAllErrors(null, VFXErrorOrigin.Compilation);
                            using (var reporter = new VFXCompileErrorReporter(graphView.errorManager))
                            {
                                VFXGraph.compileReporter = reporter;
                                AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(graphView.controller.model));
                                graph.SetExpressionGraphDirty(
                                    false); // As are implemented subgraph now, compiling dependents chain can reset dirty flag on used subgraphs, which will make an infinite loop, this is bad!
                                VFXGraph.compileReporter = null;
                            }

                            VFXGraph.explicitCompile = false;
                        }
                        else
                            graph.RecompileIfNeeded(true, true);

                        bool wasDirty = graph.IsExpressionGraphDirty();

                        controller.RecompileExpressionGraphIfNeeded();

                        // Hack to avoid infinite recompilation due to UI triggering a recompile TODO: Fix problematic cases that trigger that error
                        if (!wasDirty && graph.IsExpressionGraphDirty())
                        {
                            Debug.LogError(
                                "Expression graph was marked as dirty after compiling context for UI. Discard to avoid infinite compilation loop.");
                            graph.SetExpressionGraphDirty(false);
                        }
                    }
                }
                else
                {
                    m_DisplayedResource = null;
                }
            }

            if (VFXViewModificationProcessor.assetMoved)
            {
                graphView.AssetMoved();
                VFXViewModificationProcessor.assetMoved = false;
            }

            titleContent.text = filename;
        }

        [SerializeField]
        VisualEffectResource m_DisplayedResource;
    }
}
