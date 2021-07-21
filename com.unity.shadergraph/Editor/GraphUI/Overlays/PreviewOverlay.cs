using UnityEditor.Overlays;
using UnityEditor.ShaderGraph.GraphUI.GraphElements.Windows;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI.GraphElements.Views
{
    [Overlay(typeof(ShaderGraphEditorWindow), k_OverlayID)]
    class PreviewOverlay : GraphSubWindowOverlay
    {
        public const string k_OverlayID = "Preview";

        protected override void OnPanelContentAttached(AttachToPanelEvent evt)
        {
            var parent = (ShaderGraphEditorWindow) containerWindow;
            if (parent == null) return;

            var previewView = parent.PreviewView;
            if (previewView is null) return;

            this.displayed = true;
            previewView.style.width = 100;
            previewView.style.height = 100;

            m_Root.Clear();
            m_Root.Add(previewView);
        }
    }
}
