using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using Object = UnityEngine.Object;


namespace UnityEditor.Experimental
{
	internal class NodeDelete : IManipulate
	{

		public NodeDelete()
		{
		}

		public bool GetCaps(ManipulatorCapability cap)
		{
			if (cap == ManipulatorCapability.MultiSelection)
				return true;
			return false;
		}

		public void AttachTo(CanvasElement element)
		{
			element.KeyDown += DeleteNode;
		}

		private bool DeleteNode(CanvasElement element, Event e, Canvas2D canvas)
		{
			if (e.type == EventType.Used)
				return false;

			if (e.keyCode != KeyCode.Delete && e.modifiers != EventModifiers.None)
			{
				return false;
			}

			if (!(element is VFXEdNode) || !element.selected)
			{
				return false;
			}

			// Prepare undo
			(canvas.dataSource as VFXEdDataSource).UndoSnapshot("Deleting Node" + (element as VFXEdNode).Title);

			// Delete Edges
			VFXEdNode node = element as VFXEdNode;
			List<CanvasElement> todelete = new List<CanvasElement>();

			foreach (CanvasElement ce in canvas.dataSource.FetchElements())
			{
				if(ce is Edge<VFXEdFlowAnchor>)
				{
					 if(node.Inputs.Contains((ce as Edge<VFXEdFlowAnchor>).Left) || node.Inputs.Contains((ce as Edge<VFXEdFlowAnchor>).Right))
					{
						todelete.Add(ce);
					}
					if (node.Outputs.Contains((ce as Edge<VFXEdFlowAnchor>).Left) || node.Outputs.Contains((ce as Edge<VFXEdFlowAnchor>).Right))
					{
						todelete.Add(ce);
					}
				}
			}
			foreach(CanvasElement ce in todelete) canvas.dataSource.DeleteElement(ce);

			// Finally 
			canvas.dataSource.DeleteElement(element);
			canvas.ReloadData();
			canvas.Repaint();

			return true;
		}
		
	};
}
