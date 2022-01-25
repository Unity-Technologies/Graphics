//using UnityEditor.GraphToolsFoundation.Overdrive;
//using UnityEditor.Overlays;
//using UnityEditor.ShaderGraph.GraphUI.GraphElements.Windows;
//using UnityEditor.ShaderGraph.GraphUI.Utilities;
//using UnityEngine;
//using UnityEngine.UIElements;

//namespace UnityEditor.ShaderGraph.GraphUI.GraphElements.Views
//{
//    [Overlay(typeof(ShaderGraphEditorWindow), k_OverlayID)]
//    class BlackboardOverlay : GraphSubWindowOverlay<Blackboard>
//    {
//        public const string k_OverlayID = "Blackboard";
//        protected override string elementName => "Blackboard";
//        protected override string ussRootClassName => "ge-blackboard";

//        protected override void OnPanelContentAttached(AttachToPanelEvent evt)
//        {
//            base.OnPanelContentAttached(evt);
//            this.displayed = true;
//            this.floatingPositionChanged += OnfloatingPositionChanged;
//        }

//        void OnfloatingPositionChanged(Vector3 newPosition)
//        {
//            Debug.Log(newPosition);
//            if (newPosition.x < 0)
//            {
//                var oldRect = this.containerWindow.position;
//                this.containerWindow.position = new Rect(0, oldRect.y, oldRect.width, oldRect.height);
//            }
//        }
//    }
//}
