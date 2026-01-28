#if ENABLE_UIELEMENTS_MODULE && (UNITY_EDITOR || DEVELOPMENT_BUILD)
#define ENABLE_RENDERING_DEBUGGER_UI
#endif

using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.Rendering.Analytics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering
{
    struct WidgetChangedAction<T>
    {
        public string query_path;
        public T previous_value;
        public T new_value;
    }

    [CoreRPHelpURL("Rendering-Debugger")]
    sealed partial class DebugWindow : EditorWindowWithHelpButton
#if ENABLE_RENDERING_DEBUGGER_UI
        , IHasCustomMenu
#endif
    {
        internal static GUIContent s_TitleContent = EditorGUIUtility.TrTextContent("Rendering Debugger");

        [MenuItem("Window/Analysis/Rendering Debugger", priority = 10005)]
        static void Init()
        {
            var window = GetWindow<DebugWindow>();
            window.titleContent = s_TitleContent;
            window.minSize = new Vector2(800f, 300f);
        }

        [MenuItem("Window/Analysis/Rendering Debugger", validate = true)]
        static bool ValidateMenuItem()
        {
            return RenderPipelineManager.currentPipeline != null;
        }

        public void CreateGUI()
        {
#if ENABLE_RENDERING_DEBUGGER_UI
            RecreateGUI();

            UpdateWidgetStates();
#else
            var helpBox = new HelpBox(
                "UIElements Module is disabled. In order to use Rendering Debugger, enable the module in Package Manager > Built-in. ",
                HelpBoxMessageType.Info);
            helpBox.buttonText = "Open in Package Manager";
            helpBox.onButtonClicked += () => PackageManager.UI.Window.Open("com.unity.modules.uielements");
            rootVisualElement.Add(helpBox);
#endif
        }

#if ENABLE_RENDERING_DEBUGGER_UI
        [SerializeField]
        string m_SelectedPanelName;

        DebugUI.Panel m_SelectedPanel;
        DebugUI.Panel selectedPanel
        {
            get => m_SelectedPanel;
            set
            {
                m_SelectedPanel = value;
                m_SelectedPanelName = m_SelectedPanel?.displayName;
            }
        }

        VisualElement m_LeftPaneElement;
        VisualElement m_RightPaneElement;

        const string k_UssCommon = "Packages/com.unity.render-pipelines.core/Runtime/DEbugging/Runtime UI Resources/DebugWindowCommon.uss";
        const string k_Uss = "Packages/com.unity.render-pipelines.core/Editor/Debugging/DebugWindow.uss";
        const string k_Uxml = "Packages/com.unity.render-pipelines.core/Editor/Debugging/DebugWindow.uxml";

        bool m_IsDirty;

        Vector2 m_PanelScroll;
        Vector2 m_ContentScroll;

        void OnEnable()
        {
            DebugManager.instance.displayEditorUI = true;

            DebugManager.instance.refreshEditorRequested = false;

            hideFlags = HideFlags.HideAndDontSave;
            autoRepaintOnSceneChange = true;

            if (m_WidgetStates == null || !AreWidgetStatesValid())
                m_WidgetStates = new WidgetStateDictionary();
            if (s_WidgetStateMap == null || s_TypeMapDirty)
                RebuildTypeMaps();

            DebugManager.instance.onSetDirty += MarkDirty;

            GraphicsToolLifetimeAnalytic.WindowOpened<DebugWindow>();

            HookLegacyWidgetStateHandlingCallbacks();
            HookValueChangedAnalytics();
        }

        // We use reflection to hook analytics to the onWidgetValueChangedAnalytic callback. The callback itself is required here because
        // GraphicsToolUsageAnalytic is in the editor assembly but all widgets are in runtime. Reflection is used because we want to
        // also hook analytics for any custom user widgets derived from public DebugUI.Field<T>.
        void HookValueChangedAnalytics()
        {
            var allFieldTypes = TypeCache.GetTypesDerivedFrom(typeof(DebugUI.Field<>));
            foreach (var fieldType in allFieldTypes)
            {
                try
                {
                    var genericArgs = fieldType.BaseType.GetGenericArguments();
                    if (fieldType.IsAbstract || genericArgs.Length == 0)
                        continue;

                    var field = fieldType.GetField(nameof(DebugUI.Field<int>.onWidgetValueChangedAnalytic), BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                    var method = GetType().GetMethod(nameof(SendWidgetValueChangedAnalytic), BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                    var genericArg = fieldType.BaseType.GetGenericArguments()[0];
                    var genericMethod = method.MakeGenericMethod(genericArg);
                    var delegateType = typeof(Action<,,>).MakeGenericType(typeof(string), genericArg, genericArg);
                    var callback = Delegate.CreateDelegate(delegateType, null, genericMethod);
                    field.SetValue(null, callback);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"Failed to hook analytics for {fieldType}: {ex.Message}");
                }
            }
        }

        // Store timestamps to throttle event sending
        static readonly Dictionary<string, float> s_SentAnalyticsTimestamps = new ();

        static void SendWidgetValueChangedAnalytic<T>(string queryPath, T previousValue, T newValue)
        {
            if (queryPath == null)
                return;

            const float kMaxSendRateSeconds = 0.5f;
            float now = (float)EditorApplication.timeSinceStartup;
            if (s_SentAnalyticsTimestamps.TryGetValue(queryPath, out float lastSentAt) && now - lastSentAt < kMaxSendRateSeconds)
                return;

            s_SentAnalyticsTimestamps[queryPath] = now;

            var analytic = new List<WidgetChangedAction<T>> { new()
            {
                query_path = queryPath,
                previous_value = previousValue,
                new_value = newValue
            } };
            GraphicsToolUsageAnalytic.ActionPerformed<DebugWindow>("Widget Value Changed", analytic.ToNestedColumn());
        }

        // Note: this won't get called if the window is opened when the editor itself is closed
        void OnDestroy()
        {
            // Note: In the case where the window is maximized/unmaximized, OnEnable for the new window gets called *before* OnDestroy.
            //       Therefore you need to be careful with statics/globals. In this case, we only mark displayEditorUI as false if we are
            //       closing the only/last DebugWindow instance.
            if (Resources.FindObjectsOfTypeAll(typeof(DebugWindow)).Length == 0)
                DebugManager.instance.displayEditorUI = false;

            DebugManager.instance.onSetDirty -= MarkDirty;

            DestroyWidgetStates();
        }

        private void OnDisable()
        {
            GraphicsToolLifetimeAnalytic.WindowClosed<DebugWindow>();
        }

        void MarkDirty()
        {
            m_IsDirty = true;
        }

        void Update()
        {
            // If the render pipeline asset has been reloaded we force-refresh widget states in case
            // some debug values need to be refresh/recreated as well (e.g. frame settings on HD)
            if (DebugManager.instance.refreshEditorRequested)
            {
                ReloadWidgetStates();
                m_IsDirty = true;
                DebugManager.instance.refreshEditorRequested = false;
            }

            string requestedPanel = DebugManager.instance.GetRequestedEditorWindowPanel();
            if (requestedPanel != null)
            {
                SetSelectedPanel(requestedPanel);
            }

            if (m_IsDirty)
            {
                m_IsDirty = false;
                RecreateGUI();
            }
        }

        private void RecreateGUI()
        {
            rootVisualElement.Clear();

            var panels = DebugManager.instance.panels;

            // Adding all panels that are not inactive in editor and have at least one active child
            var activePanels = new List<DebugUI.Panel>();
            foreach (var panel in panels)
            {
                if (!panel.isInactiveInEditor)
                {
                    foreach (var child in panel.children)
                    {
                        if (!child.isInactiveInEditor)
                        {
                            activePanels.Add(panel);
                            break;
                        }
                    }
                }
            }

            if (activePanels.Count == 0)
            {
                rootVisualElement.Add(new HelpBox("No debug items registered. Make sure a Render Pipeline Asset is assigned in Quality Settings.", HelpBoxMessageType.Info));
                return;
            }

            var windowUxml = EditorGUIUtility.LoadRequired(k_Uxml) as VisualTreeAsset;
            var commonUss = EditorGUIUtility.LoadRequired(k_UssCommon) as StyleSheet;
            var windowUss = EditorGUIUtility.LoadRequired(k_Uss) as StyleSheet;

            if (commonUss == null || windowUss == null || windowUxml == null)
                throw new InvalidOperationException("Unable to find required UXML and USS files");

            rootVisualElement.styleSheets.Add(commonUss);
            rootVisualElement.styleSheets.Add(windowUss);
            windowUxml.CloneTree(rootVisualElement);

            m_LeftPaneElement = rootVisualElement.Q<VisualElement>(name: "tabs-insertion-element");
            m_RightPaneElement = rootVisualElement.Q<VisualElement>(name: "panels-inspector-insertion-element");

            if (m_LeftPaneElement == null || m_RightPaneElement == null)
                throw new InvalidOperationException("Unable to find required insertion Visual Elements");

            m_LeftPaneElement.Clear();
            m_RightPaneElement.Clear();

            var resetButton = rootVisualElement.Q<ToolbarButton>(name: "btn-reset");
            resetButton.clicked -= ResetClicked;
            resetButton.clicked += ResetClicked;

            var uiPanels = DebugUIExtensions.CreatePanels(activePanels, DebugUI.Context.Editor);

            foreach (var (tab, panel)  in uiPanels)
            {
                panel.style.display = DisplayStyle.None;
                tab.RegisterCallback<ClickEvent>(_ => SetSelectedPanel(tab.text));
                m_LeftPaneElement.Add(tab);
                m_RightPaneElement.Add(panel);
            }

            string selectedPanelName = m_SelectedPanelName;
            if (string.IsNullOrEmpty(selectedPanelName) || m_LeftPaneElement.Q<Label>(name: m_SelectedPanelName + "_Tab") == null)
            {
                // No selected panel, or selected panel is not existing anymore, pick the first
                if (m_LeftPaneElement.childCount > 0 && m_LeftPaneElement[0] is Label firstLabel)
                    selectedPanelName = firstLabel.text;
            }
            SetSelectedPanel(selectedPanelName);

            // When the window is docked/undocked, this ensures the schedulers are re-enabled
            rootVisualElement.RegisterCallback<AttachToPanelEvent>(_ =>
            {
                // The schedulers themselves are created in AttachToPanelEvent so we need to delay to ensure this has happened.
                EditorApplication.delayCall += () => SetSelectedPanel(m_SelectedPanelName);
            });
        }

        void ResetClicked()
        {
            DebugManager.instance.Reset();
            RecreateGUI();

            DebugDisplaySerializer.LoadFoldoutStates();
        }

        internal void SetSelectedPanel(string panelName) // internal for tests
        {
            if (string.IsNullOrEmpty(panelName))
                return;

            if (selectedPanel != null)
            {
                m_LeftPaneElement.Q<VisualElement>(name: $"{selectedPanel.displayName}_Tab")?.RemoveFromClassList("selected");
                if (m_RightPaneElement.Q<VisualElement>(name: $"{selectedPanel.displayName}_Content") is { } previousContent)
                    previousContent.style.display = DisplayStyle.None;

                DebugManager.instance.schedulerTracker.SetHierarchyEnabled(DebugUI.Context.Editor, selectedPanel, false);
            }

            selectedPanel = DebugManager.instance.GetPanel(panelName);

            if (selectedPanel != null)
            {
                m_LeftPaneElement.Q<VisualElement>(name: $"{selectedPanel.displayName}_Tab")?.AddToClassList("selected");
                if (m_RightPaneElement.Q<VisualElement>(name: $"{selectedPanel.displayName}_Content") is { } newContent)
                    newContent.style.display = DisplayStyle.Flex;

                DebugManager.instance.schedulerTracker.SetHierarchyEnabled(DebugUI.Context.Editor, selectedPanel, true);
            }
        }

        public void AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(EditorGUIUtility.TrTextContent("Expand All"), false, () => SetExpanded(true));
            menu.AddItem(EditorGUIUtility.TrTextContent("Collapse All"), false, () => SetExpanded(false));
        }

        void SetExpanded(bool expanded)
        {
            DebugManager.instance.ForEachWidget(widget =>
            {
                if (widget is DebugUI.Foldout foldout)
                    foldout.opened = expanded;
            });
        }
#endif
    }
}
