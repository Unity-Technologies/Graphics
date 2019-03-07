using UnityEngine;
using UnityEditor;

namespace GUIFramework
{
    public struct SliderData
    {
        public Vector3 position;
        public Vector3 forward;
        public Vector3 up;
        public Vector3 right;

        public static readonly SliderData zero = new SliderData() { position = Vector3.zero, forward = Vector3.forward, up = Vector3.up, right = Vector3.right };
    }

    public interface IGUIState
    {
        Vector2 mousePosition { get; }
        int mouseButton { get; }
        int clickCount { get; }
        bool isShiftDown { get; }
        bool isAltDown { get; }
        bool isActionKeyDown { get; }
        KeyCode keyCode { get; }
        EventType eventType { get; }
        string commandName { get; }
        int nearestControl { get; set; }
        int hotControl { get; set; }
        bool changed { get; set; }
        int GetControlID(int hint, FocusType focusType);
        void AddControl(int controlID, float distance);
        bool Slider(int id, SliderData sliderData, out Vector3 newPosition);
        void UseCurrentEvent();
        void Repaint();
        bool IsEventOutsideWindow();
        bool IsViewToolActive();
        bool HasCurrentCamera();
    }
}
