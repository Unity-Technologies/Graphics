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

            element.DoubleClick += ManageDoubleClick;
            element.MouseDown += ManageMouseDown;
        }



        private bool ManageMouseDown(CanvasElement element, Event e, Canvas2D parent)
        {
            if (e.type == EventType.Used)
                return false;


            Rect ActiveArea = VFXEditorMetrics.NodeBlockCollapserArrowRect;
            ActiveArea.position += element.canvasBoundingRect.position;

            if (ActiveArea.Contains(parent.MouseToCanvas(e.mousePosition)))
            {
                (element.parent as VFXEdNodeBlock).collapsed = !(element.parent as VFXEdNodeBlock).collapsed;
                parent.Layout();
                element.parent.Invalidate();
                e.Use();
                return true;
            }
            return false;

        }

        private bool ManageDoubleClick(CanvasElement element, Event e, Canvas2D parent)
        {
            if (e.type == EventType.Used)
                return false;

            (element.parent as VFXEdNodeBlock).collapsed = !(element.parent as VFXEdNodeBlock).collapsed;
            parent.Layout();
            element.parent.Invalidate();
            e.Use();

            return true;
        }
    };
}
