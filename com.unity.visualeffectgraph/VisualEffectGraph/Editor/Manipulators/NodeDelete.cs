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

            if (element is VFXModelHolder)
                ((VFXEdDataSource)canvas.dataSource).Remove(((VFXModelHolder)element).GetAbstractModel());

            VFXEdNodeBase node = element as VFXEdNodeBase;
            node.OnRemove();
            
            canvas.ReloadData();
            canvas.Repaint();

            return true;
        }

    };
}
