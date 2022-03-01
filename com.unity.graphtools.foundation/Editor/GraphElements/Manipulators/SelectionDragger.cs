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
        DropTarget m_CurrentDropTarget;
        bool m_Dragging;
        readonly Snapper m_Snapper = new Snapper();

        bool m_Active;

        Vector2 m_PanSpeed;

        // selectedElement is used to store a unique selection candidate for cases where user clicks on an item not to
        // drag it but just to reset the selection -- we only know this after the manipulation has ended
        GraphElement m_SelectedElement;

        List<VisualElement> m_DropTargetPickList = new List<VisualElement>();

        GraphView m_GraphView;

        Dictionary<GraphElement, OriginalPos> m_OriginalPos;
        Vector2 m_OriginalMouse;

        class OriginalPos
        {
            public Rect pos;
            public bool dragStarted;
        }

        public bool IsActive => m_Active;

        DropTarget GetDropTargetAt(Vector2 mousePosition, IReadOnlyList<VisualElement> exclusionList)
        {
            Vector2 pickPoint = mousePosition;

            m_DropTargetPickList.Clear();
            target.panel.PickAll(pickPoint, m_DropTargetPickList);

            DropTarget dropTarget = null;

            for (int i = 0; i < m_DropTargetPickList.Count; i++)
            {
                if (m_DropTargetPickList[i] == target && target != m_GraphView)
                    continue;

                VisualElement picked = m_DropTargetPickList[i];

                dropTarget = picked as DropTarget;

                if (dropTarget != null)
                {
                    foreach (var element in exclusionList)
                    {
                        if (element == picked || element.FindCommonAncestor(picked) == element)
                        {
                            dropTarget = null;
                            break;
                        }
                    }

                    if (dropTarget != null)
                        break;
                }
            }

            return dropTarget;
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
            m_PanSpeed = new Vector2(1, 1);

            m_GraphView = graphView;
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
                if (m_CurrentDropTarget != null && m_GraphView != null)
                {
                    using (DragExitedEvent eExit = DragExitedEvent.GetPooled())
                    {
                        eExit.target = m_CurrentDropTarget;
                        m_GraphView.SendEvent(eExit);
                    }
                }

                // Stop processing the event sequence if the target has lost focus, then.
                m_SelectedElement = null;
                m_CurrentDropTarget = null;
                m_Active = false;

                if (m_GraphView?.GetSelection().Any() ?? false)
                {
                    m_Snapper.EndSnap();
                }
            }
        }

        /// <summary>
        /// Callback for the MouseDown event.
        /// </summary>
        /// <param name="e">The event.</param>
        protected void OnMouseDown(MouseDownEvent e)
        {
            if (m_Active)
            {
                e.StopImmediatePropagation();
                return;
            }

            if (CanStartManipulation(e))
            {
                if (m_GraphView == null)
                    return;

                m_SelectedElement = null;

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

                // If we hit this, this likely because the element has just been unselected
                // It is important for this manipulator to receive the event so the previous one did not stop it
                // but we shouldn't let it propagate to other manipulators to avoid a re-selection
                if (!clickedElement.IsSelected())
                {
                    e.StopImmediatePropagation();
                    return;
                }
                var selection = m_GraphView.GetSelection();

                var elementsToMove = new HashSet<GraphElement>(selection.Select(model => model.GetUI(m_GraphView)).OfType<GraphElement>().Where(t=> ! (t is Edge)));

                if (!elementsToMove.Any())
                    return;

                m_SelectedElement = clickedElement;
                if (!elementsToMove.Contains(m_SelectedElement))
                    m_SelectedElement = (GraphElement)elementsToMove.First();

                m_OriginalPos = new Dictionary<GraphElement, OriginalPos>();

                var selectedPlacemats = new HashSet<Placemat>(elementsToMove.OfType<Placemat>());
                foreach (var placemat in selectedPlacemats)
                    placemat.GetElementsToMove(e.shiftKey, elementsToMove);

                foreach (GraphElement ce in elementsToMove.OfType<GraphElement>())
                {
                    if (!ce.IsMovable())
                        continue;

                    Rect geometry = ce.GetPosition();
                    Rect geometryInContentViewSpace = ce.hierarchy.parent.ChangeCoordinatesTo(m_GraphView.ContentViewContainer, geometry);
                    m_OriginalPos[ce] = new OriginalPos
                    {
                        pos = geometryInContentViewSpace,
                    };
                }

                m_OriginalMouse = e.mousePosition;
                m_ItemPanDiff = Vector3.zero;

                if (m_PanSchedule == null)
                {
                    m_PanSchedule = m_GraphView.schedule.Execute(Pan).Every(panInterval).StartingIn(panInterval);
                    m_PanSchedule.Pause();
                }

                m_Snapper.BeginSnap(m_SelectedElement);

                m_Active = true;
                target.CaptureMouse(); // We want to receive events even when mouse is not over ourself.
                e.StopImmediatePropagation();
            }
        }

        public const int panAreaWidth = 100;
        public const int panSpeed = 4;
        public const int panInterval = 10;
        public const float minSpeedFactor = 0.5f;
        public const float maxSpeedFactor = 2.5f;
        public const float maxPanSpeed = maxSpeedFactor * panSpeed;

        IVisualElementScheduledItem m_PanSchedule;
        Vector3 m_PanDiff = Vector3.zero;
        Vector3 m_ItemPanDiff = Vector3.zero;
        Vector2 m_MouseDiff = Vector2.zero;

        float m_Scale;

        internal Vector2 GetEffectivePanSpeed(Vector2 mousePos)
        {
            Vector2 effectiveSpeed = Vector2.zero;

            if (mousePos.x <= panAreaWidth)
                effectiveSpeed.x = -(((panAreaWidth - mousePos.x) / panAreaWidth) + 0.5f) * panSpeed;
            else if (mousePos.x >= m_GraphView.contentContainer.layout.width - panAreaWidth)
                effectiveSpeed.x = (((mousePos.x - (m_GraphView.contentContainer.layout.width - panAreaWidth)) / panAreaWidth) + 0.5f) * panSpeed;

            if (mousePos.y <= panAreaWidth)
                effectiveSpeed.y = -(((panAreaWidth - mousePos.y) / panAreaWidth) + 0.5f) * panSpeed;
            else if (mousePos.y >= m_GraphView.contentContainer.layout.height - panAreaWidth)
                effectiveSpeed.y = (((mousePos.y - (m_GraphView.contentContainer.layout.height - panAreaWidth)) / panAreaWidth) + 0.5f) * panSpeed;

            effectiveSpeed = Vector2.ClampMagnitude(effectiveSpeed, maxPanSpeed);

            return effectiveSpeed;
        }

        void ComputeSnappedRect(ref Rect selectedElementProposedGeom, GraphElement element)
        {
            // Check if snapping is paused first: if yes, the snapper will return the original dragging position
            if (Event.current != null)
            {
                m_Snapper.PauseSnap(Event.current.shift);
            }

            // Let the snapper compute a snapped position
            Rect geometryInContentViewContainerSpace = element.parent.ChangeCoordinatesTo(m_GraphView.ContentViewContainer, selectedElementProposedGeom);

            Vector2 mousePanningDelta = new Vector2((m_MouseDiff.x - m_ItemPanDiff.x) * m_PanSpeed.x / m_Scale, (m_MouseDiff.y - m_ItemPanDiff.y) * m_PanSpeed.y / m_Scale);
            geometryInContentViewContainerSpace = m_Snapper.GetSnappedRect(geometryInContentViewContainerSpace, element, m_Scale, mousePanningDelta);

            // Once the snapped position is computed in the GraphView.contentViewContainer's space then
            // translate it into the local space of the parent of the selected element.
            selectedElementProposedGeom = m_GraphView.ContentViewContainer.ChangeCoordinatesTo(element.parent, geometryInContentViewContainerSpace);
        }

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

            var ve = (VisualElement)e.target;
            Vector2 gvMousePos = ve.ChangeCoordinatesTo(m_GraphView.contentContainer, e.localMousePosition);
            m_PanDiff = GetEffectivePanSpeed(gvMousePos);

            if (m_PanDiff != Vector3.zero)
            {
                m_PanSchedule.Resume();
            }
            else
            {
                m_PanSchedule.Pause();
            }

            // We need to monitor the mouse diff "by hand" because we stop positioning the graph elements once the
            // mouse has gone out.
            m_MouseDiff = m_OriginalMouse - e.mousePosition;

            if (m_SelectedElement.parent != null)
            {
                // Handle the selected element
                Rect selectedElementGeom = GetSelectedElementGeom();

                ComputeSnappedRect(ref selectedElementGeom, m_SelectedElement);

                foreach (KeyValuePair<GraphElement, OriginalPos> v in m_OriginalPos)
                {
                    GraphElement ce = v.Key;

                    // Protect against stale visual elements that have been deparented since the start of the manipulation
                    if (ce.hierarchy.parent == null)
                        continue;

                    if (!v.Value.dragStarted)
                    {
                        v.Value.dragStarted = true;
                    }

                    SnapOrMoveElement(v.Key, v.Value.pos, selectedElementGeom);
                }
            }

            var selection = m_GraphView.GetSelection();
            var selectedUI = selection.Select(m => m.GetUI(m_GraphView));

            // TODO: Replace with a temp drawing or something...maybe manipulator could fake position
            // all this to let operation know which element sits under cursor...or is there another way to draw stuff that is being dragged?

            var previousDropTarget = m_CurrentDropTarget;
            m_CurrentDropTarget = GetDropTargetAt(e.mousePosition, selectedUI.OfType<VisualElement>().ToList());

            if (m_CurrentDropTarget != previousDropTarget)
            {
                if (previousDropTarget != null)
                {
                    using (DragLeaveEvent eLeave = DragLeaveEvent.GetPooled(e))
                    {
                        eLeave.target = previousDropTarget;
                        m_GraphView.SendEvent(eLeave);
                    }
                }

                if (m_CurrentDropTarget != null)
                {
                    using (DragEnterEvent eEnter = DragEnterEvent.GetPooled(e))
                    {
                        eEnter.target = m_CurrentDropTarget;
                        m_GraphView.SendEvent(eEnter);
                    }
                }
            }

            if (m_CurrentDropTarget != null)
            {
                using (DragUpdatedEvent eUpdated = DragUpdatedEvent.GetPooled(e))
                {
                    eUpdated.target = m_CurrentDropTarget;
                    m_GraphView.SendEvent(eUpdated);
                }
            }

            m_Dragging = true;
            e.StopPropagation();
        }

        void Pan(TimerState ts)
        {
            Vector3 p = m_GraphView.ContentViewContainer.transform.position - m_PanDiff;
            Vector3 s = m_GraphView.ContentViewContainer.transform.scale;
            m_GraphView.UpdateViewTransform(p, s);

            m_ItemPanDiff += m_PanDiff;

            // Handle the selected element
            Rect selectedElementGeom = GetSelectedElementGeom();

            ComputeSnappedRect(ref selectedElementGeom, m_SelectedElement);

            foreach (KeyValuePair<GraphElement, OriginalPos> v in m_OriginalPos)
            {
                SnapOrMoveElement(v.Key, v.Value.pos, selectedElementGeom);
            }
        }

        void SnapOrMoveElement(GraphElement element, Rect originalPos, Rect selectedElementGeom)
        {
            if (m_Snapper.IsActive)
            {
                Vector2 geomDiff = selectedElementGeom.position - m_OriginalPos[m_SelectedElement].pos.position;
                Vector2 position = new Vector2(originalPos.x + geomDiff.x, originalPos.y + geomDiff.y);

                element.SetPosition(new Rect(position, element.layout.size));
            }
            else
            {
                MoveElement(element, originalPos);
            }
        }

        Rect GetSelectedElementGeom()
        {
            // Handle the selected element
            Matrix4x4 g = m_SelectedElement.worldTransform;
            m_Scale = g.m00; //The scale on x is equal to the scale on y because the graphview is not distorted

            Rect selectedElementGeom = m_OriginalPos[m_SelectedElement].pos;

            if (m_Snapper.IsActive)
            {
                // Compute the new position of the selected element using the mouse delta position and panning info
                selectedElementGeom.x = selectedElementGeom.x - (m_MouseDiff.x - m_ItemPanDiff.x) * m_PanSpeed.x / m_Scale;
                selectedElementGeom.y = selectedElementGeom.y - (m_MouseDiff.y - m_ItemPanDiff.y) * m_PanSpeed.y / m_Scale;
            }

            return selectedElementGeom;
        }

        void MoveElement(GraphElement element, Rect originalPos)
        {
            Matrix4x4 g = element.worldTransform;
            var scale = new Vector3(g.m00, g.m11, g.m22);

            Rect newPos = new Rect(0, 0, originalPos.width, originalPos.height);

            // Compute the new position of the selected element using the mouse delta position and panning info
            newPos.x = originalPos.x - (m_MouseDiff.x - m_ItemPanDiff.x) * m_PanSpeed.x / scale.x * element.transform.scale.x;
            newPos.y = originalPos.y - (m_MouseDiff.y - m_ItemPanDiff.y) * m_PanSpeed.y / scale.y * element.transform.scale.y;

            newPos = m_GraphView.ContentViewContainer.ChangeCoordinatesTo(element.hierarchy.parent, newPos);

            element.SetPosition(newPos);
        }

        /// <summary>
        /// Callback for the MouseUp event.
        /// </summary>
        /// <param name="evt">The event.</param>
        protected void OnMouseUp(MouseUpEvent evt)
        {
            if (m_GraphView == null)
            {
                if (m_Active)
                {
                    target.ReleaseMouse();
                    m_SelectedElement = null;
                    m_Active = false;
                    m_Dragging = false;
                    m_CurrentDropTarget = null;
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
                        if (m_CurrentDropTarget == null || !m_CurrentDropTarget.CanAcceptDrop(selectedModels))
                        {
                            var movedElements = new HashSet<GraphElement>(m_OriginalPos.Keys);

                            KeyValuePair<GraphElement, OriginalPos> firstPos = m_OriginalPos.First();
                            var delta = firstPos.Key.GetPosition().position - firstPos.Value.pos.position;
                            var models = movedElements
                                // PF remove this Where clause. It comes from VseGraphView.OnGraphViewChanged.
                                .Where(e => !(e.Model is INodeModel) || e.IsMovable())
                                .Select(e => e.Model)
                                .OfType<IMovable>();
                            m_GraphView.Dispatch(
                                new MoveElementsCommand(delta, models.ToList()));
                        }
                    }

                    m_PanSchedule.Pause();

                    if (m_ItemPanDiff != Vector3.zero)
                    {
                        Vector3 p = m_GraphView.ContentViewContainer.transform.position;
                        Vector3 s = m_GraphView.ContentViewContainer.transform.scale;
                        m_GraphView.Dispatch(new ReframeGraphViewCommand(p, s));
                    }

                    if (selectedModels.Count > 0 && m_CurrentDropTarget != null)
                    {
                        if (m_CurrentDropTarget.CanAcceptDrop(selectedModels))
                        {
                            using (DragPerformEvent ePerform = DragPerformEvent.GetPooled(evt))
                            {
                                ePerform.target = m_CurrentDropTarget;
                                m_GraphView.SendEvent(ePerform);
                            }
                        }
                        else
                        {
                            using (DragExitedEvent eExit = DragExitedEvent.GetPooled(evt))
                            {
                                eExit.target = m_CurrentDropTarget;
                                m_GraphView.SendEvent(eExit);
                            }
                        }
                    }

                    if (selectedModels.Any())
                    {
                        m_Snapper.EndSnap();
                    }

                    target.ReleaseMouse();
                    evt.StopPropagation();
                }
                m_SelectedElement = null;
                m_Active = false;
                m_CurrentDropTarget = null;
                m_Dragging = false;
                m_CurrentDropTarget = null;
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
            foreach (KeyValuePair<GraphElement, OriginalPos> v in m_OriginalPos)
            {
                OriginalPos originalPos = v.Value;
                v.Key.style.left = originalPos.pos.x;
                v.Key.style.top = originalPos.pos.y;
            }

            m_PanSchedule.Pause();

            if (m_ItemPanDiff != Vector3.zero)
            {
                Vector3 p = m_GraphView.ContentViewContainer.transform.position;
                Vector3 s = m_GraphView.ContentViewContainer.transform.scale;
                m_GraphView.Dispatch(new ReframeGraphViewCommand(p, s));
            }

            using (DragExitedEvent eExit = DragExitedEvent.GetPooled())
            {
                eExit.target = m_CurrentDropTarget;
                m_GraphView.SendEvent(eExit);
            }

            target.ReleaseMouse();
            e.StopPropagation();
        }
    }
}
