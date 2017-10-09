using UnityEditor.Experimental.UIElements.GraphView;
using UnityEngine.Experimental.UIElements.StyleSheets;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleEnums;
using System.Collections.Generic;
using Type = System.Type;
using System.Linq;

namespace UnityEditor.VFX.UI
{
    class VFXBlockDataAnchor : VFXEditableDataAnchor
    {
        protected VFXBlockDataAnchor(Orientation anchorOrientation, Direction anchorDirection, Type type) : base(anchorOrientation, anchorDirection, type)
        {
        }

        public static new VFXBlockDataAnchor Create(VFXDataAnchorPresenter presenter)
        {
            var anchor = new VFXBlockDataAnchor(presenter.orientation, presenter.direction, presenter.anchorType);
            anchor.m_EdgeConnector = new EdgeConnector<VFXDataEdge>(anchor);
            anchor.presenter = presenter;

            anchor.AddManipulator(anchor.m_EdgeConnector);
            return anchor;
        }

        /*
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
                    Vector2 globaCenter = layout.position + (Vector2)GetLocalCenter();

                    Vector2 from = globaCenter - new Vector2(10, 0);
                    Vector2 to = globaCenter + new Vector2(5, 0);

                    GraphView view = this.GetFirstAncestorOfType<GraphView>();

                    float realWidth = edge.edgeWidth;
                    if (realWidth * view.scale < 1.0f)
                    {
                        realWidth = 1.0f / view.scale;
                    }

                    Color edgeColor = edge.style.borderColor;
                    VFXEdgeUtils.RenderLine(from, to, edgeColor, realWidth, view.scale);
                }
            }
        }*/
    }
}
