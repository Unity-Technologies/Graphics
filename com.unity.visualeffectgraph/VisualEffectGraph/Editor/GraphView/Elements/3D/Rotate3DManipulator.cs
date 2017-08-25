using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEditor;

namespace Assets.VFXEditor.Editor.GraphView.Elements._3D
{
    class Rotate3DManipulator : Manipulator
    {
        public Rotate3DManipulator(Element3D target)
        {
        }

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseUpEvent>(OnMouseUp, Capture.Capture);
            target.RegisterCallback<MouseDownEvent>(OnMouseDown, Capture.Capture);
            //target.RegisterCallback<KeyDownEvent>(OnKeyDown);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            //target.UnregisterCallback<KeyDownEvent>(OnKeyDown);
        }

        void Release()
        {
            if (m_Dragging)
            {
                m_Dragging = false;
                target.ReleaseCapture();
                EditorGUIUtility.SetWantsMouseJumping(0);

                target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            }
        }

        bool m_Dragging;

        void OnMouseDown(MouseDownEvent e)
        {
            m_Dragging = true;
            EditorGUIUtility.SetWantsMouseJumping(0);
            target.TakeCapture();
            target.RegisterCallback<MouseMoveEvent>(OnMouseMove, Capture.Capture);
        }

        void OnMouseUp(MouseUpEvent e)
        {
            Release();
        }

        void OnMouseMove(MouseMoveEvent e)
        {
            if (m_Dragging)
            {
                e.mouseDelta;
            }
        }
    }
}
