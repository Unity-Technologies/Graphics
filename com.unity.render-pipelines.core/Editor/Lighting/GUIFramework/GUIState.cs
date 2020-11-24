using UnityEngine;
using UnityEditor;

namespace UnityEditor
{
    /// <summary>
    /// An implementation of an IGUIState that represents a generic GUI state.
    /// </summary>
    public class GUIState : IGUIState
    {
        private Handles.CapFunction nullCap = (int c, Vector3 p , Quaternion r, float s, EventType ev) => {};

        /// <summary>
        /// The current mouse position.
        /// </summary>
        public Vector2 mousePosition
        {
            get { return Event.current.mousePosition; }
        }

        /// <summary>
        /// The currently pressed button.
        /// </summary>
        public int mouseButton
        {
            get { return Event.current.button; }
        }

        /// <summary>
        /// The current number of mouse clicks.
        /// </summary>
        public int clickCount
        {
            get { return Event.current.clickCount; }
            set { Event.current.clickCount = Mathf.Max(0, value); }
        }

        /// <summary>
        /// Indicates whether the shift key is pressed.
        /// </summary>
        public bool isShiftDown
        {
            get { return Event.current.shift; }
        }

        /// <summary>
        /// Indicates whether the alt key is pressed.
        /// </summary>
        public bool isAltDown
        {
            get { return Event.current.alt; }
        }
        /// <summary>
        /// Indicates whether the action key is pressed.
        /// </summary>
        public bool isActionKeyDown
        {
            get { return EditorGUI.actionKey; }
        }

        /// <summary>
        /// The KeyCode of the currently pressed key.
        /// </summary>
        public KeyCode keyCode
        {
            get { return Event.current.keyCode; }
        }

        /// <summary>
        /// The type of the current event.
        /// </summary>
        public EventType eventType
        {
            get { return Event.current.type; }
        }

        /// <summary>
        /// The name of the current event's command.
        /// </summary>
        public string commandName
        {
            get { return Event.current.commandName; }
        }

        /// <summary>
        /// The closest control to the event.
        /// </summary>
        public int nearestControl
        {
            get { return HandleUtility.nearestControl; }
            set { HandleUtility.nearestControl = value; }
        }

        /// <summary>
        /// Hot Control
        /// </summary>
        public int hotControl
        {
            get { return GUIUtility.hotControl; }
            set { GUIUtility.hotControl = value; }
        }

        /// <summary>
        /// Indicates whether the GUI has changed.
        /// </summary>
        public bool changed
        {
            get { return GUI.changed; }
            set { GUI.changed = value; }
        }

        /// <summary>
        /// Indicates whether the GUI is in 2D mode or not.
        /// </summary>
        public bool in2DMode
        {
            get
            {
                return SceneView.currentDrawingSceneView == null ||
                    SceneView.currentDrawingSceneView != null && SceneView.currentDrawingSceneView.in2DMode;
            }
        }

        /// <summary>
        /// Gets the ID of a nested control by a hint and focus type.
        /// </summary>
        /// <param name="hint">The hint this function uses to identify the control ID.</param>
        /// <param name="focusType">The focus Type</param>
        /// <returns>Returns the ID of the control that matches the hint and focus type.</returns>
        public int GetControlID(int hint, FocusType focusType)
        {
            return GUIUtility.GetControlID(hint, focusType);
        }

        /// <summary>
        /// Adds a control to the GUIState.
        /// </summary>
        /// <param name="controlID">The ID of the control to add.</param>
        /// <param name="distance">The distance from the camera to the control.</param>
        public void AddControl(int controlID, float distance)
        {
            HandleUtility.AddControl(controlID, distance);
        }

        /// <summary>
        /// Checks whether a slider value has changed.
        /// </summary>
        /// <param name="id">The ID of the slider to check.</param>
        /// <param name="sliderData">The slider's data.</param>
        /// <param name="newPosition">The new position of the slider.</param>
        /// <returns>Returns `true` if the slider has changed. Otherwise, returns `false`.</returns>
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

        /// <summary>
        /// Uses the current event.
        /// </summary>
        public void UseEvent()
        {
            Event.current.Use();
        }

        /// <summary>
        /// Repaints the GUI.
        /// </summary>
        public void Repaint()
        {
            HandleUtility.Repaint();
        }

        /// <summary>
        /// Checks if the current camera is valid. 
        /// </summary>
        /// <returns>Returns `true` if the current camera is not null. Otherwise, returns `false`.</returns>
        public bool HasCurrentCamera()
        {
            return Camera.current != null;
        }

        /// <summary>
        /// Gets the size of the handle.
        /// </summary>
        /// <param name="position">The position of the handle.</param>
        /// <returns>Returns the size of the handle.</returns>
        public float GetHandleSize(Vector3 position)
        {
            var scale = HasCurrentCamera() ? 0.01f : 0.05f;
            return HandleUtility.GetHandleSize(position) * scale;
        }

        /// <summary>
        /// Measures the GUI-space distance between two points of a segment.
        /// </summary>
        /// <param name="p1">The first point.</param>
        /// <param name="p2">The seconde point.</param>
        /// <returns>Returns the GUI-space distance between p1 and p2.</returns>
        public float DistanceToSegment(Vector3 p1, Vector3 p2)
        {
            p1 = HandleUtility.WorldToGUIPoint(p1);
            p2 = HandleUtility.WorldToGUIPoint(p2);

            return HandleUtility.DistancePointToLineSegment(Event.current.mousePosition, p1, p2);
        }

        /// <summary>
        /// Measures the distance to a circle.
        /// </summary>
        /// <param name="center">The center of the circle.</param>
        /// <param name="radius">The radius of the circle.</param>
        /// <returns>Returns the distance to a circle with the specified center and radius.</returns>
        public float DistanceToCircle(Vector3 center, float radius)
        {
            return HandleUtility.DistanceToCircle(center, radius);
        }

        /// <summary>
        /// Transforms a GUI-space position into world space.
        /// </summary>
        /// <param name="guiPosition">The GUI position</param>
        /// <param name="planeNormal">The plane normal.</param>
        /// <param name="planePos">The plane position.</param>
        /// <returns>Returns the world-space position of `guiPosition`.</returns>
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
