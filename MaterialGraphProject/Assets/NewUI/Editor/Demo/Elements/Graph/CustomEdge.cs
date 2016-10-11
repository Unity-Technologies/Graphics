using UnityEditor;
using UnityEngine;
using UnityEngine.RMGUI;

namespace RMGUI.GraphView.Demo
{
	class CustomEdge : Edge
	{
		protected override void DrawEdge(PaintContext args)
		{
			var edgeData = GetData<CustomEdgeData>();
			if (edgeData == null)
			{
				return;
			}

			IConnectable leftData = edgeData.left;
			if (leftData == null)
				return;

			Vector2 from = Vector2.zero;
			Vector2 to = Vector2.zero;
			GetFromToPoints(ref from, ref to);
			if (edgeData.candidate)
			{
				Handles.DrawAAPolyLine(15.0f, from, to);
			}
			else
			{
				Vector3[] points, tangents;
				Orientation orientation = leftData.orientation;
				GetTangents(leftData.direction, orientation, from, to, out points, out tangents);

				Color edgeColor = (GetData<GraphElementData>() != null && GetData<GraphElementData>().selected) ? Color.yellow : Color.white;
				Handles.DrawBezier(points[0], points[1], tangents[0], tangents[1], edgeColor, null, 5f);
			}
		}
	}
}
