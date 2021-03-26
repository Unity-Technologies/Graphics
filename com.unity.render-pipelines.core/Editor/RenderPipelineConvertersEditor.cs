using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.UIElements;
using UnityEngine.UIElements;


enum Status
{
    Pending,
    Warning,
    Error,
    Success
}

[Serializable]
class ConverterItemState
{
    public bool isActive;

    // Maybe add the conversion info here
    public string info;

    internal Status status;
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
    public bool isInitialized;
    public List<ConverterItemState> items;
    // This will state if there is a warning or not from the init phase.
    // This is here so
    //public bool hasWarnings;

    public int pending;
    public int warnings;
    public int errors;
    public int success;
}

[Serializable]
public class RenderPipelineConvertersEditor : EditorWindow
{
    Texture2D kImgWarn;
    Texture2D kImgHelp;
    Texture2D kImgFail;
    Texture2D kImgSuccess;
    Texture2D kImgPending;

    public VisualTreeAsset converterEditorAsset;
    public VisualTreeAsset converterListAsset;
    public VisualTreeAsset converterItem;

    ScrollView m_ScrollView;
    DropdownField m_ConversionsDropdownField;

    List<RenderPipelineConverter> m_CoreConvertersList = new List<RenderPipelineConverter>();
    // This list needs to be as long as the amount of converters
    List<List<ConverterItemDescriptor>> m_ItemsToConvert = new List<List<ConverterItemDescriptor>>();
    SerializedObject m_SerializedObject;

    List<string> conversionsChoices = new List<string>();
    // This is a list of Converter States which holds a list of which converter items/assets are active
    // There is one for each Converter.
    [SerializeField]
    List<ConverterState> m_ConverterStates = new List<ConverterState>();


    TypeCache.TypeCollection conversions;

#if RENDER_PIPELINE_CONVERTER
    [MenuItem("RenderPipelineConverter/RenderPipelineConverter")]
#endif
    public static void ShowWindow()
    {
        RenderPipelineConvertersEditor wnd = GetWindow<RenderPipelineConvertersEditor>();
        wnd.titleContent = new GUIContent("Render Pipeline Converters");
    }

