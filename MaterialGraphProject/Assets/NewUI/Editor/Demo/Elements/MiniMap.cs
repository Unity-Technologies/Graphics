using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.RMGUI;

namespace RMGUI.GraphView.Demo
{
	[GUISkinStyle("box")]
	public class MiniMap : GraphElement
	{
		private float m_PreviousContainerWidth = -1;
		private float m_PreviousContainerHeight = -1;

		private readonly Label m_Label;
		private readonly Dragger m_Dragger;

		public MiniMap()
		{
			zBias = 99;
			clipChildren = false;

			m_Label = new Label(new GUIContent("Floating Minimap"));
			AddChild(m_Label);

			m_Dragger = new Dragger {activateButton = MouseButton.LeftMouse, clampToParentEdges = true};

			AddManipulator(new ContextualMenu((evt, customData) =>
			{
				var boxData = dataProvider as MiniMapData;
				if (boxData != null)
				{
					var menu = new GenericMenu();
					menu.AddItem(new GUIContent(boxData.anchored ? "Make floating" :  "Anchor"), false,
						contentView =>
						{
							var bData = dataProvider as MiniMapData;
							if (bData != null)
								bData.anchored = !bData.anchored;
						},
								 this);
					menu.ShowAsContext();
				}
				return EventPropagation.Continue;
			}));
		}

		public override void OnDataChanged()
		{
			base.OnDataChanged();
			AdjustAnchoring();
			Resize();
		}

		private void AdjustAnchoring()
		{
			var miniMapData = dataProvider as MiniMapData;
			if (miniMapData == null)
			{
				return;
			}

			// TODO we might want to update the movable capability...
			if (miniMapData.anchored)
			{
				RemoveManipulator(m_Dragger);
				ResetPositionProperties();
				AddToClassList("anchored");
			}
			else
			{
				AddManipulator(m_Dragger);
				RemoveFromClassList("anchored");
			}
		}

		private void Resize()
		{
			var miniMapData = dataProvider as MiniMapData;
			if (miniMapData == null || parent == null)
 			{
				return;
			}

			if (parent.position.height > parent.position.width)
			{
				height = miniMapData.maxHeight;
				width = parent.position.width * height / parent.position.height;
			}
			else
			{
				width = miniMapData.maxWidth;
				height = parent.position.height * width / parent.position.width;
 			}

			// Relocate if partially visible on bottom or right side (left/top not checked, only bottom/right affected by a size change)
			if (positionLeft + width > parent.position.x + parent.position.width)
			{
				var newPosition = miniMapData.position;
				newPosition.x -= positionLeft + width - (parent.position.x + parent.position.width);
				miniMapData.position = newPosition;
			}

			if (positionTop + height > parent.position.y + parent.position.height)
			{
				var newPosition = miniMapData.position;
				newPosition.y -= positionTop + height - (parent.position.y + parent.position.height);
				miniMapData.position = newPosition;
			}
		}

		public override void DoRepaint(PaintContext args)
		{
			var gView = this.GetFirstAncestorOfType<GraphView>();
			VisualContainer container = gView.contentViewContainer;

			Matrix4x4 containerTransform = container.globalTransform;
			Vector4 containerTranslation = containerTransform.GetColumn(3);
			var containerScale = new Vector2(containerTransform.m00, containerTransform.m11);
			Rect containerPosition = container.position;

			float containerWidth = parent.position.width / containerScale.x;
			float containerHeight = parent.position.height / containerScale.y;

			if ( (containerWidth != m_PreviousContainerWidth || containerHeight != m_PreviousContainerHeight) && dataProvider != null)
			{
				m_PreviousContainerWidth = containerWidth;
				m_PreviousContainerHeight = containerHeight;
				Resize();
			}

			m_Label.content = new GUIContent("Minimap p:" +
											 String.Format("{0:0}", containerPosition.position.x) + "," + String.Format("{0:0}", containerPosition.position.y) + " t: " +
											 String.Format("{0:0}", containerTranslation.x) + "," + String.Format("{0:0}", containerTranslation.y) + " s: " +
											 String.Format("{0:N2}", containerScale.x)/* + "," + String.Format("{0:N2}", containerScale.y)*/);

			base.DoRepaint(args);

			foreach (var child in container.children)
			{
				// For some reason, I can't seem to be able to use Linq (IEnumerable.OfType() nor IEnumerable.Where appear to be working here. ??)
				var elem = child as GraphElement;

				// TODO: Should Edges be displayed at all?
				// TODO: Maybe edges need their own capabilities flag.
				if (elem == null || (elem.dataProvider.capabilities & Capabilities.Floating) != 0 || (elem.dataProvider is EdgeData))
				{
					continue;
				}

				var titleBarOffset = (int)paddingTop;
				Rect rect = child.position;

				rect.x /= containerWidth;
				rect.width /= containerWidth;
				rect.y /= containerHeight;
				rect.height /= containerHeight;

				rect.x *= position.width;
				rect.y *= position.height-titleBarOffset;
				rect.width *= position.width;
				rect.height *= position.height-titleBarOffset;

				rect.y += titleBarOffset;

				rect.x += position.x;
				rect.y += position.y;

				rect.x += containerTranslation.x * position.width / parent.position.width;
				rect.y += containerTranslation.y * (position.height-titleBarOffset) / parent.position.height;

				rect.x += containerPosition.x * position.width / containerWidth;
				rect.y += containerPosition.y * (position.height-titleBarOffset) / containerHeight;

				if (rect.x < position.xMin)
				{
					if (rect.x < position.xMin - rect.width)
					{
						continue;
					}
					rect.width -= position.xMin - rect.x;
					rect.x = position.xMin;
				}

				if (rect.x + rect.width >= position.xMax)
				{
					if (rect.x >= position.xMax)
					{
						continue;
					}
					rect.width -= rect.x + rect.width - position.xMax;
				}

				if (rect.y < position.yMin+titleBarOffset)
				{
					if (rect.y < position.yMin+titleBarOffset - rect.height)
					{
						continue;
					}
					rect.height -= position.yMin+titleBarOffset - rect.y;
					rect.y = position.yMin+titleBarOffset;
				}

				if (rect.y + rect.height >= position.yMax)
				{
					if (rect.y >= position.yMax)
					{
						continue;
					}
					rect.height -= rect.y + rect.height - position.yMax;
				}

				Handles.DrawSolidRectangleWithOutline(rect, Color.grey, Color.grey);
			}
		}
	}
}
