using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental
{
	internal class VFXEdFlowAnchor : CanvasElement, IConnect
	{
		protected Type m_Type;
		protected object m_Source;
		protected Direction m_Direction;
		private VFXEdDataSource m_Data;
		public int m_PortIndex;

		public VFXEdFlowAnchor(int portIndex, Type type, VFXEdNode node, VFXEdDataSource data, Direction direction)
		{
			m_Type = type;
			scale = new Vector3(64.0f, 32.0f, 1.0f);

			AddManipulator(new FlowEdgeConnector());
			m_Direction = direction;

			Type genericClass = typeof(PortSource<>);
			Type constructedClass = genericClass.MakeGenericType(type);
			m_Source = Activator.CreateInstance(constructedClass);
			m_Data = data;
			m_PortIndex = portIndex;
		}

		public override void Layout()
		{
			scale = new Vector3(64.0f, 32.0f, 1.0f);
			base.Layout();
		}

		public override void Render(Rect parentRect, Canvas2D canvas)
		{
			base.Render(parentRect, canvas);
			GUI.color = new Color(0.8f, 0.8f, 0.8f);
			switch (m_Direction)
			{
				case Direction.Input:
					GUI.DrawTexture(GetDrawableRect(), VFXEditor.styles.FlowConnectorIn.normal.background);
					break;

				case Direction.Output:
					GUI.DrawTexture(GetDrawableRect(), VFXEditor.styles.FlowConnectorOut.normal.background);
					break;

				default:
					break;
			}
			GUI.color = Color.white;
		}

		public void RenderOverlay(Canvas2D canvas)
		{
			RectOffset o;

			switch (m_Direction)
			{
				case Direction.Input:
					o = VFXEditor.styles.ConnectorOverlay.overflow;
					GUI.DrawTexture(canvas.CanvasToScreen(o.Add(canvasBoundingRect)), VFXEditor.styles.ConnectorOverlay.normal.background);
					break;

				case Direction.Output:
					o = VFXEditor.styles.ConnectorOverlay.overflow;
					GUI.DrawTexture(canvas.CanvasToScreen(o.Add(canvasBoundingRect)), VFXEditor.styles.ConnectorOverlay.normal.background);

					break;

				default:
					break;
			}
		}

		// IConnect
		public Direction GetDirection()
		{
			return m_Direction;
		}

		public Orientation GetOrientation()
		{
			return Orientation.Vertical;
		}

		public void Highlight(bool highlighted)
		{

		}

		public object Source()
		{
			return m_Source;
		}

		public Vector3 ConnectPosition()
		{
			return canvasBoundingRect.center;
		}

		public void OnConnect(IConnect other)
		{
			if (other == null)
				return;

			VFXEdFlowAnchor otherConnector = other as VFXEdFlowAnchor;
			m_Data.ConnectFlow(this, otherConnector);

			ParentCanvas().ReloadData();
		}

	}

}
