using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor.Categorization;
using UnityEditor.Rendering.Analytics;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering.Converter
{
    class ConverterInfo : ICategorizable
    {
        public IRenderPipelineConverter converter;
        public ConverterState state;

        public string sourcePipeline { get; }
        public string destinationPipeline { get; }

        public ConverterInfo(IRenderPipelineConverter converter, ConverterState state)
        {
            this.converter = converter;
            this.state = state;

            var converterAtt = type.GetCustomAttribute<PipelineConverterAttribute>();
            if (converterAtt != null)
            {
                sourcePipeline = converterAtt.source;
                destinationPipeline = converterAtt.destination;
            }
        }

        public Type type => converter.GetType();
    }

    [Serializable]
    [EditorWindowTitle(title = "Render Pipeline Converters")]
    internal class RenderPipelineConvertersEditor : EditorWindow, IHasCustomMenu
    {
        const string k_Uxml = "Packages/com.unity.render-pipelines.core/Editor-PrivateShared/Tools/Converter/Window/RenderPipelineConvertersEditor.uxml";
        const string k_Uss = "Packages/com.unity.render-pipelines.core/Editor-PrivateShared/Tools/Converter/Window/RenderPipelineConvertersEditor.uss";

        static Lazy<VisualTreeAsset> s_VisualTreeAsset = new Lazy<VisualTreeAsset>(() => AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_Uxml));
        static Lazy<StyleSheet> s_StyleSheet = new Lazy<StyleSheet>(() => AssetDatabase.LoadAssetAtPath<StyleSheet>(k_Uss));

        ScrollView m_ScrollView;
        Button m_ConvertButton;
        Button m_InitButton;
        VisualElement m_PipelineToolsVisualElements;
        DropdownField m_SourcePipelineDropDown;
        DropdownField m_DestinationPipelineDropDown;

        List<Node<ConverterInfo>> m_CoreConvertersList = new ();
        Node<ConverterInfo> currentContainer { get; set; }
        Dictionary<Node<ConverterInfo>, RenderPipelineConverterVisualElement> m_ConvertersVisualElements = new ();

        [SerializeField]
        private string m_CurrentTab = string.Empty;

        [SerializeField]
        private int m_SourcePipelineIndex = 0;

        [SerializeField]
        private int m_DestinationPipelineIndex = 0;

        internal static List<Node<ConverterInfo>> CategorizeConverters()
        {
            var elements = new List<ConverterInfo>();
            var manager = RenderPipelineConverterManager.instance;
            foreach (var state in manager.converterStates)
            {
                elements.Add(new ConverterInfo(state.converter, state));
            }
            return elements.SortByCategory();
        }

        [MenuItem("Window/Rendering/Render Pipeline Converter", false, 50)]
        public static void ShowWindow()
        {
            RenderPipelineConvertersEditor wnd = GetWindow<RenderPipelineConvertersEditor>();
            wnd.titleContent = new GUIContent("Render Pipeline Converter");
            DontSaveToLayout(wnd);
            wnd.minSize = new Vector2(650f, 400f);
            wnd.Show();
        }

        [MenuItem("Window/Rendering/Render Pipeline Converter", true, 50)]
        public static bool CanShowWindow()
        {
            if (EditorApplication.isPlaying)
                return false;

            foreach (var converterType in TypeCache.GetTypesDerivedFrom<IRenderPipelineConverter>())
            {
                if (!converterType.IsAbstract && !converterType.IsInterface)
                    return true;
            }

            return false;
        }

        internal static void DontSaveToLayout(EditorWindow wnd)
        {
            // Making sure that the window is not saved in layouts.
            Assembly assembly = typeof(EditorWindow).Assembly;
            var editorWindowType = typeof(EditorWindow);
            var hostViewType = assembly.GetType("UnityEditor.HostView");
            var containerWindowType = assembly.GetType("UnityEditor.ContainerWindow");
            var parentViewField = editorWindowType.GetField("m_Parent", BindingFlags.Instance | BindingFlags.NonPublic);
            var parentViewValue = parentViewField.GetValue(wnd);
            // window should not be saved to layout
            var containerWindowProperty =
                hostViewType.GetProperty("window", BindingFlags.Instance | BindingFlags.Public);
            var parentContainerWindowValue = containerWindowProperty.GetValue(parentViewValue);
            var dontSaveToLayoutField =
                containerWindowType.GetField("m_DontSaveToLayout", BindingFlags.Instance | BindingFlags.NonPublic);
            dontSaveToLayoutField.SetValue(parentContainerWindowValue, true);
        }

        private bool m_WindowAlive = true;

        void OnEnable()
        {
            m_WindowAlive = true;
            GraphicsToolLifetimeAnalytic.WindowOpened<RenderPipelineConvertersEditor>();

            // Subscribe to play mode changes
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

            // If CreateGUI already ran, update UI state now
            UpdateUiForPlayMode(EditorApplication.isPlaying);
        }

        private void OnDisable()
        {
            m_WindowAlive = false;
            GraphicsToolLifetimeAnalytic.WindowClosed<RenderPipelineConvertersEditor>();

            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            // Update visibility whenever state changes
            UpdateUiForPlayMode(EditorApplication.isPlaying);
        }

        private void UpdateUiForPlayMode(bool isPlaying)
        {
            if (rootVisualElement == null)
                return;

            var disabledHelpBox = rootVisualElement.Q<HelpBox>("disabledToolHelpBox");
            var convertersMainVE = rootVisualElement.Q<VisualElement>("converterEditorMainVE");
            var bottomButtonVE = rootVisualElement.Q<VisualElement>("bottomButtonVE");

            if (disabledHelpBox == null || convertersMainVE == null || bottomButtonVE == null)
                return;

            disabledHelpBox.style.display = isPlaying ? DisplayStyle.Flex : DisplayStyle.None;
            convertersMainVE.SetEnabled(!isPlaying);
            bottomButtonVE.SetEnabled(!isPlaying);
        }

        public void CreateGUI()
        {
            rootVisualElement.Clear();
            s_VisualTreeAsset.Value.CloneTree(rootVisualElement);
            rootVisualElement.styleSheets.Add(s_StyleSheet.Value);

            // Getting the scrollview where the converters should be added
            m_ScrollView = rootVisualElement.Q<ScrollView>("convertersScrollView");

            m_ConvertButton = rootVisualElement.Q<Button>("convertButton");
            m_ConvertButton.RegisterCallback<ClickEvent>(Convert);

            m_InitButton = rootVisualElement.Q<Button>("initializeButton");
            m_InitButton.RegisterCallback<ClickEvent>(InitializeAllActiveConverters);

            m_ConvertersVisualElements.Clear();

            m_CoreConvertersList = CategorizeConverters();

            m_PipelineToolsVisualElements = rootVisualElement.Q<VisualElement>("pipelineToolsVisualElements");

            var conversionsTabView = rootVisualElement.Q<TabView>("conversionsTabView");
            m_SourcePipelineDropDown = rootVisualElement.Q<DropdownField>("sourcePipelineDropDown");
            m_DestinationPipelineDropDown = rootVisualElement.Q<DropdownField>("targetPipelineDropDown");

            using (UnityEngine.Pool.HashSetPool<string>.Get(out var sourcePipelines))
            using (UnityEngine.Pool.HashSetPool<string>.Get(out var dstPipelines))
            {
                int tabIndex = -1;
                for (int c = 0; c < m_CoreConvertersList.Count; ++c)
                {
                    var converterNodeCategory = m_CoreConvertersList[c];
                    var tabName = converterNodeCategory.name;

                    if (string.IsNullOrEmpty(m_CurrentTab))
                        m_CurrentTab = tabName;

                    if (m_CurrentTab == tabName)
                        tabIndex = c;

                    conversionsTabView.Add(new Tab(tabName));

                    foreach (var element in converterNodeCategory.children)
                    {
                        if (!string.IsNullOrEmpty(element.data.sourcePipeline))
                            sourcePipelines.Add(element.data.sourcePipeline);

                        if (!string.IsNullOrEmpty(element.data.destinationPipeline))
                            dstPipelines.Add(element.data.destinationPipeline);

                        RenderPipelineConverterVisualElement converterVisualElement = new(element);
                        converterVisualElement.converterSelected += EnableOrDisableConvertButton;
                        m_ConvertersVisualElements.Add(element, converterVisualElement);
                    }
                }

                // Validate and clamp tab index (handles stale/removed categories)
                if (m_CoreConvertersList.Count == 0)
                {
                    Debug.LogWarning("No converter categories found.");
                    return;
                }

                tabIndex = Math.Clamp(tabIndex, 0, m_CoreConvertersList.Count - 1);
                currentContainer = m_CoreConvertersList[tabIndex];
                m_CurrentTab = currentContainer.name;

                conversionsTabView.selectedTabIndex = tabIndex;

                m_SourcePipelineDropDown.choices = sourcePipelines.ToList();
                m_SourcePipelineDropDown.index = Math.Max(0, Math.Min(m_SourcePipelineIndex, m_SourcePipelineDropDown.choices.Count - 1));

                m_DestinationPipelineDropDown.choices = dstPipelines.ToList();
                m_DestinationPipelineDropDown.index = Math.Max(0, Math.Min(m_DestinationPipelineIndex, m_DestinationPipelineDropDown.choices.Count - 1));
            }

            conversionsTabView.activeTabChanged += (from, to) =>
            {
                currentContainer = null;
                foreach (var converterNodeCategory in m_CoreConvertersList)
                {
                    if (converterNodeCategory.name == to.label)
                        currentContainer = converterNodeCategory;
                }

                m_CurrentTab = to.label;

                ConfigureUI();
            };

            m_SourcePipelineDropDown.RegisterCallback<ChangeEvent<string>>((evt) =>
            {
                m_SourcePipelineIndex = m_SourcePipelineDropDown.index;
                HideUnhideConverters();
            });

            m_DestinationPipelineDropDown.RegisterCallback<ChangeEvent<string>>((evt) =>
            {
                m_DestinationPipelineIndex = m_DestinationPipelineDropDown.index;
                HideUnhideConverters();
            });

            ConfigureUI();

            UpdateUiForPlayMode(EditorApplication.isPlaying);
        }

        private void ConfigureUI()
        {
            HideUnhideConverters();
            EnableOrDisableConvertButton();
        }

        private bool CanEnableConvert()
        {
            foreach (var child in currentContainer.children)
            {
                if (m_ConvertersVisualElements.TryGetValue(child, out var ve))
                {
                    if (ve.isSelectedAndEnabled &&
                        ve.state.isInitialized &&
                        ve.state.selectedPending > 0)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void EnableOrDisableConvertButton()
        {
            m_ConvertButton.SetEnabled(CanEnableConvert());
        }

        private void HideUnhideConverters()
        {
            if (currentContainer == null)
                throw new NullReferenceException("Current Container must not be null");

            bool pipelineConverterSelected = currentContainer.name == "Pipeline Converter";
            m_PipelineToolsVisualElements.style.display = pipelineConverterSelected ? DisplayStyle.Flex : DisplayStyle.None;

            m_ScrollView.Clear();
            foreach (var child in currentContainer.children)
            {
                if (m_ConvertersVisualElements.TryGetValue(child, out var ve))
                {
                    bool isVisible = !pipelineConverterSelected || (
                        m_SourcePipelineDropDown.text == child.data.sourcePipeline &&
                        m_DestinationPipelineDropDown.text == child.data.destinationPipeline
                    );

                    if (isVisible)
                        m_ScrollView.Add(ve);
                }  
            }
        }

        IEnumerable<RenderPipelineConverterVisualElement> selectedConverters
        {
            get
            {
                bool pipelineConverterSelected = currentContainer.name == "Pipeline Converter";

                foreach (var kvp in m_ConvertersVisualElements)
                {
                    if (kvp.Key.parent != currentContainer)
                        continue;

                    var data = kvp.Key.data;
                    bool isVisible = !pipelineConverterSelected || (
                       m_SourcePipelineDropDown.text == data.sourcePipeline &&
                       m_DestinationPipelineDropDown.text == data.destinationPipeline
                    );

                    if (!isVisible)
                        continue;

                    var converterVE = kvp.Value;
                    if (converterVE.isSelectedAndEnabled)
                        yield return converterVE;
                }
            }
        }

        void InitializeAllActiveConverters(ClickEvent evt)
        {
            if (EditorApplication.isPlaying) return;
            if (!SaveCurrentSceneAndContinue()) return;

            // Gather all the converters that are selected
            var convertersToInitialize = new List<RenderPipelineConverterVisualElement>();
            convertersToInitialize.AddRange(selectedConverters);

            // Check if any converter already has items, and confirm before clearing
            bool hasExistingItems = false;
            foreach (var converter in convertersToInitialize)
            {
                if (converter.state.isInitialized && converter.state.totalItemsCount > 0)
                {
                    hasExistingItems = true;
                    break;
                }
            }

            if (hasExistingItems)
            {
                if (!EditorUtility.DisplayDialog(
                    "Re-scan Converters",
                    "Re-scanning will erase the current converter state. Do you want to continue?",
                    "Yes, Re-scan",
                    "Cancel"))
                {
                    // User cancelled
                    return;
                }
            }

            int count = convertersToInitialize.Count;
            int iConverterIndex = 0;

            void InitializationFinish()
            {
                EditorUtility.ClearProgressBar();

                if (m_WindowAlive)
                {
                    EditorUtility.SetDirty(this);
                    RefreshUI();
                }
            }

            void ProcessNextConverter()
            {
                // Check if all the converters did finish
                if (!m_WindowAlive || iConverterIndex >= count)
                {
                    InitializationFinish();
                    return;
                }

                var current = convertersToInitialize[iConverterIndex];
                var converter = current.converter;

                if (!m_WindowAlive || EditorUtility.DisplayCancelableProgressBar("Initializing converters",
                    $"({iConverterIndex} of {count}) {current.displayName}", (float)iConverterIndex / (float)count))
                {
                    InitializationFinish();
                    return;
                }

                void OnConverterScanFinished()
                {
                    // Try to execute the next converter
                    ++iConverterIndex;
                    ProcessNextConverter();
                }

                try
                {
                    current.Scan(OnConverterScanFinished);
                }
                catch(Exception ex)
                {
                    Debug.LogError($"An exception occurred while initializing converter {current.displayName}. See console for more details.");
                    Debug.LogException(ex);
                    InitializationFinish();
                }
            }

            ProcessNextConverter();
        }

        private void RefreshUI()
        {
            foreach (var kvp in m_ConvertersVisualElements)
            {
                var converterVE = kvp.Value;
                converterVE.Refresh();
            }

            EnableOrDisableConvertButton();
        }

        private bool SaveCurrentSceneAndContinue()
        {
            Scene currentScene = SceneManager.GetActiveScene();
            if (currentScene.isDirty)
            {
                if (EditorUtility.DisplayDialog("Scene is not saved.",
                    "Current scene is not saved. Please save the scene before continuing.", "Save and Continue",
                    "Cancel"))
                {
                    EditorSceneManager.SaveScene(currentScene);
                }
                else
                {
                    return false;
                }
            }

            return true;
        }

        struct AnalyticContextInfo
        {
            public string converter_id;
            public int items_count;
        }

        void Convert(ClickEvent evt)
        {
            if (EditorApplication.isPlaying) return;

            // Early out: gather converters with items to convert first
            List<RenderPipelineConverterVisualElement> convertersToPerformConversion = new ();
            foreach (var converterVE in selectedConverters)
            {
                if (converterVE.state.isInitialized && converterVE.state.selectedPending > 0)
                {
                    convertersToPerformConversion.Add(converterVE);
                }
            }

            // Nothing to convert - skip scene save dialog and other overhead
            if (convertersToPerformConversion.Count == 0)
                return;

            if (!ShowIrreversibleChangesDialog()) return;
            // Ask to save save the current open scene and after the conversion is done reload the same scene.
            if (!SaveCurrentSceneAndContinue()) return;

            string currentScenePath = SceneManager.GetActiveScene().path;

            StringBuilder sb = new StringBuilder("=== Render Pipeline Converters Report ===\n");

            List<AnalyticContextInfo> contextInfo = new ();

            int converterCount = 1;
            int activeConvertersCount = convertersToPerformConversion.Count;
            foreach (var activeConverter in convertersToPerformConversion)
            {
                try
                {
                    activeConverter.Convert($"({converterCount} of {activeConvertersCount}) {activeConverter.displayName}", sb);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"An exception occurred while executing converter {activeConverter.displayName}. See console for more details.");
                    Debug.LogException(ex);
                }

                converterCount++;

                // Add this converter to the analytics
                contextInfo.Add(new()
                {
                    converter_id = activeConverter.displayName,
                    items_count = 0
                });

                EditorUtility.ClearProgressBar();
            }

            // Checking if we have changed current scene. If we have we reload the old scene we started from
            if (!string.IsNullOrEmpty(currentScenePath) && currentScenePath != SceneManager.GetActiveScene().path)
            {
                EditorSceneManager.OpenScene(currentScenePath);
            }

            AssetDatabase.SaveAssets();

            RefreshUI();

            GraphicsToolUsageAnalytic.ActionPerformed<RenderPipelineConvertersEditor>(nameof(Convert), contextInfo.ToNestedColumn());

            Debug.Log(sb);
        }

        public void AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem
            (
                L10n.TextContent("Reset", null, null, null),
                false,
                () =>
                {
                    RenderPipelineConverterManager.instance.Reset();
                    m_CoreConvertersList.Clear();
                    CreateGUI();
                }
            );
        }

        internal static string k_DialogKey = $"{nameof(UnityEditor)}.{nameof(Rendering)}.{nameof(RenderPipelineConvertersEditor)}.{nameof(ShowIrreversibleChangesDialog)}";
        private bool ShowIrreversibleChangesDialog()
        {
            return EditorUtility.DisplayDialog("Confirm Project Conversion",
                    "This action will modify project assets and cannot be easily undone. It is strongly recommended to have a backup or use version control before continuing.",
                    "Proceed", "Cancel", DialogOptOutDecisionType.ForThisMachine, k_DialogKey);
        }
    }
}
