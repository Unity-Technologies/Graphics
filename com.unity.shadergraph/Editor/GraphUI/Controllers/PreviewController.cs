using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.GraphUI.GraphElements.Views;

namespace UnityEditor.ShaderGraph.GraphUI.Controllers
{
    class PreviewController : GraphSubWindowController<PreviewView>
    {
        public PreviewView PreviewView => m_SubWindowContent;

        public PreviewController(CommandDispatcher dispatcher, GraphView parentGraphView) : base(dispatcher, parentGraphView)
        {
            m_SubWindowContent = new PreviewView();
        }
    }
}
