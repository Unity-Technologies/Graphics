using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEditor.GraphToolsFoundation.Overdrive.Bridge;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Elements that displays a miniature view of the graph.
    /// </summary>
    public class MiniMap : ModelView
    {
        public static readonly string ussClassName = "ge-minimap";

        static Vector3[] s_CachedRect = new Vector3[4];

        static readonly CustomStyleProperty<Color> k_ViewportColorProperty = new CustomStyleProperty<Color>("--viewport-color");
        static readonly CustomStyleProperty<Color> k_SelectedElementColorProperty = new CustomStyleProperty<Color>("--selected-element-color");
        static readonly CustomStyleProperty<Color> k_PlacematBorderColorProperty = new CustomStyleProperty<Color>("--placemat-border-color");

        static Color DefaultViewportColor
        {
            get
            {
                if (EditorGUIUtility.isProSkin)
                {
                    return new Color(230/255f, 230/255f, 230/255f, 0.5f);
                }

                return new Color(138/255f, 138/255f, 138/255f, 1f);
            }
        }

        static Color DefaultSelectedElementColor
        {
            get
            {
                if (EditorGUIUtility.isProSkin)
                {
                    return new Color(255/255f, 128/255f, 0/255f, 1.0f);
                }

                return new Color(255/255f, 128/255f, 0/255f, 1.0f);
            }
        }

        static Color DefaultPlacematBorderColor
        {
            get
            {
                if (EditorGUIUtility.isProSkin)
                {
                    return new Color(26/255f, 26/255f, 26/255f);
                }

                return new Color(138/255f, 138/255f, 138/255f, 1f);
            }
        }

        Color m_ViewportColor = DefaultViewportColor;
        Color m_SelectedElementColor = DefaultSelectedElementColor;
        Color m_PlacematBorderColor = DefaultPlacematBorderColor;

        Rect m_ViewportRect;        // Rect that represents the current viewport

        Rect m_ContentRect;         // Rect that represents the rect needed to encompass all Graph Elements

        Rect m_ContentRectLocal;    // Rect that represents the rect needed to encompass all Graph Elements in local coords

        int TitleBarOffset => (int)resolvedStyle.paddingTop;

        public IGraphModel GraphModel => Model as IGraphModel;

        public Action<string> ZoomFactorTextChanged { get; set; }

        GraphView GraphView => (RootView as MiniMapView)?.MiniMapViewModel.ParentGraphView ?? null;

        /// <summary>
        /// Initializes a new instance of the <see cref="MiniMap"/> class.
        /// </summary>
        public MiniMap()
        {
            RegisterCallback<MouseDownEvent>(OnMouseDown);
            AddToClassList(ussClassName);

            generateVisualContent += OnGenerateVisualContent;
        }

        /// <inheritdoc />
        protected override void UpdateElementFromModel()
        {
            base.UpdateElementFromModel();

            // Ask for a repaint to trigger OnGenerateVisualContent().
            MarkDirtyRepaint();
        }

        protected override void OnCustomStyleResolved(CustomStyleResolvedEvent evt)
        {
            base.OnCustomStyleResolved(evt);

            if (evt.customStyle.TryGetValue(k_ViewportColorProperty, out var viewportColor))
                m_ViewportColor = viewportColor;
            if (evt.customStyle.TryGetValue(k_SelectedElementColorProperty, out var selectedElementColor))
                m_SelectedElementColor = selectedElementColor;
            if (evt.customStyle.TryGetValue(k_PlacematBorderColorProperty, out var placematBorderColor))
                m_PlacematBorderColor = placematBorderColor;
        }

        static void ChangeToMiniMapCoords(ref Rect rect, float factor, Vector3 translation)
        {
            // Apply factor
            rect.width *= factor;
            rect.height *= factor;
            rect.x *= factor;
            rect.y *= factor;

            // Apply translation
            rect.x += translation.x;
            rect.y += translation.y;
        }

        void SetZoomFactorText(string zoomFactorText)
        {
            ZoomFactorTextChanged?.Invoke(zoomFactorText);
        }

        void CalculateRects(VisualElement container)
        {
            if (GraphView == null)
            {
                // Nothing to do in this case.
                return;
            }

            m_ContentRect = GraphView.CalculateRectToFitAll(container);
            m_ContentRectLocal = m_ContentRect;

            // Retrieve viewport rectangle as if zoom and pan were inactive
            Matrix4x4 containerInvTransform = container.WorldTransformInverse();
            Vector4 containerInvTranslation = containerInvTransform.GetColumn(3);
            var containerInvScale = new Vector2(containerInvTransform.m00, containerInvTransform.m11);

            m_ViewportRect = GraphView.GetRect();

            // Bring back viewport coordinates to (0,0), scale 1:1
            m_ViewportRect.x += containerInvTranslation.x;
            m_ViewportRect.y += containerInvTranslation.y;

            var graphViewWorldBound = GraphView.worldBound;

            m_ViewportRect.x += graphViewWorldBound.x * containerInvScale.x;
            m_ViewportRect.y += graphViewWorldBound.y * containerInvScale.y;
            m_ViewportRect.width *= containerInvScale.x;
            m_ViewportRect.height *= containerInvScale.y;

            // Update label with new value
            var containerZoomFactor = container.worldTransform.m00;
            SetZoomFactorText(String.Format(CultureInfo.InvariantCulture.NumberFormat, "{0:F2}", containerZoomFactor) + "x");

            // Adjust rects for MiniMap
            float effectiveWidth = layout.width - 1;
            float effectiveHeight = layout.height - 1;

            // Encompass viewport rectangle (as if zoom and pan were inactive)
            var totalRect = RectUtils.Encompass(m_ContentRect, m_ViewportRect);
            var minimapFactor = effectiveWidth / totalRect.width;

            // Transform each rect to MiniMap coordinates
            ChangeToMiniMapCoords(ref totalRect, minimapFactor, Vector3.zero);

            var minimapTranslation = new Vector3(-totalRect.x, TitleBarOffset - totalRect.y);
            ChangeToMiniMapCoords(ref m_ViewportRect, minimapFactor, minimapTranslation);
            ChangeToMiniMapCoords(ref m_ContentRect, minimapFactor, minimapTranslation);

            // Diminish and center everything to fit vertically
            if (totalRect.height > (effectiveHeight - TitleBarOffset))
            {
                float totalRectFactor = (effectiveHeight - TitleBarOffset) / totalRect.height;
                float totalRectOffsetX = (effectiveWidth - (totalRect.width * totalRectFactor)) / 2.0f;
                float totalRectOffsetY = TitleBarOffset - ((totalRect.y + minimapTranslation.y) * totalRectFactor);

                m_ContentRect.width *= totalRectFactor;
                m_ContentRect.height *= totalRectFactor;
                m_ContentRect.x *= totalRectFactor;
                m_ContentRect.y *= totalRectFactor;
                m_ContentRect.x += totalRectOffsetX;
                m_ContentRect.y += totalRectOffsetY;

                m_ViewportRect.width *= totalRectFactor;
                m_ViewportRect.height *= totalRectFactor;
                m_ViewportRect.x *= totalRectFactor;
                m_ViewportRect.y *= totalRectFactor;
                m_ViewportRect.x += totalRectOffsetX;
                m_ViewportRect.y += totalRectOffsetY;
            }
        }

        Rect CalculateElementRect(ModelView elem)
        {
            Rect rect = elem.ChangeCoordinatesTo(GraphView.ContentViewContainer, elem.GetRect());
            rect.x = m_ContentRect.x + ((rect.x - m_ContentRectLocal.x) * m_ContentRect.width / m_ContentRectLocal.width);
            rect.y = m_ContentRect.y + ((rect.y - m_ContentRectLocal.y) * m_ContentRect.height / m_ContentRectLocal.height);
            rect.width *= m_ContentRect.width / m_ContentRectLocal.width;
            rect.height *= m_ContentRect.height / m_ContentRectLocal.height;

            // Clip using a minimal 2 pixel wide frame around edges
            // (except yMin since we already have the titleBar offset which is enough for clipping)
            var xMin = 2;
            var yMin = 2;
            var xMax = layout.width - 2;
            var yMax = layout.height - 2;

            if (rect.x < xMin)
            {
                if (rect.x < xMin - rect.width)
                    return new Rect(0, 0, 0, 0);
                rect.width -= xMin - rect.x;
                rect.x = xMin;
            }

            if (rect.x + rect.width >= xMax)
            {
                if (rect.x >= xMax)
                    return new Rect(0, 0, 0, 0);
                rect.width -= rect.x + rect.width - xMax;
            }

            if (rect.y < yMin + TitleBarOffset)
            {
                if (rect.y < yMin + TitleBarOffset - rect.height)
                    return new Rect(0, 0, 0, 0);
                rect.height -= yMin + TitleBarOffset - rect.y;
                rect.y = yMin + TitleBarOffset;
            }

            if (rect.y + rect.height >= yMax)
            {
                if (rect.y >= yMax)
                    return new Rect(0, 0, 0, 0);
                rect.height -= rect.y + rect.height - yMax;
            }

            return rect;
        }

        void OnGenerateVisualContent(MeshGenerationContext mgc)
        {
            // This control begs to be fully rewritten and it shouldn't use immediate
            // mode rendering at all. It should maintain its vertex/index lists and only
            // update affected vertices when their respective elements are changed. This
            // way the cost of GenerateVisualContent becomes effectively only two memcpys.
            GraphViewStaticBridge.DrawImmediate(mgc, DrawMinimapContent);
        }

        void DrawSolidRectangleWithOutline(ref Vector3[] cachedRect, Color faceColor, Color typeColor)
        {
            Handles.DrawSolidRectangleWithOutline(cachedRect, faceColor, typeColor);
        }

        void DrawMinimapContent()
        {
            if (GraphView == null)
            {
                return;
            }

            Color currentColor = Handles.color;
            VisualElement container = GraphView.ContentViewContainer;

            // Retrieve all container relative information
            Matrix4x4 containerTransform = GraphView.ViewTransform.matrix;
            var containerScale = new Vector2(containerTransform.m00, containerTransform.m11);

            // Refresh MiniMap rects
            CalculateRects(container);

            DrawElements();

            // Draw viewport outline
            DrawRectangleOutline(m_ViewportRect, m_ViewportColor);

            Handles.color = currentColor;
        }

        static readonly List<ModelView> k_DrawElementsAllUIs = new List<ModelView>();
        void DrawElements()
        {
            if (GraphModel == null)
                return;

            // Draw placemats first ...
            foreach (var placemat in GraphModel.PlacematModels)
            {
                var placematUI = placemat.GetView<GraphElement>(GraphView);

                if (placematUI == null)
                    continue;

                var elemRect = CalculateElementRect(placematUI);

                s_CachedRect[0].Set(elemRect.xMin, elemRect.yMin, 0.0f);
                s_CachedRect[1].Set(elemRect.xMax, elemRect.yMin, 0.0f);
                s_CachedRect[2].Set(elemRect.xMax, elemRect.yMax, 0.0f);
                s_CachedRect[3].Set(elemRect.xMin, elemRect.yMax, 0.0f);

                var fillColor = placematUI.resolvedStyle.backgroundColor;
                fillColor.a = 0.15f;

                DrawSolidRectangleWithOutline(ref s_CachedRect, fillColor, m_PlacematBorderColor);
            }

            // ... then the other elements
            Color darken = GraphViewStaticBridge.EditorPlayModeTint;
            GraphModel.GraphElementModels.GetAllViewsInList(GraphView,
                elem => (!(elem is GraphElement ge) || ge.ShowInMiniMap && ge.visible) && !(elem is Placemat), k_DrawElementsAllUIs);
            foreach (var elem in k_DrawElementsAllUIs.OfType<GraphElement>())
            {
                var elemRect = CalculateElementRect(elem);
                s_CachedRect[0].Set(elemRect.xMin, elemRect.yMin, 0.0f);
                s_CachedRect[1].Set(elemRect.xMax, elemRect.yMin, 0.0f);
                s_CachedRect[2].Set(elemRect.xMax, elemRect.yMax, 0.0f);
                s_CachedRect[3].Set(elemRect.xMin, elemRect.yMax, 0.0f);

                Handles.color = elem.MinimapColor * darken;

                DrawSolidRectangleWithOutline(ref s_CachedRect, elem.MinimapColor, elem.MinimapColor);

                if (elem.IsSelected())
                    DrawRectangleOutline(elemRect, m_SelectedElementColor);
            }
            k_DrawElementsAllUIs.Clear();
        }

        void DrawRectangleOutline(Rect rect, Color color)
        {
            Color currentColor = Handles.color;
            Handles.color = color;

            // Draw viewport outline
            Vector3[] points = new Vector3[5];
            points[0] = new Vector3(rect.x, rect.y, 0.0f);
            points[1] = new Vector3(rect.x + rect.width, rect.y, 0.0f);
            points[2] = new Vector3(rect.x + rect.width, rect.y + rect.height, 0.0f);
            points[3] = new Vector3(rect.x, rect.y + rect.height, 0.0f);
            points[4] = new Vector3(rect.x, rect.y, 0.0f);
            Handles.DrawPolyLine(points);

            Handles.color = currentColor;
        }

        static readonly List<ModelView> k_OnMouseDownAllUIs = new List<ModelView>();
        void OnMouseDown(MouseDownEvent e)
        {
            if (GraphView == null)
            {
                // Nothing to do if we're not attached to a GraphView!
                return;
            }

            // Refresh MiniMap rects
            CalculateRects(GraphView.ContentViewContainer);

            var mousePosition = e.localMousePosition;

            GraphModel.GraphElementModels.GetAllViewsInList(GraphView,
                elem => elem != null, k_OnMouseDownAllUIs);
            foreach (var child in k_OnMouseDownAllUIs.OfType<GraphElement>())
            {
                var isSelectable = child.GraphElementModel?.IsSelectable() ?? false;
                if (!isSelectable)
                {
                    continue;
                }

                if (CalculateElementRect(child).Contains(mousePosition))
                {
                    GraphView.DispatchFrameAndSelectElementsCommand(true, child);
                    e.StopPropagation();
                    break;
                }
            }

            k_OnMouseDownAllUIs.Clear();
        }
    }
}
