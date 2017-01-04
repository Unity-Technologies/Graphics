using UnityEditor;
using UnityEngine;
using UnityEngine.RMGUI;

namespace RMGUI.GraphView.Demo
{
	public class MiniMap : GraphElement
	{
		float m_PreviousContainerWidth = -1;
		float m_PreviousContainerHeight = -1;

		readonly Label m_Label;
		Dragger m_Dragger;

		Color m_ViewportColor = new Color(1.0f, 1.0f, 0.0f, 0.35f);
		readonly Color m_SelectedChildrenColor = new Color(1.0f, 1.0f, 1.0f, 0.5f);

		// Various rects used by the MiniMap
		Rect m_ViewportRect;		// Rect that represents the current viewport
		Rect m_ContentRect;			// Rect that represents the rect needed to encompass all Graph Elements
		Rect m_ContentRectLocal;	// Rect that represents the rect needed to encompass all Graph Elements in local coords

		int titleBarOffset { get { return (int)paddingTop; } }

		public MiniMap()
		{
			clipChildren = false;

			m_Label = new Label(new GUIContent("Floating Minimap"));

			AddChild(m_Label);

			AddManipulator(new ContextualMenu((evt, customData) =>
			{
				var boxPresenter = GetPresenter<MiniMapPresenter>();
				var menu = new GenericMenu();
				menu.AddItem(new GUIContent(boxPresenter.anchored ? "Make floating" :  "Anchor"), false,
					contentView =>
					{
						var bPresenter = GetPresenter<MiniMapPresenter>();
						bPresenter.anchored = !bPresenter.anchored;
					},
					this);
				menu.ShowAsContext();
				return EventPropagation.Continue;
			}));
		}

		public override void OnDataChanged()
		{
			base.OnDataChanged();
			AdjustAnchoring();
			Resize();
		}

		void AdjustAnchoring()
		{
			var miniMapPresenter = GetPresenter<MiniMapPresenter>();
			if (miniMapPresenter == null)
				return;

			if (miniMapPresenter.anchored)
			{
				miniMapPresenter.capabilities &= ~Capabilities.Movable;
				ResetPositionProperties();
				AddToClassList("anchored");
			}
			else
			{
				if (m_Dragger == null)
				{
					m_Dragger = new Dragger {clampToParentEdges = true};
					AddManipulator(m_Dragger);
				}
				presenter.capabilities |= Capabilities.Movable;
				RemoveFromClassList("anchored");
			}
		}

