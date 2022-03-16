using System;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// The grid drawn as a background of the <see cref="GraphView"/>.
    /// </summary>
    public class GridBackground : ImmediateModeElement
    {
        static readonly CustomStyleProperty<float> k_SpacingProperty = new CustomStyleProperty<float>("--spacing");
        static readonly CustomStyleProperty<int> k_ThickLinesProperty = new CustomStyleProperty<int>("--thick-lines");
        static readonly CustomStyleProperty<Color> k_LineColorProperty = new CustomStyleProperty<Color>("--line-color");
        static readonly CustomStyleProperty<Color> k_ThickLineColorProperty = new CustomStyleProperty<Color>("--thick-line-color");
        static readonly CustomStyleProperty<Color> k_GridBackgroundColorProperty = new CustomStyleProperty<Color>("--grid-background-color");

        static float DefaultSpacing => 50f;
        static int DefaultThickLines => 10;

        static Color DefaultLineColor {
            get
            {
                if (EditorGUIUtility.isProSkin)
                {
                    return new Color(200/255f, 200/255f, 200/255f, 0.05f);
                }

                return new Color(65/255f, 65/255f, 65/255f, 0.07f);
            }
        }

        static Color DefaultThickLineColor
        {
            get
            {
                if (EditorGUIUtility.isProSkin)
                {
                    return new Color(200/255f, 200/255f, 200/255f, 0.1f);
                }

                return new Color(65/255f, 65/255f, 65/255f, 0.1f);
            }
        }

        static Color DefaultGridBackgroundColor {
            get
            {
                if (EditorGUIUtility.isProSkin)
                {
                    return new Color(32/255f, 32/255f, 32/255f, 1.0f);
                }

                return new Color(150/255f, 150/255f, 150/255f, 1.0f);
            }
        }

        /// <summary>
        /// Spacing between grid lines.
        /// </summary>
        public float Spacing { get; private set; } = DefaultSpacing;

        int ThickLines { get; set; } = DefaultThickLines;

        Color m_LineColor = DefaultLineColor;
        Color LineColor => m_LineColor * GraphViewStaticBridge.EditorPlayModeTint;

        Color m_ThickLineColor = DefaultThickLineColor;
        Color ThickLineColor => m_ThickLineColor * GraphViewStaticBridge.EditorPlayModeTint;

        Color m_GridBackgroundColor = DefaultGridBackgroundColor;
        Color GridBackgroundColor => m_GridBackgroundColor * GraphViewStaticBridge.EditorPlayModeTint;

        VisualElement m_Container;

        /// <summary>
        /// Initializes a new instance of the <see cref="GridBackground"/> class.
        /// </summary>
        public GridBackground()
        {
            pickingMode = PickingMode.Ignore;

            this.StretchToParentSize();

            RegisterCallback<CustomStyleResolvedEvent>(OnCustomStyleResolved);
        }

        Vector3 Clip(Rect clipRect, Vector3 @in)
        {
            if (@in.x < clipRect.xMin)
                @in.x = clipRect.xMin;
            if (@in.x > clipRect.xMax)
                @in.x = clipRect.xMax;

            if (@in.y < clipRect.yMin)
                @in.y = clipRect.yMin;
            if (@in.y > clipRect.yMax)
                @in.y = clipRect.yMax;

            return @in;
        }

        void OnCustomStyleResolved(CustomStyleResolvedEvent e)
        {
            var elementCustomStyle = e.customStyle;
            if (elementCustomStyle.TryGetValue(k_SpacingProperty, out var spacingValue))
                Spacing = spacingValue;

            if (elementCustomStyle.TryGetValue(k_ThickLinesProperty, out var thicklinesValue))
                ThickLines = thicklinesValue;

            if (elementCustomStyle.TryGetValue(k_ThickLineColorProperty, out var thicklineColorValue))
                m_ThickLineColor = thicklineColorValue;

            if (elementCustomStyle.TryGetValue(k_LineColorProperty, out var lineColorValue))
                m_LineColor = lineColorValue;

            if (elementCustomStyle.TryGetValue(k_GridBackgroundColorProperty, out var gridColorValue))
                m_GridBackgroundColor = gridColorValue;
        }

        protected override void ImmediateRepaint()
        {
            VisualElement target = parent;

            var graphView = target as GraphView;
            if (graphView == null)
            {
                throw new InvalidOperationException("GridBackground can only be added to a GraphView");
            }
            m_Container = graphView.ContentViewContainer;
            Rect clientRect = graphView.layout;

            // Since we're always stretch to parent size, we will use (0,0) as (x,y) coordinates
            clientRect.x = 0;
            clientRect.y = 0;

            var containerScale = new Vector3(m_Container.transform.matrix.GetColumn(0).magnitude,
                m_Container.transform.matrix.GetColumn(1).magnitude,
                m_Container.transform.matrix.GetColumn(2).magnitude);
            var containerTranslation = m_Container.transform.matrix.GetColumn(3);
            var containerPosition = m_Container.layout;

            // background
            GraphViewStaticBridge.ApplyWireMaterial();

            GL.Begin(GL.QUADS);
            GL.Color(GridBackgroundColor);
            GL.Vertex(new Vector3(clientRect.x, clientRect.y));
            GL.Vertex(new Vector3(clientRect.xMax, clientRect.y));
            GL.Vertex(new Vector3(clientRect.xMax, clientRect.yMax));
            GL.Vertex(new Vector3(clientRect.x, clientRect.yMax));
            GL.End();

            // vertical lines
            Vector3 from = new Vector3(clientRect.x, clientRect.y, 0.0f);
            Vector3 to = new Vector3(clientRect.x, clientRect.height, 0.0f);

            var tx = Matrix4x4.TRS(containerTranslation, Quaternion.identity, Vector3.one);

            from = tx.MultiplyPoint(from);
            to = tx.MultiplyPoint(to);

            from.x += (containerPosition.x * containerScale.x);
            from.y += (containerPosition.y * containerScale.y);
            to.x += (containerPosition.x * containerScale.x);
            to.y += (containerPosition.y * containerScale.y);

            float thickGridLineX = from.x;
            float thickGridLineY = from.y;

            // Update from/to to start at beginning of clientRect
            from.x = (from.x % (Spacing * (containerScale.x)) - (Spacing * (containerScale.x)));
            to.x = from.x;

            from.y = clientRect.y;
            to.y = clientRect.y + clientRect.height;

            while (from.x < clientRect.width)
            {
                from.x += Spacing * containerScale.x;
                to.x += Spacing * containerScale.x;

                GL.Begin(GL.LINES);
                GL.Color(LineColor);
                GL.Vertex(Clip(clientRect, from));
                GL.Vertex(Clip(clientRect, to));
                GL.End();
            }

            float thickLineSpacing = (Spacing * ThickLines);
            from.x = to.x = (thickGridLineX % (thickLineSpacing * (containerScale.x)) - (thickLineSpacing * (containerScale.x)));

            while (from.x < clientRect.width + thickLineSpacing)
            {
                GL.Begin(GL.LINES);
                GL.Color(ThickLineColor);
                GL.Vertex(Clip(clientRect, from));
                GL.Vertex(Clip(clientRect, to));
                GL.End();

                from.x += (Spacing * containerScale.x * ThickLines);
                to.x += (Spacing * containerScale.x * ThickLines);
            }

            // horizontal lines
            from = new Vector3(clientRect.x, clientRect.y, 0.0f);
            to = new Vector3(clientRect.x + clientRect.width, clientRect.y, 0.0f);

            from.x += (containerPosition.x * containerScale.x);
            from.y += (containerPosition.y * containerScale.y);
            to.x += (containerPosition.x * containerScale.x);
            to.y += (containerPosition.y * containerScale.y);

            from = tx.MultiplyPoint(from);
            to = tx.MultiplyPoint(to);

            from.y = to.y = (from.y % (Spacing * (containerScale.y)) - (Spacing * (containerScale.y)));
            from.x = clientRect.x;
            to.x = clientRect.width;

            while (from.y < clientRect.height)
            {
                from.y += Spacing * containerScale.y;
                to.y += Spacing * containerScale.y;

                GL.Begin(GL.LINES);
                GL.Color(LineColor);
                GL.Vertex(Clip(clientRect, from));
                GL.Vertex(Clip(clientRect, to));
                GL.End();
            }

            thickLineSpacing = Spacing * ThickLines;
            from.y = to.y = (thickGridLineY % (thickLineSpacing * (containerScale.y)) - (thickLineSpacing * (containerScale.y)));

            while (from.y < clientRect.height + thickLineSpacing)
            {
                GL.Begin(GL.LINES);
                GL.Color(ThickLineColor);
                GL.Vertex(Clip(clientRect, from));
                GL.Vertex(Clip(clientRect, to));
                GL.End();

                from.y += Spacing * containerScale.y * ThickLines;
                to.y += Spacing * containerScale.y * ThickLines;
            }
        }
    }
}
