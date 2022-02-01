using System;
using System.Collections.Generic;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    class LineView : VisualElement
    {
        GraphView m_GraphView;

        public LineView(GraphView graphView)
        {
            this.AddStylesheet("LineView.uss");
            this.StretchToParentSize();
            generateVisualContent += OnGenerateVisualContent;
            m_GraphView = graphView;
        }

        public List<Line> lines { get; } = new List<Line>();

        void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            if (m_GraphView == null)
            {
                return;
            }
            var container = m_GraphView.ContentViewContainer;
            foreach (var line in lines)
            {
                var start = container.ChangeCoordinatesTo(m_GraphView, line.Start);
                var end = container.ChangeCoordinatesTo(m_GraphView, line.End);
                var x = Math.Min(start.x, end.x);
                var y = Math.Min(start.y, end.y);
                var width = Math.Max(1, Math.Abs(start.x - end.x));
                var height = Math.Max(1, Math.Abs(start.y - end.y));
                var rect = new Rect(x, y, width, height);

                GraphViewStaticBridge.SolidRectangle(mgc, rect, GraphViewSettings.UserSettings.SnappingLineColor, ContextType.Editor);
            }
        }
    }
}
