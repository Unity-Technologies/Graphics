//using System;
//using UnityEditor.GraphToolsFoundation.Overdrive;
//using UnityEditor.Overlays;
//using UnityEditor.ShaderGraph.GraphUI.GraphElements.Views;
//using UnityEngine.GraphToolsFoundation.CommandStateObserver;
//using UnityEngine.UIElements;

//namespace UnityEditor.ShaderGraph.GraphUI.Controllers
//{
//    // Base class for all controllers of a sub-window within the shader graph
//    abstract class GraphSubWindowController<SubWindowView, ViewOverlay>
//        where SubWindowView : VisualElement
//        where ViewOverlay : GraphSubWindowOverlay<SubWindowView>
//    {
//        protected virtual string OverlayID { get => String.Empty; }

//        public SubWindowView View
//        {
//            get => m_View;
//            protected set => m_View = value;
//        }

//        // Actual view content of the SubWindow
//        // Must be initialized by child classes as needed
//        private SubWindowView m_View;

//        protected ViewOverlay Overlay
//        {
//            get => m_Overlay;
//            set => m_Overlay = value;
//        }

//        // Overlay that contains the view content
//        ViewOverlay m_Overlay;

//        // Reference to the GTF command dispatcher
//        protected CommandDispatcher m_CommandDispatcher;

//        // Reference to the containing GraphView
//        protected GraphView m_ParentGraphView;

//        protected EditorWindow m_ParentWindow;

//        // TODO: Handle layout serialization/deserialization

//        protected GraphSubWindowController(CommandDispatcher dispatcher, GraphView parentGraphView, EditorWindow parentWindow)
//        {
//            m_CommandDispatcher = dispatcher;
//            m_ParentGraphView = parentGraphView;

//            // ReSharper disable once VirtualMemberCallInConstructor
//            parentWindow.TryGetOverlay(OverlayID, out var overlayMatch);
//            if (overlayMatch != null)
//                Overlay = (ViewOverlay)overlayMatch;
//            else
//                AssertHelpers.Fail("Failed to find matching Overlay for GraphSubWindowController.");

//            m_ParentWindow = parentWindow;
//        }
//    }
//}
