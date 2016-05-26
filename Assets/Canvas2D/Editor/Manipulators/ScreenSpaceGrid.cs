using System;
using UnityEngine;

namespace UnityEditor.Experimental
{
    internal class ScreenSpaceGrid : IManipulate
    {
        private readonly float m_Spacing = 50f;
        private readonly int m_ThickLines = 10;
        private Color m_LineColor = new Color(0f, 0f, 0f, 0.18f);
        private Color m_ThickLineColor = new Color(0f, 0f, 0f, 0.38f);
        private Color m_Background = new Color(0.17f, 0.17f, 0.17f, 1.0f);

        public ScreenSpaceGrid()
        {}

        public ScreenSpaceGrid(Color lineColor, Color thickLineColor, Color backgroundColor)
        {
            m_LineColor = lineColor;
            m_ThickLineColor = thickLineColor;
            m_Background = backgroundColor;
        }

        public ScreenSpaceGrid(float spacing, int thickLineFrequency)
        {
            m_Spacing = spacing;
            m_ThickLines = thickLineFrequency;
        }

        public ScreenSpaceGrid(float spacing, int thickLineFrequency, Color lineColor, Color thickLineColor, Color background)
        {
            m_Spacing = spacing;
            m_ThickLines = thickLineFrequency;
            m_LineColor = lineColor;
            m_ThickLineColor = thickLineColor;
            m_Background = background;
        }

        public bool GetCaps(ManipulatorCapability cap)
        {
            return false;
        }

        public void AttachTo(CanvasElement element)
        {
            if (element is Canvas2D)
            {
                (element as Canvas2D).OnBackground += DrawGrid;
            }
        }

        public static bool NearlyEqual(float a, float b, float epsilon)
        {
            if ((Math.Abs(a) - Math.Abs(b)) > epsilon)
                return false;
            return true;
        }

        private Vector3 Clip(Rect clipRect, Vector3 _in)
        {
            if (_in.x < clipRect.xMin)
                _in.x = clipRect.xMin;
            if (_in.x > clipRect.xMax)
                _in.x = clipRect.xMax;

            if (_in.y < clipRect.yMin)
                _in.y = clipRect.yMin;
            if (_in.y > clipRect.yMax)
                _in.y = clipRect.yMax;

            return _in;
        }

        private bool DrawGrid(CanvasElement element, Event e, Canvas2D canvas)
        {
            Rect clientRect = new Rect(canvas.clientRect.x, canvas.clientRect.y, canvas.clientRect.width, canvas.clientRect.height);

            // background
            UIHelpers.ApplyWireMaterial();

            GL.Begin(GL.QUADS);
            GL.Color(m_Background);
            GL.Vertex(Clip(clientRect, new Vector3(clientRect.x, clientRect.y + canvas.viewOffset.y, 0.0f)));
            GL.Vertex(Clip(clientRect, new Vector3(clientRect.x + clientRect.width, clientRect.y + canvas.viewOffset.y, 0.0f)));
            GL.Vertex(Clip(clientRect, new Vector3(clientRect.x + clientRect.width, clientRect.y + clientRect.height, 0.0f)));
            GL.Vertex(Clip(clientRect, new Vector3(clientRect.x, clientRect.y + clientRect.height, 0.0f)));
            GL.End();

            Vector3 from = new Vector3(clientRect.x, clientRect.y, 0.0f);
            Vector3 to = new Vector3(clientRect.x, clientRect.height, 0.0f);

            Matrix4x4 tx = Matrix4x4.TRS(canvas.translation, Quaternion.identity, Vector3.one);


            // Grid Simplification over scale
            float invScale = 1f/canvas.scale.x;
            float exponent = Mathf.Floor(Mathf.Sqrt(invScale)) - 1;
            float spacing = m_Spacing * Mathf.Pow(m_ThickLines , exponent);
            float progress = Mathf.Sqrt(invScale) % 1f ;

            Color thinlineColor = Color.Lerp(m_LineColor, m_Background, progress);
            Color thicklineColor = Color.Lerp(m_ThickLineColor, m_LineColor, progress);
            Color nextthickLineColor = m_ThickLineColor;

            // vertical lines
            from = tx.MultiplyPoint(from);
            to = tx.MultiplyPoint(to);

            float thickGridLineX = from.x;
            float thickGridLineY = from.y;

            from.x = (from.x % (spacing * (canvas.scale.x)) - (spacing * (canvas.scale.x)));
            to.x = from.x;

            from.y = clientRect.y;
            to.y = clientRect.y + clientRect.height;

            Color oldHandlesColor = Handles.color;
            Handles.color = thinlineColor;

            while (from.x < clientRect.width)
            {
                from.x += spacing * (canvas.scale.x);
                to.x += spacing * (canvas.scale.x);

                var f = Clip(clientRect, from);
                var t = Clip(clientRect, to);
                Handles.DrawAAPolyLine(2.0f, new[] { f, t });
            }

            Handles.color = thicklineColor;
            float thickLineSpacing = (spacing * m_ThickLines);
            from.x = to.x = (thickGridLineX % (thickLineSpacing * (canvas.scale.x)) - (thickLineSpacing * (canvas.scale.x)));

            while (from.x < clientRect.width + thickLineSpacing)
            {
                var f = Clip(clientRect, from);
                var t = Clip(clientRect, to);

                Handles.DrawAAPolyLine(2.0f, new[] { f, t });
           
                from.x += (spacing * (canvas.scale.x) * m_ThickLines);
                to.x += (spacing * (canvas.scale.x) * m_ThickLines);
            }

            // horizontal lines
            from = new Vector3(clientRect.x, clientRect.y, 0.0f);
            to = new Vector3(clientRect.x + clientRect.width, clientRect.y, 0.0f);

            from = tx.MultiplyPoint(from);
            to = tx.MultiplyPoint(to);

            from.y = (from.y % (spacing * (canvas.scale.y)) - (spacing * (canvas.scale.y)));
            to.y = from.y;
            from.x = 0.0f;
            to.x += clientRect.width;

            oldHandlesColor = Handles.color;
            Handles.color = thicklineColor;
            while (from.y < clientRect.height)
            {
                from.y += spacing * (canvas.scale.y);
                to.y += spacing * (canvas.scale.y);

                var f = Clip(clientRect, from);
                var t = Clip(clientRect, to);
                Handles.DrawAAPolyLine(2.0f, new[] { f, t });
            }

            thickLineSpacing = (spacing * m_ThickLines);
            from.y = to.y = (thickGridLineY % (thickLineSpacing * (canvas.scale.y)) - (thickLineSpacing * (canvas.scale.y)));
            oldHandlesColor = Handles.color;
            Handles.color = m_ThickLineColor;
            while (from.y < clientRect.height + thickLineSpacing)
            {
                var f = Clip(clientRect, from);
                var t = Clip(clientRect, to);
                Handles.DrawAAPolyLine(2.0f, new[] { f, t });

                from.y += (spacing * (canvas.scale.y) * m_ThickLines);
                to.y += (spacing * (canvas.scale.y) * m_ThickLines);
            }

            Handles.color = nextthickLineColor;

            Handles.color = oldHandlesColor;
            return true;
        }
    };
} 
