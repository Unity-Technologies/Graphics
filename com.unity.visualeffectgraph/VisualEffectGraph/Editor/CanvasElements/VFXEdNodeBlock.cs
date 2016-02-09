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
		private const float PARAM_ADDITIONAL_HEIGHT = 14.0f;

		public VFXEdNodeBlock(VFXBlock block, Vector2 position, float width, VFXEdDataSource dataSource)
		{
			m_Block = block;
			m_Name = block.m_Name;
			translation = new Vector3(0.0f, 0.0f, 0.0f);
			scale = new Vector2(width, GetHeight());
			
			m_Caps = Capabilities.Normal;
			m_NodeBlockManipulator = new NodeBlockManipulator();
			AddManipulator(m_NodeBlockManipulator);
			AddManipulator(new NodeBlockDelete());
		}

		// Retrieve the height of a given param
		private float GetParamHeight(VFXParam param)
		{
			float height = PARAM_HEIGHT;
			switch (param.m_Type)
			{
				case VFXParam.Type.kTypeFloat2:
				case VFXParam.Type.kTypeFloat3:
				case VFXParam.Type.kTypeFloat4:
					height += PARAM_ADDITIONAL_HEIGHT;
					break;

				default: break;
			}
			return height;
		}

		// Retrieve the total height of a block
		private float GetHeight()
		{
			float height = DEFAULT_HEIGHT;
			for (int i = 0; i < m_Block.m_Params.Length; ++i)
				height += GetParamHeight(m_Block.m_Params[i]);
			return height;
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

			float currentY = r.y + DEFAULT_HEIGHT;
			for (int i = 0; i < m_Block.m_Params.Length; ++i)
			{
				VFXParam.Type paramType = m_Block.m_Params[i].m_Type;
				Rect rect = new Rect(r.x + 8, currentY, r.width - 10, 0);

				rect.height = GetParamHeight(m_Block.m_Params[i]) - 2;
				currentY += rect.height;

				switch (paramType)
				{
					case VFXParam.Type.kTypeFloat:
						EditorGUI.FloatField(rect,m_Block.m_Params[i].m_Name,0.0f);
						break;
					
					case VFXParam.Type.kTypeFloat2:
						EditorGUI.Vector2Field(rect, m_Block.m_Params[i].m_Name, new Vector2());
						break;
					
					case VFXParam.Type.kTypeFloat3:
						EditorGUI.Vector3Field(rect, m_Block.m_Params[i].m_Name, new Vector3());
						break;

					case VFXParam.Type.kTypeFloat4:
						EditorGUI.Vector4Field(rect, m_Block.m_Params[i].m_Name, new Vector4());
						break;

					case VFXParam.Type.kTypeInt:
					case VFXParam.Type.kTypeUint:
						EditorGUI.IntField(rect, m_Block.m_Params[i].m_Name, 0);
						break;
					
					default: // TODO Texture
						GUI.Label(rect, VFXParam.GetNameFromType(paramType) + " " + m_Block.m_Params[i].m_Name, VFXEditor.styles.NodeBlockParameter);
						break;
				}

			}

			base.Render(parentRect, canvas);
		}

		private VFXBlock m_Block;
	}
}

