using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using Object = UnityEngine.Object;


namespace UnityEditor.Experimental
{
	internal class NodeBlockCollapse : IManipulate
	{

		public NodeBlockCollapse()
		{

		}

		public bool GetCaps(ManipulatorCapability cap)
		{
			return false;
		}

		public void AttachTo(CanvasElement element)
		{
			element.MouseDown += ToggleCollapse;
		}

		private bool ToggleCollapse(CanvasElement element, Event e, Canvas2D parent)
		{
			if((element.parent as VFXEdNodeBlock).IsSelectedNodeBlock(parent as VFXEdCanvas))
			{
				(element.parent as VFXEdNodeBlock).collapsed = !(element.parent as VFXEdNodeBlock).collapsed;
				e.Use();
				parent.Layout();
				element.parent.Invalidate();
				return true;
			}
			else return false;
			

		}
	};
}
