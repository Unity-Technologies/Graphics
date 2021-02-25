using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class SpritesTests : GraphicTestBase
{
    public StyleSheet stylesheet;

    void Start()
    {
        var uiDoc = GetComponent<UIDocument>();
        var root = uiDoc.rootVisualElement;
        root.styleSheets.Add(stylesheet);

        // Slice tests from static atlas
        var slicedContainer = new VisualElement() { name = "sliced-container" };
        slicedContainer.Add(new VisualElement() { name = "sliced-sprite0" });
        slicedContainer.Add(new VisualElement() { name = "sliced-sprite1" });
        slicedContainer.Add(new VisualElement() { name = "sliced-sprite2" });
        slicedContainer.Add(new VisualElement() { name = "sliced-sprite0-uss" });
        slicedContainer.Add(new VisualElement() { name = "sliced-sprite1-uss" });
        slicedContainer.Add(new VisualElement() { name = "sliced-sprite2-uss" });
        slicedContainer.Add(new VisualElement() { name = "rounded-sprite0" });
        slicedContainer.Add(new VisualElement() { name = "rounded-sprite1" });
        slicedContainer.Add(new VisualElement() { name = "rounded-sprite2" });
        root.Add(slicedContainer);

        // Scale tests from static atlas
        var swordContainer = new VisualElement() { name = "sword-container" };
        swordContainer.Add(new VisualElement() { name = "sword0", classList = { "sword" } });
        swordContainer.Add(new VisualElement() { name = "sword1", classList = { "sword" } });
        swordContainer.Add(new VisualElement() { name = "sword2", classList = { "sword" } });
        swordContainer.Add(new VisualElement() { name = "sword3", classList = { "sword" } });
        root.Add(swordContainer);

        // Image tests from static atlas
        var mapContainer = new VisualElement() { name = "map-container" };
        mapContainer.Add(new Image() { name = "map0", classList = { "map" } });
        mapContainer.Add(new Image() { name = "map1", classList = { "map" } });
        mapContainer.Add(new Image() { name = "map2", classList = { "map" } });
        mapContainer.Add(new Image() { name = "map3", classList = { "map" } });
        root.Add(mapContainer);

        // Sprite atlas variant (50%)
        var atlasContainer = new VisualElement() { name = "atlas-container" };
        atlasContainer.Add(new VisualElement() { name = "square-red" });
        atlasContainer.Add(new VisualElement() { name = "square-blue" });
        atlasContainer.Add(new VisualElement() { name = "square-black" });
        atlasContainer.Add(new VisualElement() { name = "square-red-display-none" });
        root.Add(atlasContainer);

        // Tight-packed arrows
        var arrowContainerNames = new string[] { "arrows-container-large", "arrows-container-thin" };
        foreach (var containerName in arrowContainerNames)
        {
            var arrowsContainer = new VisualElement() { name = containerName };
            var scaleModes = new ScaleMode[] { ScaleMode.ScaleAndCrop, ScaleMode.ScaleToFit, ScaleMode.StretchToFill };
            var colors = new string[] { "black", "red", "green", "blue" };
            foreach (var sm in scaleModes)
            {
                var arrowsRow = new VisualElement() { classList = { "arrows-row" }};
                foreach (var c in colors)
                    arrowsRow.Add(new Image() {
                        scaleMode = sm,
                        classList = { "arrow", $"arrow-{c}" }
                    });
                arrowsContainer.Add(arrowsRow);
            }
            root.Add(arrowsContainer);
        }
    }
}
