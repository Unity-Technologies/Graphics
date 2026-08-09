using UnityEngine;

namespace UnityEditor.Rendering.Tests
{
    /// <summary>
    /// Shared helper methods for RenderGraphViewer tests.
    /// </summary>
    public static class RenderGraphViewerTestHelpers
    {
        /// <summary>
        /// Finds an open RenderGraphViewer window.
        /// </summary>
        public static RenderGraphViewer FindRenderGraphViewerWindow()
        {
            RenderGraphViewer rgv = null;
            foreach (var window in Resources.FindObjectsOfTypeAll<EditorWindow>())
            {
                if (window is RenderGraphViewer viewer)
                {
                    rgv = viewer;
                    break;
                }
            }

            return rgv;
        }

        /// <summary>
        /// Opens the RenderGraphViewer window via menu and returns it.
        /// Assumes the window opens successfully.
        /// </summary>
        public static RenderGraphViewer OpenRenderGraphViewer()
        {
            EditorApplication.ExecuteMenuItem("Window/Analysis/Render Graph Viewer");
            return FindRenderGraphViewerWindow();
        }
    }
}
