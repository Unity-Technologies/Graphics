using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.RMGUI;

namespace RMGUI.GraphView
{
	public delegate EventPropagation DropEvent(Event evt, List<ISelectable> selection, IDropTarget dropTarget);

	internal class DragAndDropDelay
	{
		const float kStartDragTreshold = 4.0f;

		Vector2 mouseDownPosition { get; set; }

		public void Init(Vector2 mousePosition)
		{
			mouseDownPosition = mousePosition;
		}

		public bool CanStartDrag(Vector2 mousePosition)
		{
			return Vector2.Distance(mouseDownPosition, mousePosition) > kStartDragTreshold;
		}
	}

	// Manipulates movable objects, can also initiate a Drag and Drop operation
	public class SelectionDropper : Manipulator
	{
		readonly DragAndDropDelay m_DragAndDropDelay;

		public event DropEvent OnDrop;

		public Vector2 panSpeed { get; set; }

		public MouseButton activateButton { get; set; }

		public bool clampToParentEdges { get; set; }

		// selectedElement is used to store a unique selection candidate for cases where user clicks on an item not to
		// drag it but just to reset the selection -- we only know this after the manipulation has ended
		GraphElement selectedElement { get; set; }

		public SelectionDropper(DropEvent handler)
		{
			OnDrop += handler;

			m_DragAndDropDelay = new DragAndDropDelay();

			activateButton = MouseButton.LeftMouse;
			panSpeed = new Vector2(1, 1);
		}

		public override EventPropagation HandleEvent(Event evt, VisualElement finalTarget)
		{
			var selectionContainer = target as ISelection;
			if (selectionContainer == null && target != null)
				selectionContainer = target.parent as ISelection;

			if (selectionContainer == null)
				return EventPropagation.Continue;

			// Keep a copy of the selection
			var selection = selectionContainer.selection.ToList();

			switch (evt.type)
			{
				case EventType.MouseDown:
					selectedElement = null;

					if (evt.button == (int)activateButton)
					{
						// avoid starting a manipulation on a non movable object
						var ce = finalTarget as GraphElement;
						if (ce == null)
						{
							ce = (finalTarget != null) ? finalTarget.GetFirstAncestorOfType<GraphElement>() : null;
							if (ce == null)
								return EventPropagation.Continue;
						}

						var presenter = ce.presenter;
						if (presenter != null && ((presenter.capabilities & Capabilities.Droppable) != Capabilities.Droppable))
							return EventPropagation.Continue;

						this.TakeCapture();

						// Reset drag and drop
						m_DragAndDropDelay.Init(evt.mousePosition);

						selectedElement = ce;

						return EventPropagation.Stop;
					}
					break;

				case EventType.MouseDrag:
					{
						if (this.HasCapture() && selection.Count > 0)
						{
							bool canStartDrag = false;
							var ce = selection[0] as GraphElement;
							if (ce != null)
							{
								var presenter = ce.presenter;
								if (presenter != null)
									canStartDrag = (presenter.capabilities & Capabilities.Droppable) == Capabilities.Droppable;
							}

							if (canStartDrag && m_DragAndDropDelay.CanStartDrag(evt.mousePosition))
							{
								DragAndDrop.PrepareStartDrag();
								DragAndDrop.objectReferences = new UnityEngine.Object[] { }; // this IS required for dragging to work
								DragAndDrop.SetGenericData("DragSelection", selection);
								string dragName = "<Single>";
								if (selection.Count > 1)
								{
									dragName = "<Multiple>";
								}
								else
								{
									GraphElement e = selection[0] as GraphElement;
									if (e != null)
										dragName = e.name;
									// Debug
									dragName = "<Debug>";
								}

								DragAndDrop.StartDrag(dragName);
								DragAndDrop.visualMode = evt.control ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Move;
							}

							return EventPropagation.Stop;
						}
						break;
					}

				case EventType.MouseUp:
					{
						if (this.HasCapture() && evt.button == (int)activateButton)
						{
							if (selectedElement != null && !evt.control)
							{
								// Since we didn't drag after all, update selection with current element only
								selectionContainer.ClearSelection();
								selectionContainer.AddToSelection(selectedElement);
							}

							this.ReleaseCapture();
							return EventPropagation.Stop;
						}
						break;
					}

				case EventType.DragUpdated:
					{
						if (this.HasCapture() && evt.button == (int)activateButton && selection.Count > 0)
						{
							selectedElement = null;

							// TODO: Replace with a temp drawing or something...maybe manipulator could fake position
							// all this to let operation know which element sits under cursor...or is there another way to draw stuff that is being dragged?

							EventPropagation result = EventPropagation.Continue;
							if (OnDrop != null)
							{
								var pickElem = panel.Pick(target.LocalToGlobal(evt.mousePosition));
								IDropTarget dropTarget = pickElem != null ? pickElem.GetFirstAncestorOfType<IDropTarget>() : null;
								result = OnDrop(evt, selection, dropTarget);
							}

							DragAndDrop.visualMode = evt.control ? DragAndDropVisualMode.Copy : DragAndDropVisualMode.Move;
							return result;
						}

						break;
					}

				case EventType.DragExited:
					{
						EventPropagation result = EventPropagation.Stop;

						if (OnDrop != null)
						{
							var pickElem = panel.Pick(target.LocalToGlobal(evt.mousePosition));
							IDropTarget dropTarget = pickElem != null ? pickElem.GetFirstAncestorOfType<IDropTarget>() : null;
							result = OnDrop(evt, selection, dropTarget);
						}

						DragAndDrop.visualMode = DragAndDropVisualMode.None;
						DragAndDrop.SetGenericData("DragSelection", null);

						this.ReleaseCapture();
						return result;
					}

				case EventType.DragPerform:
					{
						EventPropagation result = EventPropagation.Stop;

						if (this.HasCapture() && evt.button == (int)activateButton && selection.Count > 0)
						{
							if (selection.Count > 0)
							{

								if (OnDrop != null)
								{
									var pickElem = panel.Pick(target.LocalToGlobal(evt.mousePosition));
									IDropTarget dropTarget = pickElem != null ? pickElem.GetFirstAncestorOfType<IDropTarget>() : null;
									result = OnDrop(evt, selection, dropTarget);
								}

								DragAndDrop.visualMode = DragAndDropVisualMode.None;
								DragAndDrop.SetGenericData("DragSelection", null);
							}
						}

						this.ReleaseCapture();
						return result;
					}

			}
			return EventPropagation.Continue;
		}
	}
}
