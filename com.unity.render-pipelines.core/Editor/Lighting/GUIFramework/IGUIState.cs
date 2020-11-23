using UnityEngine;
using UnityEditor;

namespace UnityEditor
{
    /// <summary>
    /// SliderData
    /// </summary>
    public struct SliderData
    {
        /// <summary>
        /// Position vector
        /// </summary>
        public Vector3 position;
        /// <summary>
        /// Forward vector
        /// </summary>
        public Vector3 forward;
        /// <summary>
        /// Up vector
        /// </summary>
        public Vector3 up;
        /// <summary>
        /// Right vector
        /// </summary>
        public Vector3 right;

        /// <summary>
        /// zero definition for SliderData
        /// </summary>
        public static readonly SliderData zero = new SliderData() { position = Vector3.zero, forward = Vector3.forward, up = Vector3.up, right = Vector3.right };
    }

    /// <summary>
    /// Interface for GUIStates
    /// </summary>
    public interface IGUIState
    {
        /// <summary>
        /// Mouse Position
        /// </summary>
        Vector2 mousePosition { get; }
        /// <summary>
        /// Mouse Button
        /// </summary>
        int mouseButton { get; }
        /// <summary>
        /// Click Count
        /// </summary>
        int clickCount { get; set; }
        /// <summary>
        /// Is Shift Down
        /// </summary>
        bool isShiftDown { get; }
        /// <summary>
        /// Is Alt Down
        /// </summary>
        bool isAltDown { get; }
        /// <summary>
        /// Is Action Key Down
        /// </summary>
        bool isActionKeyDown { get; }
        /// <summary>
        /// Key code
        /// </summary>
        KeyCode keyCode { get; }
        /// <summary>
        /// Event Type
        /// </summary>
        EventType eventType { get; }
        /// <summary>
        /// Command Name
        /// </summary>
        string commandName { get; }
        /// <summary>
        /// Nearest Control
        /// </summary>
        int nearestControl { get; set; }
        /// <summary>
        /// Hot Control
        /// </summary>
        int hotControl { get; set; }
        /// <summary>
        /// Changed
        /// </summary>
        bool changed { get; set; }
        /// <summary>
        /// In 2D Mode
        /// </summary>
        bool in2DMode { get; }

        /// <summary>
        /// Get Control ID
        /// </summary>
        /// <param name="hint">The int</param>
        /// <param name="focusType">The focus Type</param>
        /// <returns>The control ID</returns>
        int GetControlID(int hint, FocusType focusType);
        /// <summary>
        /// Add Control
        /// </summary>
        /// <param name="controlID">Control ID</param>
        /// <param name="distance">Distance</param>
        void AddControl(int controlID, float distance);
        /// <summary>
        /// Slide
        /// </summary>
        /// <param name="id">ID</param>
        /// <param name="sliderData">Slider data</param>
        /// <param name="newPosition">New position</param>
        /// <returns>true if changed</returns>
        bool Slider(int id, SliderData sliderData, out Vector3 newPosition);
        /// <summary>
        /// Use event
        /// </summary>
        void UseEvent();
        /// <summary>
        /// Repaint
        /// </summary>
        void Repaint();
        /// <summary>
        /// Has Current Camera
        /// </summary>
        /// <returns>true if had current camera</returns>
        bool HasCurrentCamera();
        /// <summary>
        /// Get Handle Size
        /// </summary>
        /// <param name="position">Position</param>
        /// <returns>Handle size</returns>
        float GetHandleSize(Vector3 position);
        /// <summary>
        /// Distance to segment
        /// </summary>
        /// <param name="p1">First point</param>
        /// <param name="p2">Second point</param>
        /// <returns>Distance between p1 and p2</returns>
        float DistanceToSegment(Vector3 p1, Vector3 p2);
        /// <summary>
        /// Distance to Circle
        /// </summary>
        /// <param name="center">Center of the circle</param>
        /// <param name="radius">Radius of the circle</param>
        /// <returns>Distance to a circle centered in center with a radius of radius</returns>
        float DistanceToCircle(Vector3 center, float radius);
        /// <summary>
        /// GUI To World
        /// </summary>
        /// <param name="guiPosition">GUI Position</param>
        /// <param name="planeNormal">Plane normal</param>
        /// <param name="planePos">Plane position</param>
        /// <returns>World position of GUI</returns>
        Vector3 GUIToWorld(Vector2 guiPosition, Vector3 planeNormal, Vector3 planePos);
    }
}
