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
        public string[] Lights;
        public int batchId;
        public int color;

    }

    [MenuItem("Window/2D/LayerExplorer")]
    public static void ShowExample()
    {
        LayerExplorer wnd = GetWindow<LayerExplorer>();
        wnd.titleContent = new GUIContent("Layer Explorer");
    }

    Color[] MakeColors()
    {
        return new[]
        {
            Color.black, Color.blue, Color.cyan,
            Color.gray, Color.green, Color.magenta,
            Color.red, Color.white, Color.yellow,
        };
    }

    private List<LayerBatch> batchList;

    void MakeFakeData()
    {
        const int itemCount = 10;

        batchList = new List<LayerBatch>(itemCount);
        for (var i = 0; i < itemCount; i++)
        {
            batchList.Add(new LayerBatch
            {
                batchId = i
            });
        }

        foreach (var batch in batchList)
        {
            var count = Random.Range(1, 5);
            batch.LayerNames = new string[count];
            for (var j = 0; j < count; j++)
            {
                batch.LayerNames[j] = $"Batch{batch.batchId} Layer{j}";
            }
        }

        foreach (var batch in batchList)
        {
            var count = Random.Range(1, 10);
            batch.Lights = new string[count];
            for (var j = 0; j < count; j++)
            {
                batch.Lights[j] = $"Light_{j}";
            }
        }
    }

    VisualElement MakeLightPill(string name)
    {
        var bubble = new Button();
        bubble.AddToClassList("Pill");
        bubble.Add(new Label{text = name});
        return bubble;
    }

    void CompareBatch(int index1, int index2)
    {
        // Each editor window contains a root VisualElement object
        var root = rootVisualElement;
        var infoView = root.Query<ScrollView>("InfoScroller").First();

        var batch1 = batchList[index1];
        var batch2 = batchList[index2];

        // populate
        var title = root.Query<Label>("InfoTitle").First();
        title.text = "Comparing Batch 2 and Batch 3";

        var label1 = infoView.Query<Label>("InfoLabel1").First();
        label1.text = "Lights in Batch 2 but not in Batch 3";

        var bubble1 = infoView.Query<VisualElement>("InfoBubble1").First();
        foreach(var light in batch1.Lights)
        {
            bubble1.Add(MakeLightPill(light));
        }

        var label2 = infoView.Query<Label>("InfoLabel2").First();
        label2.text = "Lights in Batch 3 but not in Batch 2";

        var bubble2 = infoView.Query<VisualElement>("InfoBubble2").First();
        foreach(var light in batch2.Lights)
        {
            bubble2.Add(MakeLightPill(light));
        }

        var desc = root.Query<Label>("Description").First();
        desc.text = "Layers 2 and 3 are not batched together because they do not share the same set of lights.";
    }

    public void CreateGUI()
    {
        // Each editor window contains a root VisualElement object
        var root = rootVisualElement;

        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/LayerExplorer/LayerExplorer.uxml");
        var templateRoot = visualTree.Instantiate();
        templateRoot.style.flexGrow = 1;
        root.Add(templateRoot);

        MakeFakeData();
        var colors = MakeColors();

        var batchElement = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/LayerExplorer/LayerBatch.uxml");
        Func<VisualElement> makeItem = () => batchElement.Instantiate();

        Action<VisualElement, int> bindItem = (e, i) =>
        {
            // this line is required to make the child of the Listview vary in heights
            e.style.height = new StyleLength(StyleKeyword.Auto);

            var batch = batchList[i];
            var batchIndex = e.Query<Label>("BatchIndex").First();
            batchIndex.text = batch.batchId.ToString();

            var layers = e.Query<VisualElement>("LayerNames").First();
            layers.Clear();
            foreach (var layerName in batchList[i].LayerNames)
            {
                var label = new Label {text = layerName};
                label.AddToClassList("LayerNameLabel");
                layers.Add(label);
            }

            var color = e.Query<VisualElement>("BatchColor").First();
            color.style.backgroundColor = new StyleColor(colors[i % colors.Length]);
        };

        var layerList = root.Query<ListView>("LayerList").First();
        layerList.itemsSource = batchList;
        layerList.makeItem = makeItem;
        layerList.bindItem = bindItem;
        layerList.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
        layerList.showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly;

        CompareBatch(0, 1);

    }
}
