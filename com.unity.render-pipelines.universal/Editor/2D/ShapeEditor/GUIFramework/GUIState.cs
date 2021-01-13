using UnityEngine;
using UnityEditor;

namespace UnityEditor.Experimental.Rendering.Universal.Path2D.GUIFramework
{
    internal class GUIState : IGUIState
    {
        private Handles.CapFunction nullCap = (int c, Vector3 p , Quaternion r, float s, EventType ev) => {};

        public Vector2 mousePosition
        {
            get { return Event.current.mousePosition; }
        }

        public int mouseButton
        {
            get { return Event.current.button; }
        }

        public int clickCount
        {
            get { return Event.current.clickCount; }
        }

        public bool isShiftDown
        {
            get { return Event.current.shift; }
        }

        public bool isAltDown
        {
            get { return Event.current.alt; }
        }

        public bool isActionKeyDown
        {
            get { return EditorGUI.actionKey; }
        }

        public KeyCode keyCode
        {
            get { return Event.current.keyCode; }
        }

        public EventType eventType
        {
            get { return Event.current.type; }
        }

        public string commandName
        {
            get { return Event.current.commandName; }
        }

        public int nearestControl
        {
            get { return HandleUtility.nearestControl; }
            set { HandleUtility.nearestControl = value; }
        }

        public int hotControl
        {
            get { return GUIUtility.hotControl; }
            set { GUIUtility.hotControl = value; }
        }

        public bool changed
        {
            get { return GUI.changed; }
            set { GUI.changed = value; }
        }

        public int GetControlID(int hint, FocusType focusType)
        {
            return GUIUtility.GetControlID(hint, focusType);
        }

        public void AddControl(int controlID, float distance)
        {
            HandleUtility.AddControl(controlID, distance);
        }

        public bool Slider(int id, SliderData sliderData, out Vector3 newPosition)
        {
            if (mouseButton == 0 && eventType == EventType.MouseDown)
            {
                hotControl = 0;
                nearestControl = id;
            }

            EditorGUI.BeginChangeCheck();
            newPosition = Handles.Slider2D(id, sliderData.position, sliderData.forward, sliderData.right, sliderData.up, 1f, nullCap, Vector2.zero);
            return EditorGUI.EndChangeCheck();
        }

        public void UseCurrentEvent()
        {
            Event.current.Use();
        }

        public void Repaint()
        {
            HandleUtility.Repaint();
        }

        public bool IsEventOutsideWindow()
        {
            return Event.current.type == EventType.Ignore;
        }

        public bool IsViewToolActive()
        {
            return UnityEditor.Tools.current == Tool.View || isAltDown || mouseButton == 1 || mouseButton == 2;
        }

        public bool HasCurrentCamera()
        {
            return Camera.current != null;
        }

        public float GetHandleSize(Vector3 position)
        {
            var scale = HasCurrentCamera() ? 0.01f : 0.05f;
            return HandleUtility.GetHandleSize(position) * scale;
        }

        public float DistanceToSegment(Vector3 p1, Vector3 p2)
        {
            p1 = HandleUtility.WorldToGUIPoint(p1);
            p2 = HandleUtility.WorldToGUIPoint(p2);

            return HandleUtility.DistancePointToLineSegment(Event.current.mousePosition, p1, p2);
        }

        public float DistanceToCircle(Vector3 center, float radius)
        {
            return HandleUtility.DistanceToCircle(center, radius);
        }

        public Vector3 GUIToWorld(Vector2 guiPosition, Vector3 planeNormal, Vector3 planePos)
        {
            Vector3 worldPos = Handles.inverseMatrix.MultiplyPoint(guiPosition);

            if (Camera.current)
            {
                Ray ray = HandleUtility.GUIPointToWorldRay(guiPosition);

                planeNormal = Handles.matrix.MultiplyVector(planeNormal);

                planePos = Handles.matrix.MultiplyPoint(planePos);

                Plane plane = new Plane(planeNormal, planePos);

                float distance = 0f;

                if (plane.Raycast(ray, out distance))
                {
                    worldPos = Handles.inverseMatrix.MultiplyPoint(ray.GetPoint(distance));
                }
            }

            return worldPos;
        }
    }
}
