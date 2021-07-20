using UnityEditor.Overlays;
using UnityEditor.ShaderGraph.GraphUI.GraphElements.Windows;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI.GraphElements.Views
{
    [Overlay(typeof(ShaderGraphEditorWindow), "Blackboard")]
    class BlackboardOverlay : GraphSubWindowOverlay
    {
        protected override void OnPanelContentAttached(AttachToPanelEvent evt)
        {
            var parent = (ShaderGraphEditorWindow) containerWindow;
            if (parent == null) return;

            var blackboardView = parent.BlackboardView;
            if (blackboardView is null) return;

            this.displayed = true;
            // TODO: Resizable overlay
            blackboardView.style.maxWidth = 400;
            blackboardView.style.maxHeight = 500;

            m_Root.Clear();
            m_Root.Add(blackboardView);
        }
    }
}
