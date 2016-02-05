using System;
using System.Collections.Generic;
using UnityEditorInternal;
using UnityEditorInternal.Experimental;
using UnityEngine;

namespace UnityEditor.Experimental
{
    internal class Frame : IManipulate
    {
        public enum FrameType
        {
            All = 0,
            Selection = 1
        };

        private readonly FrameType m_Type;

        public Frame(FrameType type)
        {
            m_Type = type;
        }

        public bool GetCaps(ManipulatorCapability cap)
        {
            return false;
        }

        public void AttachTo(CanvasElement element)
        {
            element.KeyDown += KeyDown;
        }

        private bool KeyDown(CanvasElement element, Event e, Canvas2D parent)
        {
            if (e.type == EventType.Used)
                return false;

            if ((m_Type == FrameType.All && e.keyCode == KeyCode.A) ||
                (m_Type == FrameType.Selection && e.keyCode == KeyCode.F))
            {
                Rect rectToFit = parent.canvasRect;
                if (m_Type == FrameType.Selection)
                {
                    List<CanvasElement> s = parent.selection;
                    if (s.Count == 0)
                        return false;
                    rectToFit = s[0].boundingRect;
                    foreach (CanvasElement c in s)
                    {
                        rectToFit = RectUtils.Encompass(rectToFit, c.boundingRect);
                    }
                }

                // bring slightly smaller screen rect into GUI space
                Rect screenRect = new Rect();
                screenRect.xMin = 50;
                screenRect.xMax = Screen.width - 50;
                screenRect.yMin = 50;
                screenRect.yMax = Screen.height - 50;

                Matrix4x4 m = GUI.matrix;
                GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, Vector3.one);
                Rect identity = GUIUtility.ScreenToGUIRect(screenRect);

                // measure zoom level necessary to fit the canvas rect into the screen rect
                float zoomLevel = Math.Min(identity.width / rectToFit.width, identity.height / rectToFit.height);

                // clamp
                zoomLevel = Mathf.Clamp(zoomLevel, 0.08f, 1.0f);

                parent.scale = new Vector3(zoomLevel, zoomLevel, 1.0f);
                parent.translation = Vector3.zero;


                // make a rect of the screen in GUI space and measure the distance between that rect
                // and the canvas rect. Multiply this by the scale level to get the offset to center the view
                Vector2 edge = parent.MouseToCanvas(new Vector2(Screen.width, Screen.height));
                Vector2 origin = parent.MouseToCanvas(new Vector2(0.0f, 0.0f));

                Rect r = new Rect();
                r.min = origin;
                r.max = edge;

                Vector2 offset = (r.center - rectToFit.center) * parent.scale.x;
                parent.translation = new Vector3(offset.x, offset.y, 0.0f);

                GUI.matrix = m;

                e.Use();

                return true;
            }

            return false;
        }
    };
}
