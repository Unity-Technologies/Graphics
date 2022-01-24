using System.Reflection;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering
{
    public class RenderingDebuggerWindow : EditorWindow
    {
        [MenuItem("Window/Analysis/Rendering Debugger (UITK)", priority = 10006)]
        public static void ShowDefaultWindow()
        {
            var wnd = GetWindow<RenderingDebuggerWindow>();
            wnd.titleContent = new GUIContent("Rendering Debugger (UITK)");
        }

        public void OnEnable()
        {
            var windowTemplate = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Packages/com.unity.render-pipelines.core/Editor/Debugging/RenderingDebugger/rendering-debugger.uxml");
            if (windowTemplate != null)
            {
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

                foreach (var panel in TypeCache.GetTypesWithAttribute<RenderingDebuggerPanelAttribute>())
                {
                    var panelDescriptionAttribute = panel.GetCustomAttribute<RenderingDebuggerPanelAttribute>();
                    var panelVisualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(panelDescriptionAttribute.uiDocumentPath);
                    if (panelVisualTreeAsset != null)
                    {
                        // Create the visual element and clone the panel document
                        VisualElement panelVisualElement = new VisualElement();
                        panelVisualTreeAsset.CloneTree(panelVisualElement);

                        // TODO: Scriptable object must be saved somewhere into a rendering debugger manager, obtain it from there
                        var scriptableObject = ScriptableObject.CreateInstance(panel);

                        SerializedObject so = new SerializedObject(scriptableObject);

                        // Bind it to the visual element of the panel
                        panelVisualElement.Bind(so);

                        // For now append everything into the main view, but this should be added to the tab system
                        var panelHeader = new Label {text = panelDescriptionAttribute.panelName};

                        // EDITOR UI
                        this.rootVisualElement.Add(panelHeader);
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
            }

            this.minSize = new Vector2(400, 250);
        }
    }
}
