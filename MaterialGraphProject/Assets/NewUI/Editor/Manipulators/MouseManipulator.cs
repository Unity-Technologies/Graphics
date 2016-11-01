using UnityEngine;
using UnityEngine.RMGUI;

namespace RMGUI.GraphView
{
	public class MouseManipulator : Manipulator
	{
		public MouseButton activateButton { get; set; }
		public KeyModifiers activateModifiers { get; set; }

		public bool CanStartManipulation(Event evt)
		{
			if (evt.button != (int) activateButton)
			{
				return false;
			}

			if (((activateModifiers & KeyModifiers.Alt) != 0 && !evt.alt) ||
				((activateModifiers & KeyModifiers.Alt) == 0 && evt.alt))
			{
				return false;
			}

			if (((activateModifiers & KeyModifiers.Ctrl) != 0 && !evt.control) ||
				((activateModifiers & KeyModifiers.Ctrl) == 0 && evt.control))
			{
				return false;
			}

			if (((activateModifiers & KeyModifiers.Shift) != 0 && !evt.shift) ||
				((activateModifiers & KeyModifiers.Shift) == 0 && evt.shift))
			{
				return false;
			}

			if (((activateModifiers & KeyModifiers.Command) != 0 && !evt.command) ||
				((activateModifiers & KeyModifiers.Command) == 0 && evt.command))
			{
				return false;
			}

			return true;
		}

		public bool CanStopManipulation(Event evt)
		{
			return (evt.button == (int)activateButton && this.HasCapture());
		}
	}
}
