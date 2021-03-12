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
            List<CoreConverter.ConverterItemInfo> ConverterItemInfos;
            // Iterate over the converters
            CoreConverter conv = (CoreConverter)Activator.CreateInstance(converters[i]);

            // Making an item using the converterListAsset as a template.
            // Then adding the information needed for each converter
            // Why do I need to create a new visual element here? MTT
            VisualElement item = new VisualElement();
            //var asset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(AssetDatabase.GetAssetPath(converterListAsset)).Instantiate();
            converterListAsset.CloneTree(item);
            item.Q<Label>("converterName").text = conv.Name;
            item.Q<VisualElement>("converterTopVisualElement").tooltip = conv.Info;

            // Get the ListView for the converter items
            ListView listView = item.Q<ListView>("converterItems");

            // Need to get the assets that this converter is converting.
            // Need to return Name, Path, Initial info, Help link.
            conv.Initialize();
            ConverterItemInfos = conv.ItemInfos;
            // Update the amount of things to convert
            item.Q<Label>("converterStats").text = $"{ConverterItemInfos.Count} items";

            listView.itemsSource = ConverterItemInfos;
            listView.makeItem = converterItem.CloneTree;
            // I would like this to work, have a separate method and not inlined like this
            //listView.bindItem = BindItem;
            listView.bindItem = (element, index) =>
            {
                CoreConverter.ConverterItemInfo convItem = ConverterItemInfos[index];
                // Toggle for active. This sets if the asset will be upgraded/converted or not MTT
                element.Q<Toggle>("converterItemActive").value = convItem.Active;

                element.Q<Label>("converterItemName").text = convItem.Name;
                element.Q<Label>("converterItemPath").text = convItem.Path;

                // Help icon
                var imgHelp = EditorGUIUtility.FindTexture("_Help");
                element.Q<Image>("converterItemHelpIcon").image = imgHelp;
                element.Q<Image>("converterItemHelpIcon").tooltip = convItem.HelpLink;

                // Changing the icon here depending on the info.
                // If there is some info here we show the "warning icon"
                // If the string is empty we show the pending conversion icon.
                if (!String.IsNullOrEmpty(convItem.InitialInfo))
                {
                    var imgWarn = EditorGUIUtility.FindTexture("_Help");
                    element.Q<Image>("converterItemStatusIcon").image = imgWarn;
                    element.Q<Image>("converterItemStatusIcon").tooltip = convItem.InitialInfo;
                }
            };

            // When right clicking an item it should pop up a small menu with 2 entries
            // I also would like this a separate method instead of inline.
            listView.onSelectionChange += obj =>
            {
                conv.PrintMe(listView.selectedIndex);
            };

            scrollView.Add(item);
        }

        var button = rootVisualElement.Q<Button>("convertButton");
        button.RegisterCallback<ClickEvent>(OnClickConvert);
    }

    void OnClickConvert(ClickEvent evt)
    {
        for (int i = 0; i < scrollView.childCount; ++i)
        {
            VisualElement child = scrollView[i];
            if (child.Q<Toggle>("converterEnabled").value)
            {
                List<bool> activeList = new List<bool>();
                CoreConverter conv = (CoreConverter)Activator.CreateInstance(converters[i]);
                var converterItems = child.Q<ListView>("converterItems");
                foreach (var item in converterItems.Children())
                {
                    activeList.Add(item.Q<Toggle>("converterItemActive").value);
                }
                conv.Convert(activeList);
            }
        }
    }

    // element.RegisterCallback<MouseUpEvent>(evt =>
    // {
    //     if (evt.button != (int)MouseButton.RightMouse)
    //     {
    //         conv.PrintMe(index);
    //     }
    //     else
    //     {
    //         Debug.Log("Right");
    //     }
    // });


    // void HandleRightClick(MouseUpEvent evt)
    // {
    //     if (evt.button != (int)MouseButton.RightMouse)
    //     {
    //         //OnClicked +=
    //         Debug.Log("Wrong");
    //     }
    //     else
    //     {
    //         Debug.Log("Right");
    //     }
    // }

    // private void HandleRightClick(MouseUpEvent evt)
    // {
    //     if (evt.button != (int)MouseButton.RightMouse)
    //         return;
    //
    //     var targetElement = evt.target as VisualElement;
    //     if (targetElement == null)
    //         return;
    //
    //     var menu = new GenericMenu();
    //
    //     int menuItemValue = 5;
    //
    //     // Add a single menu item
    //     bool isSelected = true;
    //     menu.AddItem(new GUIContent("some menu item name"), isSelected,
    //         value => ChangeValueFromMenu(value),
    //         menuItemValue);
    //
    //     // Get position of menu on top of target element.
    //     var menuPosition = new Vector2(targetElement.layout.xMin, targetElement.layout.height);
    //     menuPosition = this.LocalToWorld(menuPosition);
    //     var menuRect = new Rect(menuPosition, Vector2.zero);
    //
    //     menu.DropDown(menuRect);
    // }
    //
    // private void ChangeValueFromMenu(object menuItem)
    // {
    //     doSomethingWithValue(menuItem as int);
    // }


    // void BindItem(VisualElement item, int index)
    // {
    //     CoreConverter.ConverterItemInfo convItem = ConverterItemInfos[index];
    //     item.Q<Label>("converterItemName").text = convItem.Name;
    //     item.Q<Label>("converterItemPath").text = convItem.Path;
    //     var imgHelp = EditorGUIUtility.FindTexture("_Help");
    //     item.Q<Image>("converterItemHelpIcon").image = imgHelp;
    //     item.Q<Image>("converterItemHelpIcon").tooltip = convItem.HelpLink;
    //
    //     if (!String.IsNullOrEmpty(convItem.InitialInfo))
    //     {
    //         var imgWarn = EditorGUIUtility.FindTexture("_Help");
    //         item.Q<Image>("converterItemStatusIcon").image = imgWarn;
    //         item.Q<Image>("converterItemStatusIcon").tooltip = convItem.InitialInfo;
    //     }
    // }
}

// converters = TypeCache.GetTypesDerivedFrom<CoreConverter>();
// converterEditorAsset.CloneTree(rootVisualElement);
// listView = rootVisualElement.Q<ListView>("convertersListView");
// listView.itemsSource = converters;
// listView.makeItem = converterListAsset.CloneTree;
// listView.bindItem = BindItem;


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
