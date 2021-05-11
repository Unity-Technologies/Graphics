using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.Search;
using UnityEditor.UIElements;
using UnityEngine.UIElements;


namespace UnityEditor.Rendering.Universal
{
    // Status for each row item to say in which state they are in.
    // This will make sure they are showing the correct icon
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
        public List<ConverterItemState> items;

        public int pending;
        public int warnings;
        public int errors;
        public int success;
        internal int index;

        public bool shouldInit => isInitialized || !isEnabled || !isActive;
    }

    [Serializable]
    [EditorWindowTitle(title = "Render Pipeline Converters")]
    internal class RenderPipelineConvertersEditor : EditorWindow
    {
        public VisualTreeAsset converterEditorAsset;
        public VisualTreeAsset converterListAsset;
        public VisualTreeAsset converterItem;

        ScrollView m_ScrollView;
        DropdownField m_ConversionsDropdownField;

        List<RenderPipelineConverter> m_CoreConvertersList;

        // This list needs to be as long as the amount of converters
        List<List<ConverterItemDescriptor>> m_ItemsToConvert = new List<List<ConverterItemDescriptor>>();
        SerializedObject m_SerializedObject;

        List<string> m_ConversionsChoices = new List<string>();

        // This is a list of Converter States which holds a list of which converter items/assets are active
        // There is one for each Converter.
        [SerializeField] List<ConverterState> m_ConverterStates = new List<ConverterState>();

        TypeCache.TypeCollection m_ConverterContainers;

        // Name of the index file
        string m_URPConverterIndex = "URPConverterIndex";

        [MenuItem("Window/Rendering/Render Pipeline Converter", false, 50)]
        public static void ShowWindow()
        {
            RenderPipelineConvertersEditor wnd = GetWindow<RenderPipelineConvertersEditor>();
            wnd.titleContent = new GUIContent("Render Pipeline Converter");
            DontSaveToLayout(wnd);
            wnd.maxSize = new Vector2(645f, 4000f);
            wnd.minSize = new Vector2(645f, 400f);
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
            m_CoreConvertersList = new List<RenderPipelineConverter>();

            // This is the drop down choices.
            m_ConverterContainers = TypeCache.GetTypesDerivedFrom<RenderPipelineConverterContainer>();

            var converters = TypeCache.GetTypesDerivedFrom<RenderPipelineConverter>();
            for (int i = 0; i < converters.Count; ++i)
            {
                // Iterate over the converters
                RenderPipelineConverter conv = (RenderPipelineConverter)Activator.CreateInstance(converters[i]);
                m_CoreConvertersList.Add(conv);

                // Create a new ConvertState which holds the active state of the converter
                var converterState = new ConverterState
                {
                    isEnabled = conv.IsEnabled,
                    isActive = true,
                    isInitialized = false,
                    items = new List<ConverterItemState>(),
                    index = i,
                };
                m_ConverterStates.Add(converterState);

                // This just creates empty entries in the m_ItemsToConvert.
                // This list need to have the same amount of entries as the converters
                List<ConverterItemDescriptor> converterItemInfos = new List<ConverterItemDescriptor>();
                m_ItemsToConvert.Add(converterItemInfos);
            }
        }

        public void CreateGUI()
        {
            m_SerializedObject = new SerializedObject(this);
            converterEditorAsset.CloneTree(rootVisualElement);

            // This is temp now to get the information filled in
            RenderPipelineConverterContainer converterContainer =
                (RenderPipelineConverterContainer)Activator.CreateInstance(m_ConverterContainers[0]);
            rootVisualElement.Q<Label>("conversionName").text = converterContainer.name;
            rootVisualElement.Q<TextElement>("conversionInfo").text = converterContainer.info;

            rootVisualElement.Q<Image>("converterContainerHelpIcon").image = CoreEditorStyles.iconHelp;

            // Getting the scrollview where the converters should be added
            m_ScrollView = rootVisualElement.Q<ScrollView>("convertersScrollView");
            for (int i = 0; i < m_CoreConvertersList.Count; ++i)
            {
                // Making an item using the converterListAsset as a template.
                // Then adding the information needed for each converter
                VisualElement item = new VisualElement();
                converterListAsset.CloneTree(item);
                var conv = m_CoreConvertersList[i];
                item.SetEnabled(conv.IsEnabled);
                item.Q<Label>("converterName").text = conv.name;
                item.Q<Label>("converterInfo").text = conv.info;
                item.Q<VisualElement>("converterTopVisualElement").tooltip = conv.info;

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
                item.Q<Image>("successImage").image = CoreEditorStyles.iconSuccess;
                item.Q<Image>("successImage").tooltip = "Success";
                var successLabel = item.Q<Label>("successLabel");

                var converterEnabledToggle = item.Q<Toggle>("converterEnabled");
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

                VisualElement child = item;
                ListView listView = child.Q<ListView>("converterItems");

                listView.showBoundCollectionSize = false;
                listView.bindingPath = $"{nameof(m_ConverterStates)}.Array.data[{i}].{nameof(ConverterState.items)}";

                int id = i;
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
                    var bindable = (BindableElement)element;
                    bindable.BindProperty(property);

                    // Adding index here to userData so it can be retrieved later
                    element.userData = index;

                    Status status = (Status)property.FindPropertyRelative("status").enumValueIndex;
                    string info = property.FindPropertyRelative("message").stringValue;

                    // Update the amount of things to convert
                    child.Q<Label>("converterStats").text = $"{m_ItemsToConvert[id].Count} items";

                    ConverterItemDescriptor convItemDesc = m_ItemsToConvert[id][index];

                    element.Q<Label>("converterItemName").text = convItemDesc.name;
                    element.Q<Label>("converterItemPath").text = convItemDesc.info;

                    element.Q<Image>("converterItemHelpIcon").image = CoreEditorStyles.iconHelp;
                    element.Q<Image>("converterItemHelpIcon").tooltip = convItemDesc.helpLink;

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
                            icon = CoreEditorStyles.iconSuccess;
                            break;
                    }

                    element.Q<Image>("converterItemStatusIcon").image = icon;
                    element.Q<Image>("converterItemStatusIcon").tooltip = info;
                };
                listView.onSelectionChange += obj => { m_CoreConvertersList[id].OnClicked(listView.selectedIndex); };
                listView.unbindItem = (element, index) =>
                {
                    var bindable = (BindableElement)element;
                    bindable.Unbind();
                };

                m_ScrollView.Add(item);
            }

            rootVisualElement.Bind(m_SerializedObject);
            var button = rootVisualElement.Q<Button>("convertButton");
            button.RegisterCallback<ClickEvent>(Convert);

            var initButton = rootVisualElement.Q<Button>("initializeButton");
            initButton.RegisterCallback<ClickEvent>(Init);
        }

        void GetAndSetData(int i)
        {
            // This need to be in Init method
            // Need to get the assets that this converter is converting.
            // Need to return Name, Path, Initial info, Help link.
            // New empty list of ConverterItemInfos
            List<ConverterItemDescriptor> converterItemInfos = new List<ConverterItemDescriptor>();
            var initCtx = new InitializeConverterContext {items = converterItemInfos};

            var conv = m_CoreConvertersList[i];

            m_ConverterStates[i].isLoading = true;

            // This should also go to the init method
            // This will fill out the converter item infos list
            int id = i;
            conv.OnInitialize(initCtx,
                () =>
                {
                    // Set the item infos list to to the right index
                    m_ItemsToConvert[id] = converterItemInfos;
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
                });
        }

        void OnSearchIndexCreated(string name, string path, IEnumerable<SearchItem> items, Action finished)
        {
            var siObj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            Debug.Log($"Search index {name} is ready to be used", siObj);

            // TODO: Run your query on the temporary index
            var searchQuery = $"p: a={name} t:Material";
            Debug.Log($"Listing materials using <b>{searchQuery}</b>");
            UnityEditor.Search.SearchService.Request(searchQuery, (context, materials) =>
            {
                // Process materials...
                foreach (var m in materials)
                    Debug.Log(m.GetDescription(context));
            }, _ =>
                {
                    AllQueriesFinished();
                });

            void AllQueriesFinished()
            {
                Type type = typeof(EditorApplication);
                EditorApplication.CallbackFunction callback = () =>
                {
                    Debug.Log($"Project upgrading finished");
                    finished();
                };
                MethodInfo delayedMethod = type.GetMethod("CallDelayed", BindingFlags.NonPublic | BindingFlags.Static);
                object[] parameters = new object[] {callback, 3d};

                delayedMethod?.Invoke(null, parameters);
            }
        }

        void Init(ClickEvent evt)
        {
            // Need to check if the indexing should be created or not
            bool createIndex = ShouldCreateIndex();

            if (createIndex)
            {
                CreateSearchIndex(m_URPConverterIndex, OnSearchIndexCreated);
            }

            for (int i = 0; i < m_ConverterStates.Count; ++i)
            {
                if (m_ConverterStates[i].shouldInit)
                {
                    GetAndSetData(i);
                }
            }
        }

        bool ShouldCreateIndex()
        {
            for (int i = 0; i < m_ConverterStates.Count; ++i)
            {
                if (m_ConverterStates[i].shouldInit)
                {
                    var converter = m_CoreConvertersList[i];
                    if (converter.NeedsIndexing)
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
                e => { ConvertIndex(coreConverterIndex, (int)ve.userData); },
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
            child.Q<ListView>("converterItems").Refresh();
        }

        void Convert(ClickEvent evt)
        {
            List<ConverterState> activeConverterStates = new List<ConverterState>();
            // Get the names of the converters
            // Get the amount of them
            // Make the string "name x/y"

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
                var converterName = m_CoreConvertersList[index].name;
                var itemCount = m_ItemsToConvert[index].Count;
                string progressTitle = $"{converterName}           Converter : {currentCount}/{activeConvertersCount}";
                for (var j = 0; j < itemCount; j++)
                {
                    if (activeConverterState.items[j].isActive)
                    {
                        if (EditorUtility.DisplayCancelableProgressBar(progressTitle,
                            string.Format("({0} of {1}) {2}", j, itemCount, m_ItemsToConvert[index][j].info),
                            (float)j / (float)itemCount))
                            break;
                        ConvertIndex(index, j);
                    }
                }

                EditorUtility.ClearProgressBar();
            }
        }

        void ConvertIndex(int coreConverterIndex, int index)
        {
            if (!m_ConverterStates[coreConverterIndex].items[index].hasConverted)
            {
                m_ConverterStates[coreConverterIndex].items[index].hasConverted = true;
                var item = new ConverterItemInfo()
                {
                    index = index,
                    descriptor = m_ItemsToConvert[coreConverterIndex][index],
                };
                var ctx = new RunItemContext(item);
                m_CoreConvertersList[coreConverterIndex].OnRun(ref ctx);
                UpdateInfo(coreConverterIndex, ctx);
            }
        }

        /// <summary>
        /// Handler called when the temporary search index is created and ready to be used.
        /// </summary>
        /// <param name="name">Name of the search index</param>
        /// <param name="path">Asset path of the temporary index</param>
        /// <param name="items">Search results of the temporary index.</param>
        /// <param name="finished">Callback to be invoked when you are done using the temporary index.</param>
        public delegate void SearchIndexCreatedHandler(string name, string path, IEnumerable<SearchItem> items, Action finished);

        /// <summary>
        /// Create a search index. When the search index is ready to be used, callback the user code.
        /// IMPORTANT: The user code must call finished() when the search index can be disposed.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="onIndexReady"></param>
        public static void CreateSearchIndex(string name, SearchIndexCreatedHandler onIndexReady)
        {
            // Create <guid>.index in the project
            var title = $"Building {name} search index";
            EditorUtility.DisplayProgressBar(title, "Creating search index...", -1f);

            // Private implementation of a file naming function which puts the file at the selected path.
            Type assetdatabase = typeof(AssetDatabase);
            var indexPath = (string)assetdatabase.GetMethod("GetUniquePathNameAtSelectedPath", BindingFlags.NonPublic | BindingFlags.Static).Invoke(assetdatabase, new object[] {name});

            // Write search index manifest
            System.IO.File.WriteAllText(indexPath,
                @"{
                ""roots"": [""Assets""],
                ""includes"": [],
                ""excludes"": [],
                ""options"": {
                    ""types"": true,
                    ""properties"": true,
                    ""extended"": true,
                    ""dependencies"": true
                    },
                ""baseScore"": 9999
                }");

            // Import the search index
            AssetDatabase.ImportAsset(indexPath, ImportAssetOptions.ForceSynchronousImport | ImportAssetOptions.DontDownloadFromCacheServer);
            EditorApplication.delayCall += () =>
            {
                // Wait for the index to be finished
                var context = Search.SearchService.CreateContext("asset", $"p: a=\"{name}\"");
                Search.SearchService.Request(context, (_, items) =>
                {
                    // Raise onIndexReady callback
                    onIndexReady?.Invoke(name, indexPath, items, () =>
                    {
                        context?.Dispose();
                        context = null;

                        // Client code has finished with the created index. We can delete it.
                        AssetDatabase.DeleteAsset(indexPath);
                        EditorUtility.ClearProgressBar();
                    });
                });
            };
        }
    }
}
