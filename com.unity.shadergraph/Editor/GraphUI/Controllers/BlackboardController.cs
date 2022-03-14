//using UnityEditor.GraphToolsFoundation.Overdrive;
//using UnityEditor.ShaderGraph.GraphUI.GraphElements.Views;
//using UnityEngine;

//namespace UnityEditor.ShaderGraph.GraphUI.Controllers
//{
//    class BlackboardController : GraphSubWindowController<Blackboard, BlackboardOverlay>
//    {
//        protected override string OverlayID => BlackboardOverlay.k_OverlayID;

//        public BlackboardController(CommandDispatcher dispatcher, GraphView parentGraphView, EditorWindow parentWindow) : base(dispatcher, parentGraphView, parentWindow)
//        {
//            View = new Blackboard();
//            View.SetupBuildAndUpdate(View.Model, dispatcher, parentGraphView);
//        }

//        public void InitializeWindowPosition()
//        {
//            var oldRect = Overlay.containerWindow.position;
//            Overlay.Undock();
//            Overlay.floatingPosition = new Vector2(0, m_ParentWindow.position.height * 0.15f);
//        }

//        public void Update()
//        {
//        }
//    }
//}
