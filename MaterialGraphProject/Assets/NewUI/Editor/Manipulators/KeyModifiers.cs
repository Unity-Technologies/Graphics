using System;

namespace RMGUI.GraphView
{
	// TODO We could move that higher in RMGUI.
	[Flags]
	public enum KeyModifiers
	{
		None = 0,
		Alt = 1 << 0,
		Ctrl = 1 << 1,
		Shift = 1 << 2,
		Command = 1 << 3
	}
}
