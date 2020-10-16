using System.Collections.Generic;
using System.Linq;

using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Profiling;

using Type = System.Type;



using PositionType = UnityEngine.UIElements.Position;

namespace UnityEditor.VFX.UI
{
    class VFXEdgeConnector : EdgeConnector<VFXDataEdge>
    {
        VFXDataAnchor m_Anchor;
        public VFXEdgeConnector(VFXDataAnchor anchor):base(anchor)
        {
            m_Anchor = anchor;
        }


        protected override void OnMouseMove(MouseMoveEvent e)
        {
            base.OnMouseMove(e);

            if (!e.isPropagationStopped)
                return;
            
            VFXView view = m_Anchor.GetFirstAncestorOfType<VFXView>();
            if (view == null)
                return;

            s_PickedList.Clear();
            view.panel.PickAll(e.mousePosition, s_PickedList);

            VFXDataAnchor anchor = s_PickedList.OfType<VFXDataAnchor>().FirstOrDefault();

            if(anchor != null)
                view.StartEdgeDragInfo(this.edgeDragHelper.draggedPort as VFXDataAnchor, anchor);
            else
                view.StopEdgeDragInfo();
        }

        protected override void OnMouseUp(MouseUpEvent e)
        {
            base.OnMouseUp(e);

            if (!e.isPropagationStopped)
                return;

            VFXView view = m_Anchor.GetFirstAncestorOfType<VFXView>();
            if (view == null)
                return;
            view.StopEdgeDragInfo();

        }

        static List<VisualElement> s_PickedList = new List<VisualElement>();
    }
}
