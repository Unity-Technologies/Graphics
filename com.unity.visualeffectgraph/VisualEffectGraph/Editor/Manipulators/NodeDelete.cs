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

            if (!(element is VFXEdNodeBase) || !element.selected)
            {
                return false;
            }  

            // TMP
            if (element is VFXEdContextNode)
            {
                ((VFXEdDataSource)canvas.dataSource).Remove(((VFXEdContextNode)element).Model);
                canvas.ReloadData();
                canvas.Repaint();
                return true;
            }

            // Delete Edges
            VFXEdNodeBase node = element as VFXEdNodeBase;
            List<CanvasElement> todelete = new List<CanvasElement>();

            foreach (CanvasElement ce in canvas.dataSource.FetchElements())
            {
                if (ce is Edge<VFXEdFlowAnchor>)
                {
                    if (node.inputs.Contains((ce as Edge<VFXEdFlowAnchor>).Left) || node.inputs.Contains((ce as Edge<VFXEdFlowAnchor>).Right))
                    {
                        todelete.Add(ce);
                    }
                    if (node.outputs.Contains((ce as Edge<VFXEdFlowAnchor>).Left) || node.outputs.Contains((ce as Edge<VFXEdFlowAnchor>).Right))
                    {
                        todelete.Add(ce);
                    }
                }
            }
            foreach (CanvasElement ce in todelete)
                canvas.dataSource.DeleteElement(ce);


            // Remove the NodeBlocks and Handle properly
            node.OnRemove();

            // Finally
            canvas.dataSource.DeleteElement(element);
            canvas.ReloadData();
            canvas.Repaint();

            return true;
        }

    };
}
