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

        public NodeBlockDelete()
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

            if (e.keyCode != KeyCode.Delete)
            {
                return false;
            }

            if (!(element is VFXEdNodeBlock) || !((canvas as VFXEdCanvas).SelectedNodeBlock == element))
            {
                return false;
            }

            // Prepare undo
            (canvas.dataSource as VFXEdDataSource).UndoSnapshot("Deleting NodeBlock " + (element as VFXEdNodeBlock).name);

            // Delete Edges
            VFXEdNodeBlock node = element as VFXEdNodeBlock;
            VFXEdNodeBlockContainer container = (node.parent as VFXEdNodeBlockContainer);
            container.RemoveNodeBlock(node);

            // TODO : Delete DataEdges when implemented.

            // Finally 
            canvas.ReloadData();
            canvas.Layout();
            canvas.Repaint();
            return true;
        }

    };
}
