using System;
using UnityEngine;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    abstract class SnapStrategy
    {
        internal class SnapResult
        {
            public Rect SnappableRect { get; set; }
            public float Offset { get; set; }
            public float Distance => Math.Abs(Offset);
        }

        protected enum SnapReference
        {
            LeftEdge,
            HorizontalCenter,
            RightEdge,
            TopEdge,
            VerticalCenter,
            BottomEdge
        }

        public bool Enabled { get; set; }

        protected float m_CurrentScale = 1.0f;
        protected GraphView m_GraphView;
        protected float SnapDistance { get; }
        protected bool IsPaused { get; private set; }
        protected bool IsActive { get; set; }

        const float k_DefaultSnapDistance = 8.0f;

        protected SnapStrategy()
        {
            SnapDistance = k_DefaultSnapDistance;
        }

        public abstract void BeginSnap(GraphElement selectedElement);

        public abstract Rect GetSnappedRect(ref Vector2 snappingOffset, Rect sourceRect, GraphElement selectedElement, float scale, Vector2 mousePanningDelta = default);

        public abstract void EndSnap();

        public void PauseSnap(bool isPaused)
        {
            if (!IsActive)
            {
                throw new InvalidOperationException("SnapStrategy.PauseSnap: Already inactive. Call BeginSnap() first.");
            }

            IsPaused = isPaused;
        }
    }
}
