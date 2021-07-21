using UnityEditor.Overlays;
using UnityEditor.ShaderGraph.GraphUI.GraphElements.Windows;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI.GraphElements.Views
{
    [Overlay(typeof(ShaderGraphEditorWindow), k_OverlayID)]
    class InspectorOverlay : GraphSubWindowOverlay
    {
        public const string k_OverlayID = "Inspector";

        protected override void OnPanelContentAttached(AttachToPanelEvent evt)
        {
            var parent = (ShaderGraphEditorWindow) containerWindow;
            if (parent == null) return;

            var inspector = parent.InspectorView;
            if (inspector is null) return;

            this.displayed = true;

            m_Root.Clear();
            m_Root.Add(inspector);
        }

    }
}
