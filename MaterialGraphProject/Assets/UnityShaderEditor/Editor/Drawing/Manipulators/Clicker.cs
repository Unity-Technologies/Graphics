using UnityEngine.Experimental.UIElements;
using MouseManipulator = UnityEngine.Experimental.UIElements.MouseManipulator;

using ManipulatorActivationFilter = UnityEngine.Experimental.UIElements.ManipulatorActivationFilter;
using MouseButton = UnityEngine.Experimental.UIElements.MouseButton;

namespace UnityEditor.MaterialGraph.Drawing
{
    public enum ClickerState
    {
        Inactive,
        Active
    }

	// TODO JOCE: This is to mimic the behavior of a button. Remove and replace with actual button in TitleBar.
    public class Clicker : MouseManipulator
    {
        public delegate void StateChangeCallback(ClickerState newState);
        public delegate void ClickCallback();

        public StateChangeCallback onStateChange { get; set; }
        public ClickCallback onClick { get; set; }

        VisualElement initialTarget;
        ClickerState state;

		public Clicker()
		{
			activators.Add(new ManipulatorActivationFilter {button = MouseButton.LeftMouse});
		}

        protected override void RegisterCallbacksOnTarget()
        {
            target.RegisterCallback<MouseUpEvent>(OnMouseUp, Capture.Capture);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp, Capture.Capture);
        }

        void OnMouseUp(MouseUpEvent evt)
        {
            onClick();
        }
    }
}
