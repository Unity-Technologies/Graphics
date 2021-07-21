using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.GraphUI.GraphElements.Views;

namespace UnityEditor.ShaderGraph.GraphUI.Controllers
{
    class PreviewController : GraphSubWindowController<PreviewView, PreviewOverlay>
    {
        protected override string OverlayID => PreviewOverlay.k_OverlayID;

        public PreviewController(CommandDispatcher dispatcher, GraphView parentGraphView, EditorWindow parentWindow) : base(dispatcher, parentGraphView, parentWindow)
        {
            View = new PreviewView();
        }
    }
}
