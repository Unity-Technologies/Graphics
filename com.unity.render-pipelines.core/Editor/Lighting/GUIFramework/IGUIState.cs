using UnityEngine;
using UnityEditor;

namespace UnityEditor.GUIFramework
{
    /// <summary>
    /// Represents transform data for a slider.
    /// </summary>
    /// <remarks>
    /// Unity uses this data to position and orient the slider in the custom editor.
    /// </remarks>
    public struct SliderData
    {
        /// <summary>
        /// The slider's position.
        /// </summary>
        public Vector3 position;
        /// <summary>
        /// The slider's forward vector.
        /// </summary>
        public Vector3 forward;
        /// <summary>
        /// The slider's up vector.
        /// </summary>
        public Vector3 up;
        /// <summary>
        /// The slider's right vector.
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
        /// The mouse position.
        /// </summary>
        Vector2 mousePosition { get; }
        /// <summary>
        /// The mouse button pressed.
        /// </summary>
        int mouseButton { get; }
        /// <summary>
        /// The number of mouse clicks.
        /// </summary>
        int clickCount { get; set; }
        /// <summary>
        /// Indicates whether the shift key is pressed.
        /// </summary>
        bool isShiftDown { get; }
        /// <summary>
        /// Indicates whether the alt key is pressed.
        /// </summary>
        bool isAltDown { get; }
        /// <summary>
        /// Indicates whether the action key is pressed.
        /// </summary>
        bool isActionKeyDown { get; }
        /// <summary>
        /// The KeyCode of the currently pressed key.
        /// </summary>
        KeyCode keyCode { get; }
        /// <summary>
        /// The type of the event.
        /// </summary>
        EventType eventType { get; }
        /// <summary>
        /// The name of the event's command.
        /// </summary>
        string commandName { get; }
        /// <summary>
        /// The closest control to the event.
        /// </summary>
        int nearestControl { get; set; }
        /// <summary>
        /// Hot Control
        /// </summary>
        int hotControl { get; set; }
        /// <summary>
        /// Indicates whether the GUI has changed.
        /// </summary>
        bool changed { get; set; }
        /// <summary>
        /// Indicates whether the GUI is in 2D mode or not.
        /// </summary>
        bool in2DMode { get; }

        /// <summary>
        /// Gets the ID of a nested control by a hint and focus type.
        /// </summary>
        /// <param name="hint">The hint this function uses to identify the control ID.</param>
        /// <param name="focusType">The focus Type</param>
        /// <returns>Returns the ID of the control that matches the hint and focus type.</returns>
        int GetControlID(int hint, FocusType focusType);
        /// <summary>
        /// Adds a control to the GUIState.
        /// </summary>
        /// <param name="controlID">The ID of the control to add.</param>
        /// <param name="distance">The distance from the camera to the control.</param>
        void AddControl(int controlID, float distance);
        /// <summary>
        /// Checks whether a slider value has changed.
        /// </summary>
        /// <param name="id">The ID of the slider to check.</param>
        /// <param name="sliderData">The slider's data.</param>
        /// <param name="newPosition">The new position of the slider.</param>
        /// <returns>Returns `true` if the slider has changed. Otherwise, returns `false`.</returns>
        bool Slider(int id, SliderData sliderData, out Vector3 newPosition);
        /// <summary>
        /// Uses the event.
        /// </summary>
        void UseEvent();
        /// <summary>
        /// Repaints the GUI.
        /// </summary>
        void Repaint();
        /// <summary>
        /// Checks if the current camera is valid. 
        /// </summary>
        /// <returns>Returns `true` if the current camera is not null. Otherwise, returns `false`.</returns>
        bool HasCurrentCamera();
        /// <summary>
        /// Gets the size of the handle.
        /// </summary>
        /// <param name="position">The position of the handle.</param>
        /// <returns>Returns the size of the handle.</returns>
        float GetHandleSize(Vector3 position);
        /// <summary>
        /// Measures the GUI-space distance between two points of a segment.
        /// </summary>
        /// <param name="p1">The first point.</param>
        /// <param name="p2">The second point.</param>
        /// <returns>Returns the GUI-space distance between p1 and p2.</returns>
        float DistanceToSegment(Vector3 p1, Vector3 p2);
        /// <summary>
        /// Measures the distance to a circle.
        /// </summary>
        /// <param name="center">The center of the circle</param>
        /// <param name="radius">The radius of the circle</param>
        /// <returns>Returns the distance to a circle with the specified center and radius.</returns>
        float DistanceToCircle(Vector3 center, float radius);
        /// <summary>
        /// Transforms a GUI-space position into world space.
        /// </summary>
        /// <param name="guiPosition">The GUI Position.</param>
        /// <param name="planeNormal">The plane normal.</param>
        /// <param name="planePos">The plane position.</param>
        /// <returns>Returns the world-space position of `guiPosition`.</returns>
        Vector3 GUIToWorld(Vector2 guiPosition, Vector3 planeNormal, Vector3 planePos);
    }
}
