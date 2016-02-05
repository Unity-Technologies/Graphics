using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental
{
	internal class VFXEdNodeBlockContainer : CanvasElement
	{
		public string Title;
		private VFXEdDataSource m_DataSource;
		public Rect NodeRect;
		public bool Highlight = false;

		public VFXEdNodeBlockContainer(Vector2 position, Vector2 size, VFXEdDataSource dataSource, string name)
		{
			this.translation = position;
			this.scale = new Vector2(size.x, size.y);
			this.m_DataSource = dataSource;
			this.Title = name;
			
			this.NodeRect = new Rect(position.x, position.y, size.x, size.y);
			this.m_Caps = Capabilities.Normal;
			AddNodeBlock(new VFXEdNodeBlock(new Vector2(0, 0), new Vector2(this.scale.x, 64), dataSource, "Node Block Example"));
		}

		public void AddNodeBlock(VFXEdNodeBlock block)
		{
			if (block.parent == null) AddChild(block);
			else block.SetParent(this);
			block.translation = Vector3.zero;
			Layout();
		}

		
		public override void Layout()
		{
			base.Layout();
			if(this.Children().Length > 0)
			{
				float top = 0.0f;
				foreach (CanvasElement e in this.Children())
				{
					VFXEdNodeBlock b = e as VFXEdNodeBlock;
					b.translation = new Vector3(0.0f, top, 0.0f);
					b.scale = new Vector2(this.scale.x, b.scale.y);
					top += b.scale.y;
				}

				this.scale = new Vector2(scale.x, top);
			}
			else
			{
				this.scale = new Vector2(scale.x, 80.0f);
			}


		}

		public override void Render(Rect parentRect, Canvas2D canvas)
		{
			Rect r = GetDrawableRect();
			if(Highlight)
				Handles.DrawSolidRectangleWithOutline(r, new Color(1.0f, 1.0f, 1.0f, 0.05f), new Color(1.0f, 1.0f, 1.0f, 0.1f));
			if (this.Children().Length == 0)
			{
				GUI.Label(r, "Node Is Empty, please fill me.", VFXEditor.Styles.NodeInfoText);
			}
			base.Render(parentRect, canvas);
		}

	}
}

