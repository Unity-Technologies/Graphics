using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEditor.Search;
using UnityEditor.UIElements;
using UnityEngine.UIElements;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;


namespace UnityEditor.Rendering.Universal
{
    // Status for each row item to say in which state they are in.
    // This will make sure they are showing the correct icon
    [Serializable]
    enum Status
    {
        Pending,
        Warning,
        Error,
        Success
    }

    // This is the serialized class that stores the state of each item in the list of items to convert
    [Serializable]
    class ConverterItemState
    {
        public bool isActive;

        // Message that will be displayed on the icon if warning or failed.
        public string message;

        // Status of the converted item, Pending, Warning, Error or Success
        public Status status;

        internal bool hasConverted = false;
    }

    // Each converter uses the active bool
    // Each converter has a list of active items/assets
    // We do this so that we can use the binding system of the UI Elements
    [Serializable]
    class ConverterState
    {
        // This is the enabled state of the whole converter
        public bool isEnabled;
        public bool isActive;
        public bool isLoading; // to name
        public bool isInitialized;
        public List<ConverterItemState> items = new List<ConverterItemState>();

        public int pending;
        public int warnings;
        public int errors;
        public int success;
        internal int index;

        public bool isActiveAndEnabled => isEnabled && isActive;
        public bool requiresInitialization => !isInitialized && isActiveAndEnabled;
    }

    [Serializable]
    internal struct ConverterItems
    {
        public List<ConverterItemDescriptor> itemDescriptors;
    }

    [Serializable]
    [EditorWindowTitle(title = "Render Pipeline Converters")]
    internal class RenderPipelineConvertersEditor : EditorWindow
    {
        Tuple<string, Texture2D> converterStateInfoDisabled;
        Tuple<string, Texture2D> converterStateInfoPendingInitialization;
        Tuple<string, Texture2D> converterStateInfoPendingConversion;
        Tuple<string, Texture2D> converterStateInfoPendingConversionWarning;
        Tuple<string, Texture2D> converterStateInfoCompleteErrors;
        Tuple<string, Texture2D> converterStateInfoComplete;

        public VisualTreeAsset converterEditorAsset;
        public VisualTreeAsset converterItem;
        public VisualTreeAsset converterWidgetMainAsset;

        ScrollView m_ScrollView;
        VisualElement m_ConverterSelectedVE;
        Button m_ConvertButton;
        Button m_InitButton;
        Button m_InitAnConvertButton;
        Button m_ContainerHelpButton;

        bool m_InitAndConvert;

        List<RenderPipelineConverter> m_CoreConvertersList = new List<RenderPipelineConverter>();
        List<VisualElement> m_VEList = new List<VisualElement>();

        // This list needs to be as long as the amount of converters
        List<ConverterItems> m_ItemsToConvert = new List<ConverterItems>();
        //List<List<ConverterItemDescriptor>> m_ItemsToConvert = new List<List<ConverterItemDescriptor>>();
        SerializedObject m_SerializedObject;

        List<string> m_ContainerChoices = new List<string>();
        List<RenderPipelineConverterContainer> m_Containers = new List<RenderPipelineConverterContainer>();
        int m_ContainerChoiceIndex = 0;
        int m_WorkerCount;

        // This is a list of Converter States which holds a list of which converter items/assets are active
        // There is one for each Converter.
        [SerializeField] List<ConverterState> m_ConverterStates = new List<ConverterState>();

        TypeCache.TypeCollection m_ConverterContainers;

        RenderPipelineConverterContainer currentContainer => m_Containers[m_ContainerChoiceIndex];

        // Name of the index file
        string m_URPConverterIndex = "URPConverterIndex";

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

        void OnEnable()
        {
            InitIfNeeded();
        }

