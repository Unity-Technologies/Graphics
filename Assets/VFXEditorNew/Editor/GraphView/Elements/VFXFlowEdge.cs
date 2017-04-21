using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UIElements.GraphView;
using UnityEngine.Experimental.UIElements;

namespace UnityEditor.VFX.UI
{
    public class VFXFlowEdge : Edge
    {
        public override int layer
        {
            get
            {
                return 1;
            }
        }


        public VFXFlowEdge()
        {

        }

        // Only flow anchors are of interest to flow edges
        public override IEnumerable<NodeAnchor> GetAllAnchors(bool input, bool output)
        {
            foreach( var anchor in this.GetFirstOfType<VFXView>().GetAllFlowAnchors(input, output))
                yield return anchor;
        }
            /*
            protected override void DrawEdge(IStylePainter painter)
            {
                var edgePresenter = GetPresenter<EdgePresenter>();

                NodeAnchorPresenter outputPresenter = edgePresenter.output;
                NodeAnchorPresenter inputPresenter = edgePresenter.input;

                if (outputPresenter == null && inputPresenter == null)
                    return;

                Vector2 from = Vector2.zero;
                Vector2 to = Vector2.zero;
                GetFromToPoints(ref from, ref to);

                Color edgeColor = edgePresenter.selected ? new Color(240/255f,240/255f,240/255f) : new Color(146/255f,146/255f,146/255f);

                Orientation orientation = Orientation.Vertical;
                Vector3[] points, tangents;
                GetTangents(orientation, from, to, out points, out tangents);
                Handles.DrawBezier(points[0], points[1], tangents[0], tangents[1], edgeColor, null, 15f);

            }
            */
        }
}
