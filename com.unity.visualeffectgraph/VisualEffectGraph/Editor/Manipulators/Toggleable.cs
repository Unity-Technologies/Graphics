using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor.Experimental;
using UnityEditor.Experimental.Graph;
using Object = UnityEngine.Object;

namespace UnityEditor.Experimental
{
    internal class Toggleable : IManipulate
    {
        private bool m_DoubleClick;
        private Rect m_Area;
        VFXBlockModel m_BlockModel;

        public Toggleable(Rect area, VFXBlockModel model)
        {
            m_Area = area;
            m_BlockModel = model;
        }

        public bool GetCaps(ManipulatorCapability cap)
        {
            return false;
        }

        public void AttachTo(CanvasElement element)
        {
            element.MouseDown += ManageMouseDown;
        }

        private bool ManageMouseDown(CanvasElement element, Event e, Canvas2D parent)
        {
            if (e.type == EventType.Used)
                return false;

            Rect ActiveArea = m_Area;
            ActiveArea.position += element.canvasBoundingRect.position;

            if (ActiveArea.Contains(parent.MouseToCanvas(e.mousePosition)))
            {
                DoToggle(element,e,parent);
                element.parent.Invalidate();
                e.Use();
                return true;
            }
            return false;
        }

        protected void DoToggle(CanvasElement element, Event e, Canvas2D parent)
        { 
            m_BlockModel.Enabled = !m_BlockModel.Enabled;
            (parent.dataSource as VFXEdDataSource).SyncView(m_BlockModel);
        }
    }
}