        void InitIfNeeded()
        {
            if (m_CoreConvertersList.Any())
                return;
            m_CoreConvertersList = new List<RenderPipelineConverter>();

            // This is the drop down choices.
            m_ConverterContainers = TypeCache.GetTypesDerivedFrom<RenderPipelineConverterContainer>();

            foreach (var containerType in m_ConverterContainers)
            {
                var container = (RenderPipelineConverterContainer)Activator.CreateInstance(containerType);
                m_Containers.Add(container);
            }

            // this need to be sorted by Priority property
            m_Containers = m_Containers
                .OrderBy(o => o.priority).ToList();

            foreach (var container in m_Containers)
            {
                m_ContainerChoices.Add(container.name);
            }

            if (m_ConverterContainers.Any())
            {
                GetConverters();
            }
            else
            {
                ClearConverterStates();
            }
        }

        void ClearConverterStates()
        {
            m_CoreConvertersList.Clear();
            m_ConverterStates.Clear();
            m_ItemsToConvert.Clear();
            m_VEList.Clear();
        }

        void GetConverters()
        {
            ClearConverterStates();
            var converterList = TypeCache.GetTypesDerivedFrom<RenderPipelineConverter>();

            for (int i = 0; i < converterList.Count; ++i)
            {
                // Iterate over the converters that are used by the current container
                RenderPipelineConverter conv = (RenderPipelineConverter)Activator.CreateInstance(converterList[i]);
                m_CoreConvertersList.Add(conv);
            }

            // this need to be sorted by Priority property
            m_CoreConvertersList = m_CoreConvertersList
                .OrderBy(o => o.priority).ToList();

            for (int i = 0; i < m_CoreConvertersList.Count; i++)
            {
                // Create a new ConvertState which holds the active state of the converter
                var converterState = new ConverterState
                {
                    isEnabled = m_CoreConvertersList[i].isEnabled,
                    isActive = false,
                    isInitialized = false,
                    items = new List<ConverterItemState>(),
                    index = i,
                };
                m_ConverterStates.Add(converterState);

                // This just creates empty entries in the m_ItemsToConvert.
                // This list need to have the same amount of entries as the converters
                List<ConverterItemDescriptor> converterItemInfos = new List<ConverterItemDescriptor>();
                m_ItemsToConvert.Add(new ConverterItems { itemDescriptors = converterItemInfos });
            }
        }

        public void CreateGUI()
        {
            converterStateInfoDisabled = new("Converter Disabled", null);
            converterStateInfoPendingInitialization = new("Pending Initialization", CoreEditorStyles.iconPending);
            converterStateInfoPendingConversion = new("Pending Conversion", CoreEditorStyles.iconPending);
            converterStateInfoPendingConversionWarning = new("Pending Conversion with Warnings", CoreEditorStyles.iconWarn);
            converterStateInfoCompleteErrors = new("Conversion Complete with Errors", CoreEditorStyles.iconFail);
            converterStateInfoComplete = new("Conversion Complete", CoreEditorStyles.iconComplete);

            string theme = EditorGUIUtility.isProSkin ? "dark" : "light";
            InitIfNeeded();

            if (m_ConverterContainers.Any())
            {
                m_SerializedObject = new SerializedObject(this);
                converterEditorAsset.CloneTree(rootVisualElement);

                rootVisualElement.Q<DropdownField>("conversionsDropDown").choices = m_ContainerChoices;
                rootVisualElement.Q<DropdownField>("conversionsDropDown").index = m_ContainerChoiceIndex;

                // Getting the scrollview where the converters should be added
                m_ScrollView = rootVisualElement.Q<ScrollView>("convertersScrollView");

                m_ConvertButton = rootVisualElement.Q<Button>("convertButton");
                m_ConvertButton.RegisterCallback<ClickEvent>(Convert);

                m_InitButton = rootVisualElement.Q<Button>("initializeButton");
                m_InitButton.RegisterCallback<ClickEvent>(InitializeAllActiveConverters);

                m_InitAnConvertButton = rootVisualElement.Q<Button>("initializeAndConvert");
                m_InitAnConvertButton.RegisterCallback<ClickEvent>(InitializeAndConvert);

                m_ContainerHelpButton = rootVisualElement.Q<Button>("containerHelpButton");
                m_ContainerHelpButton.RegisterCallback<ClickEvent>(GotoHelpURL);
                m_ContainerHelpButton.Q<Image>("containerHelpImage").image = CoreEditorStyles.iconHelp;
                m_ContainerHelpButton.RemoveFromClassList("unity-button");
                m_ContainerHelpButton.AddToClassList(theme);

                RecreateUI();
            }
        }

