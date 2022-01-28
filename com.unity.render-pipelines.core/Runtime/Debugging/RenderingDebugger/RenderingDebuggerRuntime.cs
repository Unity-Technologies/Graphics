using UnityEditor.Rendering.UI;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

#if UNITY_EDITOR
using UnityEditor; // TODO: still required for TypeCache & bindings
#endif

namespace UnityEngine.Rendering
{
    public class RenderingDebuggerRuntime : MonoBehaviour
    {
        public const string k_UnselectedContentClassName = "unselectedContent";

        private UIDocument m_RuntimeUIDocument;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void CreateRuntimeRenderingDebuggerUI()
        {
            UIDocument runtimeUIDocument = null;
            var runtimeRenderingDebugger = FindObjectOfType<RenderingDebuggerRuntime>();
            if (runtimeRenderingDebugger != null)
            {
                runtimeRenderingDebugger.GetComponent<UIDocument>().rootVisualElement.Clear();
                RenderingDebuggerState.instance.OnSelectedPanelChanged -= runtimeRenderingDebugger.OnSelectedPanelChanged;
                RenderingDebuggerState.instance.OnReset -= runtimeRenderingDebugger.OnReset;
                CoreUtils.Destroy(runtimeRenderingDebugger.gameObject);
            }

            if (!Application.isPlaying)
                return;

            var runtimeRenderingDebuggerGO = new GameObject("[Rendering Debugger]");
            DontDestroyOnLoad(runtimeRenderingDebuggerGO);
            runtimeRenderingDebugger = runtimeRenderingDebuggerGO.AddComponent<RenderingDebuggerRuntime>();
            runtimeUIDocument = runtimeRenderingDebuggerGO.AddComponent<UIDocument>();
            runtimeUIDocument.visualTreeAsset = Resources.Load<VisualTreeAsset>("RenderingDebuggerRuntimeContainer");
            runtimeUIDocument.panelSettings = Resources.Load<PanelSettings>("RenderingDebuggerPanelSettings");
            runtimeUIDocument.panelSettings.themeStyleSheet = Resources.Load<ThemeStyleSheet>("Styles/UnityDefaultRuntimeTheme");

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

            var resetButtonElement = runtimeUIDocument.rootVisualElement.Q<Button>("ResetButton");
            resetButtonElement.clicked += () => RenderingDebuggerState.instance.Reset();

            runtimeRenderingDebugger.SetUp(runtimeUIDocument, tabsVisualElement);
        }

        private void FindFocus()
        {
            var currentTabContent = m_RuntimeUIDocument.rootVisualElement.Q<TemplateContainer>(className: TabbedMenuController.k_SelectedContentClassName);
            if (currentTabContent == null)
            {
                Debug.LogWarning("No tab selected");
                return;
            }

            var firstFieldElement = currentTabContent.Q(className: "unity-base-field");
            firstFieldElement?.Focus();
        }

        private PanelTab m_PanelTab = null;
        void SetUp(UIDocument runtimeUIDocument, PanelTab panelTab)
        {
            m_RuntimeUIDocument = runtimeUIDocument;
            m_PanelTab = panelTab;
            panelTab.OnTabSelected += tabName =>
            {
                RenderingDebuggerState.instance.selectedPanelName = tabName;
                FindFocus();
            };
            panelTab.SetSelectedChoice(RenderingDebuggerState.instance.selectedPanelName);
            RenderingDebuggerState.instance.OnSelectedPanelChanged += OnSelectedPanelChanged;
            RenderingDebuggerState.instance.OnReset += OnReset;

            m_RuntimeUIDocument.rootVisualElement.RegisterCallback<KeyDownEvent>(OnKeyDown);

            FindFocus();
        }

        void OnSelectedPanelChanged(string selectedPanel)
        {
            m_PanelTab.SetSelectedChoice(RenderingDebuggerState.instance.selectedPanelName);
        }

        void OnReset()
        {
            CreateRuntimeRenderingDebuggerUI();
        }

        void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode == KeyCode.PageDown)
            {
                m_PanelTab.OnPreviousClicked();
                FindFocus();
            }
            else if (evt.keyCode == KeyCode.PageUp)
            {
                m_PanelTab.OnNextClicked();
                FindFocus();
            }
        }
    }
}
