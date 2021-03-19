using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

[Serializable]
class ConverterItemState
{
    public bool isActive;
}

// Each converter uses the active bool
// Each converter has a list of active items/assets
// We do this so that we can use the binding system of the UI Elements
[Serializable]
class ConverterState
{
    public bool isActive;
    public bool isInitialized;
    public List<ConverterItemState> items;
}

[Serializable]
public class RenderPipelineConvertersEditor : EditorWindow
{
    public VisualTreeAsset converterEditorAsset;
    public VisualTreeAsset converterListAsset;
    public VisualTreeAsset converterItem;

    ScrollView m_ScrollView;
    List<RenderPipelineConverter> m_CoreConvertersList = new List<RenderPipelineConverter>();
    // This list needs to be as long as the amount of converters
    List<List<ConverterItemInfo>> m_ItemsToConvert = new List<List<ConverterItemInfo>>();
    SerializedObject m_SerializedObject;

    // This is a list of Converter States which holds a list of which converter items/assets are active
    // There is one for each Converter.
    [SerializeField]
    List<ConverterState> m_ConverterStates = new List<ConverterState>();

    [MenuItem("RenderPipelineConverter/RenderPipelineConverter")]
    public static void ShowWindow()
    {
        RenderPipelineConvertersEditor wnd = GetWindow<RenderPipelineConvertersEditor>();
        wnd.titleContent = new GUIContent("Render Pipeline Converters");
    }

    void OnEnable()
    {
        var converters = TypeCache.GetTypesDerivedFrom<RenderPipelineConverter>();
        for (int i = 0; i < converters.Count; ++i)
        {
            // Iterate over the converters
            RenderPipelineConverter conv = (RenderPipelineConverter)Activator.CreateInstance(converters[i]);
            m_CoreConvertersList.Add(conv);

            // Create a new ConvertState which holds the active state of the converter
            var converterState = new ConverterState
            {
                isActive = true,
                isInitialized = false,
                items = null
            };
            m_ConverterStates.Add(converterState);

            // This just creates empty entries in the m_ItemsToConvert.
            // This list need to have the same amount of entries as the converters
            List<ConverterItemInfo> converterItemInfos = new List<ConverterItemInfo>();
            m_ItemsToConvert.Add(converterItemInfos);
        }
    }

    public void CreateGUI()
    {
        m_SerializedObject = new SerializedObject(this);
        converterEditorAsset.CloneTree(rootVisualElement);

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
            item.Q<Label>("converterName").text = conv.name;
            item.Q<VisualElement>("converterTopVisualElement").tooltip = conv.info;

            var converterEnabledToggle = item.Q<Toggle>("converterEnabled");
            converterEnabledToggle.bindingPath = $"{nameof(m_ConverterStates)}.Array.data[{i}].{nameof(ConverterState.isActive)}";

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
            // Checking if this converter should get the data
            if (m_ConverterStates[i].isActive && !m_ConverterStates[i].isInitialized)
            {
                // This need to be in Init method
                // Need to get the assets that this converter is converting.
                // Need to return Name, Path, Initial info, Help link.
                // New empty list of ConverterItemInfos
                List<ConverterItemInfo> converterItemInfos = new List<ConverterItemInfo>();
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
                    m_ConverterStates[i].items.Add(new ConverterItemState
                    {
                        isActive = true
                    });
                }

                // Add this converterState to the list of converterStates.
                m_ConverterStates[i].isInitialized = true;
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
            VisualElement child = m_ScrollView[i];
            if (m_ConverterStates[i].isActive)
            {
                // This should be in the INIT
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

                    ConverterItemInfo convItem = converterItemInfos[index];

                    element.Q<Label>("converterItemName").text = convItem.name;
                    element.Q<Label>("converterItemPath").text = convItem.path;

                    var imgHelp = EditorGUIUtility.FindTexture("_Help");
                    element.Q<Image>("converterItemHelpIcon").image = imgHelp;
                    element.Q<Image>("converterItemHelpIcon").tooltip = convItem.helpLink;

                    // Changing the icon here depending on the info.
                    // If there is some info here we show the "warning icon"
                    // If the string is empty we show the pending conversion icon.
                    if (!String.IsNullOrEmpty(convItem.initialInfo))
                    {
                        var imgWarn = EditorGUIUtility.FindTexture("_Help");
                        element.Q<Image>("converterItemStatusIcon").image = imgWarn;
                        element.Q<Image>("converterItemStatusIcon").tooltip = convItem.initialInfo;
                    }
                };
                listView.unbindItem = (element, index) =>
                {
                    var bindable = (BindableElement)element;
                    bindable.Unbind();
                };
                listView.Refresh();

                // When right clicking an item it should pop up a small menu with 2 entries
                // I also would like this a separate method instead of inline.
                listView.onSelectionChange += obj =>
                {
                    //conv.PrintMe(listView.selectedIndex);
                };
            }
        }

        rootVisualElement.Bind(m_SerializedObject);
    }

    void Convert(ClickEvent evt)
    {
        for (int i = 0; i < m_ConverterStates.Count; ++i)
        {
            var state = m_ConverterStates[i];
            if (state.isActive && state.isInitialized)
            {
                var items = new List<ConverterItemInfo>(m_ItemsToConvert[i]);
                for (var j = state.items.Count - 1; j >= 0; j--)
                {
                    if (!state.items[j].isActive) items.RemoveAt(j);
                }

                var ctx = new RunConverterContext { m_Items = items };
                m_CoreConvertersList[i].OnRun(ctx);
            }
        }
    }
}