        void GotoHelpURL(ClickEvent evt)
        {
            if (DocumentationUtils.TryGetHelpURL(currentContainer.GetType(), out var url))
            {
                Help.BrowseURL(url);
            }
        }

        void InitOrConvert()
        {
            bool allSelectedHasInitialized = true;
            // Check if all ticked ones have been initialized.
            // If not then Init Button should be active
            // Get all active converters

            if (m_ConverterStates.Any())
            {
                foreach (ConverterState state in m_ConverterStates)
                {
                    if (state.isActiveAndEnabled && !state.isInitialized)
                    {
                        allSelectedHasInitialized = false;
                        break;
                    }
                }
            }
            else
            {
                // If no converters is active.
                // we should make the text somewhat disabled
                allSelectedHasInitialized = false;
            }

            if (allSelectedHasInitialized)
            {
                m_ConvertButton.style.display = DisplayStyle.Flex;
                m_InitButton.style.display = DisplayStyle.None;
            }
            else
            {
                m_ConvertButton.style.display = DisplayStyle.None;
                m_InitButton.style.display = DisplayStyle.Flex;
            }
        }

        void UpdateSelectedConverterItems(int index, VisualElement element)
        {
            int count = 0;
            foreach (ConverterItemState state in m_ConverterStates[index].items)
            {
                if (state.isActive)
                {
                    count++;
                }
            }

            element.Q<Label>("converterStats").text = $"{count}/{m_ItemsToConvert[index].itemDescriptors.Count} selected";
        }

        void ShowConverterLayout(VisualElement element)
        {
            m_ConverterSelectedVE = element;
            rootVisualElement.Q<VisualElement>("converterEditorMainVE").style.display = DisplayStyle.None;
            rootVisualElement.Q<VisualElement>("singleConverterVE").style.display = DisplayStyle.Flex;
            rootVisualElement.Q<VisualElement>("singleConverterVE").Add(element);
            element.Q<VisualElement>("converterItems").style.display = DisplayStyle.Flex;
            element.Q<VisualElement>("informationVE").style.display = DisplayStyle.Flex;

            rootVisualElement.Q<Button>("backButton").RegisterCallback<ClickEvent>(BackToConverters);
        }

        void HideConverterLayout(VisualElement element)
        {
            rootVisualElement.Q<VisualElement>("converterEditorMainVE").style.display = DisplayStyle.Flex;
            rootVisualElement.Q<VisualElement>("singleConverterVE").style.display = DisplayStyle.None;
            rootVisualElement.Q<VisualElement>("singleConverterVE").Remove(element);

            element.Q<VisualElement>("converterItems").style.display = DisplayStyle.None;
            element.Q<VisualElement>("informationVE").style.display = DisplayStyle.None;

            RecreateUI();
            m_ConverterSelectedVE = null;
        }
        void ToggleAllNone(ClickEvent evt, int index, bool value, VisualElement item)
        {
            var conv = m_ConverterStates[index];
            if (conv.items.Count > 0)
            {
                foreach (var convItem in conv.items)
                {
                    convItem.isActive = value;
                }
                UpdateSelectedConverterItems(index, item);
                // Changing the look of the labels
                if (value)
                {
                    item.Q<Label>("all").AddToClassList("selected");
                    item.Q<Label>("all").RemoveFromClassList("not_selected");

                    item.Q<Label>("none").AddToClassList("not_selected");
                    item.Q<Label>("none").RemoveFromClassList("selected");
                }
                else
                {
                    item.Q<Label>("none").AddToClassList("selected");
                    item.Q<Label>("none").RemoveFromClassList("not_selected");

                    item.Q<Label>("all").AddToClassList("not_selected");
                    item.Q<Label>("all").RemoveFromClassList("selected");
                }
            }
        }

