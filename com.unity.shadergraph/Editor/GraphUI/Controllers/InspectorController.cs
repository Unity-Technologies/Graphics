using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI.Controllers
{
    class InspectorController : GraphSubWindowController<GraphToolState, ModelInspectorView>
    {
        public ModelInspectorView InspectorView => m_SubWindowContent;

        public InspectorController(CommandDispatcher dispatcher) : base(dispatcher)
        {
            m_SubWindowContent = new ModelInspectorView(dispatcher);
        }

        protected override void Observe(GraphToolState state)
        {
        }
    }
}
