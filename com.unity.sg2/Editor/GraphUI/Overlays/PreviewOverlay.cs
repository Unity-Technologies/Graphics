using System;
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

        /// <summary>
        /// Is set in ShaderGraphEditorWindow::InitializeOverlayWindows to a function of the preview manager
        /// </summary>
        public Func<Texture> getCachedMainPreviewTexture;

        public override VisualElement CreatePanelContent()
        {
            var emptyPlaceholder = new VisualElement();

            var window = containerWindow as ShaderGraphEditorWindow;
            if (window == null || window.GraphTool == null)
                return emptyPlaceholder;

            m_MainPreviewView = new MainPreviewView(window.GraphTool.Dispatcher);
            m_MainPreviewView.AddToClassList("MainPreviewView");
            m_MainPreviewView.AddStylesheet("MainPreviewView.uss");

            window.SetMainPreviewReference(m_MainPreviewView);

            // TODO: The overlays should be persisting the size and driving the main preview size
            minSize = new Vector2(130, 130);
            // Note: MaxSize needs to be different from size and non-zero for resizing manipulators to work
            maxSize = new Vector2(1000, 1000);
            if(Single.IsNaN(size.x) || Single.IsNaN(size.y))
                size = minSize;

            m_MainPreviewView.Initialize(size);

            return m_MainPreviewView;
        }
    }
}
