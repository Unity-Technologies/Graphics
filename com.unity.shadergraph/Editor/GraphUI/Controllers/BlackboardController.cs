using UnityEditor.GraphToolsFoundation.Overdrive;
using UnityEditor.ShaderGraph.GraphUI.GraphElements.Views;

namespace UnityEditor.ShaderGraph.GraphUI.Controllers
{
    class BlackboardController : GraphSubWindowController<Blackboard, BlackboardOverlay>
    {
        protected override string OverlayID => BlackboardOverlay.k_OverlayID;

        public BlackboardController(CommandDispatcher dispatcher, GraphView parentGraphView, EditorWindow parentWindow) : base(dispatcher, parentGraphView, parentWindow)
        {
            View = new Blackboard();
            View.SetupBuildAndUpdate(View.Model, dispatcher, parentGraphView);
        }
    }
}
