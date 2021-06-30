using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.Overlays;
using UnityEngine.UIElements;

namespace GtfPlayground
{
    [Overlay(typeof(PlaygroundGraphWindow), OverlayId)]
    public class BlackboardOverlay : Overlay
    {
        public const string OverlayId = "Blackboard";

        protected override Layout supportedLayouts => Layout.Panel;

        VisualElement m_Root;

        public override VisualElement CreatePanelContent()
        {
            m_Root = new VisualElement();
            m_Root.RegisterCallback<AttachToPanelEvent>(_ =>
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
            });

            return m_Root;
        }
    }
}
