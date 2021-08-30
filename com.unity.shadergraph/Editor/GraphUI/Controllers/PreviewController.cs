using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.GraphUI.EditorCommon.CommandStateObserver;
using UnityEditor.ShaderGraph.GraphUI.GraphElements.Views;

namespace UnityEditor.ShaderGraph.GraphUI.Controllers
{
    class PreviewController : GraphSubWindowController<Preview, PreviewOverlay>
    {
        GraphPreviewStateObserver m_PreviewStateObserver;

        protected override string OverlayID => PreviewOverlay.k_OverlayID;

        public PreviewController(CommandDispatcher dispatcher, GraphView parentGraphView, EditorWindow parentWindow) : base(dispatcher, parentGraphView, parentWindow)
        {
            View = new Preview();

            m_PreviewStateObserver = new GraphPreviewStateObserver();
            dispatcher.RegisterObserver(m_PreviewStateObserver);
        }
    }
}