		void Resize()
		{
			if (parent == null)
				return;

			var miniMapPresenter = GetPresenter<MiniMapPresenter>();
			width = miniMapPresenter.maxWidth;
			height = miniMapPresenter.maxHeight;

			// Relocate if partially visible on bottom or right side (left/top not checked, only bottom/right affected by a size change)
			if (positionLeft + width > parent.position.x + parent.position.width)
			{
				var newPosition = miniMapPresenter.position;
				newPosition.x -= positionLeft + width - (parent.position.x + parent.position.width);
				miniMapPresenter.position = newPosition;
			}

			if (positionTop + height > parent.position.y + parent.position.height)
			{
				var newPosition = miniMapPresenter.position;
				newPosition.y -= positionTop + height - (parent.position.y + parent.position.height);
				miniMapPresenter.position = newPosition;
			}

			var newMiniMapPos = miniMapPresenter.position;
			newMiniMapPos.width = width;
			newMiniMapPos.height = height;
			newMiniMapPos.x = Mathf.Max(parent.position.x, newMiniMapPos.x);
			newMiniMapPos.y = Mathf.Max(parent.position.y, newMiniMapPos.y);
			miniMapPresenter.position = newMiniMapPos;

			if (!miniMapPresenter.anchored)
			{
				// Update to prevent onscreen mishaps especially at tiny window sizes
				position = miniMapPresenter.position;
			}
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

		void CalculateRects(VisualContainer container)
		{
			m_ContentRect = GraphView.CalculateRectToFitAll(container);
			m_ContentRectLocal = m_ContentRect;

			// Retrieve viewport rectangle as if zoom and pan were inactive
			Matrix4x4 containerInvTransform = container.globalTransform.inverse;
			Vector4 containerInvTranslation = containerInvTransform.GetColumn(3);
			var containerInvScale = new Vector2(containerInvTransform.m00, containerInvTransform.m11);

			m_ViewportRect = parent.position;

			// Bring back viewport coordinates to (0,0), scale 1:1
			m_ViewportRect.x += containerInvTranslation.x;
			m_ViewportRect.y += containerInvTranslation.y;
			m_ViewportRect.width *= containerInvScale.x;
			m_ViewportRect.height *= containerInvScale.y;

			// Update label with new value
			m_Label.content.text = "MiniMap v: " +
								   string.Format("{0:0}", m_ViewportRect.width) + "x" +
								   string.Format("{0:0}", m_ViewportRect.height);

			// Adjust rects for MiniMap

			// Encompass viewport rectangle (as if zoom and pan were inactive)
			var totalRect = RectUtils.Encompass(m_ContentRect, m_ViewportRect);
			var minimapFactor = position.width/totalRect.width;

			// Transform each rect to MiniMap coordinates
			ChangeToMiniMapCoords(ref totalRect, minimapFactor, Vector3.zero);

			var minimapTranslation = new Vector3(position.x - totalRect.x, position.y + titleBarOffset - totalRect.y);
			ChangeToMiniMapCoords(ref m_ViewportRect, minimapFactor, minimapTranslation);
			ChangeToMiniMapCoords(ref m_ContentRect, minimapFactor, minimapTranslation);

			// Diminish and center everything to fit vertically
			if (totalRect.height > (position.height - titleBarOffset))
			{
				float totalRectFactor = (position.height - titleBarOffset) / totalRect.height;
				float totalRectOffsetX = (position.width - (totalRect.width*totalRectFactor)) / 2.0f;
				float totalRectOffsetY = position.y + titleBarOffset - ((totalRect.y + minimapTranslation.y) * totalRectFactor);

				m_ContentRect.width *= totalRectFactor;
				m_ContentRect.height *= totalRectFactor;
				m_ContentRect.y *= totalRectFactor;
				m_ContentRect.x += totalRectOffsetX;
				m_ContentRect.y += totalRectOffsetY;

				m_ViewportRect.width *= totalRectFactor;
				m_ViewportRect.height *= totalRectFactor;
				m_ViewportRect.y *= totalRectFactor;
				m_ViewportRect.x += totalRectOffsetX;
				m_ViewportRect.y += totalRectOffsetY;
			}
		}

		Rect CalculateElementRect(GraphElement elem)
		{
			// TODO: Should Edges be displayed at all?
			// TODO: Maybe edges need their own capabilities flag.
			var elementPresenter = elem.GetPresenter<GraphElementPresenter>();
			if ((elementPresenter.capabilities & Capabilities.Floating) != 0 ||
			    (elementPresenter is EdgePresenter))
			{
				return new Rect(0, 0, 0, 0);
			}

			Rect rect = elem.localBound;
			rect.x = m_ContentRect.x + ((rect.x - m_ContentRectLocal.x) * m_ContentRect.width / m_ContentRectLocal.width);
			rect.y = m_ContentRect.y + ((rect.y - m_ContentRectLocal.y) * m_ContentRect.height / m_ContentRectLocal.height);
			rect.width *= m_ContentRect.width / m_ContentRectLocal.width;
			rect.height *= m_ContentRect.height / m_ContentRectLocal.height;

			// Clip using a minimal 2 pixel wide frame around edges
			// (except yMin since we already have the titleBar offset which is enough for clipping)
			var xMin = position.xMin + 2;
			var xMax = position.xMax - 2;
			var yMax = position.yMax - 2;

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

			if (rect.y < position.yMin + titleBarOffset)
			{
				if (rect.y < position.yMin + titleBarOffset - rect.height)
					return new Rect(0, 0, 0, 0);
				rect.height -= position.yMin + titleBarOffset - rect.y;
				rect.y = position.yMin + titleBarOffset;
			}

			if (rect.y + rect.height >= yMax)
			{
				if (rect.y >= yMax)
					return new Rect(0, 0, 0, 0);
				rect.height -= rect.y + rect.height - yMax;
			}

			return rect;
		}

		public override void DoRepaint(IStylePainter painter)
		{
			var gView = this.GetFirstAncestorOfType<GraphView>();
			VisualContainer container = gView.contentViewContainer;

			// Retrieve all container relative information
			Matrix4x4 containerTransform = container.globalTransform;
			var containerScale = new Vector2(containerTransform.m00, containerTransform.m11);
			float containerWidth = parent.position.width / containerScale.x;
			float containerHeight = parent.position.height / containerScale.y;

			if (Mathf.Abs(containerWidth - m_PreviousContainerWidth) > Mathf.Epsilon ||
				Mathf.Abs(containerHeight - m_PreviousContainerHeight) > Mathf.Epsilon)
			{
				m_PreviousContainerWidth = containerWidth;
				m_PreviousContainerHeight = containerHeight;
				Resize();
			}

			// Refresh MiniMap rects
			CalculateRects(container);

			// Now let's draw some stuff
			base.DoRepaint(painter);

			// Display elements in the MiniMap
			Color currentColor = Handles.color;
			foreach (var child in container.children)
			{
				var elem = child as GraphElement;
				if (elem == null)
					continue;

				var rect = CalculateElementRect(elem);
				Handles.color = elem.elementTypeColor;
				Handles.DrawSolidRectangleWithOutline(rect, elem.elementTypeColor, elem.elementTypeColor);
				var elementPresenter = elem.GetPresenter<GraphElementPresenter>();
				if (elementPresenter != null && elementPresenter.selected)
					DrawRectangleOutline(rect, m_SelectedChildrenColor);
			}

			// Draw viewport outline
			DrawRectangleOutline(m_ViewportRect, m_ViewportColor);

			Handles.color = currentColor;
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

		public override EventPropagation HandleEvent(Event evt, VisualElement finalTarget)
		{
			switch (evt.type)
			{
				case EventType.MouseDown:
				{
					var gView = this.GetFirstAncestorOfType<GraphView>();
					VisualContainer container = gView.contentViewContainer;

					// Refresh MiniMap rects
					CalculateRects(container);

					var mousePosition = evt.mousePosition;
					mousePosition.x += position.x;
					mousePosition.y += position.y;

					foreach (var child in container.children)
					{
						var elem = child as GraphElement;
						if (elem == null)
							continue;
						var selectable = child.GetFirstOfType<ISelectable>();
						if (selectable == null || !selectable.IsSelectable())
							continue;

						if (CalculateElementRect(elem).Contains(mousePosition))
						{
							gView.ClearSelection();
							gView.AddToSelection(selectable);
							gView.FrameSelection();
							return EventPropagation.Stop;
						}
					}
				}
				break;
			}

			return EventPropagation.Continue;
		}
 	}
}
