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

            // TODO fix - needs editor assembly, use Resources.Load instead?
            var panelStyle = AssetDatabase.LoadAssetAtPath<StyleSheet>("Packages/com.unity.render-pipelines.core/Runtime/Debugging/RenderingDebugger/Styles/RenderingDebuggerPanelStyle.uss");

            // TODO use Instantiate?
            windowTemplate.CloneTree(this.rootVisualElement);

            var tabsVisualElement = this.rootVisualElement.Q<VisualElement>("tabs");
            var tabContentVisualElement = this.rootVisualElement.Q<VisualElement>("tabContent");

            bool firstTabAdded = false;
            foreach (var panelType in TypeCache.GetTypesDerivedFrom<RenderingDebuggerPanel>())
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
                panelVisualElement.styleSheets.Add(panelStyle);

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
            controller.OnTabSelected += tabName => { RenderingDebuggerState.instance.selectedPanelName = tabName; };

            BindPanels();
        }

        public void BindPanels()
        {
            foreach (var panelType in TypeCache.GetTypesDerivedFrom<RenderingDebuggerPanel>())
            {
                var panel = RenderingDebuggerState.instance.GetPanel(panelType);
                this.rootVisualElement
                    .Q<VisualElement>($"{panel.panelName}{TabbedMenuController.k_ContentNameSuffix}")
                    .Bind(new SerializedObject(panel));
            }
        }
    }
}
