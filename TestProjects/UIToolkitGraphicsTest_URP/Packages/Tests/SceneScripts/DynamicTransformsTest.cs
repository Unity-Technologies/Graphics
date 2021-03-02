using UnityEngine;
using UnityEngine.UIElements;

public class DynamicTransformsTest : GraphicTestBase
{
    void Start()
    {
        var doc = GetComponent<UIDocument>();

        for (int y = 0; y < 100; ++y)
        {
            for (int x = 0; x < 100; ++x)
            {
                doc.rootVisualElement.Add(new VisualElement() {
                    usageHints = UsageHints.DynamicTransform,
                    style =
                    {
                        position = Position.Absolute,
                        left = x * 5 + 7, top = y * 5 + 7,
                        width = 3, height = 3,
                        backgroundColor = Color.red
                    }
                });
            }
        }
    }
}
