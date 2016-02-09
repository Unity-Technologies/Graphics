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
		public string name { get { return m_Name; } }
		private string m_Name;

		private NodeBlockManipulator m_NodeBlockManipulator;
		
		private const float DEFAULT_HEIGHT = 26.0f;
		private const float PARAM_HEIGHT = 20.0f;

		public VFXEdNodeBlock(VFXBlock block, Vector2 position, float width, VFXEdDataSource dataSource)
		{
			m_Block = block;
			m_Name = block.m_Name;
			translation = new Vector3(0.0f, 0.0f, 0.0f);
			scale = new Vector2(width, DEFAULT_HEIGHT + m_Block.m_Params.Length * PARAM_HEIGHT);
			
			m_Caps = Capabilities.Normal;
			m_NodeBlockManipulator = new NodeBlockManipulator();
			AddManipulator(m_NodeBlockManipulator);
			AddManipulator(new NodeBlockDelete());
		}
	


		public override void Render(Rect parentRect, Canvas2D canvas)
		{
			Rect r = GetDrawableRect();

			// TODO : Manage Nodeblock Selection properly

			if(parent is VFXEdNodeBlockContainer)
			{
				if ((canvas as VFXEdCanvas).SelectedNodeBlock != this)
					GUI.Box(r, "", VFXEditor.styles.NodeBlock);
				else
					GUI.Box(r, "", VFXEditor.styles.NodeBlockSelected);
			}
			else // If currently dragged...
			{
				Color c = GUI.color;
				GUI.color = new Color(GUI.color.r,GUI.color.g,GUI.color.a,0.75f);
				GUI.Box(r, "", VFXEditor.styles.NodeBlockSelected);
				GUI.color = c;

			}


			//GUI.Box(r, "", VFXEditor.styles.NodeBlock);
			GUI.Box(new Rect(r.x +4, r.y + 4, 16, 16), "", VFXEditor.styles.Foldout);
			GUI.Label(new Rect(r.x + 16, r.y, r.width, 24), m_Block.m_Name, VFXEditor.styles.NodeBlockTitle);

			for (int i = 0; i < m_Block.m_Params.Length; ++i)
				GUI.Label(new Rect(r.x + 8, r.y + DEFAULT_HEIGHT + i * PARAM_HEIGHT, r.width, PARAM_HEIGHT - 2), m_Block.m_Params[i].m_Name, VFXEditor.styles.NodeBlockParameter);

			base.Render(parentRect, canvas);
		}

		private VFXBlock m_Block;
	}
}

