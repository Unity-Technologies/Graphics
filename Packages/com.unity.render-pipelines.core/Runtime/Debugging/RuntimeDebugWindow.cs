#if ENABLE_UIELEMENTS_MODULE && (UNITY_EDITOR || DEVELOPMENT_BUILD)
#define ENABLE_RENDERING_DEBUGGER_UI
#endif

#if ENABLE_RENDERING_DEBUGGER_UI
using System.Collections.Generic;
using UnityEngine.UIElements;

namespace UnityEngine.Rendering
{
    [AddComponentMenu("")] // Hide from Add Component menu
    class RuntimeDebugWindow : MonoBehaviour
    {
        UIDocument m_Document;
        VisualElement m_PanelRootElement;
        TabView m_TabViewElement;

        DebugUI.Panel m_SelectedPanel;
        bool m_PortraitOrientation;
        bool m_IsDirty;

        void Awake()
        {
            DebugManager.instance.onSetDirty -= RequestRecreateGUI;
            DebugManager.instance.onSetDirty += RequestRecreateGUI;

            RecreateGUI();
        }

        internal void RecreateGUI()
        {
            var resources = GraphicsSettings.GetRenderPipelineSettings<RenderingDebuggerRuntimeResources>();
            if (m_Document == null)
                m_Document = gameObject.AddComponent<UIDocument>();
            m_Document.panelSettings = resources.panelSettings;
            m_Document.visualTreeAsset = resources.visualTreeAsset;

            var rootVisualElement = m_Document.rootVisualElement;
            m_PanelRootElement = rootVisualElement.panel.visualTree;
            var styleSheets = resources.styleSheets;
            foreach (var uss in styleSheets)
                m_PanelRootElement.styleSheets.Add(uss);

            UpdateOrientation(forceUpdate: true);

            m_TabViewElement = rootVisualElement.Q<TabView>(name: "debug-window-tabview");
            m_TabViewElement.Clear();

            var resetButton = rootVisualElement.Q<Button>(name: "btn-reset");
            resetButton.clicked -= ResetClicked;
            resetButton.clicked += ResetClicked;

            var panels = DebugManager.instance.panels;
            var activePanels = new List<DebugUI.Panel>();

            // Filter out editor only panels and panels with no active children
            foreach (var panel in panels)
            {
                bool isEditorOnlyPanel = panel.isEditorOnly;
                bool hasVisibleChildren = false;
                foreach (var w in panel.children)
                {
                    if (!w.isEditorOnly && !w.isHidden)
                    {
                        hasVisibleChildren = true;
                        break;
                    }
                }

                if (!isEditorOnlyPanel && hasVisibleChildren)
                {
                    activePanels.Add(panel);
                }
            }

            bool hasDebugItems = activePanels.Count > 0;
            m_Document.rootVisualElement.Q<HelpBox>(name: "no-debug-items-message").style.display = hasDebugItems ? DisplayStyle.None : DisplayStyle.Flex;
            m_Document.rootVisualElement.Q<VisualElement>(name: "content-container").style.display = hasDebugItems ? DisplayStyle.Flex : DisplayStyle.None;

            if (!hasDebugItems)
                return;

            var uiPanels = DebugUIExtensions.CreatePanels(activePanels, DebugUI.Context.Runtime);

            foreach (var (tabLabel, panel) in uiPanels)
            {
                ScrollView scrollView = new ScrollView();
                scrollView.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
                scrollView.verticalScroller.slider.focusable = false;
                scrollView.Add(panel);

                Tab tab = new Tab(tabLabel.text);
                tab.name = tabLabel.name;
                tab.selected += t => SetSelectedPanel(t.label);
                tab.Add(scrollView);

                m_TabViewElement.Add(tab);
            }

            // We want to treat Up/Down NavigationMoveEvents as Next/Previous instead to get correct focus ring behavior, i.e. make
            // up/down arrows behave as tab/shift+tab. To do this we intercept the Up/Down events and send Next/Previous instead.
            m_PanelRootElement.UnregisterCallback<NavigationMoveEvent>(ConvertNavigationMoveEvents, TrickleDown.TrickleDown);
            m_PanelRootElement.RegisterCallback<NavigationMoveEvent>(ConvertNavigationMoveEvents, TrickleDown.TrickleDown);

            string selectedPanelName;
            if (m_SelectedPanel == null || !activePanels.Contains(m_SelectedPanel))
                selectedPanelName = DebugManager.instance.panels.Count > 0 ? DebugManager.instance.panels[0].displayName : null;
            else
                selectedPanelName = m_SelectedPanel.displayName;

            SetSelectedPanel(selectedPanelName);
        }

