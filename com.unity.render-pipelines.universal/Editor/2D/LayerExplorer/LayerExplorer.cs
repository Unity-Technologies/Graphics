using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Rendering.Universal;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UIElements;

/**
 * Todos:
 * - Live updated when layers are changed added
 * - Hook up to the actual render pass (duh)
 * - Move assets into package
 */
internal class LayerExplorer : EditorWindow
{
    private const string ResourcePath = "Packages/com.unity.render-pipelines.universal/Editor/2D/LayerExplorer/";

    private class LayerBatch
    {
        public List<string> LayerNames = new List<string>();
        public List<Light2D> Lights = new List<Light2D>();
        public int batchId;
        public int color;

    }

    [MenuItem("Window/2D/LayerExplorer")]
    public static void ShowExample()
    {
        LayerExplorer wnd = GetWindow<LayerExplorer>();
        wnd.titleContent = new GUIContent("Layer Explorer");
    }

    private Color[] MakeColors()
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
    private bool isDataPopulated;

    private bool PopulateData()
    {
        batchList = new List<LayerBatch>();
        var renderer = Light2DEditorUtility.GetRenderer2DData();
        if (renderer == null || renderer.lightCullResult == null)
            return false;

        var layers = Light2DManager.GetCachedSortingLayer();
        var batches = LayerUtility.CalculateBatches(renderer.lightCullResult, out var batchCount);

        for (var i = 0; i < batchCount; i++)
        {
            var batchInfo = new LayerBatch
            {
                batchId = i
            };

            var batch = batches[i];
            // get the lights
            foreach (var light in renderer.lightCullResult.visibleLights)
            {
                // If the lit layers are different, or if they are lit but this is a shadow casting light then don't batch.
                if (light.IsLitLayer(batch.startLayerID))
                    batchInfo.Lights.Add(light);
            }

            for (var batchIndex = batch.startIndex; batchIndex <= batch.endIndex; batchIndex++)
            {
                batchInfo.LayerNames.Add(layers[batchIndex].name);
            }

            batchList.Add(batchInfo);
        }

        isDataPopulated = true;
        return true;
    }

    private VisualElement MakeLightPill(Light2D light)
    {
        var bubble = new Button();
        bubble.AddToClassList("Pill");
        bubble.text = light.name;
        // bubble.Add(new Label{text = name});

        bubble.clicked += () =>
        {
            Selection.activeObject = light;
        };

        return bubble;
    }

    private VisualElement GetOrCreateInfoView()
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

    private void ViewBatch(int index)
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

    private void CompareBatch(int index1, int index2)
    {
        // Each editor window contains a root VisualElement object
        var root = rootVisualElement;
        var infoView = GetOrCreateInfoView();

        LayerBatch batch1;
        LayerBatch batch2;

        if (batchList[index1].batchId < batchList[index2].batchId)
        {
            batch1 = batchList[index1];
            batch2 = batchList[index2];
        }
        else
        {
            batch1 = batchList[index2];
            batch2 = batchList[index1];
        }

        var lightSet1 = new HashSet<Light2D>();
        foreach(var light in batch1.Lights)
            lightSet1.Add(light);

        var lightSet2 = new HashSet<Light2D>();
        foreach(var light in batch2.Lights)
            lightSet2.Add(light);


        // populate
        var title = root.Query<Label>("InfoTitle").First();
        title.text = $"Comparing Batch {batch1.batchId} and Batch {batch2.batchId}";

        var label1 = infoView.Query<Label>("InfoLabel1").First();
        label1.text = $"Lights in Batch {batch1.batchId} but not in Batch {batch2.batchId}";

        var bubble1 = infoView.Query<VisualElement>("InfoBubble1").First();
        bubble1.Clear();
        foreach(var light in batch1.Lights)
        {
            if(!lightSet2.Contains(light))
                bubble1.Add(MakeLightPill(light));
        }

        var label2 = infoView.Query<Label>("InfoLabel2").First();
        label2.text = $"Lights in Batch {batch2.batchId} but not in Batch {batch1.batchId}";

        var bubble2 = infoView.Query<VisualElement>("InfoBubble2").First();
        bubble2.Clear();
        foreach(var light in batch2.Lights)
        {
            if(!lightSet1.Contains(light))
                bubble2.Add(MakeLightPill(light));
        }

        var desc = root.Query<Label>("Description").First();
        desc.text = $"Batch {batch1.batchId} and {batch2.batchId} are not batched together because they do not share the same set of lights.";
    }

    void OnEnable()
    {
        AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
    }

    private void OnAfterAssemblyReload()
    {
        isDataPopulated = false;
    }

    public void CreateGUI()
    {

        if (!PopulateData())
            return;

        var root = rootVisualElement;
        root.Clear();

        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(ResourcePath + "LayerExplorer.uxml");
        var templateRoot = visualTree.Instantiate();
        templateRoot.style.flexGrow = 1;
        root.Add(templateRoot);


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

    private void OnGUI()
    {
        if(!isDataPopulated)
            CreateGUI();
    }
}
