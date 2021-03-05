using System;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;

public class URPConvertersEditor : EditorWindow
{
    public VisualTreeAsset converterEditorAsset;
    public VisualTreeAsset converterListAsset;
    ListView listView;
    TypeCache.TypeCollection converters;

    [MenuItem("URPConverter/URPConverter")]
    public static void ShowWindow()
    {
        URPConvertersEditor wnd = GetWindow<URPConvertersEditor>();
        wnd.titleContent = new GUIContent("Universal Render Pipeline Converters");
    }

    public void CreateGUI()
    {
        converters = TypeCache.GetTypesDerivedFrom<CoreConverter>();
        converterEditorAsset.CloneTree(rootVisualElement);
        listView = rootVisualElement.Q<ListView>("convertersListView");
        listView.itemsSource = converters;
        listView.makeItem = converterListAsset.CloneTree;
        listView.bindItem = BindItem;
    }

    void BindItem(VisualElement item, int index)
    {
        CoreConverter conv = (CoreConverter)Activator.CreateInstance(converters[index]);
        item.Q<Label>("converterName").text = conv.Name;
        item.Q<Label>("converterInfo").text = conv.Info;
    }
}
