using UnityEngine;
using RMGUI.GraphView;
using UnityEngine.RMGUI.StyleSheets;

namespace UnityEditor.VFX.UI
{
    public class VFXDataEdge : Edge
    {
        public VFXDataEdge()
        {
        }

        public override void OnDataChanged()
        {
            base.OnDataChanged();


            RemoveFromClassList(VFXTypeDefinition.GetTypeCSSClasses());


            var edgePresenter = GetPresenter<EdgePresenter>();

            NodeAnchorPresenter outputPresenter = edgePresenter.output;
            NodeAnchorPresenter inputPresenter = edgePresenter.input;


            if (outputPresenter == null && inputPresenter == null)
                return;

            System.Type type = inputPresenter != null ? inputPresenter.anchorType : outputPresenter.anchorType;

            AddToClassList(VFXTypeDefinition.GetTypeCSSClass(type));
        }

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

            //Color edgeColor = edgePresenter.selected ? new Color(240/255f,240/255f,240/255f) : new Color(146/255f,146/255f,146/255f);

            System.Type type = inputPresenter != null ? inputPresenter.anchorType : outputPresenter.anchorType;


            Color edgeColor = borderColor;
            /*
            if( edgePresenter.selected )
            {
                edgeColor.r += 0.3f;
                edgeColor.g += 0.3f;
                edgeColor.b += 0.3f;
            }*/

            Orientation orientation = Orientation.Horizontal;
			Vector3[] points, tangents;
			GetTangents(orientation, from, to, out points, out tangents);
			Handles.DrawBezier(points[0], points[1], tangents[0], tangents[1], edgeColor, null, 2f);

		}

    }
}
