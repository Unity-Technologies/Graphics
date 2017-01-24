using System;
using UnityEngine;
using UnityEngine.RMGUI;

namespace RMGUI.GraphView
{
	public class SelectionDragger : Dragger
	{
		// selectedElement is used to store a unique selection candidate for cases where user clicks on an item not to
		// drag it but just to reset the selection -- we only know this after the manipulation has ended
		GraphElement selectedElement { get; set; }

		public SelectionDragger()
		{
			activators.Add(new ManipActivator {button = MouseButton.LeftMouse});
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
					selectedElement = null;

					if (CanStartManipulation(evt))
					{
						// avoid starting a manipulation on a non movable object
						var ce = finalTarget as GraphElement;
						if (ce == null)
						{
							ce = finalTarget.GetFirstAncestorOfType<GraphElement>();
							if (ce == null)
								return EventPropagation.Continue;
						}

						selectedElement = ce;

						GraphElementPresenter elementPresenter = ce.presenter;
						if (elementPresenter != null && ((ce.presenter.capabilities & Capabilities.Movable) != Capabilities.Movable))
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
							if (ce == null || ce.presenter == null)
								continue;

							if ((ce.presenter.capabilities & Capabilities.Movable) != Capabilities.Movable)
								continue;

							Matrix4x4 g = ce.globalTransform;
							var scale = new Vector3(g.m00, g.m11, g.m22);

							ce.SetPosition(CalculatePosition(ce.position.x + evt.delta.x * panSpeed.x / scale.x,
															 ce.position.y + evt.delta.y*panSpeed.y/scale.y,
															 ce.position.width, ce.position.height));
							ce.Touch(ChangeType.Layout);
						}

						selectedElement = null;

						return EventPropagation.Stop;
					}
					break;

				case EventType.MouseUp:
					if (CanStopManipulation(evt))
					{
						if (selectedElement != null && !evt.control)
						{
							// Since we didn't drag after all, update selection with current element only
							graphView.ClearSelection();
							graphView.AddToSelection(selectedElement);
						}
						else
						{
							foreach (ISelectable s in graphView.selection)
							{
								GraphElement ce = s as GraphElement;
								if (ce == null || ce.presenter == null)
									continue;

								GraphElementPresenter elementPresenter = ce.presenter;
								if ((ce.presenter.capabilities & Capabilities.Movable) != Capabilities.Movable)
									continue;

								elementPresenter.position = ce.position;
								elementPresenter.CommitChanges();

							}
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
