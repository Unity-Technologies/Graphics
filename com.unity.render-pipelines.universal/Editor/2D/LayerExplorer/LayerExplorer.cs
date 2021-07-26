using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.TextCore.Text;
using UnityEngine;
using UnityEngine.UIElements;
using Random = UnityEngine.Random;

/**
 * Todos:
 * - Live updated when layers are changed added
 * - Hook up to the actual render pass (duh)
 * - Move assets into package
 */
internal class LayerExplorer : EditorWindow
{
    private const string ResourcePath = "Packages/com.unity.render-pipelines.universal/Editor/2D/LayerExplorer/";

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
    private int primaryIndex;

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
        bubble.text = name;
        // bubble.Add(new Label{text = name});

        bubble.clicked += () =>
        {
            Debug.Log($"Clicked {name}");
        };

        return bubble;
    }

    VisualElement GetOrCreateInfoView()
    {
        var root = rootVisualElement;
        var infoView = root.Query<VisualElement>("InfoScroller").First();
        if (infoView != null)
            return infoView;

        var infoContainer = root.Query<VisualElement>("InfoContainer").First();
        infoContainer.Clear();

        // load it
        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ResourcePath + "LayerBatchInfoView.uxml");
        infoView = visualTree.Instantiate();
        infoContainer.Add(infoView);

        return infoView;
    }

    void ViewBatch(int index)
    {
        var root = rootVisualElement;
        var infoView = GetOrCreateInfoView();

        var batch1 = batchList[index];

        var title = root.Query<Label>("InfoTitle").First();
        title.text = $"Batch {batch1.batchId}";

        var label1 = infoView.Query<Label>("InfoLabel1").First();
        label1.text = $"Lights in Batch {batch1.batchId}";

        var bubble1 = infoView.Query<VisualElement>("InfoBubble1").First();
        bubble1.Clear();
        foreach(var light in batch1.Lights)
        {
            bubble1.Add(MakeLightPill(light));
        }

        var label2 = infoView.Query<Label>("InfoLabel2").First();
        label2.text = "";

        var bubble2 = infoView.Query<VisualElement>("InfoBubble2").First();
        bubble2.Clear();

        var desc = root.Query<Label>("Description").First();
        desc.text = "";
    }

    void CompareBatch(int index1, int index2)
    {
        // Each editor window contains a root VisualElement object
        var root = rootVisualElement;
        var infoView = GetOrCreateInfoView();

        var batch1 = batchList[index1];
        var batch2 = batchList[index2];

        // populate
        var title = root.Query<Label>("InfoTitle").First();
        title.text = "Comparing Batch 2 and Batch 3";

        var label1 = infoView.Query<Label>("InfoLabel1").First();
        label1.text = "Lights in Batch 2 but not in Batch 3";

        var bubble1 = infoView.Query<VisualElement>("InfoBubble1").First();
        bubble1.Clear();
        foreach(var light in batch1.Lights)
        {
            bubble1.Add(MakeLightPill(light));
        }

        var label2 = infoView.Query<Label>("InfoLabel2").First();
        label2.text = "Lights in Batch 3 but not in Batch 2";

        var bubble2 = infoView.Query<VisualElement>("InfoBubble2").First();
        bubble2.Clear();
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

        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ResourcePath + "LayerExplorer.uxml");
        var templateRoot = visualTree.Instantiate();
        templateRoot.style.flexGrow = 1;
        root.Add(templateRoot);

        MakeFakeData();
        var colors = MakeColors();

        var batchElement = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ResourcePath + "LayerBatch.uxml");
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

        var batchListView = root.Query<ListView>("BatchList").First();
        batchListView.itemsSource = batchList;
        batchListView.makeItem = makeItem;
        batchListView.bindItem = bindItem;
        batchListView.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
        batchListView.showAlternatingRowBackgrounds = AlternatingRowBackground.ContentOnly;
        batchListView.selectionType = SelectionType.Multiple;

        batchListView.onSelectionChange += objects =>
        {
            switch (batchListView.selectedIndices.Count())
            {
                case 1:
                    primaryIndex = batchListView.selectedIndices.First();
                    ViewBatch(primaryIndex);
                    break;
                case 2:
                    // new indices are first in array!!??
                    var secondIndex = batchListView.selectedIndices.First();
                    CompareBatch(primaryIndex, secondIndex);
                    break;
                default:
                    // assign new primary
                    primaryIndex = batchListView.selectedIndices.First();
                    ViewBatch(primaryIndex);
                    batchListView.SetSelection(primaryIndex);
                    break;
            }
        };
    }
}
