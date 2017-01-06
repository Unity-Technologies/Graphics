using System;
using RMGUI.GraphView;
using UnityEditor.Graphing.Drawing;
using UnityEngine;
using UnityEngine.RMGUI;

namespace UnityEditor.Graphing.Drawing
{
    public class ClickGlobalSelector : MouseManipulator
    {
        public ClickGlobalSelector()
        {
            phaseInterest = EventPhase.Capture;
            activators.Add(new ManipActivator {button = MouseButton.LeftMouse});
            activators.Add(new ManipActivator {button = MouseButton.RightMouse});
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
