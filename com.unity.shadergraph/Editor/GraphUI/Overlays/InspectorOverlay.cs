using UnityEditor.Overlays;
using UnityEditor.ShaderGraph.GraphUI.GraphElements.Windows;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI.GraphElements.Views
{
    [Overlay(typeof(ShaderGraphEditorWindow), "Inspector")]
    class InspectorOverlay : GraphSubWindowOverlay
    {
        protected override void OnPanelContentAttached(AttachToPanelEvent evt)
        {
            var parent = (ShaderGraphEditorWindow) containerWindow;
            if (parent == null) return;

            var inspector = parent.InspectorView;
            if (inspector is null) return;

            m_Root.Clear();
            m_Root.Add(inspector);
        }
    }
}
