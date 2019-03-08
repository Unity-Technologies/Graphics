using UnityEngine;
using UnityEditor;

namespace UnityEditor.Experimental.Rendering.LWRP.GUIFramework
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
            EditorGUI.BeginChangeCheck();

            //if (HasCurrentCamera())
                newPosition = Handles.Slider2D(id, sliderData.position, sliderData.forward, sliderData.right, sliderData.up, 1f, nullCap, Vector2.zero);
            //else
            //    newPosition = Slider2D.Do(id, sliderData.position, null);

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
    }
}
