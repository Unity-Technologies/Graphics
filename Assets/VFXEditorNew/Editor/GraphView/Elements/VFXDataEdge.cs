using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UIElements.GraphView;
using UnityEngine.Experimental.UIElements;
using UnityEngine.Experimental.UIElements.StyleSheets;

namespace UnityEditor.VFX.UI
{
    public class VFXDataEdge : Edge
    {
        public VFXDataEdge()
        {
        }

        public override int layer
        {
            get
            {
                return -1;
            }
        }


        public override void OnDataChanged()
        {
            base.OnDataChanged();

            foreach (var cls in VFXTypeDefinition.GetTypeCSSClasses())
                RemoveFromClassList(cls);


            var edgePresenter = GetPresenter<EdgePresenter>();

            NodeAnchorPresenter outputPresenter = edgePresenter.output;
            NodeAnchorPresenter inputPresenter = edgePresenter.input;


            if (outputPresenter == null && inputPresenter == null)
                return;

            System.Type type = inputPresenter != null ? inputPresenter.anchorType : outputPresenter.anchorType;

            AddToClassList(VFXTypeDefinition.GetTypeCSSClass(type));
        }

        public override IEnumerable<NodeAnchor> GetAllAnchors(bool input, bool output)
        {
            foreach (var anchor in this.GetFirstOfType<VFXView>().GetAllDataAnchors(input, output))
                yield return anchor;
        }

        internal override void DrawEdge(IStylePainter painter)
        {
            var edgePresenter = GetPresenter<EdgePresenter>();

            NodeAnchorPresenter outputPresenter = edgePresenter.output;
            NodeAnchorPresenter inputPresenter = edgePresenter.input;

            if (outputPresenter == null && inputPresenter == null)
                return;

            Vector2 from = Vector2.zero;
            Vector2 to = Vector2.zero;
            GetFromToPoints(ref from, ref to);

            Color edgeColor = borderColor;

            Orientation orientation = Orientation.Horizontal;
            Vector3[] points, tangents;
            GetTangents(orientation, from, to, out points, out tangents);
            if (edgePresenter.selected)
            {
                Handles.DrawBezier(points[0] + Vector3.down, points[1] + Vector3.down , tangents[0] + Vector3.down , tangents[1] + Vector3.down , edgeColor, null, 2f);
                Handles.DrawBezier(points[0] + Vector3.up , points[1] + Vector3.up , tangents[0] + Vector3.up , tangents[1] + Vector3.up , edgeColor, null, 2f);
            }
            Handles.DrawBezier(points[0], points[1], tangents[0], tangents[1], edgeColor, null, 2f);
        }
    }
}
