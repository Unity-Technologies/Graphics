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

        [SerializeField]
        private RenderingDebuggerState m_State;


        [MenuItem("Window/Analysis/Rendering Debugger (UITK)", priority = 10006)]
        public static void ShowDefaultWindow()
        {
            var wnd = GetWindow<RenderingDebuggerWindow>();
            wnd.titleContent = new GUIContent("Rendering Debugger (UITK)");
        }

        public void CreateGUI()
        {
            if (m_State == null || m_State.Equals(null))
                m_State = RenderingDebuggerState.Load();

            var windowTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/com.unity.render-pipelines.core/Editor/Debugging/RenderingDebugger/RenderingDebuggerEditorWindow.uxml");
            if (windowTemplate == null)
                throw new InvalidOperationException("Root document not found");

            // TODO use Instantiate
            windowTemplate.CloneTree(this.rootVisualElement);

            // TODO temporary way to trigger saving - should think about better ways to do it
            this.rootVisualElement.RegisterCallback<FocusOutEvent>(OnFocusLost);

            var tabsVisualElement = this.rootVisualElement.Q<VisualElement>("tabs");
            var tabContentVisualElement = this.rootVisualElement.Q<VisualElement>("tabContent");

            bool firstTabAdded = false;
            foreach (var panelType in TypeCache.GetTypesDerivedFrom<RenderingDebuggerPanel>())
            {
                RenderingDebuggerPanel panel = m_State.GetPanel(panelType);

                var panelVisualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(panel.uiDocument);
                if (panelVisualTreeAsset == null)
                    continue;

                // Create the tab
                var panelHeader = new Label()
                {
                    name = $"{panel.panelName}{TabbedMenuController.k_TabNameSuffix}",
                    text = panel.panelName
                };
                panelHeader.AddToClassList(TabbedMenuController.k_TabClassName);

                // Create the content of the tab
                VisualElement panelVisualElement = new VisualElement() {name = $"{panel.panelName}{TabbedMenuController.k_ContentNameSuffix}"};

                // TODO use Instantiate
                panelVisualTreeAsset.CloneTree(panelVisualElement);

                if (firstTabAdded == false && string.IsNullOrEmpty(m_State.selectedPanelName))
                {
                    firstTabAdded = true;
                    m_State.selectedPanelName = panelHeader.name;
                }

                if (panelHeader.name.Equals(m_State.selectedPanelName))
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
            controller.OnTabSelected += tabName => { m_State.selectedPanelName = tabName; };

            BindPanels();
        }

        public void BindPanels()
        {
            foreach (var panelType in TypeCache.GetTypesDerivedFrom<RenderingDebuggerPanel>())
            {
                var panel = m_State.GetPanel(panelType);
                this.rootVisualElement
                    .Q<VisualElement>($"{panel.panelName}{TabbedMenuController.k_ContentNameSuffix}")
                    .Bind(new SerializedObject(panel));
            }
        }

        public void OnFocusLost(FocusOutEvent evt)
        {
            RenderingDebuggerState.Save(m_State);
        }

        public void OnEnable()
        {
            /*
            var windowTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/com.unity.render-pipelines.core/Editor/Debugging/RenderingDebugger/RenderingDebuggerEditorWindow.uxml");
            if (windowTemplate == null)
                throw new InvalidOperationException("Root document not found");

            windowTemplate.CloneTree(this.rootVisualElement);

            var tabsVisualElement = this.rootVisualElement.Q<VisualElement>("tabs");
            var tabContentVisualElement = this.rootVisualElement.Q<VisualElement>("tabContent");

            // TODO: Move this to another place on runtime checking the command for opening rendering debugger
            UIDocument runtimeUIDocument = null;
            var runtimeRenderingDebugger = FindObjectOfType<RenderingDebuggerRuntime>();
            if (runtimeRenderingDebugger != null)
            {
                DestroyImmediate(runtimeRenderingDebugger.gameObject);
            }

            var runtimeRenderingDebuggerGO = new GameObject("RenderingDebugger");
            runtimeRenderingDebugger = runtimeRenderingDebuggerGO.AddComponent<RenderingDebuggerRuntime>();
            runtimeUIDocument = runtimeRenderingDebuggerGO.AddComponent<UIDocument>();
            runtimeUIDocument.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/com.unity.render-pipelines.core/Runtime/Debugging/RenderingDebugger/RenderingDebuggerRuntimeContainer.uxml");
            runtimeUIDocument.panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>("Packages/com.unity.render-pipelines.core/Runtime/Debugging/RenderingDebugger/RenderingDebuggerPanelSettings.asset");
            runtimeUIDocument.panelSettings.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>("Assets/UI Toolkit/UnityThemes/UnityDefaultRuntimeTheme.tss");
            // END TODO

            if (m_State == null)
                m_State = CreateInstance<RenderingDebuggerState>();

            bool firstTabAdded = false;
            foreach (var panelType in TypeCache.GetTypesDerivedFrom<RenderingDebuggerPanel>())
            {
                RenderingDebuggerPanel panel = m_State.GetPanel(panelType);

                var panelVisualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(panel.uiDocument);
                if (panelVisualTreeAsset == null)
                    continue;

                // Create the tab
                var panelHeader = new Label()
                {
                    name = $"{panel.panelName}{TabbedMenuController.k_TabNameSuffix}",
                    text = panel.panelName
                };
                panelHeader.AddToClassList(TabbedMenuController.k_TabClassName);

                // Create the content of the tab
                VisualElement panelVisualElement = new VisualElement() {name = $"{panel.panelName}{TabbedMenuController.k_ContentNameSuffix}"};
                panelVisualTreeAsset.CloneTree(panelVisualElement);

                //var scriptableObject = ScriptableObject.CreateInstance(panel);

                if (firstTabAdded == false && string.IsNullOrEmpty(m_State.selectedPanelName))
                {
                    firstTabAdded = true;
                    m_State.selectedPanelName = panelHeader.name;
                }

                if (panelHeader.name.Equals(m_State.selectedPanelName))
                {
                    panelHeader.AddToClassList(TabbedMenuController.k_CurrentlySelectedTabClassName);
                }
                else
                {
                    panelVisualElement.AddToClassList(TabbedMenuController.k_UnselectedContentClassName);
                }

                tabsVisualElement.Add(panelHeader);
                tabContentVisualElement.Add(panelVisualElement);

                SerializedObject so = new SerializedObject(panel);

                // Bind it to the visual element of the panel
                panelVisualElement.Bind(so);

                // RUNTIME UI TODO MOVE
                // Create the visual element and clone the panel document
                var panelHeaderRuntime = new Label {text = panel.panelName};
                VisualElement panelVisualElementRuntime = new VisualElement();
                panelVisualTreeAsset.CloneTree(panelVisualElementRuntime);
                runtimeUIDocument.rootVisualElement.Add(panelHeaderRuntime);
                runtimeUIDocument.rootVisualElement.Add(panelVisualElementRuntime);
                panelVisualElementRuntime.Bind(so);
            }

            controller = new(rootVisualElement);
            controller.RegisterTabCallbacks();
            controller.OnTabSelected += tabName => { m_State.selectedPanelName = tabName; };
            this.minSize = new Vector2(400, 250);
            */
        }
    }
}
