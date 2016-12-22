using System;
using UnityEngine;
using UnityEngine.RMGUI;

namespace RMGUI.GraphView
{
	public class MouseManipulator : Manipulator
	{
		MouseButton manipButton { get; set; }

		bool[] m_ActivateButtons = new bool[Enum.GetNames(typeof(MouseButton)).Length];
		public bool[] activateButtons { get { return m_ActivateButtons; } }

		public KeyModifiers activateModifiers { get; set; }

		protected bool CanStartManipulation(Event evt)
		{
			if (!activateButtons[evt.button])
			{
				return false;
			}

			manipButton = (MouseButton)evt.button;

			return true;
		}

		protected bool CanStopManipulation(Event evt)
		{
			return (((MouseButton)evt.button == manipButton) && this.HasCapture());
		}

		protected bool HasModifiers(Event evt)
		{
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
	}
}
