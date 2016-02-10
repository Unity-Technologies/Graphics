using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental
{
	internal class VFXEdNodeBlockCollapser : CanvasElement
	{

		private NodeBlockCollapse m_NodeBlockCollapseManipulator;

		public VFXEdNodeBlockCollapser(Vector2 position, VFXEdDataSource dataSource)
		{
			translation = new Vector3(position.x, position.y, 0.0f);
			scale = new Vector2(16.0f, 16.0f);

			m_NodeBlockCollapseManipulator = new NodeBlockCollapse();
			AddManipulator(m_NodeBlockCollapseManipulator);
		}

		public override void Layout()
		{
			base.Layout();
		}


		public override void Render(Rect parentRect, Canvas2D canvas)
		{
			Rect r = GetDrawableRect();
			GUI.Box(r, "", VFXEditor.styles.Foldout);
			base.Render(parentRect, canvas);
		}

		private VFXBlock m_Block;
	}
}

