using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental
{
	internal class VFXEdDataAnchor : CanvasElement, IConnect
	{
		protected Type m_Type;
		protected object m_Source;
		protected Direction m_Direction;
		private VFXEdDataSource m_Data;
		public VFXEdNode m_Node;
		public int m_PortIndex;

		public VFXEdDataAnchor(int portIndex, Vector3 position, Type type, VFXEdNode node, VFXEdDataSource data)
		{
			m_Type = type;
			scale = new Vector3(15.0f, 15.0f, 1.0f);
			translation = position;
			AddManipulator(new EdgeConnector<NodeAnchor>());
			m_Direction = Direction.Input;

			Type genericClass = typeof(PortSource<>);
			Type constructedClass = genericClass.MakeGenericType(type);
			m_Source = Activator.CreateInstance(constructedClass);
			m_Data = data;
			m_Node = node;
			m_PortIndex = portIndex;
		}

		public override void Render(Rect parentRect, Canvas2D canvas)
		{
			var anchorColor = Color.yellow;
			anchorColor.a = 0.7f;
			base.Render(parentRect, canvas);
			EditorGUI.DrawRect(new Rect(translation.x, translation.y, scale.x, scale.y), anchorColor);
			Rect labelRect = new Rect(translation.x + scale.x + 10.0f, translation.y, parentRect.width, 20.0f);
			GUI.Label(labelRect, m_Type.Name);
		}

		// IConnect
		public Direction GetDirection()
		{
			return m_Direction;
		}

		public void Highlight(bool highlighted)
		{
		}

		public void RenderOverlay(Canvas2D canvas)
		{
			Rect thisRect = canvasBoundingRect;
			thisRect.x += 4;
			thisRect.y += 4;
			thisRect.width -= 8;
			thisRect.height -= 8;
			thisRect = canvas.CanvasToScreen(thisRect);
			EditorGUI.DrawRect(thisRect, new Color(0.0f, 0.0f, 0.8f));
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

			VFXEdDataAnchor otherConnector = other as VFXEdDataAnchor;
			m_Data.Connect(this, otherConnector);

			ParentCanvas().ReloadData();
		}

	}

}
