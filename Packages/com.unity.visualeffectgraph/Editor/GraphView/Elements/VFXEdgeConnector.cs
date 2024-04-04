using System.Collections.Generic;
using System.Linq;

using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

namespace UnityEditor.VFX.UI
{
    class VFXEdgeConnector : EdgeConnector<VFXDataEdge>
    {
        VFXDataAnchor m_Anchor;
        public VFXEdgeConnector(VFXDataAnchor anchor) : base(anchor)
        {
            m_Anchor = anchor;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            if (!m_Anchor.controller.isSubgraphActivation)
                base.RegisterCallbacksOnTarget();
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

            if (anchor != null)
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

        protected override void Abort()
        {
            base.Abort();
            if (m_Anchor.GetFirstAncestorOfType<VFXView>() is { } view)
            {
                view.StopEdgeDragInfo();
            }
        }

        static List<VisualElement> s_PickedList = new List<VisualElement>();
    }
}