    //d__Help.png
    void OnEnable()
    {
        kImgWarn = EditorGUIUtility.FindTexture("console.warnicon");
        kImgHelp = EditorGUIUtility.FindTexture("_Help");
        kImgFail = EditorGUIUtility.FindTexture("console.erroricon");
        kImgSuccess = EditorGUIUtility.FindTexture("TestPassed");
        kImgPending = EditorGUIUtility.FindTexture("Toolbar Minus");

        // This is the drop down choices.
        conversions = TypeCache.GetTypesDerivedFrom<RenderPipelineConversion>();
        for (int j = 0; j < conversions.Count; j++)
        {
            // Iterate over the converters
            RenderPipelineConversion conversion = (RenderPipelineConversion)Activator.CreateInstance(conversions[j]);
            conversionsChoices.Add(conversion.name);
        }


        var converters = TypeCache.GetTypesDerivedFrom<RenderPipelineConverter>();
        for (int i = 0; i < converters.Count; ++i)
        {
            // Iterate over the converters
            RenderPipelineConverter conv = (RenderPipelineConverter)Activator.CreateInstance(converters[i]);
            m_CoreConvertersList.Add(conv);

            // Create a new ConvertState which holds the active state of the converter
            var converterState = new ConverterState
            {
                isEnabled = conv.enabled(),
                isActive = true,
                isInitialized = false,
                items = null,
                //hasWarnings = false
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

        // Adding the different conversions
        // Right now the .choices attribute is internal so we can not add it. This will be public in the future.
        m_ConversionsDropdownField = rootVisualElement.Q<DropdownField>("conversionDropDown");
        //m_ConversionsDropdownField.choices = conversionsChoices;

        // Getting the scrollview where the converters should be added
        m_ScrollView = rootVisualElement.Q<ScrollView>("convertersScrollView");
        for (int i = 0; i < m_CoreConvertersList.Count; ++i)
        {
            // Making an item using the converterListAsset as a template.
            // Then adding the information needed for each converter
            // Why do I need to create a new visual element here? MTT
            VisualElement item = new VisualElement();
            converterListAsset.CloneTree(item);
            var conv = m_CoreConvertersList[i];
            item.SetEnabled(conv.enabled());
            item.Q<Label>("converterName").text = conv.name;
            item.Q<Label>("converterInfo").text = conv.info;
            item.Q<VisualElement>("converterTopVisualElement").tooltip = conv.info;

            // setup the images
            item.Q<Image>("pendingImage").image = kImgPending;
            item.Q<Image>("pendingImage").tooltip = "Pending";
            var pendingLabel = item.Q<Label>("pendingLabel");
            item.Q<Image>("warningImage").image = kImgWarn;
            item.Q<Image>("warningImage").tooltip = "Warnings";
            var warningLabel = item.Q<Label>("warningLabel");
            item.Q<Image>("errorImage").image = kImgFail;
            item.Q<Image>("errorImage").tooltip = "Failed";
            var errorLabel = item.Q<Label>("errorLabel");
            item.Q<Image>("successImage").image = kImgSuccess;
            item.Q<Image>("successImage").tooltip = "Success";
            var successLabel = item.Q<Label>("successLabel");

            var converterEnabledToggle = item.Q<Toggle>("converterEnabled");
            converterEnabledToggle.bindingPath = $"{nameof(m_ConverterStates)}.Array.data[{i}].{nameof(ConverterState.isActive)}";

            pendingLabel.bindingPath = $"{nameof(m_ConverterStates)}.Array.data[{i}].{nameof(ConverterState.pending)}";
            warningLabel.bindingPath = $"{nameof(m_ConverterStates)}.Array.data[{i}].{nameof(ConverterState.warnings)}";
            errorLabel.bindingPath = $"{nameof(m_ConverterStates)}.Array.data[{i}].{nameof(ConverterState.errors)}";
            successLabel.bindingPath = $"{nameof(m_ConverterStates)}.Array.data[{i}].{nameof(ConverterState.success)}";

            m_ScrollView.Add(item);
        }
        rootVisualElement.Bind(m_SerializedObject);
        var button = rootVisualElement.Q<Button>("convertButton");
        button.RegisterCallback<ClickEvent>(Convert);

        var initButton = rootVisualElement.Q<Button>("initializeButton");
        initButton.RegisterCallback<ClickEvent>(Init);
    }

    void GetAndSetData()
    {
        for (int i = 0; i < m_ConverterStates.Count; ++i)
        {
            // Checking if this converter is enabled or not
            if (m_ConverterStates[i].isEnabled)
            {
                // Checking if this converter should get the data
                if (m_ConverterStates[i].isActive && !m_ConverterStates[i].isInitialized)
                {
                    // This need to be in Init method
                    // Need to get the assets that this converter is converting.
                    // Need to return Name, Path, Initial info, Help link.
                    // New empty list of ConverterItemInfos
                    List<ConverterItemDescriptor> converterItemInfos = new List<ConverterItemDescriptor>();
                    var initCtx = new InitializeConverterContext { m_Items = converterItemInfos };

                    var conv = m_CoreConvertersList[i];

                    // This should also go to the init method
                    // This will fill out the converter item infos list
                    conv.OnInitialize(initCtx);

                    // Set the item infos list to to the right index
                    m_ItemsToConvert[i] = converterItemInfos;
                    m_ConverterStates[i].items = new List<ConverterItemState>(converterItemInfos.Count);

                    // Default all the entries to true
                    for (var j = 0; j < converterItemInfos.Count; j++)
                    {
                        string info = "";
                        Status status;
                        // If this data hasn't been filled in from the init phase then we can assume that there are no issues / warnings
                        if (string.IsNullOrEmpty(converterItemInfos[j].initialInfo))
                        {
                            status = Status.Pending;
                        }
                        else
                        {
                            status = Status.Warning;
                            info = converterItemInfos[j].initialInfo;
                            m_ConverterStates[i].warnings++;
                        }

                        m_ConverterStates[i].items.Add(new ConverterItemState
                        {
                            isActive = true,
                            info = info,
                            status = status,
                        });
                    }

                    // Add this converterState to the list of converterStates.
                    m_ConverterStates[i].isInitialized = true;

                    // Making sure that the pending amount is set to the amount of items needs converting
                    m_ConverterStates[i].pending = m_ConverterStates[i].items.Count;
                }
            }
        }

        EditorUtility.SetDirty(this);
        m_SerializedObject.Update();
    }

    void Init(ClickEvent evt)
    {
        Undo.RegisterCompleteObjectUndo(this, "Initialize Converts");

        GetAndSetData();

        for (int i = 0; i < m_ConverterStates.Count; ++i)
        {
            var id = i;
            VisualElement child = m_ScrollView[i];
            if (m_ConverterStates[i].isActive)
            {
                // Get the ListView for the converter items
                ListView listView = child.Q<ListView>("converterItems");

                var converterItemInfos = m_ItemsToConvert[i];
                // Update the amount of things to convert
                child.Q<Label>("converterStats").text = $"{converterItemInfos.Count} items";

                listView.makeItem = converterItem.CloneTree;
                listView.showBoundCollectionSize = false;
                //listView.itemsSource = converterItemInfos;

                listView.bindingPath = $"{nameof(m_ConverterStates)}.Array.data[{i}].{nameof(ConverterState.items)}";
                // I would like this to work, have a separate method and not inlined like this
                listView.bindItem = (element, index) =>
                {
                    // ListView doesn't bind the child elements for us properly, so we do that for it
                    var property = m_SerializedObject.FindProperty($"{listView.bindingPath}.Array.data[{index}]");
                    // In the UXML our root is a BindableElement, as we can't bind otherwise.
                    var bindable = (BindableElement)element;
                    bindable.BindProperty(property);

                    ConverterItemDescriptor convItemDesc = converterItemInfos[index];

                    element.Q<Label>("converterItemName").text = convItemDesc.name;
                    element.Q<Label>("converterItemPath").text = convItemDesc.path;

                    element.Q<Image>("converterItemHelpIcon").image = kImgHelp;
                    element.Q<Image>("converterItemHelpIcon").tooltip = convItemDesc.helpLink;


                    // Changing the icon here depending on the info.
                    // If there is some info here we show the "warning icon"
                    // If the string is empty we show the pending conversion icon.

                    Status status = m_ConverterStates[id].items[index].status;
                    string info = m_ConverterStates[id].items[index].info;
                    Texture2D icon = null;

                    switch (status)
                    {
                        case Status.Pending:
                            icon = kImgPending;
                            break;
                        case Status.Error:
                            icon = kImgFail;
                            break;
                        case Status.Warning:
                            icon = kImgWarn;
                            break;
                        case Status.Success:
                            icon = kImgSuccess;
                            break;
                    }

                    // if (!String.IsNullOrEmpty(convItemDesc.initialInfo))
                    // {
                    //     element.Q<Image>("converterItemStatusIcon").image = kImgWarn;
                    //     element.Q<Image>("converterItemStatusIcon").tooltip = convItemDesc.initialInfo;
                    // }
                    // else if (element.Q<Label>("converterItemInfo").text != "")
                    // {
                    //     element.Q<Image>("converterItemStatusIcon").image = kImgFail;
                    //     element.Q<Image>("converterItemStatusIcon").tooltip = element.Q<Label>("converterItemInfo").text;
                    // }
                    // else
                    // {
                    //     element.Q<Image>("converterItemStatusIcon").image = null;
                    //     element.Q<Image>("converterItemStatusIcon").tooltip = "";
                    // }

                    element.Q<Image>("converterItemStatusIcon").image = icon;
                    element.Q<Image>("converterItemStatusIcon").tooltip = info;
                };
                listView.onSelectionChange += obj =>
                {
                    m_CoreConvertersList[id].OnClicked(listView.selectedIndex);
                };
                listView.unbindItem = (element, index) =>
                {
                    var bindable = (BindableElement)element;
                    bindable.Unbind();
                };
                listView.Refresh();

                // When right clicking an item it should pop up a small menu with 2 entries
                // I also would like this a separate method instead of inline.
            }
        }

        rootVisualElement.Bind(m_SerializedObject);
    }

    void UpdateInfo(int stateIndex, RunConverterContext ctx)
    {
        var failedItems = ctx.m_FailedItems;
        var failedCount = failedItems.Count;
        var successCount = ctx.m_SuccessfulItems.Count;

        foreach (FailedItem failedItem in failedItems)
        {
            // This put the error message onto the icon and also changes the icon to the fail one since there is binding going on in the background
            m_ConverterStates[stateIndex].items[failedItem.index].info = failedItem.message;
            m_ConverterStates[stateIndex].items[failedItem.index].status = Status.Error;
        }

        foreach (SuccessfulItem successfulItem in ctx.m_SuccessfulItems)
        {
            m_ConverterStates[stateIndex].items[successfulItem.index].status = Status.Success;
        }

        m_ConverterStates[stateIndex].success = successCount;
        m_ConverterStates[stateIndex].pending -= failedCount;
        m_ConverterStates[stateIndex].pending -= successCount;
        m_ConverterStates[stateIndex].errors = failedCount;
    }

    void Convert(ClickEvent evt)
    {
        for (int i = 0; i < m_ConverterStates.Count; ++i)
        {
            var state = m_ConverterStates[i];
            if (state.isActive && state.isInitialized)
            {
                var itemCount = m_ItemsToConvert[i].Count;
                var items = new List<ConverterItemInfo>(itemCount);
                for (var j = 0; j < itemCount; j++)
                {
                    if (state.items[j].isActive)
                    {
                        items.Add(new ConverterItemInfo
                        {
                            index = j,
                            descriptor = m_ItemsToConvert[i][j]
                        });
                    }
                }

                // Running the converter with the context
                // in the converter step the converter adds if it failed to convert
                var ctx = new RunConverterContext(items);
                m_CoreConvertersList[i].OnRun(ctx);

                UpdateInfo(i, ctx);
            }
        }
    }
}
