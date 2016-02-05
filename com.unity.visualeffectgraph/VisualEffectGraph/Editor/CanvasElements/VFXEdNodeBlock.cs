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
		private NodeBlockManipulator m_NodeBlockManipulator;
		
		private const float DEFAULT_HEIGHT = 26.0f;
		private const float PARAM_HEIGHT = 20.0f;

		public VFXEdNodeBlock(VFXBlock block, Vector2 position, float width, VFXEdDataSource dataSource)
		{
			m_Block = block;

			translation = new Vector3(0.0f, 0.0f, 0.0f);
			scale = new Vector2(width, DEFAULT_HEIGHT + m_Block.m_Params.Length * PARAM_HEIGHT);
			
			m_Caps = Capabilities.Normal;
			m_NodeBlockManipulator = new NodeBlockManipulator();
			AddManipulator(this.m_NodeBlockManipulator);
		}
	
		public void EnableDrag()
		{
			caps = Capabilities.Normal;
		}

		public void DisableDrag()
		{
			caps = Capabilities.Unselectable;
		}

		public override void Render(Rect parentRect, Canvas2D canvas)
		{
			Rect r = GetDrawableRect();

			// TODO : Manage Nodeblock Selection properly
			// if(!selected) GUI.Box(r, "", VFXEditor.Styles.NodeBlock);
			// else GUI.Box(r, "", VFXEditor.Styles.NodeBlockSelected);

			GUI.Box(r, "", VFXEditor.Styles.NodeBlock);
			GUI.Box(new Rect(r.x +4, r.y + 4, 16, 16), "", VFXEditor.Styles.Foldout);
			GUI.Label(new Rect(r.x + 16, r.y, r.width, 24), m_Block.m_Name, VFXEditor.Styles.NodeBlockTitle);

			for (int i = 0; i < m_Block.m_Params.Length; ++i)
				GUI.Label(new Rect(r.x + 8, r.y + DEFAULT_HEIGHT + i * PARAM_HEIGHT, r.width, PARAM_HEIGHT - 2), m_Block.m_Params[i].m_Name, VFXEditor.Styles.NodeBlockTitle);

			base.Render(parentRect, canvas);
		}

		private VFXBlock m_Block;
	}
}

