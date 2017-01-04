using UnityEngine;

namespace RMGUI.GraphView
{
	public struct ManipActivator
	{
		public MouseButton button;
		public KeyModifiers modifiers;

		public bool Matches(Event evt)
		{
			return button == (MouseButton) evt.button && HasModifiers(evt);
		}

		private bool HasModifiers(Event evt)
		{
			if (((modifiers & KeyModifiers.Alt) != 0 && !evt.alt) ||
				((modifiers & KeyModifiers.Alt) == 0 && evt.alt))
			{
				return false;
			}

			if (((modifiers & KeyModifiers.Ctrl) != 0 && !evt.control) ||
				((modifiers & KeyModifiers.Ctrl) == 0 && evt.control))
			{
				return false;
			}

			if (((modifiers & KeyModifiers.Shift) != 0 && !evt.shift) ||
				((modifiers & KeyModifiers.Shift) == 0 && evt.shift))
			{
				return false;
			}

			if (((modifiers & KeyModifiers.Command) != 0 && !evt.command) ||
				((modifiers & KeyModifiers.Command) == 0 && evt.command))
			{
				return false;
			}

			return true;
		}
	}
}
