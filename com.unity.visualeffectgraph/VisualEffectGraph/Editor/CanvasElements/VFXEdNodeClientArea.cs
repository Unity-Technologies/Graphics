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
		public string Title;
		private VFXEdDataSource m_DataSource;
		public Rect NodeRect;

		public VFXEdNodeBlockContainer NodeBlockContainer { get { return this.m_NodeBlockContainer; } }
		private VFXEdNodeBlockContainer m_NodeBlockContainer;

		public VFXEdNodeClientArea(Vector2 position, Vector2 size, VFXEdDataSource dataSource, string name)
		{
			this.translation = new Vector3(0.0f, 24.0f, 0.0f);
			this.m_DataSource = dataSource;
			this.Title = name;
			this.scale = new Vector2(size.x, size.y);
			this.NodeRect = new Rect(0, 24, size.x, size.y);
			this.m_Caps = Capabilities.Normal;
			this.m_NodeBlockContainer = new VFXEdNodeBlockContainer(new Vector2(7, 30), new Vector2(size.x - 15, size.y - 40), dataSource, name);
			this.AddChild(m_NodeBlockContainer);
		}


		public override void Layout()
		{
			base.Layout();
			this.scale = new Vector2(this.scale.x, m_NodeBlockContainer.scale.y + 41);
		}


		public override void Render(Rect parentRect, Canvas2D canvas)
		{
			Rect r = GetDrawableRect();

			if (this.selected)
				GUI.Box(r, "", VFXEditor.Styles.NodeSelected);
			else
				GUI.Box(r, "", VFXEditor.Styles.Node);

			GUI.Label(new Rect(0, r.y, r.width, 24), this.Title, VFXEditor.Styles.NodeTitle);

			base.Render(parentRect, canvas);
		}

	}
}