        void ConverterStatusInfo(int index, VisualElement item)
        {
            Tuple<string, Texture2D> info = converterStateInfoDisabled;;
            // Check if it is active
            if (m_ConverterStates[index].isActive)
            {
                info = converterStateInfoPendingInitialization;
            }
            if (m_ConverterStates[index].isInitialized)
            {
                info = converterStateInfoPendingConversion;
            }
            if (m_ConverterStates[index].warnings > 0)
            {
                info = converterStateInfoPendingConversionWarning;
            }
            if (m_ConverterStates[index].errors > 0)
            {
                info = converterStateInfoCompleteErrors;
            }
            if (m_ConverterStates[index].errors == 0 && m_ConverterStates[index].warnings == 0 && m_ConverterStates[index].success > 0)
            {
                info = converterStateInfoComplete;
            }
            if (!m_ConverterStates[index].isActive)
            {
                info = converterStateInfoDisabled;
            }
            item.Q<Label>("converterStateInfoL").text = info.Item1;
            item.Q<Image>("converterStateInfoIcon").image = info.Item2;
        }

        void BackToConverters(ClickEvent evt)
        {
            HideConverterLayout(m_ConverterSelectedVE);
        }

        void RecreateUI()
        {
            m_SerializedObject.Update();
            // This is temp now to get the information filled in
            rootVisualElement.Q<DropdownField>("conversionsDropDown").RegisterCallback<ChangeEvent<string>>((evt) =>
            {
                m_ContainerChoiceIndex = rootVisualElement.Q<DropdownField>("conversionsDropDown").index;
                rootVisualElement.Q<TextElement>("conversionInfo").text = currentContainer.info;
                HideUnhideConverters();
            });

            rootVisualElement.Q<TextElement>("conversionInfo").text = currentContainer.info;
            m_ScrollView.Clear();

            for (int i = 0; i < m_CoreConvertersList.Count; ++i)
            {
                // Making an item using the converterListAsset as a template.
                // Then adding the information needed for each converter
                VisualElement item = new VisualElement();
                converterWidgetMainAsset.CloneTree(item);
                // Adding the VE so that we can hide it and unhide it when needed
                m_VEList.Add(item);

                RenderPipelineConverter conv = m_CoreConvertersList[i];
                item.name = $"{conv.name}#{conv.container.AssemblyQualifiedName}";
                item.SetEnabled(conv.isEnabled);
                item.Q<Label>("converterName").text = conv.name;
                item.Q<Label>("converterInfo").text = conv.info;

                int id = i;
                var converterEnabledToggle = item.Q<Toggle>("converterEnabled");
                converterEnabledToggle.RegisterCallback<ClickEvent>((evt) =>
                {
                    ConverterStatusInfo(id, item);
                    InitOrConvert();
                    // This toggle needs to stop propagation since it is inside another clickable element
                    evt.StopPropagation();
                });
                var topElement = item.Q<VisualElement>("converterTopVisualElement");
                topElement.RegisterCallback<ClickEvent>((evt) =>
                {
                    ShowConverterLayout(item);
                    item.Q<VisualElement>("allNoneVE").style.display = DisplayStyle.Flex;
                    item.Q<Label>("all").RegisterCallback<ClickEvent>(evt =>
                    {
                        ToggleAllNone(evt, id, true, item);
                    });
                    item.Q<Label>("none").RegisterCallback<ClickEvent>(evt =>
                    {
                        ToggleAllNone(evt, id, false, item);
                    });
                });

                // setup the images
                item.Q<Image>("pendingImage").image = CoreEditorStyles.iconPending;
                item.Q<Image>("pendingImage").tooltip = "Pending";
                var pendingLabel = item.Q<Label>("pendingLabel");
                item.Q<Image>("warningImage").image = CoreEditorStyles.iconWarn;
                item.Q<Image>("warningImage").tooltip = "Warnings";
                var warningLabel = item.Q<Label>("warningLabel");
                item.Q<Image>("errorImage").image = CoreEditorStyles.iconFail;
                item.Q<Image>("errorImage").tooltip = "Failed";
                var errorLabel = item.Q<Label>("errorLabel");
                item.Q<Image>("successImage").image = CoreEditorStyles.iconComplete;
                item.Q<Image>("successImage").tooltip = "Success";
                var successLabel = item.Q<Label>("successLabel");

                converterEnabledToggle.bindingPath =
                    $"{nameof(m_ConverterStates)}.Array.data[{i}].{nameof(ConverterState.isActive)}";
                pendingLabel.bindingPath =
                    $"{nameof(m_ConverterStates)}.Array.data[{i}].{nameof(ConverterState.pending)}";
                warningLabel.bindingPath =
                    $"{nameof(m_ConverterStates)}.Array.data[{i}].{nameof(ConverterState.warnings)}";
                errorLabel.bindingPath =
                    $"{nameof(m_ConverterStates)}.Array.data[{i}].{nameof(ConverterState.errors)}";
                successLabel.bindingPath =
                    $"{nameof(m_ConverterStates)}.Array.data[{i}].{nameof(ConverterState.success)}";

                ConverterStatusInfo(id, item);

                VisualElement child = item;
                ListView listView = child.Q<ListView>("converterItems");

                listView.showBoundCollectionSize = false;
                listView.bindingPath = $"{nameof(m_ConverterStates)}.Array.data[{i}].{nameof(ConverterState.items)}";

                // Update the amount of things to convert
                UpdateSelectedConverterItems(id, child);

                listView.makeItem = () =>
                {
                    var convertItem = converterItem.CloneTree();
                    // Adding the contextual menu for each item
                    convertItem.AddManipulator(new ContextualMenuManipulator(evt => AddToContextMenu(evt, id)));
                    return convertItem;
                };

                listView.bindItem = (element, index) =>
                {
                    m_SerializedObject.Update();
                    var property = m_SerializedObject.FindProperty($"{listView.bindingPath}.Array.data[{index}]");

                    // ListView doesn't bind the child elements for us properly, so we do that for it
                    // In the UXML our root is a BindableElement, as we can't bind otherwise.
                    var bindable = (BindableElement) element;
                    bindable.BindProperty(property);

                    // Adding index here to userData so it can be retrieved later
                    element.userData = index;

                    Status status = (Status) property.FindPropertyRelative("status").enumValueIndex;
                    string info = property.FindPropertyRelative("message").stringValue;

                    element.Q<Toggle>("converterItemActive").RegisterCallback<ClickEvent>((evt) =>
                    {
                        UpdateSelectedConverterItems(id, child);
                        DeselectAllNoneLabels(item);
                    });

                    ConverterItemDescriptor convItemDesc = m_ItemsToConvert[id].itemDescriptors[index];

                    element.Q<Label>("converterItemName").text = convItemDesc.name;
                    element.Q<Label>("converterItemPath").text = convItemDesc.info;

                    if (!string.IsNullOrEmpty(convItemDesc.helpLink))
                    {
                        element.Q<Image>("converterItemHelpIcon").image = CoreEditorStyles.iconHelp;
                        element.Q<Image>("converterItemHelpIcon").tooltip = convItemDesc.helpLink;
                    }

                    // Changing the icon here depending on the status.
                    Texture2D icon = null;

                    switch (status)
                    {
                        case Status.Pending:
                            icon = CoreEditorStyles.iconPending;
                            break;
                        case Status.Error:
                            icon = CoreEditorStyles.iconFail;
                            break;
                        case Status.Warning:
                            icon = CoreEditorStyles.iconWarn;
                            break;
                        case Status.Success:
                            icon = CoreEditorStyles.iconComplete;
                            break;
                    }

                    element.Q<Image>("converterItemStatusIcon").image = icon;
                    element.Q<Image>("converterItemStatusIcon").tooltip = info;
                };
#if UNITY_2022_2_OR_NEWER
                listView.selectionChanged += obj => { m_CoreConvertersList[id].OnClicked(listView.selectedIndex); };
#else
                listView.onSelectionChange += obj => { m_CoreConvertersList[id].OnClicked(listView.selectedIndex); };
#endif
                listView.unbindItem = (element, index) =>
                {
                    var bindable = (BindableElement)element;
                    bindable.Unbind();
                };

                m_ScrollView.Add(item);
            }

            InitOrConvert();
            HideUnhideConverters();
            rootVisualElement.Bind(m_SerializedObject);
        }

