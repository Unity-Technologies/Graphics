//using System;
//using UnityEditor.GraphToolsFoundation.Overdrive;
//using UnityEditor.Overlays;
//using UnityEditor.ShaderGraph.GraphUI.GraphElements.Windows;
//using UnityEditor.ShaderGraph.GraphUI.Utilities;
//using UnityEngine;
//using UnityEngine.UIElements;

//namespace UnityEditor.ShaderGraph.GraphUI.GraphElements.Views
//{
//    abstract class GraphSubWindowOverlay<OverlayContent> : Overlay
//    where OverlayContent : VisualElement
//    {
//        protected VisualElement m_OverlayRoot;

//        protected OverlayContent m_OverlayContent;

//        /* Meant to be overriden by child classes as needed */

//        // Styling
//        protected virtual string elementName => String.Empty;
//        protected virtual string ussRootClassName => String.Empty;

//        // Layout
//        protected override Layout supportedLayouts => Layout.Panel;

//        public override VisualElement CreatePanelContent()
//        {
//            m_OverlayRoot = new VisualElement();
//            m_OverlayRoot.name = "root";
//            m_OverlayRoot.RegisterCallback<AttachToPanelEvent>(OnPanelContentAttached);
//            return m_OverlayRoot;
//        }

//        protected virtual void OnPanelContentAttached(AttachToPanelEvent evt)
//        {
//            var parent = (ShaderGraphEditorWindow) containerWindow;
//            if (parent is null) return;

//            var overlayContent = parent.GetGraphSubWindow<OverlayContent>();
//            m_OverlayContent = overlayContent as OverlayContent;
//            if(m_OverlayContent is null) return;

//            m_OverlayRoot.Clear();
//            GraphElementHelper.LoadTemplateAndStylesheet(m_OverlayRoot, elementName, ussRootClassName);

//            var contentElement = m_OverlayRoot.Q("content");
//            if(contentElement != null)
//                contentElement.Add(m_OverlayContent);
//        }
//    }
//}
