using System;
using UnityEditor.Experimental.UIElements.GraphView;
using UnityEditor.MaterialGraph.Drawing;
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
            activators.Add(new ManipulatorActivationFilter {button = MouseButton.LeftMouse});
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.RightMouse});
        }

        public void HandleEvent(MouseEventBase evt)
        {
            var graphView = target as MaterialGraphView;
			if (graphView == null)
				throw new InvalidOperationException("Manipulator can only be added to a SerializableGraphView");

            graphView.SetGlobalSelection();
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseDownEvent>(HandleEvent, Capture.Capture);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(HandleEvent, Capture.Capture);
        }
    }
}
