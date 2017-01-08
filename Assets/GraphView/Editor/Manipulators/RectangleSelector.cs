using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.RMGUI;
using UnityEngine.RMGUI.StyleEnums;

namespace RMGUI.GraphView
{
	public class RectangleSelector : MouseManipulator
	{
		private RectangleSelect m_Rectangle;

		public RectangleSelector()
		{
			activators.Add(new ManipActivator {button = MouseButton.LeftMouse});
			m_Rectangle = new RectangleSelect
			{
				positionType = PositionType.Absolute,
				positionTop = 0,
				positionLeft = 0,
				positionBottom = 0,
				positionRight = 0
			};
		}

		// get the AA aligned bound
		public Rect ComputeAAAlignedBound(Rect position, Matrix4x4 transform)
		{
			Vector3 min = transform.MultiplyPoint3x4(position.min);
			Vector3 max = transform.MultiplyPoint3x4(position.max);
			return Rect.MinMaxRect(Math.Min(min.x, max.x), Math.Min(min.y, max.y), Math.Max(min.x, max.x), Math.Max(min.y, max.y));
		}

		public override EventPropagation HandleEvent(Event evt, VisualElement finalTarget)
		{
			if (finalTarget != target && !this.HasCapture())
				return EventPropagation.Continue;

			var graphView = target as GraphView;
			if (graphView == null)
			{
				throw new InvalidOperationException("Manipulator can only be added to a GraphView");
			}

			switch (evt.type)
			{
				case EventType.MouseDown:
					if (CanStartManipulation(evt))
					{
						if (!evt.control)
						{
							graphView.ClearSelection();
						}

						this.TakeCapture();

						var c = (VisualContainer)target;
						c.AddChild(m_Rectangle);

 						m_Rectangle.m_Start = evt.mousePosition;
						m_Rectangle.m_End = m_Rectangle.m_Start;
						return EventPropagation.Stop;
					}
					break;

				case EventType.MouseUp:
					if (CanStopManipulation(evt))
					{
						this.ReleaseCapture();

						var c = (VisualContainer)target;
						c.RemoveChild(m_Rectangle);

						m_Rectangle.m_End = evt.mousePosition;

						var selectionRect = new Rect()
						{
							min = new Vector2(Math.Min(m_Rectangle.m_Start.x, m_Rectangle.m_End.x), Math.Min(m_Rectangle.m_Start.y, m_Rectangle.m_End.y)),
							max = new Vector2(Math.Max(m_Rectangle.m_Start.x, m_Rectangle.m_End.x), Math.Max(m_Rectangle.m_Start.y, m_Rectangle.m_End.y))
						};

						selectionRect = ComputeAAAlignedBound(selectionRect, graphView.contentViewContainer.transform.inverse);

						List<ISelectable> selection = graphView.selection;

						List<VisualElement>.Enumerator children = graphView.contentViewContainer.GetChildren();
						while (children.MoveNext())
						{
							VisualElement child = children.Current;
							if (child == null)
								continue;

							var selectable = child as ISelectable;

							Matrix4x4 selectableTransform = child.transform.inverse;
							var localSelRect = new Rect(selectableTransform.MultiplyPoint3x4(selectionRect.position), selectableTransform.MultiplyPoint3x4(selectionRect.size));
							if (selectable != null && selectable.IsSelectable() && selectable.Overlaps(localSelRect))
							{
								if (selection.Contains(selectable))
									graphView.RemoveFromSelection(selectable);
								else
									graphView.AddToSelection(selectable);
							}
						}
						return EventPropagation.Stop;
					}
					break;

				case EventType.MouseDrag:
					if (this.HasCapture())
					{
						m_Rectangle.m_End = evt.mousePosition;
						return EventPropagation.Stop;
					}
					break;
			}
			return EventPropagation.Continue;
		}

		public override void OnLostCapture()
		{
			var c = target as VisualContainer;
			if (c != null)
				c.RemoveChild(m_Rectangle);
		}

		class RectangleSelect : VisualElement
		{
			public Vector2 m_Start = Vector2.zero;
			public Vector2 m_End = Vector2.zero;

			public override void DoRepaint(IStylePainter painter)
			{
				VisualElement t = parent;
				Vector2 screenStart = m_Start;
				Vector2 screenEnd = m_End;

				// Avoid drawing useless information
				if (m_Start == m_End)
					return;

				// Apply offset
				screenStart += t.position.position;
				screenEnd += t.position.position;

				var r = new Rect
				{
					min = new Vector2(Math.Min(screenStart.x, screenEnd.x), Math.Min(screenStart.y, screenEnd.y)),
					max = new Vector2(Math.Max(screenStart.x, screenEnd.x), Math.Max(screenStart.y, screenEnd.y))
				};

				var lineColor = new Color(1.0f, 0.6f, 0.0f, 1.0f);
				var segmentSize = 5f;

				Vector3[] points =
				{
					new Vector3(r.xMin, r.yMin, 0.0f),
					new Vector3(r.xMax, r.yMin, 0.0f),
					new Vector3(r.xMax, r.yMax, 0.0f),
					new Vector3(r.xMin, r.yMax, 0.0f)
				};

				DrawDottedLine(points[0], points[1], segmentSize, lineColor);
				DrawDottedLine(points[1], points[2], segmentSize, lineColor);
				DrawDottedLine(points[2], points[3], segmentSize, lineColor);
				DrawDottedLine(points[3], points[0], segmentSize, lineColor);

				var str = "(" + String.Format("{0:0}", m_Start.x) + ", " + String.Format("{0:0}", m_Start.y) + ")";
				GUI.skin.label.Draw(new Rect(screenStart.x, screenStart.y - 18.0f, 200.0f, 20.0f), new GUIContent(str), 0);
				str = "(" + String.Format("{0:0}", m_End.x) + ", " + String.Format("{0:0}", m_End.y) + ")";
				GUI.skin.label.Draw(new Rect(screenEnd.x - 80.0f, screenEnd.y + 5.0f, 200.0f, 20.0f), new GUIContent(str), 0);
			}

			private void DrawDottedLine(Vector3 p1, Vector3 p2, float segmentsLength, Color col)
			{
				UIHelpers.ApplyWireMaterial();

				GL.Begin(GL.LINES);
				GL.Color(col);

				float length = Vector3.Distance(p1, p2); // ignore z component
				int count = Mathf.CeilToInt(length / segmentsLength);
				for (int i = 0; i < count; i += 2)
				{
					GL.Vertex((Vector3.Lerp(p1, p2, i * segmentsLength / length)));
					GL.Vertex((Vector3.Lerp(p1, p2, (i + 1) * segmentsLength / length)));
				}

				GL.End();
			}
		}
	}
}
