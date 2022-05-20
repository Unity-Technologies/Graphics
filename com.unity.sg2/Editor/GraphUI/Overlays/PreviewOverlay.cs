using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    [Overlay(typeof(ShaderGraphEditorWindow), k_OverlayID, "Preview", defaultDisplay = true,
        defaultDockZone = DockZone.RightColumn, defaultLayout = Layout.Panel)]
    class PreviewOverlay : Overlay
    {
        public const string k_OverlayID = "Preview";

        MainPreviewView m_MainPreviewView;

        public override VisualElement CreatePanelContent()
        {
            var emptyPlaceholder = new VisualElement();

            var window = containerWindow as ShaderGraphEditorWindow;
            if (window == null || window.GraphTool == null)
                return emptyPlaceholder;

            m_MainPreviewView = new MainPreviewView(window.GraphTool.Dispatcher);
            if (m_MainPreviewView != null)
            {
                m_MainPreviewView.AddToClassList("MainPreviewView");
                m_MainPreviewView.AddStylesheet("MainPreviewView.uss");
                size = new Vector2(125, 125);
                window.SetMainPreviewReference(m_MainPreviewView);
                return m_MainPreviewView;
            }

            return emptyPlaceholder;
        }
    }
}
