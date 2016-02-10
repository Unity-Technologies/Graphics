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
		
		public VFXEdNodeBlock(VFXBlock block, Vector2 position, float width, VFXEdDataSource dataSource)
		{
			m_Block = block;
			m_Name = block.m_Name;
			translation = new Vector3(0.0f, 0.0f, 0.0f);
			m_Caps = Capabilities.Normal;

			AddChild(new VFXEdNodeBlockCollapser(width, dataSource, m_Name));

			m_NodeBlockManipulator = new NodeBlockManipulator();
			AddManipulator(m_NodeBlockManipulator);
			AddManipulator(new NodeBlockDelete());

		}

		// Retrieve the height of a given param
		private float GetParamHeight(VFXParam param)
		{
			float height = VFXEditorMetrics.NodeBlockParameterHeight;
			switch (param.m_Type)
			{
				case VFXParam.Type.kTypeFloat2:
				case VFXParam.Type.kTypeFloat3:
				case VFXParam.Type.kTypeFloat4:
					height += VFXEditorMetrics.NodeBlockAdditionalHeight;
					break;

				default: break;
			}
			return height;
		}

		// Retrieve the total height of a block
		private float GetHeight()
		{
			float height = VFXEditorMetrics.NodeBlockHeaderHeight;
			for (int i = 0; i < m_Block.m_Params.Length; ++i)
				height += GetParamHeight(m_Block.m_Params[i]);
			return height;
		}
	
		public override void Layout()
		{
			if(collapsed)
			{
				scale = new Vector2(scale.x, VFXEditorMetrics.NodeBlockHeaderHeight);
			}
			else
			{
				scale = new Vector2(scale.x, GetHeight());
			}

			base.Layout();

		}

		public bool IsSelectedNodeBlock(VFXEdCanvas canvas)
		{
			if (parent is VFXEdNodeBlockContainer)
			{
				return canvas.SelectedNodeBlock == this;
			}
			else
			{
				return false;
			}
		}


		public override void Render(Rect parentRect, Canvas2D canvas)
		{
			Rect r = GetDrawableRect();

			if(parent is VFXEdNodeBlockContainer)
			{
				if (IsSelectedNodeBlock(canvas as VFXEdCanvas))
					GUI.Box(r, "", VFXEditor.styles.NodeBlockSelected);
				else
					GUI.Box(r, "", VFXEditor.styles.NodeBlock);
			}
			else // If currently dragged...
			{
				Color c = GUI.color;
				GUI.color = new Color(GUI.color.r,GUI.color.g,GUI.color.a,0.75f);
				GUI.Box(r, "", VFXEditor.styles.NodeBlockSelected);
				GUI.color = c;
			}

			//GUI.Label(new Rect(r.x + 16, r.y, r.width, 24), m_Block.m_Name, VFXEditor.styles.NodeBlockTitle);

			if (!collapsed)
			{
			float currentY = r.y + VFXEditorMetrics.NodeBlockHeaderHeight;
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
			}

			base.Render(parentRect, canvas);
		}

		private VFXBlock m_Block;
	}
}

