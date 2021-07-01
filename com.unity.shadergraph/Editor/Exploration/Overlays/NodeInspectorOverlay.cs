using UnityEditor.Overlays;
using UnityEngine.UIElements;

namespace GtfPlayground
{
    [Overlay(typeof(PlaygroundGraphWindow), "Node Inspector")]
    public class NodeInspectorOverlay : PlaygroundOverlay
    {
        protected override Layout supportedLayouts => Layout.Panel;

        protected override void OnPanelContentAttached(AttachToPanelEvent evt)
        {
            var parent = (PlaygroundGraphWindow) containerWindow;
            if (parent == null) return;

            var inspector = parent.nodeInspector;
            if (inspector is null) return;

            m_Root.Clear();
            m_Root.Add(inspector);
        }
    }
}
