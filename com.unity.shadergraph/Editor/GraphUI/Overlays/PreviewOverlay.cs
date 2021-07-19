using UnityEditor.Overlays;
using UnityEditor.ShaderGraph.GraphUI.GraphElements.Windows;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI.GraphElements.Views
{
    [Overlay(typeof(ShaderGraphEditorWindow), "Preview")]
    class PreviewOverlay : GraphSubWindowOverlay
    {
        protected override void OnPanelContentAttached(AttachToPanelEvent evt)
        {
        }
    }
}