        void OnDestroy()
        {
            // Need to unregister here as well because when the UI is closed and reopened, it is a different object so the member
            // function will be a different object and the Unregister call in RecreateGUI does nothing.
            m_PanelRootElement.UnregisterCallback<NavigationMoveEvent>(ConvertNavigationMoveEvents, TrickleDown.TrickleDown);
        }

        void ConvertNavigationMoveEvents(NavigationMoveEvent evt)
        {
            if (IsPopupOpen())
                return; // Popup navigation uses up/down normally

            if (evt.direction != NavigationMoveEvent.Direction.Up &&
                evt.direction != NavigationMoveEvent.Direction.Down)
                return;

            evt.StopPropagation();
            m_PanelRootElement.focusController.IgnoreEvent(evt);

            var newDirection = evt.direction == NavigationMoveEvent.Direction.Down
                ? NavigationMoveEvent.Direction.Next
                : NavigationMoveEvent.Direction.Previous;

            using (var newEvt = NavigationMoveEvent.GetPooled(newDirection))
            {
                newEvt.target = evt.target;
                m_PanelRootElement.panel.visualTree.SendEvent(newEvt);
            }
        }

        internal void RequestRecreateGUI()
        {
            m_IsDirty = true;
        }

        void Update()
        {
            UpdateOrientation();

            if (m_IsDirty)
            {
                m_IsDirty = false;
                RecreateGUI();
            }
        }

        void UpdateOrientation(bool forceUpdate = false)
        {
            // We use screen dimensions instead of Screen.orientation to handle desktop platforms where it's better
            // to treat typical screen resolutions as "Landscape", but Screen.orientation reports it as "Portrait".
            bool portraitOrientation = Screen.width < Screen.height;
            if (forceUpdate || m_PortraitOrientation != portraitOrientation)
            {
                m_PortraitOrientation = portraitOrientation;

                const string portraitClassName = "portrait-orientation";
                const string landscapeClassName = "landscape-orientation";

                var debugWindowElement = m_PanelRootElement.Q("debug-window");
                debugWindowElement.RemoveFromClassList(portraitClassName);
                debugWindowElement.RemoveFromClassList(landscapeClassName);

                if (m_PortraitOrientation)
                {
                    debugWindowElement.AddToClassList(portraitClassName);
                }
                else
                {
                    debugWindowElement.AddToClassList(landscapeClassName);
                }
            }
        }

        void ResetClicked()
        {
            DebugDisplaySerializer.SaveFoldoutStates();

            DebugDisplaySerializer.Clear();
            DebugManager.instance.Reset();

            DebugDisplaySerializer.LoadFoldoutStates();
        }

        void SetSelectedPanel(string panelName)
        {
            if (string.IsNullOrEmpty(panelName))
                return;

            if (m_SelectedPanel != null)
            {
                var previousPanel = DebugManager.instance.GetPanel(m_SelectedPanel.displayName);
                if (previousPanel != null)
                    DebugManager.instance.schedulerTracker.SetHierarchyEnabled(DebugUI.Context.Runtime, previousPanel, false);
            }

            m_SelectedPanel = DebugManager.instance.GetPanel(panelName);

            if (m_SelectedPanel != null)
            {
                var newSelectedTab = m_TabViewElement.Q<Tab>(name: $"{m_SelectedPanel.displayName}_Tab");
                if (newSelectedTab != null)
                    m_TabViewElement.activeTab = newSelectedTab;

                DebugManager.instance.schedulerTracker.SetHierarchyEnabled(DebugUI.Context.Runtime, m_SelectedPanel, true);

                // Focus first focusable child in the panel
                foreach (var widget in m_SelectedPanel.children)
                {
                    if (widget.m_VisualElement is { focusable: true })
                    {
                        widget.m_VisualElement.Focus();
                        break;
                    }
                }
            }
        }

        internal bool IsPopupOpen()
        {
            if (m_PanelRootElement == null)
                return false;

            const string popupClassName = "unity-base-dropdown";
            var numPanelChildren = m_PanelRootElement.childCount;
            // reverse loop because the popup will appear at the end
            for (int i = numPanelChildren - 1; i >= 0; i--)
            {
                var child = m_PanelRootElement[i];
                if (child != null && child.ClassListContains(popupClassName))
                    return true;
            }
            return false;
        }

        internal void SelectNextPanel() => SelectPanelWithOffset(+1);

        internal void SelectPreviousPanel() => SelectPanelWithOffset(-1);

        void SelectPanelWithOffset(int offset)
        {
            if (m_SelectedPanel == null)
                return;

            var panels = DebugManager.instance.panels;
            int index = DebugManager.instance.FindPanelIndex(m_SelectedPanel.displayName);

            int Mod(int x, int m)
            {
                return (x % m + m) % m; // Handle negative offset correctly
            }

            int nextIndex = Mod(index + offset, panels.Count);
            SetSelectedPanel(panels[nextIndex].displayName);
        }
    }
}
#endif
