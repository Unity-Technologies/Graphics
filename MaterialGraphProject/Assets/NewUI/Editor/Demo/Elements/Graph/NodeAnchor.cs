using System;
using UnityEditor;
using UnityEngine;
using UnityEngine.RMGUI;
using UnityEngine.RMGUI.StyleEnums.Values;

namespace RMGUI.GraphView.Demo
{
	[CustomDataView(typeof(NodeAnchorData))]
	internal class NodeAnchor : GraphElement
	{
		public const float k_NodeSize = 15.0f;

		private EdgeConnector<EdgeData> m_RegularConnector = new EdgeConnector<EdgeData>();
		private EdgeConnector<CustomEdgeData> m_CustomConnector = new EdgeConnector<CustomEdgeData>();

		private IManipulator m_CurrentConnector;

		public NodeAnchor(NodeAnchorData data)
		{
			m_CurrentConnector = m_RegularConnector;
			AddManipulator(m_CurrentConnector);

			dataProvider = data;
		}

		private void UpdateConnector()
		{
			var nodeAnchorData = dataProvider as NodeAnchorData;
			if (nodeAnchorData == null)
				return;

			RemoveManipulator(m_CurrentConnector);
			if (!nodeAnchorData.connected || nodeAnchorData.direction != Direction.Input)
			{
				if (nodeAnchorData.orientation == Orientation.Horizontal)
				{
					m_CurrentConnector = m_RegularConnector;
				}
				else
				{
					m_CurrentConnector = m_CustomConnector;
				}
				AddManipulator(m_CurrentConnector);
			}
		}

		private Rect GetAnchorRect(NodeAnchorData nodeAnchorData)
		{
			Rect rect = new Rect();

			if (nodeAnchorData.orientation == Orientation.Horizontal)
			{
				// TODO: placement could be better handled using better CSS properties to place the node anchor itself.
				if (nodeAnchorData.direction == Direction.Input)
				{
					rect = new Rect(position.x + 2, position.y + 2, k_NodeSize, k_NodeSize);
				}
				else if (nodeAnchorData.direction == Direction.Output)
				{
					rect = new Rect(position.x + position.width - (7 + k_NodeSize), position.y + 2, k_NodeSize, k_NodeSize);
				}
			}
			else
			{
				if (nodeAnchorData.direction == Direction.Input)
				{
					rect = new Rect(position.x + (position.width - k_NodeSize)/2, position.y + 4, k_NodeSize, k_NodeSize);
				}
				else if (nodeAnchorData.direction == Direction.Output)
				{
					rect = new Rect(position.x + (position.width - k_NodeSize)/2, position.y + position.height - k_NodeSize - 8, k_NodeSize, k_NodeSize);
				}
			}
			return rect;
		}

		protected virtual void DrawConnector()
		{
			// TODO This cast here is not ideal
			var nodeAnchorData = dataProvider as NodeAnchorData;
			if (nodeAnchorData == null)
				return;

			var anchorColor = Color.yellow;

			anchorColor.a = 0.7f;
			Rect rect = GetAnchorRect(nodeAnchorData);
			Handles.DrawSolidRectangleWithOutline(rect, anchorColor, anchorColor);

			if (nodeAnchorData.highlight)
			{
				var highlightColor = Color.blue;
				Rect highlighRect = rect;
				highlighRect.x += 4;
				highlighRect.y += 4;
				highlighRect.width -= 8;
				highlighRect.height -= 8;
				Handles.DrawSolidRectangleWithOutline(highlighRect, highlightColor, highlightColor);
			}
		}

		public override void DoRepaint(PaintContext args)
		{
			base.DoRepaint(args);
			DrawConnector();
		}

		public override void OnDataChanged()
		{
			UpdateConnector();
			ClearChildren();

			var nodeAnchorData = dataProvider as NodeAnchorData;
			if (nodeAnchorData == null)
				return;

			Type type = nodeAnchorData.type;

			Type genericClass = typeof(PortSource<>);
			Type constructedClass = genericClass.MakeGenericType(type);
			nodeAnchorData.source = Activator.CreateInstance(constructedClass);

			Label label;
			// TODO: I figure this placement could be more generic with a better use of CSS placement
			if (nodeAnchorData.orientation == Orientation.Horizontal)
			{
				label = new Label(new GUIContent(nodeAnchorData.name))
				{
					positionType = PositionType.Absolute,
					positionTop = 0,
					positionLeft = 20,
					positionRight = 0,
					positionBottom = 0
				};

				if (nodeAnchorData.direction == Direction.Output)
				{
					label.textAlignment = TextAnchor.UpperRight;
					label.positionLeft = 0;
					label.positionRight = 25;
				}
			}
			else
			{
				label = new Label(new GUIContent(type.Name))
				{
					positionType = PositionType.Absolute,
					positionTop = 20,
					positionLeft = 0,
					positionRight = 0,
					positionBottom = 0
				};

				if (nodeAnchorData.direction == Direction.Output)
				{
					label.textAlignment = TextAnchor.LowerCenter;
					label.positionTop = 0;
					label.positionBottom = 25;
				}
				else
				{
					label.textAlignment = TextAnchor.UpperCenter;
				}
			}

			GetData<GraphElementData>().capabilities &= ~Capabilities.Selectable;

			label.pickingMode = PickingMode.Ignore;
			AddChild(label);
		}

		public Rect GetSelectionRect()
		{
			var nodeAnchorData = dataProvider as NodeAnchorData;
			if (nodeAnchorData == null)
				return new Rect();

			return GetAnchorRect(nodeAnchorData);
		}

		public override Vector3 GetGlobalCenter()
		{
			var center = GetSelectionRect().center;
			var globalCenter = new Vector3(center.x + parent.position.x, center.y + parent.position.y);
			return parent.globalTransform.MultiplyPoint3x4(globalCenter);
		}

		public override bool Overlaps(Rect rect)
		{
			return GetSelectionRect().Overlaps(rect);
		}

		public override bool ContainsPoint(Vector2 localPoint)
		{
			return GetSelectionRect().Contains(localPoint);
		}
	}
}
