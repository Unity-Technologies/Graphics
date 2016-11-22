using System;
using RMGUI.GraphView;
using UnityEngine;
using UnityEngine.RMGUI;

namespace UnityEditor.Graphing.Drawing
{
    public class Clicker : MouseManipulator
    {

        public delegate void StateChangeCallback();
        public delegate void ClickCallback();

        public ClickCallback onClick { get; set; }

        VisualElement initialTarget;
        bool withinInitialTarget;

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
                        withinInitialTarget = initialTarget != null && initialTarget.ContainsPoint(evt.mousePosition);
                        if (withinInitialTarget && onClick != null)
                        {
                            onClick();
                        }
                    }
                    break;

            }
            return this.HasCapture() ? EventPropagation.Stop : EventPropagation.Continue;
        }
    }
}
