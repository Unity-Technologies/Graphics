using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Selects an element when they receive a mouse down event.
    /// </summary>
    public class ClickSelector : MouseManipulator
    {
        static bool WasSelectableDescendantHitByMouse(GraphElement currentTarget, MouseDownEvent evt)
        {
            VisualElement targetElement = evt.target as VisualElement;

            if (targetElement == null || currentTarget == targetElement)
                return false;

            VisualElement descendant = targetElement;

            while (descendant != null && currentTarget != descendant)
            {
                GraphElement selectableDescendant = descendant as GraphElement;

                if (selectableDescendant != null && selectableDescendant.enabledInHierarchy && selectableDescendant.pickingMode != PickingMode.Ignore && selectableDescendant.Model.IsSelectable())
                {
                    Vector2 localMousePosition = currentTarget.ChangeCoordinatesTo(descendant, evt.localMousePosition);

                    if (selectableDescendant.ContainsPoint(localMousePosition))
                    {
                        return true;
                    }
                }
                descendant = descendant.parent;
            }
            return false;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClickSelector"/> class.
        /// </summary>
        public ClickSelector()
        {
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse, modifiers = EventModifiers.Shift });
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse, modifiers = EventModifiers.Alt });

            activators.Add(new ManipulatorActivationFilter { button = MouseButton.RightMouse });
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.RightMouse, modifiers = EventModifiers.Shift });
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.RightMouse, modifiers = EventModifiers.Alt });

            if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer)
            {
                activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse, modifiers = EventModifiers.Command });
            }
            else
            {
                activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse, modifiers = EventModifiers.Control });
            }
        }

        /// <inheritdoc />
        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
        }

        /// <inheritdoc />
        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
        }

        protected void OnMouseDown(MouseDownEvent e)
        {
            if (!(e.currentTarget is GraphElement graphElement))
            {
                return;
            }

            if (CanStartManipulation(e) && graphElement.Model.IsSelectable() &&
                graphElement.ContainsPoint(e.localMousePosition) &&
                !WasSelectableDescendantHitByMouse(graphElement, e))
            {
                if (!graphElement.IsSelected() || e.actionKey)
                {
                    var selectionMode = e.actionKey ? SelectElementsCommand.SelectionMode.Toggle : SelectElementsCommand.SelectionMode.Replace;
                    graphElement.GraphView.Dispatch(new SelectElementsCommand(selectionMode, graphElement.Model));
                }

                // Do not stop the propagation as it is common case for a parent start to move the selection on a mouse down.
            }
        }
    }
}
