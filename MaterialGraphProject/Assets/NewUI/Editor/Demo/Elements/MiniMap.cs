using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.RMGUI;

namespace RMGUI.GraphView.Demo
{
	[GUISkinStyle("box")]
	[CustomDataView(typeof(MiniMapData))]
	public class MiniMap : GraphElement
	{
		private Label m_Label;

		private float m_PreviousContainerWidth = -1;
		private float m_PreviousContainerHeight = -1;

		public MiniMap()
		{
			zBias = 99;
			clipChildren = true; // TODO this is the source of UNI-459

			m_Label = new Label(new GUIContent("Floating Minimap"));
			AddChild(m_Label);

			AddManipulator(new Dragger {activateButton = MouseButton.LeftMouse, clampToParentEdges = true});
		}

		public override void OnDataChanged()
		{
			base.OnDataChanged();
			Resize();
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
		}

		public override void DoRepaint(PaintContext args)
		{
			var gView = this.GetFirstAncestorOfType<GraphView>();
			var container = gView.contentViewContainer;

			Matrix4x4 containerTransform = container.globalTransform;
			Vector4 containerTranslation = containerTransform.GetColumn(3);
			var containerScale = new Vector2(containerTransform.m00, containerTransform.m11);
			Rect containerPosition = container.position;

			var containerWidth = parent.position.width / containerScale.x;
			var containerHeight = parent.position.height / containerScale.y;

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
				GraphElement elem = child as GraphElement;

				// TODO: Should Edges be displayed at all?
				// TODO: Maybe edges need their own capabilities flag.
				if (elem == null || (elem.GetData<GraphElementData>().capabilities & Capabilities.Floating) != 0 || (elem.dataProvider is EdgeData))
				{
					continue;
				}

				int titleBarOffset = (int)paddingTop;
				var rect = child.position;

				rect.x /= containerWidth;
				rect.width /= containerWidth;
				rect.y /= containerHeight;
				rect.height /= containerHeight;

				rect.x *= position.width;
				rect.y *= (position.height-titleBarOffset);
				rect.width *= position.width;
				rect.height *= (position.height-titleBarOffset);

				rect.y += titleBarOffset;

				rect.x += position.x;
				rect.y += position.y;

				rect.x += containerTranslation.x * position.width / parent.position.width;
				rect.y += containerTranslation.y * (position.height-titleBarOffset) / parent.position.height;

				rect.x += containerPosition.x * position.width / containerWidth;
				rect.y += containerPosition.y * (position.height-titleBarOffset) / containerHeight;

				if (rect.x < position.xMin)
				{
					if (rect.x < (position.xMin - rect.width))
					{
						continue;
					}
					rect.width -= (position.xMin - rect.x);
					rect.x = position.xMin;
				}

				if (rect.x + rect.width >= position.xMax)
				{
					if (rect.x >= position.xMax)
					{
						continue;
					}
					rect.width -= (rect.x + rect.width) - position.xMax;
				}

				if (rect.y < (position.yMin+titleBarOffset))
				{
					if (rect.y < ((position.yMin+titleBarOffset) - rect.height))
					{
						continue;
					}
					rect.height -= ((position.yMin+titleBarOffset) - rect.y);
					rect.y = (position.yMin+titleBarOffset);
				}

				if (rect.y + rect.height >= position.yMax)
				{
					if (rect.y >= position.yMax)
					{
						continue;
					}
					rect.height -= (rect.y + rect.height) - position.yMax;
				}

				Handles.DrawSolidRectangleWithOutline(rect, Color.grey, Color.grey);
			}
		}
	}
}
