using System;
using RMGUI.GraphView;
using UnityEngine;
using UnityEngine.Graphing;
using UnityEngine.RMGUI;

namespace UnityEditor.Graphing.Drawing
{
    public class NodeExpander : MouseManipulator
    {
        public HeaderDrawData data;
        private VisualElement initialTarget;

        public override EventPropagation HandleEvent(Event evt, VisualElement finalTarget)
        {
            switch (evt.type)
            {
                case EventType.MouseDown:
                    if (CanStartManipulation(evt))
                    {
                        this.TakeCapture();
                        initialTarget = finalTarget;
                    }
                    break;

                case EventType.MouseUp:
                    if (CanStopManipulation(evt))
                    {
                        this.ReleaseCapture();
                        var withinInitialTarget = initialTarget != null && initialTarget.ContainsPoint(evt.mousePosition);
                        if (true || withinInitialTarget)
                        {
                            data.expanded = !data.expanded;
                        }
                    }
                    break;

            }
            return this.HasCapture() ? EventPropagation.Stop : EventPropagation.Continue;
        }
    }
}