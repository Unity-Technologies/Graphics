using UnityEditor.GraphToolsFoundation.Overdrive;

namespace UnityEditor.ShaderGraph.GraphUI.Controllers
{
    class BlackboardController : GraphSubWindowController<Blackboard>
    {
        public Blackboard BlackboardView => m_SubWindowContent;

        public BlackboardController(CommandDispatcher dispatcher, GraphView parentGraphView) : base(dispatcher, parentGraphView)
        {
            m_SubWindowContent = new Blackboard();
            m_SubWindowContent.SetupBuildAndUpdate(BlackboardView.Model, dispatcher, parentGraphView);
        }
    }
}
