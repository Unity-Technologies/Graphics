using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

public class URPConvertersEditor : EditorWindow
{
    public VisualTreeAsset converterEditorAsset;
    public VisualTreeAsset converterListAsset;
    public VisualTreeAsset converterItem;

    ScrollView scrollView;
    TypeCache.TypeCollection converters;
    List<CoreConverter.ConverterItemInfo> ConverterItemInfos;

    [MenuItem("URPConverter/URPConverter")]
    public static void ShowWindow()
    {
        URPConvertersEditor wnd = GetWindow<URPConvertersEditor>();
        wnd.titleContent = new GUIContent("Universal Render Pipeline Converters");
    }

    public void CreateGUI()
    {
        // Getting all the converters
        converters = TypeCache.GetTypesDerivedFrom<CoreConverter>();
        converterEditorAsset.CloneTree(rootVisualElement);

        // Getting the scrollview where the converters should be added
        scrollView = rootVisualElement.Q<ScrollView>("convertersScrollView");
        for (int i = 0; i < converters.Count; ++i)
        {
            // Iterate over the converters
            CoreConverter conv = (CoreConverter)Activator.CreateInstance(converters[i]);

            // Making an item using the converterListAsset as a template.
            // Then adding the information needed for each converter
            VisualElement item = new VisualElement();
            converterListAsset.CloneTree(item);
            item.Q<Label>("converterName").text = conv.Name;
            item.Q<VisualElement>("converterTopVisualElement").tooltip = conv.Info;

            // Get the ListView for the converter items
            ListView listView = item.Q<ListView>("converterItems");


            // Need to get the assets that this converter is converting.
            // Need to return Name, Path, Initial info, Help link.
            ConverterItemInfos = conv.Initialize();

            listView.itemsSource = ConverterItemInfos;
            listView.makeItem = converterItem.CloneTree;
            listView.bindItem = BindItem;

            // // Iterate over all the items for the converter and populate the UI
            // for (int k = 0; k < ConverterItemInfos.Count; ++k)
            // {
            //     //VisualElement convItem = new VisualElement();
            //     //converterItem.CloneTree(convItem);
            //
            //     convItem.Q<Label>("converterItemName").text = ConverterItemInfos[i].Name;
            //     convItem.Q<Label>("converterItemPath").text = ConverterItemInfos[i].Path;
            //     listView.hierarchy.Add(convItem);
            // }

            scrollView.Add(item);
        }

        // converters = TypeCache.GetTypesDerivedFrom<CoreConverter>();
        // converterEditorAsset.CloneTree(rootVisualElement);
        // listView = rootVisualElement.Q<ListView>("convertersListView");
        // listView.itemsSource = converters;
        // listView.makeItem = converterListAsset.CloneTree;
        // listView.bindItem = BindItem;

        //listView.itemsSource = converters;
        //listView.makeItem = converterListAsset.CloneTree;
        //listView.bindItem = BindItem;
    }

    void BindItem(VisualElement item, int index)
    {
        CoreConverter.ConverterItemInfo convItem = ConverterItemInfos[index];
        item.Q<Label>("converterItemName").text = convItem.Name;
        item.Q<Label>("converterItemPath").text = convItem.Path;
    }
}
