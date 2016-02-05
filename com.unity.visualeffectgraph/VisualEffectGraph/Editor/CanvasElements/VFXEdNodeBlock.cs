using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental
{
	internal class VFXEdNodeBlock : CanvasElement
	{
		public string Title;
		private NodeBlockManipulator m_NodeBlockManipulator;

		public VFXEdNodeBlock(Vector2 position, Vector2 size, VFXEdDataSource dataSource, string name)
		{
			this.translation = new Vector3(0.0f, 0.0f, 0.0f);
			this.Title = name;
			this.scale = new Vector2(size.x, size.y);
			
			this.m_Caps = Capabilities.Normal;
			this.m_NodeBlockManipulator = new NodeBlockManipulator();
			this.AddManipulator(this.m_NodeBlockManipulator);
		}

		public void EnableDrag()
		{
			this.caps = Capabilities.Normal;
		}

		public void DisableDrag()
		{
			this.caps = Capabilities.Unselectable;
		}

		public override void Render(Rect parentRect, Canvas2D canvas)
		{
			Rect r = GetDrawableRect();

			// TODO : Manage Nodeblock Selection properly
			// if(!selected) GUI.Box(r, "", VFXEditor.Styles.NodeBlock);
			// else GUI.Box(r, "", VFXEditor.Styles.NodeBlockSelected);

			GUI.Box(r, "", VFXEditor.Styles.NodeBlock);
			GUI.Box(new Rect(r.x +4, r.y + 4, 16, 16), "", VFXEditor.Styles.Foldout);
			GUI.Label(new Rect(r.x + 16, r.y, r.width, 24), this.Title, VFXEditor.Styles.NodeBlockTitle);

			base.Render(parentRect, canvas);
		}
	}
}

