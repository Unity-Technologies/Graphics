using UnityEditor;
using UnityEngine;

namespace RMGUI.GraphView.Demo
{
	class CustomEdge : Edge
	{
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
			if (edgePresenter.candidate)
			{
				Handles.DrawAAPolyLine(2.0f, from, to);
			}
			else
			{
				Vector3[] points, tangents;
				Orientation orientation = outputPresenter != null ? outputPresenter.orientation : inputPresenter.orientation;
				GetTangents(orientation, from, to, out points, out tangents);

				Color edgeColor = edgePresenter.selected ? new Color(240/255f,240/255f,240/255f) : new Color(146/255f,146/255f,146/255f);
				Handles.DrawBezier(points[0], points[1], tangents[0], tangents[1], edgeColor, null, 2f);
			}
		}
	}
}
