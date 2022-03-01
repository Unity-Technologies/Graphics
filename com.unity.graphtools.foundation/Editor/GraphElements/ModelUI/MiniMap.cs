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
    public class MiniMap : GraphElement
    {
        public new static readonly string ussClassName = "ge-minimap";
        public static readonly string anchoredModifierClassName = ussClassName.WithUssModifier("anchored");
        public static readonly string windowedModifierClassName = ussClassName.WithUssModifier("windowed");

        static Vector3[] s_CachedRect = new Vector3[4];

        float m_PreviousContainerWidth = -1;

        float m_PreviousContainerHeight = -1;

        readonly Label m_Label;

        Dragger m_Dragger;

        readonly Color m_ViewportColor = new Color(1.0f, 1.0f, 0.0f, 0.35f);

        protected readonly Color m_SelectedChildrenColor = new Color(1.0f, 1.0f, 1.0f, 0.5f);

        readonly Color m_PlacematBorderColor = new Color(0.23f, 0.23f, 0.23f);

        Rect m_ViewportRect;        // Rect that represents the current viewport

        Rect m_ContentRect;         // Rect that represents the rect needed to encompass all Graph Elements

        Rect m_ContentRectLocal;    // Rect that represents the rect needed to encompass all Graph Elements in local coords

        bool m_Anchored;

        bool m_Windowed;

        public float MaxHeight { get; set; }

        public float MaxWidth { get; set; }

        int TitleBarOffset => (int)resolvedStyle.paddingTop;

        Dragger Dragger
        {
            get => m_Dragger;
            set
            {
                if (!m_Windowed)
                    this.RemoveManipulator(m_Dragger);
                m_Dragger = value;
                if (!m_Windowed)
                    this.AddManipulator(m_Dragger);
            }
        }

        public Action<string> ZoomFactorTextChanged { get; set; }

        public bool Anchored
        {
            get => m_Anchored;
            set
            {
                if (Windowed || m_Anchored == value)
                    return;

                m_Anchored = value;

                if (m_Anchored)
                {
                    this.ResetPositionProperties();
                    AddToClassList(anchoredModifierClassName);
                }
                else
                {
                    RemoveFromClassList(anchoredModifierClassName);
                }

                Resize();
            }
        }

        public bool Windowed
        {
            get => m_Windowed;
            set
            {
                if (m_Windowed == value) return;

                if (value)
                {
                    Anchored = false; // Can't be anchored and windowed
                    AddToClassList(windowedModifierClassName);
                    this.RemoveManipulator(m_Dragger);
                }
                else
                {
                    RemoveFromClassList(windowedModifierClassName);
                    this.AddManipulator(m_Dragger);
                }
                m_Windowed = value;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MiniMap"/> class.
        /// </summary>
        public MiniMap()
        {
            Dragger = new Dragger { ClampToParentEdges = true };

            Anchored = false;

            MaxWidth = 200;
            MaxHeight = 200;

            m_Label = new Label("Floating Minimap");

            Add(m_Label);

            RegisterCallback<MouseDownEvent>(OnMouseDown);
            m_Label.RegisterCallback<MouseDownEvent>(EatMouseDown);

            this.AddStylesheet("Minimap.uss");
            // TODO VladN: fix for light skin, remove when GTF supports light skin
            if (!EditorGUIUtility.isProSkin)
                this.AddStylesheet("Minimap_lightFix.uss");

            AddToClassList(ussClassName);

            generateVisualContent += OnGenerateVisualContent;
        }

        public override bool IsMovable()
        {
            return !m_Windowed && !m_Anchored;
        }

        void Resize()
        {
            if (Windowed || parent == null)
                return;

            style.width = MaxWidth;
            style.height = MaxHeight;

            // Relocate if partially visible on bottom or right side (left/top not checked, only bottom/right affected by a size change)
            if (resolvedStyle.left + resolvedStyle.width > parent.layout.x + parent.layout.width)
            {
                var newPosition = layout;
                newPosition.x -= resolvedStyle.left + resolvedStyle.width - (parent.layout.x + parent.layout.width);
                this.SetLayout(newPosition);
            }

            if (resolvedStyle.top + resolvedStyle.height > parent.layout.y + parent.layout.height)
            {
                var newPosition = layout;
                newPosition.y -= resolvedStyle.top + resolvedStyle.height - (parent.layout.y + parent.layout.height);
                this.SetLayout(newPosition);
            }

            var newMiniMapPos = layout;
            newMiniMapPos.width = resolvedStyle.width;
            newMiniMapPos.height = resolvedStyle.height;
            newMiniMapPos.x = Mathf.Max(parent.layout.x, newMiniMapPos.x);
            newMiniMapPos.y = Mathf.Max(parent.layout.y, newMiniMapPos.y);
            this.SetLayout(newMiniMapPos);
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
            m_Label.text = "MiniMap  " + zoomFactorText;
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

            var graphViewWB = GraphView.worldBound;

            m_ViewportRect.x += graphViewWB.x * containerInvScale.x;
            m_ViewportRect.y += graphViewWB.y * containerInvScale.y;
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

        Rect CalculateElementRect(ModelUI elem)
        {
            Rect rect = elem.ChangeCoordinatesTo(GraphView.ContentViewContainer, elem.GetRect());
            rect.x = m_ContentRect.x + ((rect.x - m_ContentRectLocal.x) * m_ContentRect.width / m_ContentRectLocal.width);
            rect.y = m_ContentRect.y + ((rect.y - m_ContentRectLocal.y) * m_ContentRect.height / m_ContentRectLocal.height);
            rect.width *= m_ContentRect.width / m_ContentRectLocal.width;
            rect.height *= m_ContentRect.height / m_ContentRectLocal.height;

            // Clip using a minimal 2 pixel wide frame around edges
            // (except yMin since we already have the titleBar offset which is enough for clipping)
            var xMin = 2;
            var yMin = Windowed ? 2 : 0;
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
            Color currentColor = Handles.color;

            if (GraphView == null)
            {
                // Just need to draw the minimum rect.
                Resize();
                return;
            }

            VisualElement container = GraphView.ContentViewContainer;

            // Retrieve all container relative information
            Matrix4x4 containerTransform = GraphView.ViewTransform.matrix;
            var containerScale = new Vector2(containerTransform.m00, containerTransform.m11);
            float containerWidth = parent.layout.width / containerScale.x;
            float containerHeight = parent.layout.height / containerScale.y;

            if (Mathf.Abs(containerWidth - m_PreviousContainerWidth) > Mathf.Epsilon ||
                Mathf.Abs(containerHeight - m_PreviousContainerHeight) > Mathf.Epsilon)
            {
                m_PreviousContainerWidth = containerWidth;
                m_PreviousContainerHeight = containerHeight;
                Resize();
            }

            // Refresh MiniMap rects
            CalculateRects(container);

            DrawElements();

            // Draw viewport outline
            DrawRectangleOutline(m_ViewportRect, m_ViewportColor);

            Handles.color = currentColor;
        }

        static readonly List<ModelUI> k_DrawElementsAllUIs = new List<ModelUI>();
        void DrawElements()
        {
            if (GraphView.GraphModel == null)
                return;

            // Draw placemats first ...
            foreach (var placemat in GraphView.GraphModel.PlacematModels)
            {
                var placematUI = placemat.GetUI<GraphElement>(View);

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
            GraphView.GraphModel.GraphElementModels.GetAllUIsInList(GraphView,
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
                    DrawRectangleOutline(elemRect, m_SelectedChildrenColor);
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

        void EatMouseDown(MouseDownEvent e)
        {
            // The minimap should not let any left mouse down go through when it's not movable.
            if (e.button == (int)MouseButton.LeftMouse && !IsMovable())
            {
                e.StopPropagation();
            }
        }

        static readonly List<ModelUI> k_OnMouseDownAllUIs = new List<ModelUI>();
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

            GraphView.GraphModel.GraphElementModels.GetAllUIsInList(GraphView,
                elem => elem != null, k_OnMouseDownAllUIs);
            foreach (var child in k_OnMouseDownAllUIs)
            {
                var selectable = child.GetFirstOfType<GraphElement>();
                var isSelectable = selectable?.Model?.IsSelectable() ?? false;
                if (!isSelectable)
                {
                    k_OnMouseDownAllUIs.Clear();
                    return;
                }

                if (CalculateElementRect(child).Contains(mousePosition))
                {
                    GraphView.DispatchFrameAndSelectElementsCommand(true, selectable);
                    e.StopPropagation();
                }
            }
            k_OnMouseDownAllUIs.Clear();
            EatMouseDown(e);
        }
    }
}
