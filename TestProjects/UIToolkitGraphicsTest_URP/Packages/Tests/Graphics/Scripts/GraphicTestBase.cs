using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

public class GraphicTestBase : MonoBehaviour
{
    public virtual void DrawUI(RenderTexture rt)
    {
        var document = GetComponentInChildren<UIDocument>();
        if (document == null)
            document = FindObjectOfType<UIDocument>();
        Assume.That(document, Is.Not.Null);
        var panelSettings = document.panelSettings;
        Assume.That(panelSettings, Is.Not.Null);

        // Backup
        var previousTexture = panelSettings.targetTexture;
        var previousClearColor = panelSettings.clearColor;
        var previousClearDepth = panelSettings.clearDepthStencil;

        // Paint
        panelSettings.targetTexture = rt;
        panelSettings.clearColor = false;
        panelSettings.clearDepthStencil = true;
        UIElementsRuntimeUtility.UpdateRuntimePanels();
        UIElementsRuntimeUtility.RepaintOverlayPanels();

        // Restore
        panelSettings.targetTexture = previousTexture;
        panelSettings.clearColor = previousClearColor;
        panelSettings.clearDepthStencil = previousClearDepth;
    }
}
