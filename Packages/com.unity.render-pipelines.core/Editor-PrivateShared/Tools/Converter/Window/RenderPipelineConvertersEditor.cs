using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor.Categorization;
using UnityEditor.Rendering.Analytics;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace UnityEditor.Rendering.Converter
{
    // This is the serialized class that stores the state of each item in the list of items to convert
    [Serializable]
    class ConverterItemState
    {
        public bool isSelected;
        public IRenderPipelineConverterItem item;
        public (Status Status, string Message) conversionResult = (Status.Pending, string.Empty);
        internal bool hasConverted => conversionResult.Status != Status.Pending;
    }

    // Each converter uses the active bool
    // Each converter has a list of active items/assets
    // We do this so that we can use the binding system of the UI Elements
    [Serializable]
    class ConverterState
    {
        public bool isExpanded;
        public bool isSelected;
        public bool isLoading; // to name
        public bool isInitialized;
        public List<ConverterItemState> items = new List<ConverterItemState>();
        [SerializeReference]
        public IRenderPipelineConverter converter;

        private int CountItemWithFlag(Status status)
        {
            int count = 0;
            foreach (ConverterItemState itemState in items)
            {
                if (itemState.conversionResult.Status == status)
                {
                    count++;
                }
            }
            return count;
        }
        public int pending => CountItemWithFlag(Status.Pending);
        public int warnings => CountItemWithFlag(Status.Warning);
        public int errors => CountItemWithFlag(Status.Error);
        public int success => CountItemWithFlag(Status.Success);

        public override string ToString()
        {
            return $"Warnings: {warnings} - Errors: {errors} - Ok: {success} - Total: {items?.Count ?? 0}";
        }

        public void Clear()
        {
            isInitialized = false;
            items.Clear();
        }

        public int selectedItemsCount
        {
            get
            {
                int count = 0;
                foreach (ConverterItemState itemState in items)
                {
                    if (itemState.isSelected)
                    {
                        count++;
                    }
                }
                return count;
            }
        }
    }

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

            if (disabledHelpBox == null || convertersMainVE == null)
                return;

            disabledHelpBox.style.display = isPlaying ? DisplayStyle.Flex : DisplayStyle.None;
            convertersMainVE.SetEnabled(!isPlaying);
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

            using (HashSetPool<string>.Get(out var sourcePipelines))
            using (HashSetPool<string>.Get(out var dstPipelines))
            {
                foreach (var converterNodeCategory in m_CoreConvertersList)
                {
                    conversionsTabView.Add(new Tab(converterNodeCategory.name));

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

                currentContainer = m_CoreConvertersList[0];

                conversionsTabView.tabIndex = 0;

                m_SourcePipelineDropDown.choices = sourcePipelines.ToList();
                m_SourcePipelineDropDown.index = 0;

                m_DestinationPipelineDropDown.choices = dstPipelines.ToList();
                m_DestinationPipelineDropDown.index = 0;
            }

            conversionsTabView.activeTabChanged += (from, to) =>
            {
                currentContainer = null;
                foreach (var converterNodeCategory in m_CoreConvertersList)
                {
                    if (converterNodeCategory.name == to.label)
                        currentContainer = converterNodeCategory;
                }
                HideUnhideConverters();
            };

            m_SourcePipelineDropDown.RegisterCallback<ChangeEvent<string>>((evt) =>
            {
                HideUnhideConverters();
            });

            m_DestinationPipelineDropDown.RegisterCallback<ChangeEvent<string>>((evt) =>
            {
                HideUnhideConverters();
            });

            HideUnhideConverters();
            EnableOrDisableConvertButton();

            UpdateUiForPlayMode(EditorApplication.isPlaying);
        }

        private bool CanEnableConvert()
        {
            foreach (var kvp in m_ConvertersVisualElements)
            {
                var ve = kvp.Value;
                if (ve.isSelectedAndEnabled &&
                    ve.state.isInitialized &&
                    ve.state.selectedItemsCount > 0 &&
                    ve.state.pending > 0)
                {
                    return true;
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

        void InitializeAllActiveConverters(ClickEvent evt)
        {
            if (!SaveCurrentSceneAndContinue())
                return;

            // Gather all the converters that are selected
            var selectedConverters = new List<RenderPipelineConverterVisualElement>();
            foreach (var kvp in m_ConvertersVisualElements)
            {
                if (kvp.Key.parent != currentContainer)
                    continue;

                var converterVE = kvp.Value;
                if (converterVE.isSelectedAndEnabled)
                    selectedConverters.Add(converterVE);
            }

            int count = selectedConverters.Count;
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

                var current = selectedConverters[iConverterIndex];
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

                current.Scan(OnConverterScanFinished);
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
            if (!ShowIrreversibleChangesDialog()) return;
            // Ask to save save the current open scene and after the conversion is done reload the same scene.
            if (!SaveCurrentSceneAndContinue()) return;

            string currentScenePath = SceneManager.GetActiveScene().path;

            StringBuilder sb = new StringBuilder("=== Render Pipeline Converters Report ===\n");

            List<RenderPipelineConverterVisualElement> activeConverterStates = new ();

            // Getting all the active converters to use in the cancelable progressbar
            foreach (var kvp in m_ConvertersVisualElements)
            {
                if (kvp.Key.parent != currentContainer)
                    continue;

                var ve = kvp.Value;
                if (ve.isSelectedAndEnabled && ve.state.isInitialized)
                {
                    activeConverterStates.Add(ve);
                }
            }

            List<AnalyticContextInfo> contextInfo = new ();

            int converterCount = 1;
            int activeConvertersCount = activeConverterStates.Count;
            foreach (var activeConverter in activeConverterStates)
            {
                activeConverter.Convert($"({converterCount} of {activeConvertersCount}) {activeConverter.displayName}", sb);
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
                EditorGUIUtility.TrTextContent("Reset"),
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
