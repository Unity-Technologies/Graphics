using System;
using System.Linq;
using UnityEngine;
using UnityEngine.RMGUI;

namespace RMGUI.GraphView
{
	public class SelectionDragger : Dragger
	{
		public SelectionDragger()
		{
			activateButton = MouseButton.LeftMouse;
			panSpeed = new Vector2(1, 1);
			clampToParentEdges = false;
		}

		public override EventPropagation HandleEvent(Event evt, VisualElement finalTarget)
		{
			if (finalTarget == target && !this.HasCapture())
				return EventPropagation.Continue;

			var graphView = target as GraphView;
			if (graphView == null)
			{
				throw new InvalidOperationException("Manipulator can only be added to a GraphView");
			}

			switch (evt.type)
			{
				case EventType.MouseDown:
					if (evt.button == (int)activateButton)
					{
						// avoid starting a manipulation on a non movable object
						var ce = finalTarget as GraphElement;
						if (ce == null)
						{
							ce = finalTarget.GetFirstAncestorOfType<GraphElement>();
							if (ce == null)
								return EventPropagation.Continue;
						}

						var data = ce.dataProvider;
						if (data != null && ((ce.dataProvider.capabilities & Capabilities.Movable) != Capabilities.Movable))
							return EventPropagation.Continue;

						this.TakeCapture();
						return EventPropagation.Stop;
					}
					break;

				case EventType.MouseDrag:
					if (this.HasCapture())
					{
						foreach (ISelectable s in graphView.selection)
						{
							GraphElement ce = s as GraphElement;
							if (ce == null || ce.dataProvider == null)
								continue;

							var data = ce.dataProvider;
							if ((ce.dataProvider.capabilities & Capabilities.Movable) != Capabilities.Movable)
								continue;

							var g = ce.globalTransform;
							var scale = new Vector3(g.m00, g.m11, g.m22);

							if (data != null)
							{
								data.position = CalculatePosition(data.position.x + evt.delta.x * panSpeed.x / scale.x,
																	data.position.y + evt.delta.y * panSpeed.y / scale.y,
																	data.position.width, data.position.height);
							}
							else
							{
								throw new Exception("Graph Element should have valid data");
							}
						}

						return EventPropagation.Stop;
					}
					break;

				case EventType.MouseUp:
					if (this.HasCapture() && evt.button == (int)activateButton)
					{
					    foreach (var s in graphView.selection.OfType<GraphElement>())
					    {
					        if (s.dataProvider == null)
					            continue;

					        s.dataProvider.CommitChanges();
					    }
					    this.ReleaseCapture();
						return EventPropagation.Stop;
					}
					break;
			}
			return EventPropagation.Continue;
		}
	}
}
