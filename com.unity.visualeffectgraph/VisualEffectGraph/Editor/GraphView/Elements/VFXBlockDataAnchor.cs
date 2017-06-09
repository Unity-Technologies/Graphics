using UIElements.GraphView;
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using System.Collections.Generic;
using Type = System.Type;
using System.Linq;

namespace UnityEditor.VFX.UI
{
    class VFXBlockDataAnchor : VFXEditableDataAnchor
    {
        protected VFXBlockDataAnchor(VFXDataAnchorPresenter presenter) : base(presenter)
        {
        }

        public static new VFXBlockDataAnchor Create<TEdgePresenter>(VFXDataAnchorPresenter presenter) where TEdgePresenter : VFXDataEdgePresenter
        {
            var anchor = new VFXBlockDataAnchor(presenter);
            anchor.m_EdgeConnector = new EdgeConnector<TEdgePresenter>(anchor);
            anchor.presenter = presenter;

            anchor.AddManipulator(anchor.m_EdgeConnector);
            return anchor;
        }

        internal override void DoRepaint(IStylePainter pc)
        {
            base.DoRepaint(pc);

            VFXContextDataAnchorPresenter presenter = GetPresenter<VFXContextDataAnchorPresenter>();

            if (presenter.connected)
            {
                var edgePresenter = presenter.connections.First() as VFXDataEdgePresenter;

                VFXDataEdge edge = this.GetFirstAncestorOfType<VFXView>().GetDataEdgeByPresenter(edgePresenter);

                if (edge != null)
                {
                    Vector2 globaCenter = position.position + (Vector2)GetLocalCenter();

                    Vector2 from = globaCenter - new Vector2(10, 0);
                    Vector2 to = globaCenter + new Vector2(5, 0);

                    GraphView view = this.GetFirstAncestorOfType<GraphView>();

                    float realWidth = edge.presenter.selected ? edge.borderWidth * 2 : edge.borderWidth;
                    if (realWidth * view.scale < 1.5f)
                    {
                        realWidth = 1.5f / view.scale;
                    }

                    VFXFlowEdge.RenderLine(from, to, edge.borderColor, realWidth);
                }
            }
        }
    }
}
