using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI.Controllers
{
    class InspectorController : GraphSubWindowController<ModelInspectorView>
    {
        public ModelInspectorView InspectorView => m_SubWindowContent;

        public InspectorController(CommandDispatcher dispatcher, GraphView parentGraphView) : base(dispatcher, parentGraphView)
        {
            m_SubWindowContent = new ModelInspectorView(dispatcher);
        }
    }
}
