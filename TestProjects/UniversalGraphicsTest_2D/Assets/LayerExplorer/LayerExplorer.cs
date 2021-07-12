using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;


internal class LayerExplorer : EditorWindow
{
    public class LayerBatch
    {
        public string[] LayerNames;
        public int batchId;
        public int color;
    }


    [MenuItem("Window/2D/LayerExplorer")]
    public static void ShowExample()
    {
        LayerExplorer wnd = GetWindow<LayerExplorer>();
        wnd.titleContent = new GUIContent("Layer Explorer");
    }

    public void CreateGUI()
    {
        // Each editor window contains a root VisualElement object
        var root = rootVisualElement;
        var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/LayerExplorer/LayerExplorer.uss");
        root.styleSheets.Add(styleSheet);

        // Import UXML
        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/LayerExplorer/LayerExplorer.uxml");
        VisualElement templateRoot = visualTree.Instantiate();
        templateRoot.style.flexGrow = 1;
        // templateRoot.styleSheets.Add(styleSheet);
        root.Add(templateRoot);

        const int itemCount = 10;
        var colors = new[]
        {
            Color.black, Color.blue, Color.cyan,
            Color.gray, Color.green, Color.magenta,
            Color.red, Color.white, Color.yellow,
        };
        var items = new List<LayerBatch>(itemCount);
        for (var i = 0; i < itemCount; i++)
        {
            items.Add(new LayerBatch
            {
                batchId = i
            });
        }

        foreach (var batch in items)
        {
            var count = Random.Range(1, 5);
            batch.LayerNames = new string[count];
            for (var j = 0; j < count; j++)
            {
                batch.LayerNames[j] = $"Batch{batch.batchId} Layer{j}";
            }
        }

        var batchElement = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/LayerExplorer/LayerBatch.uxml");
        Func<VisualElement> makeItem = () => batchElement.Instantiate();

        Action<VisualElement, int> bindItem = (e, i) =>
        {
            // this line is required to make the child of the Listview vary in heights
            e.style.height = new StyleLength(StyleKeyword.Auto);

            var batch = items[i];
            var batchIndex = e.Query<Label>("BatchIndex").First();
            batchIndex.text = batch.batchId.ToString();

            var layers = e.Query<VisualElement>("LayerNames").First();
            layers.Clear();
            foreach (var layerName in items[i].LayerNames)
            {
                var label = new Label {text = layerName};
                label.AddToClassList("LayerNameLabel");
                layers.Add(label);
            }

            var color = e.Query<VisualElement>("BatchColor").First();
            color.style.backgroundColor = new StyleColor(colors[i % colors.Length]);
        };

        var layerList = root.Query<ListView>("LayerList").First();
        layerList.itemsSource = items;
        layerList.makeItem = makeItem;
        layerList.bindItem = bindItem;
        layerList.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;


        var batchViewAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/LayerExplorer/LayerBatchView.uxml");
        var batchView = batchViewAsset.Instantiate();

        // populate
        var title = batchView.Query<VisualElement>("Title").First();
        title.Add(new Label{text = "Comparing Batch 2 and Batch 3"});

        var label1 = batchView.Query<VisualElement>("Label1").First();
        label1.Add(new Label{text = "Lights in Batch 2 but not in Batch 3"});

        var bubble1 = batchView.Query<VisualElement>("Bubble1").First();
        bubble1.Add(new Label{text = "[Bubble1]"});

        var label2 = batchView.Query<VisualElement>("Label2").First();
        label2.Add(new Label{text = "Lights in Batch 3 but not in Batch 2"});

        var bubble2 = batchView.Query<VisualElement>("Bubble2").First();
        bubble2.Add(new Label{text = "[Bubble2]"});

        var desc = root.Query<VisualElement>("Description").First();
        desc.Add(new Label{text = "Layers 2 and 3 are not batched together because they do not share the same set of lights."});

        var infoView = root.Query<ScrollView>("InfoScroller").First();
        infoView.Add(batchView);
    }
}
