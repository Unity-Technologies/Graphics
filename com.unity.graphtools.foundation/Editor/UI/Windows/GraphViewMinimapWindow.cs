using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Window to display the minimap.
    /// </summary>
    public class GraphViewMinimapWindow : GraphViewToolWindow
    {
        static readonly string k_ToolName = "MiniMap";

        MiniMapView m_MiniMapView;
        Label m_ZoomLabel;

        /// <inheritdoc />
        protected override string ToolName => k_ToolName;

        /// <inheritdoc />
        protected override void OnEnable()
        {
            base.OnEnable();

            OnGraphViewChanged();

            m_ZoomLabel = new Label();
            m_ZoomLabel.AddToClassList("ge-zoom-label");
            m_Toolbar.Add(m_ZoomLabel);

            m_ToolbarContainer.AddStylesheet("MinimapToolbar.uss");
        }

        /// <inheritdoc />
        protected override void OnGraphViewChanging()
        {
            if (m_MiniMapView != null)
            {
                m_MiniMapView.RemoveFromHierarchy();
                m_MiniMapView = null;
                m_ZoomLabel.text = "";
            }
        }

        /// <inheritdoc />
        protected override void OnGraphViewChanged()
        {
            if (m_MiniMapView != null && m_MiniMapView.MiniMapViewModel.ParentGraphView != SelectedGraphView)
            {
                m_MiniMapView.RemoveFromHierarchy();
                m_MiniMapView = null;
                m_ZoomLabel.text = "";
            }

            if (m_MiniMapView == null && SelectedGraphView != null)
            {
                m_MiniMapView = new MiniMapView(this, SelectedGraphView);
                rootVisualElement.Add(m_MiniMapView);
            }
        }

        /// <summary>
        /// Sets the zoom label text.
        /// </summary>
        /// <param name="zoomLevel">The zoom level.</param>
        public void UpdateZoomLevelLabel(float zoomLevel)
        {
            m_ZoomLabel.text = (zoomLevel < 0) ? "" : $"Zoom: {zoomLevel:P0}";
        }
    }
}
