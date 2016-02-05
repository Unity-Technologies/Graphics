using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental
{
	internal class VFXEdNodeClientArea : CanvasElement
	{
		private string m_Title;

		public VFXEdNodeBlockContainer NodeBlockContainer { get { return m_NodeBlockContainer; } }
		private VFXEdNodeBlockContainer m_NodeBlockContainer;

		public VFXEdNodeClientArea(Vector2 position, Vector2 size, VFXEdDataSource dataSource, string name)
		{
			translation = new Vector3(0.0f, 24.0f, 0.0f);
			m_Title = name;
			scale = new Vector2(size.x, size.y);
			m_Caps = Capabilities.Normal;
			m_NodeBlockContainer = new VFXEdNodeBlockContainer(new Vector2(7, 30), new Vector2(size.x - 15, size.y - 40), dataSource, name);
			AddChild(m_NodeBlockContainer);
		}


		public override void Layout()
		{
			base.Layout();
			scale = new Vector2(scale.x, m_NodeBlockContainer.scale.y + 41);
		}


		public override void Render(Rect parentRect, Canvas2D canvas)
		{
			Rect r = GetDrawableRect();

			if (selected)
				GUI.Box(r, "", VFXEditor.styles.NodeSelected);
			else
				GUI.Box(r, "", VFXEditor.styles.Node);

			GUI.Label(new Rect(0, r.y, r.width, 24), m_Title, VFXEditor.styles.NodeTitle);

			base.Render(parentRect, canvas);
		}

	}
}

