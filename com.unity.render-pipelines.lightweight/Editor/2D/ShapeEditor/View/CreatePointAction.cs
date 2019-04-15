using System;
using UnityEngine;
using UnityEditor.Experimental.Rendering.LWRP.Path2D.GUIFramework;

namespace UnityEditor.Experimental.Rendering.LWRP.Path2D
{
    internal class CreatePointAction : ClickAction
    {
        private Control m_PointControl;

        public Func<IGUIState, Vector2, Vector3> guiToWorld;
        public Action<int, Vector3> onCreatePoint;
        public CreatePointAction(Control pointControl, Control edgeControl) : base(edgeControl, 0, false)
        {
            m_PointControl = pointControl;
        }

        protected override void OnTrigger(IGUIState guiState)
        {
            base.OnTrigger(guiState);

            var index = hoveredControl.layoutData.index;
            var position = GetMousePositionWorld(guiState);

            if (onCreatePoint != null)
                onCreatePoint(index, position);

            guiState.nearestControl = m_PointControl.ID;

            var data = m_PointControl.layoutData;
            data.index = index + 1;
            data.position = position;
            data.distance = 0f;

            m_PointControl.layoutData = data;

            guiState.changed = true;
        }

        private Vector3 GetMousePositionWorld(IGUIState guiState)
        {
            if (guiToWorld != null)
                return guiToWorld(guiState, guiState.mousePosition);

            return guiState.GUIToWorld(guiState.mousePosition, hoveredControl.layoutData.forward, hoveredControl.layoutData.position);
        }
    }
}
