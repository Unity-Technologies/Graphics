using System;
using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.Overlays;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI.GraphElements.Views
{
    abstract class GraphSubWindowOverlay : Overlay
    {
        protected VisualElement m_Root;
        protected override Layout supportedLayouts => Layout.Panel;

        // TODO: Handle styling

        public override VisualElement CreatePanelContent()
        {
            m_Root = new VisualElement();
            m_Root.RegisterCallback<AttachToPanelEvent>(OnPanelContentAttached);

            return m_Root;
        }

        public override void OnCreated()
        {
            base.OnCreated();
        }

        public override void OnWillBeDestroyed()
        {
            base.OnWillBeDestroyed();
        }

        protected abstract void OnPanelContentAttached(AttachToPanelEvent evt);
    }
}
