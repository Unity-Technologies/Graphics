using UnityEngine;
using UnityEngine.UIElements;

public class UpdateBufferRangesPanel : GraphicTestBase
{
    public Material material;

    void Start()
    {
        var repaintUpdater = new UpdateBufferRangesRepaintUpdater(material);

        var uiDoc = GetComponent<UIDocument>();
        var p = uiDoc.rootVisualElement.panel as Panel;
        p.SetUpdater(repaintUpdater, VisualTreeUpdatePhase.Repaint);

        for (int i = 0; i < 100; ++i)
        {
            var ve = new VisualElement() {
                style =
                {
                    position = Position.Absolute,
                    left = 10 + (i % 10) * 15,
                    top = 10 + (i / 10) * 15,
                    width = 10,
                    height = 10,
                    backgroundColor = UIRendererTests.ComputeUniqueColor(i, 200)
                }
            };
            uiDoc.rootVisualElement.Add(ve);
        }

        repaintUpdater.RegisterRoot(uiDoc.rootVisualElement);
    }
}
