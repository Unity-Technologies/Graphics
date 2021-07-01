using UnityEditor.Overlays;
using UnityEngine.UIElements;

namespace GtfPlayground
{
    [Overlay(typeof(PlaygroundGraphWindow), "Blackboard")]
    public class BlackboardOverlay : PlaygroundOverlay
    {
        protected override Layout supportedLayouts => Layout.Panel;

        protected override void OnPanelContentAttached(AttachToPanelEvent evt)
        {
            var parent = (PlaygroundGraphWindow) containerWindow;
            if (parent == null) return;

            var blackboard = parent.GraphView?.GetBlackboard();
            if (blackboard is null) return;

            // TODO: Resizable overlay
            blackboard.style.maxWidth = 400;
            blackboard.style.maxHeight = 500;

            m_Root.Clear();
            m_Root.Add(blackboard);
        }
    }
}
