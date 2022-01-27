using UnityEditor.Rendering.UI;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Rendering
{
    public class RenderingDebuggerRuntime : UnityEngine.MonoBehaviour
    {
        public const string k_UnselectedContentClassName = "unselectedContent";

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

            var tabsVisualElement = runtimeUIDocument.rootVisualElement.Q<PanelTab>("tabs");
            var tabContentVisualElement = runtimeUIDocument.rootVisualElement.Q<VisualElement>("tabContent");
            tabsVisualElement.tabContentVisualElement = tabContentVisualElement;

            bool firstTabAdded = false;
            foreach (var panelType in TypeCache.GetTypesDerivedFrom<RenderingDebuggerPanel>())
            {
                RenderingDebuggerPanel panel = RenderingDebuggerState.instance.GetPanel(panelType);

                // Create the tab
                var panelHeader = new Label()
                {
                    name = $"{panel.panelName}{TabbedMenuController.k_TabNameSuffix}", text = panel.panelName
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

                tabsVisualElement.AddTab(panelHeader);
                tabContentVisualElement.Add(panelVisualElement);
                panelVisualElement.Bind(new SerializedObject(panel));
            }

            var firstFieldElement = runtimeUIDocument.rootVisualElement.Q(null, "unity-base-field");
            firstFieldElement?.Focus();

            runtimeRenderingDebugger.SetUp(tabsVisualElement);
        }

        void SetUp(PanelTab panelTab)
        {
            panelTab.OnTabSelected += tabName => { RenderingDebuggerState.instance.selectedPanelName = tabName; };
            panelTab.SetSelectedChoice(RenderingDebuggerState.instance.selectedPanelName);
        }
    }
}
