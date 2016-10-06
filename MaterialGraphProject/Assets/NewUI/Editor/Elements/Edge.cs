using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.RMGUI;

namespace RMGUI.GraphView
{
	[CustomDataView(typeof(EdgeData))]
	class Edge : GraphElement
	{
		const float k_EndPointRadius = 4.0f;
		const float k_InterceptWidth = 3.0f;

		public override void OnDataChanged()
		{
			base.OnDataChanged();
			this.Touch(ChangeType.Repaint);
		}

		protected static void GetTangents(Direction direction, Orientation orientation, Vector2 start, Vector2 end, out Vector3[] points, out Vector3[] tangents)
		{
			if (direction == Direction.Output)
			{
				Vector2 t = end;
				end = start;
				start = t;
			}

			bool invert = false;
			if (end.x < start.x)
			{
				Vector3 t = start;
				start = end;
				end = t;
				invert = true;
			}

			points = new Vector3[] {start, end};
			tangents = new Vector3[2];

			const float minTangent = 30;

			float weight = .5f;
			float weight2 = 1 - weight;
			float y = 0;

			float cleverness = Mathf.Clamp01(((start - end).magnitude - 10) / 50);

			if (orientation == Orientation.Horizontal)
			{
				tangents[0] = start + new Vector2((end.x - start.x) * weight + minTangent, y) * cleverness;
				tangents[1] = end + new Vector2((end.x - start.x) * -weight2 - minTangent, -y) * cleverness;
			}
			else
			{
				float inverse = (invert) ? 1.0f : -1.0f;
				tangents[0] = start + new Vector2(y, inverse * ((end.x - start.x) * weight + minTangent)) * cleverness;
				tangents[1] = end + new Vector2(-y, inverse * ((end.x - start.x) * -weight2 - minTangent)) * cleverness;
			}
		}

		public override bool Overlaps(Rect rect)
		{
			// bounding box check succeeded, do more fine grained check by checking intersection between the rectangles' diagonal
			// and the line segments
			var edgeData = GetData<EdgeData>();
			if (edgeData == null)
				return false;

			IConnectable leftData = edgeData.Left;
			IConnectable rightData = edgeData.Right ?? leftData;
			if (leftData == null || rightData == null)
				return false;

			Vector2 from = Vector2.zero;
			Vector2 to = Vector2.zero;
			GetFromToPoints(ref from, ref to);

			Orientation orientation = leftData.orientation;

			Vector3[] points, tangents;

			GetTangents(leftData.direction, orientation, from, to, out points, out tangents);
			Vector3[] allPoints = Handles.MakeBezierPoints(points[0], points[1], tangents[0], tangents[1], 20);

			for (int a = 0; a < allPoints.Length; a++)
			{
				if (a >= allPoints.Length - 1)
				{
					break;
				}

				Vector2 segmentA = new Vector2(allPoints[a].x, allPoints[a].y);
				Vector2 segmentB = new Vector2(allPoints[a + 1].x, allPoints[a + 1].y);

				if (RectUtils.IntersectsSegment(rect, segmentA, segmentB))
					return true;
			}

			return false;
		}

		public override bool ContainsPoint(Vector2 localPoint)
		{
			// bounding box check succeeded, do more fine grained check by measuring distance to bezier points
			var edgeData = GetData<EdgeData>();
			if (edgeData == null)
				return false;

			IConnectable leftData = edgeData.Left;
			IConnectable rightData = edgeData.Right ?? leftData;
			if (leftData == null || rightData == null)
				return false;

			Vector2 from = Vector2.zero;
			Vector2 to = Vector2.zero;
			GetFromToPoints(ref from, ref to);

			// exclude endpoints
			if (Vector2.Distance(from, localPoint) <= 2 * k_EndPointRadius ||
				Vector2.Distance(to, localPoint) <= 2 * k_EndPointRadius)
			{
				return false;
			}

			Orientation orientation = leftData.orientation;

			Vector3[] points, tangents;
			GetTangents(leftData.direction, orientation, from, to, out points, out tangents);
			Vector3[] allPoints = Handles.MakeBezierPoints(points[0], points[1], tangents[0], tangents[1], 20);

			float minDistance = Mathf.Infinity;
			foreach (Vector3 currentPoint in allPoints)
			{
				float distance = Vector3.Distance(currentPoint, localPoint);
				minDistance = Mathf.Min(minDistance, distance);
				if (minDistance < k_InterceptWidth)
				{
					return true;
				}
			}

			return false;
		}

		public override void DoRepaint(PaintContext args)
		{
			base.DoRepaint(args);
			DrawEdge(args);
		}

		protected void GetFromToPoints(ref Vector2 from, ref Vector2 to)
		{
			var edgeData = GetData<EdgeData>();
			if (edgeData == null)
				return;

			IConnectable leftData = edgeData.Left;
			IConnectable rightData = edgeData.Right ?? leftData;
			if (leftData == null)
				return;

			GraphElement leftAnchor = parent.allElements.OfType<GraphElement>().First(e => e.dataProvider as IConnectable == leftData);
			if (leftAnchor != null)
			{
				from = leftAnchor.GetGlobalCenter();
				from = globalTransform.inverse.MultiplyPoint3x4(from);
			}

			if (edgeData.candidate)
			{
				to = globalTransform.inverse.MultiplyPoint3x4(new Vector3(edgeData.candidatePosition.x, edgeData.candidatePosition.y));
			}
			else
			{
				GraphElement rightAnchor = parent.allElements.OfType<GraphElement>().First(e => e.dataProvider as IConnectable == rightData);
				if (rightAnchor != null)
				{
					to = rightAnchor.GetGlobalCenter();
					to = globalTransform.inverse.MultiplyPoint3x4(to);
				}
			}
		}

		protected virtual void DrawEdge(PaintContext args)
		{
			var edgeData = GetData<EdgeData>();
			if (edgeData == null)
				return;

			IConnectable leftData = edgeData.Left;
			if (leftData == null)
				return;

			Vector2 from = Vector2.zero;
			Vector2 to = Vector2.zero;
			GetFromToPoints(ref from, ref to);

			Color edgeColor = (GetData<GraphElementData>() != null && GetData<GraphElementData>().selected) ? Color.yellow : Color.white;

			Orientation orientation = leftData.orientation;

			Vector3[] points, tangents;
			GetTangents(leftData.direction, orientation, from, to, out points, out tangents);
			Handles.DrawBezier(points[0], points[1], tangents[0], tangents[1], edgeColor, null, 5f);

			// little widget on the middle of the edge
			Vector3[] allPoints = Handles.MakeBezierPoints(points[0], points[1], tangents[0], tangents[1], 20);
			Color oldColor = Handles.color;
			Handles.color = Color.blue;
			Handles.DrawSolidDisc(allPoints[10], new Vector3(0.0f, 0.0f, -1.0f), 6f);
			Handles.color = edgeColor;
			Handles.DrawWireDisc(allPoints[10], new Vector3(0.0f, 0.0f, -1.0f), 6f);
			Handles.DrawWireDisc(allPoints[10], new Vector3(0.0f, 0.0f, -1.0f), 5f);

			// dot on top of anchor showing it's connected
			Handles.color = new Color(0.3f, 0.4f, 1.0f, 1.0f);
			Handles.DrawSolidDisc(from, new Vector3(0.0f, 0.0f, -1.0f), k_EndPointRadius);
			if (edgeData.Right == null)
				Handles.color = oldColor;
			Handles.DrawSolidDisc(to, new Vector3(0.0f, 0.0f, -1.0f), k_EndPointRadius);
			Handles.color = oldColor;
		}
	}
}