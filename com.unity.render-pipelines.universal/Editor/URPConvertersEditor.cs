using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEditor;
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
public class URPConvertersEditor : EditorWindow
{
    public VisualTreeAsset converterEditorAsset;
    public VisualTreeAsset converterListAsset;
    public VisualTreeAsset converterItem;

    ScrollView m_ScrollView;
    List<CoreConverter> m_CoreConvertersList = new List<CoreConverter>();
    // This list needs to be as long as the amount of converters
    List<List<ConverterItemInfo>> m_ItemsToConvert = new List<List<ConverterItemInfo>>();
    SerializedObject m_SerializedObject;

    // This is a list of Converter States which holds a list of which converter items/assets are active
    // There is one for each Converter.
    [SerializeField]
    List<ConverterState> m_ConverterStates = new List<ConverterState>();

    [MenuItem("URPConverter/URPConverter")]
    public static void ShowWindow()
    {
        URPConvertersEditor wnd = GetWindow<URPConvertersEditor>();
        wnd.titleContent = new GUIContent("Universal Render Pipeline Converters");
    }

    void OnEnable()
    {
        var converters = TypeCache.GetTypesDerivedFrom<CoreConverter>();
        for (int i = 0; i < converters.Count; ++i)
        {
            // Iterate over the converters
            CoreConverter conv = (CoreConverter)Activator.CreateInstance(converters[i]);
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
            // This list need to have the same amount of entries as teh converters
            List<ConverterItemInfo> converterItemInfos = new List<ConverterItemInfo>();
            m_ItemsToConvert.Add(converterItemInfos);

            // // This need to be in Init method
            // // Need to get the assets that this converter is converting.
            // // Need to return Name, Path, Initial info, Help link.
            // // New empty list of ConverterItemInfos
            // List<ConverterItemInfo> converterItemInfos = new List<ConverterItemInfo>();
            // var initCtx = new InitializeConverterContext { m_Items = converterItemInfos };
            //
            // // This should also go to the init method
            // // This will fill out the converter item infos list
            // conv.OnInitialize(initCtx);
            //
            // // Add the item infos list to a new list of items to convert
            // m_ItemsToConvert.Add(converterItemInfos);
            //
            // // Create a new ConvertState which holds the active state of each item/asset
            // var converterState = new ConverterState
            // {
            //     isActive = true,
            //     items = new List<ConverterItemState>(converterItemInfos.Count)
            // };
            // // Default all the entries to true
            // for (var j = 0; j < converterItemInfos.Count; j++)
            // {
            //     converterState.items.Add(new ConverterItemState
            //     {
            //         isActive = true
            //     });
            // }
            // // Add this converterState to the list of converterStates.
            // m_ConverterStates.Add(converterState);
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


            // // This should be in the INIT
            // // Get the ListView for the converter items
            // ListView listView = item.Q<ListView>("converterItems");
            //
            // var converterItemInfos = m_ItemsToConvert[i];
            // // Update the amount of things to convert
            // item.Q<Label>("converterStats").text = $"{converterItemInfos.Count} items";
            //
            // listView.makeItem = converterItem.CloneTree;
            // listView.showBoundCollectionSize = false;
            //
            // listView.bindingPath = $"{nameof(m_ConverterStates)}.Array.data[{i}].{nameof(ConverterState.items)}";
            // // I would like this to work, have a separate method and not inlined like this
            // var i1 = i;
            // listView.bindItem = (element, index) =>
            // {
            //     // ListView doesn't bind the child elements for us properly, so we do that for it
            //     var property = m_SerializedObject.FindProperty($"{listView.bindingPath}.Array.data[{index}]");
            //     // In the UXML our root is a BindableElement, as we can't bind otherwise.
            //     var bindable = (BindableElement)element;
            //     bindable.BindProperty(property);
            //
            //     ConverterItemInfo convItem = converterItemInfos[index];
            //
            //     element.Q<Label>("converterItemName").text = convItem.name;
            //     element.Q<Label>("converterItemPath").text = convItem.path;
            //
            //     var imgHelp = EditorGUIUtility.FindTexture("_Help");
            //     element.Q<Image>("converterItemHelpIcon").image = imgHelp;
            //     element.Q<Image>("converterItemHelpIcon").tooltip = convItem.helpLink;
            //
            //     // Changing the icon here depending on the info.
            //     // If there is some info here we show the "warning icon"
            //     // If the string is empty we show the pending conversion icon.
            //     if (!String.IsNullOrEmpty(convItem.initialInfo))
            //     {
            //         var imgWarn = EditorGUIUtility.FindTexture("_Help");
            //         element.Q<Image>("converterItemStatusIcon").image = imgWarn;
            //         element.Q<Image>("converterItemStatusIcon").tooltip = convItem.initialInfo;
            //     }
            // };
            // listView.unbindItem = (element, index) =>
            // {
            //     var bindable = (BindableElement)element;
            //     bindable.Unbind();
            // };
            // listView.Refresh();
            //
            // // When right clicking an item it should pop up a small menu with 2 entries
            // // I also would like this a separate method instead of inline.
            // listView.onSelectionChange += obj =>
            // {
            //     //conv.PrintMe(listView.selectedIndex);
            // };
            // ////

            m_ScrollView.Add(item);
            //rootVisualElement.Bind(m_SerializedObject);
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
            if (m_ConverterStates[i].isActive)
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
                //m_ItemsToConvert.Add(converterItemInfos);
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
                //m_ConverterStates.Add(converterState);

                m_ConverterStates[i].isInitialized = true;
            }
        }
    }

    void Init(ClickEvent evt)
    {
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
                listView.itemsSource = converterItemInfos;

                listView.bindingPath = $"{nameof(m_ConverterStates)}.Array.data[{i}].{nameof(ConverterState.items)}";
                // I would like this to work, have a separate method and not inlined like this
                //var i1 = i;
                listView.bindItem = (element, index) =>
                {
                    // ListView doesn't bind the child elements for us properly, so we do that for it
                    //Debug.Log($"{listView.bindingPath}.Array.data[{index}]");
                    // Can't get this binding to work again. :/
                    //var property = m_SerializedObject.FindProperty($"{listView.bindingPath}.Array.data[{index}]");
                    // In the UXML our root is a BindableElement, as we can't bind otherwise.
                    //var bindable = (BindableElement)element;
                    //Debug.Log(property);
                    //bindable.BindProperty(property);

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
                ////
            }
        }
    }

    void Convert(ClickEvent evt)
    {
        for (int i = 0; i < m_ConverterStates.Count; ++i)
        {
            var state = m_ConverterStates[i];
            if (state.isActive && state.isInitialized)
            {
                Debug.Log(m_CoreConvertersList[i].name);
                var items = new List<ConverterItemInfo>(m_ItemsToConvert[i]);
                Debug.Log($"Items before: {items.Count}");
                for (var j = state.items.Count - 1; j >= 0; j--)
                {
                    if (!state.items[j].isActive) items.RemoveAt(j);
                }

                Debug.Log($"Items after: {items.Count}");
                var ctx = new RunConverterContext { m_Items = items };
                m_CoreConvertersList[i].OnRun(ctx);
            }
        }
    }
}
