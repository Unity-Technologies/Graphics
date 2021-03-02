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
        Debug.Assert(document != null);
        var panelSettings = document.panelSettings;
        Debug.Assert(panelSettings != null);

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
