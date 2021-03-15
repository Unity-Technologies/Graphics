using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

[Serializable]
class ConverterState
{
    public List<bool> itemStatuses;
}

public class URPConvertersEditor : EditorWindow
{
    public VisualTreeAsset converterEditorAsset;
    public VisualTreeAsset converterListAsset;
    public VisualTreeAsset converterItem;

    ScrollView scrollView;
    List<CoreConverter> coreConvertersList = new List<CoreConverter>();
    List<List<ConverterItemInfo>> itemsToConvert = new List<List<ConverterItemInfo>>();
    SerializedObject m_SerializedObject;

    [SerializeField]
    List<ConverterState> m_ConverterStates = new List<ConverterState>();

    [MenuItem("URPConverter/URPConverter")]
    public static void ShowWindow()
    {
        URPConvertersEditor wnd = GetWindow<URPConvertersEditor>();
        wnd.titleContent = new GUIContent("Universal Render Pipeline Converters");
    }

    public void CreateGUI()
    {
        m_SerializedObject = new SerializedObject(this);
        // Getting all the converters
        var converters = TypeCache.GetTypesDerivedFrom<CoreConverter>();
        converterEditorAsset.CloneTree(rootVisualElement);
        rootVisualElement.Bind(m_SerializedObject);

        // Getting the scrollview where the converters should be added
        scrollView = rootVisualElement.Q<ScrollView>("convertersScrollView");
        for (int i = 0; i < converters.Count; ++i)
        {
            // Iterate over the converters
            CoreConverter conv = (CoreConverter)Activator.CreateInstance(converters[i]);
            coreConvertersList.Add(conv);
            // Making an item using the converterListAsset as a template.
            // Then adding the information needed for each converter
            // Why do I need to create a new visual element here? MTT
            VisualElement item = new VisualElement();
            converterListAsset.CloneTree(item);

            item.Q<Label>("converterName").text = conv.name;
            item.Q<VisualElement>("converterTopVisualElement").tooltip = conv.info;

            // Get the ListView for the converter items
            ListView listView = item.Q<ListView>("converterItems");

            // Need to get the assets that this converter is converting.
            // Need to return Name, Path, Initial info, Help link.
            List<ConverterItemInfo> converterItemInfos = new List<ConverterItemInfo>();
            var initCtx = new InitializeConverterContext { m_Items = converterItemInfos };

            conv.OnInitialize(initCtx);
            itemsToConvert.Add(converterItemInfos);
            var converterState = new ConverterState { itemStatuses = new List<bool>(converterItemInfos.Count)};
            for (var j = 0; j < converterItemInfos.Count; j++)
            {
                converterState.itemStatuses.Add(true);
            }
            m_ConverterStates.Add(converterState);

            // Update the amount of things to convert
            item.Q<Label>("converterStats").text = $"{converterItemInfos.Count} items";

            listView.itemsSource = converterItemInfos;
            listView.makeItem = converterItem.CloneTree;
            // I would like this to work, have a separate method and not inlined like this
            //listView.bindItem = BindItem;
            listView.bindItem = (element, index) =>
            {
                ConverterItemInfo convItem = converterItemInfos[index];
                // Toggle for active. This sets if the asset will be upgraded/converted or not MTT
                var toggle = element.Q<Toggle>("converterItemActive");
                toggle.bindingPath = $"{nameof(m_ConverterStates)}[{i}].{nameof(ConverterState.itemStatuses)}[{index}]";

                element.Q<Label>("converterItemName").text = convItem.name;
                element.Q<Label>("converterItemPath").text = convItem.path;

                // Help icon
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

            // When right clicking an item it should pop up a small menu with 2 entries
            // I also would like this a separate method instead of inline.
            listView.onSelectionChange += obj =>
            {
                //conv.PrintMe(listView.selectedIndex);
            };

            scrollView.Add(item);
        }

        var button = rootVisualElement.Q<Button>("convertButton");
        button.RegisterCallback<ClickEvent>(evt =>
        {
            for (int i = 0; i < scrollView.childCount; ++i)
            {
                VisualElement child = scrollView[i];
                if (child.Q<Toggle>("converterEnabled").value)
                {
                    var items = itemsToConvert[i];
                    for (var j = m_ConverterStates.Count - 1; j >= 0; j--)
                    {
                        items.RemoveAt(j);
                    }
                    var ctx = new RunConverterContext {m_Items = items};
                    coreConvertersList[i].OnRun(ctx);
                }
            }
        });
    }
}
