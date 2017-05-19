using System;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using ManipulatorActivationFilter = UnityEngine.Experimental.UIElements.ManipulatorActivationFilter;
using MouseButton = UnityEngine.Experimental.UIElements.MouseButton;
using MouseManipulator = UnityEngine.Experimental.UIElements.MouseManipulator;

namespace UnityEditor.Graphing.Drawing
{
    public class ClickGlobalSelector : MouseManipulator
    {
        public ClickGlobalSelector()
        {
            phaseInterest = PropagationPhase.Capture;
            activators.Add(new ManipulatorActivationFilter {button = MouseButton.LeftMouse});
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.RightMouse});
        }

        public override EventPropagation HandleEvent(Event evt, VisualElement finalTarget)
        {
            var graphView = target as SerializableGraphView;
			if (graphView == null)
				throw new InvalidOperationException("Manipulator can only be added to a SerializableGraphView");

            graphView.SetGlobalSelection();

            return EventPropagation.Continue;
        }
    }
}
