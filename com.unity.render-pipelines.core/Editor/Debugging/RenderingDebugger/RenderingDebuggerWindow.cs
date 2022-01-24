using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering
{
    public class RenderingDebuggerWindow : EditorWindow
    {
        private IMGUIContainer tabContainer;
        private List<SerializedObject> serializedObjects = new ();

        private int selectedPanel; // TODO needs to be serialized to support undo & domain reload persistence
        private string selectedPanelName;

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

            UIDocument runtimeUIDocument = null;
            // TODO: Move this to another place on runtime checking the command for opening rendering debugger
            var runtimeRenderingDebugger = FindObjectOfType<RenderingDebuggerRuntime>();
            if (runtimeRenderingDebugger == null)
            {
                var runtimeRenderingDebuggerGO = new GameObject("RenderingDebugger");
                runtimeRenderingDebugger = runtimeRenderingDebuggerGO.AddComponent<RenderingDebuggerRuntime>();
                runtimeUIDocument = runtimeRenderingDebuggerGO.AddComponent<UIDocument>();
                runtimeUIDocument.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/com.unity.render-pipelines.core/Runtime/Debugging/RenderingDebugger/runtime-container.uxml");
                runtimeUIDocument.panelSettings = AssetDatabase.LoadAssetAtPath<PanelSettings>("Packages/com.unity.render-pipelines.core/Runtime/Debugging/RenderingDebugger/RenderingDebuggerPanelSettings.asset");
            }
            else
            {
                runtimeUIDocument = runtimeRenderingDebugger.GetComponent<UIDocument>();
            }
            // END TODO

            tabContainer = new IMGUIContainer(() => TabContainerOnGUI());
            this.rootVisualElement.Add(tabContainer);

            serializedObjects.Clear();

            foreach (var panel in TypeCache.GetTypesWithAttribute<RenderingDebuggerPanelAttribute>())
            {
                var panelDescriptionAttribute = panel.GetCustomAttribute<RenderingDebuggerPanelAttribute>();
                var panelVisualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(panelDescriptionAttribute.uiDocumentPath);
                if (panelVisualTreeAsset != null)
                {
                    // Create the visual element and clone the panel document
                    VisualElement panelVisualElement = new VisualElement();
                    panelVisualElement.AddToClassList("PanelElement");
                    panelVisualElement.name = panelDescriptionAttribute.panelName;
                    panelVisualTreeAsset.CloneTree(panelVisualElement);

                    // TODO: Scriptable object must be saved somewhere into a rendering debugger manager, obtain it from there
                    var scriptableObject = ScriptableObject.CreateInstance(panel);

                    SerializedObject so = new SerializedObject(scriptableObject);

                    // Bind it to the visual element of the panel
                    panelVisualElement.Bind(so);

                    // Initialize first selected
                    if (!serializedObjects.Any())
                    {
                        selectedPanel = 0;
                        selectedPanelName = panelVisualElement.name;
                    }
                    else
                    {
                        panelVisualElement.AddToClassList("Hidden");
                    }

                    serializedObjects.Add(so);

                    // EDITOR UI
                    this.rootVisualElement.Add(panelVisualElement);

                    // RUNTIME UI
                    // Create the visual element and clone the panel document
                    var panelHeaderRuntime = new Label {text = panelDescriptionAttribute.panelName};
                    VisualElement panelVisualElementRuntime = new VisualElement();
                    panelVisualTreeAsset.CloneTree(panelVisualElementRuntime);
                    panelVisualElementRuntime.Bind(so);
                    runtimeUIDocument.rootVisualElement.Add(panelHeaderRuntime);
                    runtimeUIDocument.rootVisualElement.Add(panelVisualElementRuntime);
                }
            }

            this.minSize = new Vector2(400, 250);
        }

        public void SetSelectedPanel(int panelIndex, string panelName)
        {
            // hide previously selected panel
            this.rootVisualElement.Q<VisualElement>(selectedPanelName, "PanelElement").AddToClassList("Hidden");

            selectedPanel = panelIndex;
            selectedPanelName = panelName;

            // display newly selected panel
            this.rootVisualElement.Q<VisualElement>(panelName, "PanelElement").RemoveFromClassList("Hidden");
        }

        public void TabContainerOnGUI()
        {
            var numPanels = serializedObjects.Count;
            for (int i = 0; i < numPanels; i++)
            {
                var so = serializedObjects[i];

                var panelAttribute = so.targetObject.GetType().GetCustomAttribute<RenderingDebuggerPanelAttribute>();

                var elementRect = GUILayoutUtility.GetRect(EditorGUIUtility.TrTextContent(panelAttribute.panelName), DebugWindow.s_Styles.sectionElement, GUILayout.ExpandWidth(true));

                if (selectedPanel == i && Event.current.type == EventType.Repaint)
                    DebugWindow.s_Styles.selected.Draw(elementRect, false, false, false, false);

                EditorGUI.BeginChangeCheck();
                GUI.Toggle(elementRect, selectedPanel == i, panelAttribute.panelName, DebugWindow.s_Styles.sectionElement);
                if (EditorGUI.EndChangeCheck())
                {
                    //Undo.RegisterCompleteObjectUndo(m_Settings, $"Debug Panel '{panel.displayName}' Selection");
                    SetSelectedPanel(i, panelAttribute.panelName);
                }
            }
        }
    }
}
