using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.GraphToolsFoundation.Overdrive
{
    /// <summary>
    /// Customization of the <see cref="ContextualMenuManipulator"/> for the <see cref="GraphView"/>.
    /// </summary>
    public class GraphViewContextualMenuManipulator : ContextualMenuManipulator
    {
        /// <inheritdoc />
        public GraphViewContextualMenuManipulator(Action<ContextualMenuPopulateEvent> menuBuilder)
            : base(menuBuilder)
        {
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.RightMouse });
            if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer)
            {
                activators.Add(new ManipulatorActivationFilter { button = MouseButton.RightMouse, modifiers = EventModifiers.Command });
            }
            else
            {
                activators.Add(new ManipulatorActivationFilter { button = MouseButton.RightMouse, modifiers = EventModifiers.Control });
            }
        }

        /// <inheritdoc />
        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseDownEvent>(SelectOnMouseDown);
            base.RegisterCallbacksOnTarget();
        }

        void SelectOnMouseDown(MouseDownEvent e)
        {
            if (CanStartManipulation(e))
            {
                var baseEvent = (EventBase)e;

                if (baseEvent.currentTarget is GraphElement graphElement)
                {
                    if (!graphElement.IsSelected())
                    {
                        GraphViewClickSelector.SelectElements(graphElement, e.actionKey);
                    }

                    // Prevent parent graph elements to change the selection.
                    e.StopPropagation();
                }
            }
        }
    }
}
