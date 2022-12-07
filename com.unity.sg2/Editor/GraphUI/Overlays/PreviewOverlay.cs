using System;
using UnityEditor.Overlays;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.ShaderGraph.GraphUI
{
    [Overlay(typeof(ShaderGraphEditorWindow), k_OverlayID, "Preview", defaultDisplay = true,
        defaultDockZone = DockZone.RightColumn, defaultDockPosition = DockPosition.Bottom,
        defaultLayout = Layout.Panel, defaultWidth = 130, defaultHeight = 130)]
    class PreviewOverlay : Overlay
    {
        public const string k_OverlayID = "Preview";

        MainPreviewView m_MainPreviewView;

        public PreviewOverlay()
        {
            minSize = new Vector2(130, 130);
            maxSize = new Vector2(1000, 1000);
        }

        public override VisualElement CreatePanelContent()
        {
            var emptyPlaceholder = new VisualElement();

            var window = containerWindow as ShaderGraphEditorWindow;
            if (window == null || window.GraphTool == null)
                return emptyPlaceholder;

            // Panel content gets rebuilt on show/hide and dock/undock. By reusing our existing preview view, we
            // avoid an issue where the preview, until asked to update, goes blank.
            if (m_MainPreviewView != null)
                return m_MainPreviewView;

            m_MainPreviewView = new MainPreviewView(window.GraphTool);
            m_MainPreviewView.AddToClassList("MainPreviewView");
            m_MainPreviewView.AddStylesheet("MainPreviewView.uss");

            window.SetMainPreviewReference(m_MainPreviewView);

            return m_MainPreviewView;
        }
    }
}
