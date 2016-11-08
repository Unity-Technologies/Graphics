using System;

namespace RMGUI.GraphView
{
	[Flags]
	public enum Capabilities
	{
		Normal = 1 << 0,
		Selectable = 1 << 1,
		DoesNotCollapse = 1 << 2,
		Floating = 1 << 3,
		Resizable = 1 << 4,
		Movable = 1 << 5,
		Deletable = 1 << 6
	}
}
