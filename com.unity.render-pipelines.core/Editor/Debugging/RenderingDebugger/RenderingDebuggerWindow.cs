using System;
using System.Reflection;
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

        [MenuItem("Window/Analysis/Rendering Debugger (UITK)", priority = 10006)]
        public static void ShowDefaultWindow()
        {
            var wnd = GetWindow<RenderingDebuggerWindow>();
            wnd.titleContent = new GUIContent("Rendering Debugger (UITK)");
        }

        public void OnEnable()
        {
            var windowTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/com.unity.render-pipelines.core/Editor/Debugging/RenderingDebugger/RenderingDebuggerEditorWindow.uxml");
            if (windowTemplate == null)
                throw new InvalidOperationException("Root document not found");

            windowTemplate.CloneTree(this.rootVisualElement);

            var tabsVisualElement = this.rootVisualElement.Q<VisualElement>("tabs");
            var tabContentVisualElement = this.rootVisualElement.Q<VisualElement>("tabContent");

            UIDocument runtimeUIDocument = null;
            // TODO: Move this to another place on runtime checking the command for opening rendering debugger
            var runtimeRenderingDebugger = FindObjectOfType<RenderingDebuggerRuntime>();
            if (runtimeRenderingDebugger == null)
            {
                var runtimeRenderingDebuggerGO = new GameObject("RenderingDebugger");
                runtimeRenderingDebugger = runtimeRenderingDebuggerGO.AddComponent<RenderingDebuggerRuntime>();
                runtimeUIDocument = runtimeRenderingDebuggerGO.AddComponent<UIDocument>();
                runtimeUIDocument.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/com.unity.render-pipelines.core/Runtime/Debugging/RenderingDebugger/RenderingDebuggerRuntimeContainer.uxml");
                runtimeUIDocument.panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>("Packages/com.unity.render-pipelines.core/Runtime/Debugging/RenderingDebugger/RenderingDebuggerPanelSettings.asset");
                runtimeUIDocument.panelSettings.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>("Assets/UI Toolkit/UnityThemes/UnityDefaultRuntimeTheme.tss");
            }
            else
            {
                runtimeUIDocument = runtimeRenderingDebugger.GetComponent<UIDocument>();
            }
            // END TODO

            bool firstTabAdded = false;
            foreach (var panel in TypeCache.GetTypesWithAttribute<RenderingDebuggerPanelAttribute>())
            {
                var panelDescriptionAttribute = panel.GetCustomAttribute<RenderingDebuggerPanelAttribute>();
                var panelVisualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(panelDescriptionAttribute.uiDocumentPath);
                if (panelVisualTreeAsset == null)
                    continue;

                // Create the tab
                var panelHeader = new Label()
                {
                    name = $"{panelDescriptionAttribute.panelName}{TabbedMenuController.k_TabNameSuffix}",
                    text = panelDescriptionAttribute.panelName
                };
                panelHeader.AddToClassList(TabbedMenuController.k_TabClassName);

                // Create the content of the tab
                VisualElement panelVisualElement = new VisualElement() {name = $"{panelDescriptionAttribute.panelName}{TabbedMenuController.k_ContentNameSuffix}"};
                panelVisualTreeAsset.CloneTree(panelVisualElement);

                var scriptableObject = ScriptableObject.CreateInstance(panel);

                SerializedObject so = new SerializedObject(scriptableObject);

                // Bind it to the visual element of the panel
                panelVisualElement.Bind(so);

                if (firstTabAdded == false)
                {
                    panelHeader.AddToClassList(TabbedMenuController.k_CurrentlySelectedTabClassName);
                    firstTabAdded = true;
                }
                else
                {
                    panelVisualElement.AddToClassList(TabbedMenuController.k_UnselectedContentClassName);
                }

                tabsVisualElement.Add(panelHeader);
                tabContentVisualElement.Add(panelVisualElement);


                // RUNTIME UI TODO MOVE
                // Create the visual element and clone the panel document
                var panelHeaderRuntime = new Label {text = panelDescriptionAttribute.panelName};
                VisualElement panelVisualElementRuntime = new VisualElement();
                panelVisualTreeAsset.CloneTree(panelVisualElementRuntime);
                panelVisualElementRuntime.Bind(so);
                runtimeUIDocument.rootVisualElement.Add(panelHeaderRuntime);
                runtimeUIDocument.rootVisualElement.Add(panelVisualElementRuntime);
            }

            controller = new(rootVisualElement);
            controller.RegisterTabCallbacks();

            this.minSize = new Vector2(400, 250);
        }
    }
}
