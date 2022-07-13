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
            if (m_MainPreviewView != null)
            {
                m_MainPreviewView.AddToClassList("MainPreviewView");
                m_MainPreviewView.AddStylesheet("MainPreviewView.uss");
                // TODO: Get size from preview data/user prefs
                size = new Vector2(130, 130);
                // Note: MaxSize needs to be different from size and non-zero for resizing manipulators to work
                maxSize = new Vector2(500.0f, 500.0f);
                window.SetMainPreviewReference(m_MainPreviewView);
                var cachedTexture = getCachedMainPreviewTexture?.Invoke();
                if (cachedTexture != null)
                    m_MainPreviewView.mainPreviewTexture = cachedTexture;

                if(window.GraphTool.ToolState.GraphModel is ShaderGraphModel graphModel)
                    m_MainPreviewView.Initialize(graphModel.MainPreviewData);

                return m_MainPreviewView;
            }

            return emptyPlaceholder;
        }
    }
}
