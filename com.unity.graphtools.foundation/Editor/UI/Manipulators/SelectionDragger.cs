using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Manipulator to move the selected elements by click and drag.
    /// </summary>
    public class SelectionDragger : MouseManipulator
    {
        ISelectionDraggerTarget m_CurrentSelectionDraggerTarget;
        bool m_Dragging;
        readonly Snapper m_Snapper = new Snapper();

        bool m_Active;

        // selectedElement is used to store a unique selection candidate for cases where user clicks on an item not to
        // drag it but just to reset the selection -- we only know this after the manipulation has ended
        GraphElement m_SelectedElement => m_SelectedMovingElement.Element;

        MovingElement m_SelectedMovingElement => m_MovingElements.Count > m_SelectedMovingElementIndex ? m_MovingElements[m_SelectedMovingElementIndex] : default;

        int m_SelectedMovingElementIndex;

        List<VisualElement> m_DropTargetPickList = new List<VisualElement>();

        GraphView m_GraphView;
        Vector2 m_MouseStartInGraph;
        Vector2 m_TotalMouseDelta;

        /// <summary>
        /// Elements to be dragged and their initial position
        /// </summary>
        List<MovingElement> m_MovingElements;

        struct MovingElement
        {
            public GraphElement Element;
            public Vector2 InitialPosition;
        }

        public bool IsActive => m_Active;

        ISelectionDraggerTarget GetTargetAt(Vector2 mousePosition, IReadOnlyList<ModelView> exclusionList)
        {
            Vector2 pickPoint = mousePosition;

            m_DropTargetPickList.Clear();
            target.panel.PickAll(pickPoint, m_DropTargetPickList);

            ISelectionDraggerTarget selectionDraggerTarget = null;

            for (int i = 0; i < m_DropTargetPickList.Count; i++)
            {
                if (m_DropTargetPickList[i] == target && target != m_GraphView)
                    continue;

                VisualElement picked = m_DropTargetPickList[i];

                selectionDraggerTarget = picked as ISelectionDraggerTarget;

                if (selectionDraggerTarget != null)
                {
                    foreach (var element in exclusionList)
                    {
                        if (element == picked || element.FindCommonAncestor(picked) == element)
                        {
                            selectionDraggerTarget = null;
                            break;
                        }
                    }

                    if (selectionDraggerTarget != null)
                        break;
                }
            }

            return selectionDraggerTarget;
        }

        public SelectionDragger(GraphView graphView)
        {
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse, modifiers = EventModifiers.Shift });
            if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer)
            {
                activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse, modifiers = EventModifiers.Command });
            }
            else
            {
                activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse, modifiers = EventModifiers.Control });
            }

            m_GraphView = graphView;
            m_MovingElements = new List<MovingElement>();
            m_SelectedMovingElementIndex = 0;
        }

        /// <inheritdoc />
        protected override void RegisterCallbacksOnTarget()
        {
            if (!(target is IDragSource))
            {
                throw new InvalidOperationException("Manipulator can only be added to a control that supports selection");
            }

            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
            target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            target.RegisterCallback<MouseUpEvent>(OnMouseUp);

            target.RegisterCallback<KeyDownEvent>(OnKeyDown);
            target.RegisterCallback<MouseCaptureOutEvent>(OnMouseCaptureOutEvent);

            m_Dragging = false;
        }

        /// <inheritdoc />
        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);

            target.UnregisterCallback<KeyDownEvent>(OnKeyDown);
            target.UnregisterCallback<MouseCaptureOutEvent>(OnMouseCaptureOutEvent);
        }

        /// <summary>
        /// Callback for the MouseCaptureOut event.
        /// </summary>
        /// <param name="e">The event.</param>
        protected void OnMouseCaptureOutEvent(MouseCaptureOutEvent e)
        {
            if (m_Active)
            {
                m_CurrentSelectionDraggerTarget?.ClearDropHighlightStatus();

                // Stop processing the event sequence if the target has lost focus, then.
                m_SelectedMovingElementIndex = 0;
                m_CurrentSelectionDraggerTarget = null;
                m_Active = false;

                if (m_GraphView?.GetSelection().Any() ?? false)
                {
                    m_Snapper.EndSnap();
                }
            }
        }

        Vector2 GetViewPositionInGraphSpace(Vector2 localPosition)
        {
            var gvPos = new Vector2(m_GraphView.ViewTransform.position.x, m_GraphView.ViewTransform.position.y);
            var gvScale = m_GraphView.ViewTransform.scale.x;
            return (localPosition - gvPos) / gvScale;
        }

        Vector2 GetGraphPositionInViewSpace(Vector2 graphPosition)
        {
            var gvPos = new Vector2(m_GraphView.ViewTransform.position.x, m_GraphView.ViewTransform.position.y);
            var gvScale = m_GraphView.ViewTransform.scale.x;
            return gvPos + gvScale * graphPosition;
        }

        /// <summary>
        /// Callback for the MouseDown event.
        /// </summary>
        /// <param name="e">The event.</param>
        protected void OnMouseDown(MouseDownEvent e)
        {
            if (m_Active)
            {
                e.StopPropagation();
                return;
            }

            if (CanStartManipulation(e))
            {
                if (m_GraphView == null)
                    return;

                // avoid starting a manipulation on a non movable object
                var clickedElement = e.target as GraphElement;
                if (clickedElement == null)
                {
                    var ve = e.target as VisualElement;
                    clickedElement = ve?.GetFirstAncestorOfType<GraphElement>();
                    if (clickedElement == null)
                        return;
                }

                // Only start manipulating if the clicked element is movable, selected and that the mouse is in its clickable region (it must be deselected otherwise).
                if (!clickedElement.IsMovable() || !clickedElement.ContainsPoint(clickedElement.WorldToLocal(e.mousePosition)))
                    return;

                var selection = m_GraphView.GetSelection();

                var elementsToMove = new HashSet<GraphElement>(selection
                    .Select(model => model.GetView(m_GraphView))
                    .OfType<GraphElement>()
                    .Where(t=> !(t is Edge) && t.IsMovable()));

                if (!elementsToMove.Any())
                    return;

                m_GraphView.PanZoomIsOverriddenByManipulator = true;
                m_TotalMouseDelta = Vector2.zero;
                m_SelectedMovingElementIndex = 0;
                m_TotalFreePanTravel = Vector2.zero;

                var selectedPlacemats = new HashSet<Placemat>(elementsToMove.OfType<Placemat>());
                foreach (var placemat in selectedPlacemats)
                    placemat.GetElementsToMove(e.shiftKey, elementsToMove);

                m_MovingElements.Clear();
                if (elementsToMove.Count > m_MovingElements.Capacity)
                    m_MovingElements.Capacity = elementsToMove.Count;

                foreach (GraphElement ce in elementsToMove)
                {
                    ce.PositionIsOverriddenByManipulator = true;

                    if (ce == clickedElement)
                        m_SelectedMovingElementIndex = m_MovingElements.Count;
                    m_MovingElements.Add(new MovingElement
                    {
                        Element = ce, InitialPosition = ce.layout.position
                    });
                }

                m_MouseStartInGraph = GetViewPositionInGraphSpace(e.localMousePosition);
                m_TotalFreePanTravel = Vector2.zero;

                if (m_PanSchedule == null)
                {
                    var panInterval = GraphView.panInterval;
                    m_PanSchedule = m_GraphView.schedule.Execute(Pan).Every(panInterval).StartingIn(panInterval);
                    m_PanSchedule.Pause();
                }

                m_Snapper.BeginSnap(m_SelectedElement);

                m_Active = true;
                e.StopPropagation();
            }
        }

        IVisualElementScheduledItem m_PanSchedule;
        /// <summary>
        /// The offset by which the graphview should be panned based on the last mouse move.
        /// </summary>
        Vector2 m_CurrentPanSpeed = Vector2.zero;
        /// <summary>
        /// The offset by which the graphview has been panned during the move.
        /// <remarks>Used to figure out if we need to send a reframe command or not.</remarks>
        /// </summary>
        Vector2 m_TotalFreePanTravel = Vector2.zero;

        /// <summary>
        /// Callback for the MouseMove event.
        /// </summary>
        /// <param name="e">The event.</param>
        protected void OnMouseMove(MouseMoveEvent e)
        {
            if (!m_Active)
                return;

            if (m_GraphView == null)
                return;

            if ((e.pressedButtons & (1 << (int) MouseButton.MiddleMouse)) != 0)
            {
                OnMouseUp(e);
                return;
            }

            // We want the manipulator target to receive events even when mouse is not over it.
            // We wait for the (first) mouse move to capture the mouse because this is here that the interaction really begins.
            // At the mouse down stage, it is still to early, since the interaction could simply be a click and then should
            // be fully handled by another manipulator/element.
            if (!target.HasMouseCapture())
            {
                target.CaptureMouse();
            }

            m_CurrentPanSpeed = m_GraphView.GetEffectivePanSpeed(e.mousePosition);
            m_TotalFreePanTravel = Vector2.zero;

            if (m_CurrentPanSpeed != Vector2.zero)
            {
                m_PanSchedule.Resume();
            }
            else
            {
                m_PanSchedule.Pause();
            }

            if (m_SelectedElement.parent != null)
            {
                m_TotalMouseDelta = GetDragAndSnapOffset(GetViewPositionInGraphSpace(e.localMousePosition));

                MoveElements(m_TotalMouseDelta);
            }

            var selection = m_GraphView.GetSelection();
            var selectedUI = selection.Select(m => m.GetView(m_GraphView));

            var previousTarget = m_CurrentSelectionDraggerTarget;
            m_CurrentSelectionDraggerTarget = GetTargetAt(e.mousePosition, selectedUI.ToList());

            if (m_CurrentSelectionDraggerTarget != previousTarget)
            {
                previousTarget?.ClearDropHighlightStatus();
                m_CurrentSelectionDraggerTarget?.SetDropHighlightStatus(selection);
            }

            m_Dragging = true;
            e.StopPropagation();
        }

        Vector2 GetDragAndSnapOffset(Vector2 mouseGraphPosition)
        {
            var dragDelta = mouseGraphPosition - m_MouseStartInGraph;

            if (m_Snapper.IsActive)
            {
                dragDelta = GetSnapCorrectedDelta(m_SelectedMovingElement, dragDelta);
            }

            return dragDelta;
        }

        void MoveElements(Vector2 delta)
        {
            foreach (var movingElement in m_MovingElements)
            {
                // Protect against stale visual elements that have been deparented since the start of the manipulation
                if (movingElement.Element.hierarchy.parent == null)
                    continue;

                movingElement.Element.SetPositionOverride(movingElement.InitialPosition + delta);
            }
            using (var updater = m_GraphView.GraphViewModel.GraphViewState.UpdateScope)
            {
                updater.MarkContentUpdated();
            }
        }

        Vector2 GetSnapCorrectedDelta(MovingElement movingElement, Vector2 delta)
        {
            // Check if snapping is paused first: if yes, the snapper will return the original dragging position
            if (Event.current != null)
            {
                m_Snapper.PauseSnap(Event.current.shift);
            }

            Rect initialRect = movingElement.Element.layout;
            initialRect.position = movingElement.InitialPosition + delta;
            var snappedRect = m_Snapper.GetSnappedRect(initialRect, movingElement.Element);
            return snappedRect.position - movingElement.InitialPosition;
        }

        void Pan(TimerState ts)
        {
            var travelThisFrame = m_CurrentPanSpeed * ts.deltaTime;
            Vector3 p = m_GraphView.ContentViewContainer.transform.position - (Vector3)travelThisFrame;
            Vector3 s = m_GraphView.ContentViewContainer.transform.scale;
            m_GraphView.UpdateViewTransform(p, s);

            m_TotalFreePanTravel += travelThisFrame / s;
            MoveElements(m_TotalMouseDelta + m_TotalFreePanTravel);
        }

        /// <summary>
        /// Callback for the MouseUp event.
        /// </summary>
        /// <param name="evt">The event.</param>
        protected void OnMouseUp(IMouseEvent evt)
        {
            if (m_GraphView == null)
            {
                if (m_Active)
                {
                    target.ReleaseMouse();
                    m_SelectedMovingElementIndex = 0;
                    m_Active = false;
                    m_Dragging = false;
                    m_CurrentSelectionDraggerTarget = null;
                }

                return;
            }

            var selectedModels = m_GraphView.GetSelection();

            if (CanStopManipulation(evt))
            {
                if (m_Active)
                {
                    if (m_Dragging || m_SelectedElement == null)
                    {

                        if (target is GraphView graphView)
                        {
                            graphView.StopSelectionDragger();
                            graphView.PositionDependenciesManager.StopNotifyMove();
                        }

                        // if we stop dragging on something else than a DropTarget, just move elements
                        if (m_CurrentSelectionDraggerTarget == null || !m_CurrentSelectionDraggerTarget.CanAcceptDrop(selectedModels))
                        {
                            var models = m_MovingElements.Select(m => m.Element)
                                // PF remove this Where clause. It comes from VseGraphView.OnGraphViewChanged.
                                .Where(e => !(e.Model is INodeModel) || e.IsMovable())
                                .Select(e => e.Model)
                                .OfType<IMovable>()
                                .ToList();
                            var dragDelta = GetDragAndSnapOffset(GetViewPositionInGraphSpace(evt.localMousePosition));
                            m_GraphView.Dispatch(new MoveElementsCommand(dragDelta, models));
                        }
                    }

                    m_PanSchedule.Pause();

                    // save potentially changed zoom and scale in the graphview state
                    m_GraphView.PanZoomIsOverriddenByManipulator = false;
                    Vector3 p = m_GraphView.ContentViewContainer.transform.position;
                    Vector3 s = m_GraphView.ContentViewContainer.transform.scale;
                    m_GraphView.Dispatch(new ReframeGraphViewCommand(p, s));

                    m_CurrentSelectionDraggerTarget?.ClearDropHighlightStatus();
                    if (m_CurrentSelectionDraggerTarget?.CanAcceptDrop(selectedModels) ?? false)
                    {
                        m_CurrentSelectionDraggerTarget?.PerformDrop(selectedModels);
                    }

                    if (selectedModels.Any())
                    {
                        m_Snapper.EndSnap();
                    }

                    target.ReleaseMouse();
                    ((EventBase)evt).StopPropagation();

                    foreach (var element in m_MovingElements)
                    {
                        element.Element.PositionIsOverriddenByManipulator = false;
                    }
                }
                m_SelectedMovingElementIndex = 0;
                m_Active = false;
                m_CurrentSelectionDraggerTarget = null;
                m_Dragging = false;
            }
        }

        /// <summary>
        /// Callback for the KeyDown event.
        /// </summary>
        /// <param name="e">The event.</param>
        protected void OnKeyDown(KeyDownEvent e)
        {
            if (e.keyCode != KeyCode.Escape || m_GraphView == null || !m_Active)
                return;

            // Reset the items to their original pos.
            foreach (var movingElement in m_MovingElements)
            {
                var originalPos = movingElement.InitialPosition;
                movingElement.Element.style.left = originalPos.x;
                movingElement.Element.style.top = originalPos.y;
            }

            m_PanSchedule.Pause();

            if (m_TotalFreePanTravel != Vector2.zero)
            {
                Vector3 p = m_GraphView.ContentViewContainer.transform.position;
                Vector3 s = m_GraphView.ContentViewContainer.transform.scale;
                m_GraphView.Dispatch(new ReframeGraphViewCommand(p, s));
            }

            m_CurrentSelectionDraggerTarget?.ClearDropHighlightStatus();

            target.ReleaseMouse();
            e.StopPropagation();
        }
    }
}