        private void HideUnhideConverters()
        {
            var type = currentContainer.GetType();
            if (DocumentationUtils.TryGetHelpURL(type, out var url))
            {
                m_ContainerHelpButton.style.display = DisplayStyle.Flex;
            }
            else
            {
                m_ContainerHelpButton.style.display = DisplayStyle.None;
            }

            foreach (VisualElement childElement in m_ScrollView.Q<VisualElement>().Children())
            {
                var container = Type.GetType(childElement.name.Split('#').Last());
                if (container == type)
                {
                    childElement.style.display = DisplayStyle.Flex;
                }
                else
                {
                    childElement.style.display = DisplayStyle.None;
                }
            }
        }

        void DeselectAllNoneLabels(VisualElement item)
        {
            item.Q<Label>("all").AddToClassList("not_selected");
            item.Q<Label>("all").RemoveFromClassList("selected");

            item.Q<Label>("none").AddToClassList("not_selected");
            item.Q<Label>("none").RemoveFromClassList("selected");
        }

        void GetAndSetData(int i, Action onAllConvertersCompleted = null)
        {
            // This need to be in Init method
            // Need to get the assets that this converter is converting.
            // Need to return Name, Path, Initial info, Help link.
            // New empty list of ConverterItemInfos
            List<ConverterItemDescriptor> converterItemInfos = new List<ConverterItemDescriptor>();
            var initCtx = new InitializeConverterContext { items = converterItemInfos };
            var conv = m_CoreConvertersList[i];

            m_ConverterStates[i].isLoading = true;

            // This should also go to the init method
            // This will fill out the converter item infos list
            int id = i;
            conv.OnInitialize(initCtx, OnConverterCompleteDataCollection);

            void OnConverterCompleteDataCollection()
            {
                // Set the item infos list to to the right index
                m_ItemsToConvert[id] = new ConverterItems { itemDescriptors = converterItemInfos };
                m_ConverterStates[id].items = new List<ConverterItemState>(converterItemInfos.Count);

                // Default all the entries to true
                for (var j = 0; j < converterItemInfos.Count; j++)
                {
                    string message = string.Empty;
                    Status status;
                    bool active = true;
                    // If this data hasn't been filled in from the init phase then we can assume that there are no issues / warnings
                    if (string.IsNullOrEmpty(converterItemInfos[j].warningMessage))
                    {
                        status = Status.Pending;
                    }
                    else
                    {
                        status = Status.Warning;
                        message = converterItemInfos[j].warningMessage;
                        active = false;
                        m_ConverterStates[id].warnings++;
                    }

                    m_ConverterStates[id].items.Add(new ConverterItemState
                    {
                        isActive = active,
                        message = message,
                        status = status,
                        hasConverted = false,
                    });
                }

                m_ConverterStates[id].isLoading = false;
                m_ConverterStates[id].isInitialized = true;

                // Making sure that the pending amount is set to the amount of items needs converting
                m_ConverterStates[id].pending = m_ConverterStates[id].items.Count;

                EditorUtility.SetDirty(this);
                m_SerializedObject.ApplyModifiedProperties();

                CheckAllConvertersCompleted();
                InitOrConvert();
            }

            void CheckAllConvertersCompleted()
            {
                int convertersToInitialize = 0;
                int convertersInitialized = 0;

                for (var j = 0; j < m_ConverterStates.Count; j++)
                {
                    var converter = m_ConverterStates[j];

                    // Skip inactive converters
                    if (!converter.isActiveAndEnabled)
                        continue;

                    if (converter.isInitialized)
                        convertersInitialized++;
                    else
                        convertersToInitialize++;
                }

                var sum = convertersToInitialize + convertersInitialized;

                Assert.IsFalse(sum == 0);

                // Show our progress so far
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayProgressBar($"Initializing converters", $"Initializing converters ({convertersInitialized}/{sum})...", (float)convertersInitialized / sum);

                // If all converters are initialized call the complete callback
                if (convertersToInitialize == 0)
                {
                    onAllConvertersCompleted?.Invoke();
                }
            }
        }

