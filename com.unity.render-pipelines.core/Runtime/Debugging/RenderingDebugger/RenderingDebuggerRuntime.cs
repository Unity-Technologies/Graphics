using UnityEditor.UIElements;
using UnityEngine.UIElements;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Rendering
{
    public class RenderingDebuggerRuntime : UnityEngine.MonoBehaviour
    {
        #if UNITY_EDITOR
        [MenuItem("Rendering Debugger (UITK)/Runtime")]
        #endif
        static void CreateRuntimeRenderingDebuggerUI()
        {
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

            foreach (var panelType in TypeCache.GetTypesDerivedFrom<RenderingDebuggerPanel>())
            {
                RenderingDebuggerPanel panel = RenderingDebuggerState.instance.GetPanel(panelType);

                var panelVisualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(panel.uiDocument);
                if (panelVisualTreeAsset == null)
                    continue;

                // Create the tab
                var panelHeader = new Label()
                {
                    name = panel.panelName,
                    text = panel.panelName
                };
                runtimeUIDocument.rootVisualElement.Add(panelHeader);

                // Create the content of the tab
                VisualElement panelVisualElement = new VisualElement() {name = panel.panelName};

                // TODO use Instantiate
                panelVisualTreeAsset.CloneTree(panelVisualElement);

                runtimeUIDocument.rootVisualElement.Add(panelVisualElement);
                panelVisualElement.Bind(new SerializedObject(panel));
            }
        }
    }
}
