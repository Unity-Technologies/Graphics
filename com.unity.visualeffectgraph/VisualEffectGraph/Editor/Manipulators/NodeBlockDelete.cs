using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using Object = UnityEngine.Object;


namespace UnityEditor.Experimental
{
    internal class NodeBlockDelete : IManipulate
    {
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

            if (e.keyCode != KeyCode.Delete)
            {
                return false;
            }

            if (!(element is VFXEdNodeBlock) || !((canvas as VFXEdCanvas).SelectedNodeBlock == element))
            {
                return false;
            }

            // Delete Edges
            /*VFXEdNodeBlockDraggable node = element as VFXEdNodeBlockDraggable;
            VFXEdNodeBlockContainer container = (node.parent as VFXEdNodeBlockContainer);
            container.RemoveNodeBlock(node);*/

            if (element is VFXModelHolder)
            {
                ((VFXEdDataSource)canvas.dataSource).Remove(((VFXModelHolder)element).GetAbstractModel());
                canvas.ReloadData();
                canvas.Repaint();
                return true;
            }

            // TODO : Delete DataEdges when implemented.

            // Finally 
            canvas.ReloadData();
            canvas.Layout();
            canvas.Repaint();
            return true;
        }
    };
}
