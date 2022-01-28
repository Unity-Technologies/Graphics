using System;
using UnityEditor.Rendering.UI;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering
{
    public class RenderingDebuggerWindow : EditorWindow
    {
        private TabbedMenuController controller;

        [MenuItem("Rendering Debugger (UITK)/Editor")]
        public static void ShowRenderingDebugger()
        {
            var wnd = GetWindow<RenderingDebuggerWindow>();
            wnd.titleContent = new GUIContent("Rendering Debugger (UITK)");
        }

        public void CreateGUI()
        {
            var windowTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/com.unity.render-pipelines.core/Editor/Debugging/RenderingDebugger/RenderingDebuggerEditorWindow.uxml");
            if (windowTemplate == null)
                throw new InvalidOperationException("Root document not found");

            // TODO use Instantiate?
            windowTemplate.CloneTree(this.rootVisualElement);

            var tabsVisualElement = this.rootVisualElement.Q<VisualElement>("tabs");
            var tabContentVisualElement = this.rootVisualElement.Q<VisualElement>("tabContent");

            bool firstTabAdded = false;
            foreach (var panelType in RenderingDebuggerPanel.GetPanelTypes())
            {
                RenderingDebuggerPanel panel = RenderingDebuggerState.instance.GetPanel(panelType);

                // Create the tab
                var panelHeader = new Label()
                {
                    name = $"{panel.panelName}{TabbedMenuController.k_TabNameSuffix}",
                    text = panel.panelName
                };
                panelHeader.AddToClassList(TabbedMenuController.k_TabClassName);

                // Create the content of the tab
                VisualElement panelVisualElement = panel.CreatePanel();
                panelVisualElement.name = $"{panel.panelName}{TabbedMenuController.k_ContentNameSuffix}";

                if (firstTabAdded == false && string.IsNullOrEmpty(RenderingDebuggerState.instance.selectedPanelName))
                {
                    firstTabAdded = true;
                    RenderingDebuggerState.instance.selectedPanelName = panelHeader.name;
                }

                if (panelHeader.name.Equals(RenderingDebuggerState.instance.selectedPanelName))
                {
                    panelHeader.AddToClassList(TabbedMenuController.k_CurrentlySelectedTabClassName);
                }
                else
                {
                    panelVisualElement.AddToClassList(TabbedMenuController.k_UnselectedContentClassName);
                }

                tabsVisualElement.Add(panelHeader);
                tabContentVisualElement.Add(panelVisualElement);
            }

            controller = new(rootVisualElement);
            controller.RegisterTabCallbacks();

            RenderingDebuggerState.instance.OnSelectedPanelChanged += OnSelectedPanelChanged;
            controller.OnTabSelected += tabName => { RenderingDebuggerState.instance.selectedPanelName = tabName; };

            RenderingDebuggerState.instance.OnReset += OnReset;
            var resetButtonElement = this.rootVisualElement.Q<Button>("ResetButton");
            resetButtonElement.clicked += () => RenderingDebuggerState.instance.Reset();

            BindPanels();
        }

        void OnSelectedPanelChanged(string selectedPanel)
        {
            controller.OnLabelClick(rootVisualElement.Q<Label>(selectedPanel));
        }

        public void BindPanels()
        {
            foreach (var panelType in TypeCache.GetTypesDerivedFrom<RenderingDebuggerPanel>())
            {
                var panel = RenderingDebuggerState.instance.GetPanel(panelType);
                var targetElement = this.rootVisualElement
                    .Q<VisualElement>($"{panel.panelName}{TabbedMenuController.k_ContentNameSuffix}");
                panel.BindTo(targetElement);
            }
        }

        private void OnReset()
        {
            this.rootVisualElement.Clear();
            RenderingDebuggerState.instance.OnReset -= OnReset;
            RenderingDebuggerState.instance.OnSelectedPanelChanged -= OnSelectedPanelChanged;
            CreateGUI();
        }
    }
}
