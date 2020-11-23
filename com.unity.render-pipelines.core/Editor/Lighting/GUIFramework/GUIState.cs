using UnityEngine;
using UnityEditor;

namespace UnityEditor
{
    /// <summary>
    /// Implementation of an IGUIState
    /// </summary>
    public class GUIState : IGUIState
    {
        private Handles.CapFunction nullCap = (int c, Vector3 p , Quaternion r, float s, EventType ev) => {};

        /// <summary>
        /// Mouse position
        /// </summary>
        public Vector2 mousePosition
        {
            get { return Event.current.mousePosition; }
        }

        /// <summary>
        /// Which mouse button was pressed.
        /// </summary>
        public int mouseButton
        {
            get { return Event.current.button; }
        }

        /// <summary>
        /// Click count
        /// </summary>
        public int clickCount
        {
            get { return Event.current.clickCount; }
            set { Event.current.clickCount = Mathf.Max(0, value); }
        }

        /// <summary>
        /// Is shift button down
        /// </summary>
        public bool isShiftDown
        {
            get { return Event.current.shift; }
        }

        /// <summary>
        /// Is Alt button down
        /// </summary>
        public bool isAltDown
        {
            get { return Event.current.alt; }
        }
        /// <summary>
        /// Is Action Key down
        /// </summary>
        public bool isActionKeyDown
        {
            get { return EditorGUI.actionKey; }
        }

        /// <summary>
        /// Key code
        /// </summary>
        public KeyCode keyCode
        {
            get { return Event.current.keyCode; }
        }

        /// <summary>
        /// Event type
        /// </summary>
        public EventType eventType
        {
            get { return Event.current.type; }
        }

        /// <summary>
        /// Command Name
        /// </summary>
        public string commandName
        {
            get { return Event.current.commandName; }
        }

        /// <summary>
        /// Nested control
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
        /// Is Changed
        /// </summary>
        public bool changed
        {
            get { return GUI.changed; }
            set { GUI.changed = value; }
        }

        /// <summary>
        /// Is in 2D mode
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
        /// Get Control ID
        /// </summary>
        /// <param name="hint">Hint</param>
        /// <param name="focusType">Focus Type</param>
        /// <returns>A Control ID</returns>
        public int GetControlID(int hint, FocusType focusType)
        {
            return GUIUtility.GetControlID(hint, focusType);
        }

        /// <summary>
        /// Add Control
        /// </summary>
        /// <param name="controlID">Control ID</param>
        /// <param name="distance">The distance</param>
        public void AddControl(int controlID, float distance)
        {
            HandleUtility.AddControl(controlID, distance);
        }

        /// <summary>
        /// Slider
        /// </summary>
        /// <param name="id">ID</param>
        /// <param name="sliderData">Slider Data</param>
        /// <param name="newPosition">New position</param>
        /// <returns>true if changed</returns>
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
        /// Use event
        /// </summary>
        public void UseEvent()
        {
            Event.current.Use();
        }

        /// <summary>
        /// Calls the methods in its invocation list when repaint
        /// </summary>
        public void Repaint()
        {
            HandleUtility.Repaint();
        }

        /// <summary>
        /// Test if has current camera
        /// </summary>
        /// <returns></returns>
        public bool HasCurrentCamera()
        {
            return Camera.current != null;
        }

        /// <summary>
        /// Get Handle size
        /// </summary>
        /// <param name="position">Position</param>
        /// <returns>Handle size</returns>
        public float GetHandleSize(Vector3 position)
        {
            var scale = HasCurrentCamera() ? 0.01f : 0.05f;
            return HandleUtility.GetHandleSize(position) * scale;
        }

        /// <summary>
        /// Helper to measure distance to segment describes 2 points
        /// </summary>
        /// <param name="p1">First point</param>
        /// <param name="p2">Seconde point</param>
        /// <returns>Distance between p1 and p2</returns>
        public float DistanceToSegment(Vector3 p1, Vector3 p2)
        {
            p1 = HandleUtility.WorldToGUIPoint(p1);
            p2 = HandleUtility.WorldToGUIPoint(p2);

            return HandleUtility.DistancePointToLineSegment(Event.current.mousePosition, p1, p2);
        }

        /// <summary>
        /// Helper to measure distance to circle
        /// </summary>
        /// <param name="center">Center of the circle</param>
        /// <param name="radius">Radius of the circle</param>
        /// <returns>Distance to circle</returns>
        public float DistanceToCircle(Vector3 center, float radius)
        {
            return HandleUtility.DistanceToCircle(center, radius);
        }

        /// <summary>
        /// GUI to world
        /// </summary>
        /// <param name="guiPosition">GUI position</param>
        /// <param name="planeNormal">Plane Normal</param>
        /// <param name="planePos">Plane position</param>
        /// <returns>World Pos of GUI</returns>
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
