using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Mathematics;
using UnityEditor;
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

        // Import UXML
        var styleSheet = AssetDatabase.LoadAssetAtPath<StyleSheet>("Assets/LayerExplorer/LayerExplorer.uss");
        var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/LayerExplorer/LayerExplorer.uxml");
        VisualElement templateRoot = visualTree.Instantiate();
        templateRoot.style.flexGrow = 1;
        templateRoot.styleSheets.Add(styleSheet);
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
                batchId = i,
                LayerNames = new []
                {
                    $"Layer {i}.A", $"Layer {i}.B", $"Layer {i}.C",
                    $"Layer {i}.D", $"Layer {i}.E", $"Layer {i}.F",
                    $"Layer {i}.G", $"Layer {i}.H", $"Layer {i}.J"
                }
            });
        }

        var batchElement = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/LayerExplorer/LayerBatch.uxml");
        Func<VisualElement> makeItem = () => batchElement.Instantiate();

        Action<VisualElement, int> bindItem = (e, i) =>
        {
            // this line is required to make the child of the Listview vary in heights
            e.style.height = new StyleLength(StyleKeyword.Auto);
            var batchIndex = e.Query<Label>("BatchIndex").First();
            batchIndex.text = items[i].batchId.ToString();

            var layers = e.Query<ListView>().First();
            var layerNames = items[i].LayerNames.Take(Random.Range(1,items[i].LayerNames.Length)).ToList();
            layers.itemsSource = layerNames;
            layers.selectionType = SelectionType.None;
            // layers.fixedItemHeight = 16;
            layers.makeItem = () => new Label();
            layers.bindItem = (element, index) => (element as Label).text = layerNames[index];

            var color = e.Query<VisualElement>("BatchColor").First();
            color.style.backgroundColor = new StyleColor(colors[i % colors.Length]);
        };

        var layerList = root.Query<ListView>("LayerList").First();
        layerList.itemsSource = items;
        layerList.makeItem = makeItem;
        layerList.bindItem = bindItem;
        // layerList.selectionType = SelectionType.None;
    }
}
