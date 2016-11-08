using UnityEditor;
using UnityEngine;
using UnityEngine.RMGUI;

namespace RMGUI.GraphView.Demo
{
	class CustomEdge : Edge
	{
		protected override void DrawEdge(IStylePainter painter)
		{
			var edgeData = GetData<EdgeData>();
			if (edgeData == null)
			{
				return;
			}

			IConnector outputData = edgeData.output;
			IConnector inputData = edgeData.input;

			if (outputData == null && inputData == null)
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
				Orientation orientation = outputData != null ? outputData.orientation : inputData.orientation;
				GetTangents(orientation, from, to, out points, out tangents);

				Color edgeColor = (GetData<EdgeData>() != null && GetData<EdgeData>().selected) ? Color.yellow : Color.white;
				Handles.DrawBezier(points[0], points[1], tangents[0], tangents[1], edgeColor, null, 5f);
			}
		}
	}
}
