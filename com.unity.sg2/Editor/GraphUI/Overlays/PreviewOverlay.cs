using UnityEditor.Overlays;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    [Overlay(typeof(ShaderGraphEditorWindow), k_OverlayID)]
    class PreviewOverlay : Overlay
    {
        public const string k_OverlayID = "Preview";

        public override VisualElement CreatePanelContent()
        {
            throw new System.NotImplementedException();
        }
    }
}
