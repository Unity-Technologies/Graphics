using UnityEditor.Overlays;
using UnityEngine.UIElements;

namespace GtfPlayground
{
    // TODO: Actual name
    public abstract class PlaygroundOverlay : Overlay
    {
        protected VisualElement m_Root;

        public override VisualElement CreatePanelContent()
        {
            m_Root = new VisualElement();
            m_Root.RegisterCallback<AttachToPanelEvent>(OnPanelContentAttached);

            return m_Root;
        }

        protected abstract void OnPanelContentAttached(AttachToPanelEvent evt);
    }
}