        void InitializeAndConvert(ClickEvent evt)
        {
            m_InitAndConvert = ShouldCreateSearchIndex();

            InitializeAllActiveConverters(evt);
            if (!m_InitAndConvert)
            {
                Convert(evt);
            }
        }

        void InitializeAllActiveConverters(ClickEvent evt)
        {
            if (!SaveCurrentSceneAndContinue()) return;

            // If we use search index, go async
            if (ShouldCreateSearchIndex())
            {
                // Save the current worker count. So it can be reset after the index file has been created.
                m_WorkerCount = AssetDatabase.DesiredWorkerCount;
                AssetDatabase.ForceToDesiredWorkerCount();

                AssetDatabase.DesiredWorkerCount = System.Convert.ToInt32(Math.Ceiling(Environment.ProcessorCount * 0.8));
                CreateSearchIndex(m_URPConverterIndex);
            }
            // Otherwise do everything directly
            else
            {
                ConverterCollectData(() => { EditorUtility.ClearProgressBar(); });
            }

            void CreateSearchIndex(string name)
            {
                // Create <guid>.index in the project
                var title = $"Building {name} search index";
                EditorUtility.DisplayProgressBar(title, "Creating search index...", -1f);

                Search.SearchService.CreateIndex(name, IndexingOptions.Temporary | IndexingOptions.Extended,
                    new[] { "Assets" },
                    new[] { ".prefab", ".unity", ".asset" },
                    null, OnSearchIndexCreated);
            }

            void OnSearchIndexCreated(string name, string path, Action onComplete)
            {
                EditorUtility.ClearProgressBar();
                ConverterCollectData(() =>
                {
                    if (m_InitAndConvert)
                    {
                        Convert(null);
                        m_InitAndConvert = false;
                    }
                    EditorUtility.ClearProgressBar();
                    AssetDatabase.DesiredWorkerCount = m_WorkerCount;
                    AssetDatabase.ForceToDesiredWorkerCount();

                    RecreateUI();
                    onComplete();
                });
            }

            void ConverterCollectData(Action onConverterDataCollectionComplete)
            {
                EditorUtility.DisplayProgressBar($"Initializing converters", $"Initializing converters...", -1f);

                int convertersToConvert = 0;
                for (int i = 0; i < m_ConverterStates.Count; ++i)
                {
                    if (m_ConverterStates[i].requiresInitialization)
                    {
                        convertersToConvert++;
                        GetAndSetData(i, onConverterDataCollectionComplete);
                    }
                }

                // If we did not kick off any converter initialization
                // We can complete everything immediately
                if (convertersToConvert == 0)
                {
                    onConverterDataCollectionComplete?.Invoke();
                }
            }

            RecreateUI();
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

        bool ShouldCreateSearchIndex()
        {
            for (int i = 0; i < m_ConverterStates.Count; ++i)
            {
                if (m_ConverterStates[i].requiresInitialization)
                {
                    var converter = m_CoreConvertersList[i];
                    if (converter.needsIndexing)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        void AddToContextMenu(ContextualMenuPopulateEvent evt, int coreConverterIndex)
        {
            var ve = (VisualElement)evt.target;
            // Checking if this context menu should be enabled or not
            var isActive = m_ConverterStates[coreConverterIndex].items[(int)ve.userData].isActive &&
                !m_ConverterStates[coreConverterIndex].items[(int)ve.userData].hasConverted;

            evt.menu.AppendAction("Run converter for this asset",
                e =>
                {
                    ConvertIndex(coreConverterIndex, (int)ve.userData);
                    // Refreshing the list to show the new state
                    m_ConverterSelectedVE.Q<ListView>("converterItems").Rebuild();
                },
                isActive ? DropdownMenuAction.AlwaysEnabled : DropdownMenuAction.AlwaysDisabled);
        }

        void UpdateInfo(int stateIndex, RunItemContext ctx)
        {
            if (ctx.didFail)
            {
                m_ConverterStates[stateIndex].items[ctx.item.index].message = ctx.info;
                m_ConverterStates[stateIndex].items[ctx.item.index].status = Status.Error;
                m_ConverterStates[stateIndex].errors++;
            }
            else
            {
                m_ConverterStates[stateIndex].items[ctx.item.index].status = Status.Success;
                m_ConverterStates[stateIndex].success++;
            }

            m_ConverterStates[stateIndex].pending--;

            // Making sure that this is set here so that if user is clicking Convert again it will not run again.
            ctx.hasConverted = true;

            VisualElement child = m_ScrollView[stateIndex];
            child.Q<ListView>("converterItems").Rebuild();
        }

        void Convert(ClickEvent evt)
        {
            // Ask to save save the current open scene and after the conversion is done reload the same scene.
            if (!SaveCurrentSceneAndContinue()) return;
            string currentScenePath = SceneManager.GetActiveScene().path;

            List<ConverterState> activeConverterStates = new List<ConverterState>();

            // Getting all the active converters to use in the cancelable progressbar
            foreach (ConverterState state in m_ConverterStates)
            {
                if (state.isActive && state.isInitialized)
                {
                    activeConverterStates.Add(state);
                }
            }

            int currentCount = 0;
            int activeConvertersCount = activeConverterStates.Count;
            foreach (ConverterState activeConverterState in activeConverterStates)
            {
                currentCount++;
                var index = activeConverterState.index;
                m_CoreConvertersList[index].OnPreRun();
                var converterName = m_CoreConvertersList[index].name;
                var itemCount = m_ItemsToConvert[index].itemDescriptors.Count;
                string progressTitle = $"{converterName}           Converter : {currentCount}/{activeConvertersCount}";
                for (var j = 0; j < itemCount; j++)
                {
                    if (activeConverterState.items[j].isActive)
                    {
                        if (EditorUtility.DisplayCancelableProgressBar(progressTitle,
                            string.Format("({0} of {1}) {2}", j, itemCount, m_ItemsToConvert[index].itemDescriptors[j].info),
                            (float)j / (float)itemCount))
                            break;
                        ConvertIndex(index, j);
                    }
                }
                m_CoreConvertersList[index].OnPostRun();
                AssetDatabase.SaveAssets();
                EditorUtility.ClearProgressBar();
            }

            // Checking if we have changed current scene. If we have we reload the old scene we started from
            if (!string.IsNullOrEmpty(currentScenePath) && currentScenePath != SceneManager.GetActiveScene().path)
            {
                EditorSceneManager.OpenScene(currentScenePath);
            }

            RecreateUI();
        }

        void ConvertIndex(int coreConverterIndex, int index)
        {
            if (!m_ConverterStates[coreConverterIndex].items[index].hasConverted)
            {
                m_ConverterStates[coreConverterIndex].items[index].hasConverted = true;
                var item = new ConverterItemInfo()
                {
                    index = index,
                    descriptor = m_ItemsToConvert[coreConverterIndex].itemDescriptors[index],
                };
                var ctx = new RunItemContext(item);
                m_CoreConvertersList[coreConverterIndex].OnRun(ref ctx);
                UpdateInfo(coreConverterIndex, ctx);
            }
        }
    }
}
