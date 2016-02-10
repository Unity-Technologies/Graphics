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
			Rect r = element.boundingRect;

			(element.parent as VFXEdNodeBlock).collapsed = !(element.parent as VFXEdNodeBlock).collapsed;
			Debug.Log((element.parent as VFXEdNodeBlock).collapsed);
			e.Use();
			parent.Layout();
			element.parent.Invalidate();
			return true;
			
		}
	};
}
