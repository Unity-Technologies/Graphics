using UnityEditor.Overlays;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI.GraphElements.Views
{
    abstract class GraphSubWindowOverlay : Overlay
    {
        protected VisualElement m_Root;

        protected override Layout supportedLayouts => Layout.Panel;

        public override VisualElement CreatePanelContent()
        {
            m_Root = new VisualElement();
            m_Root.RegisterCallback<AttachToPanelEvent>(OnPanelContentAttached);

            return m_Root;
        }

        protected abstract void OnPanelContentAttached(AttachToPanelEvent evt);
    }
}
