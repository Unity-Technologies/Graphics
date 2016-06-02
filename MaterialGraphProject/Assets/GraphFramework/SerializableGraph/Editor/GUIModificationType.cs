using System;

namespace UnityEditor.Graphing
{
    [Flags]
    public enum GUIModificationType
    {
        None = 0,
        Repaint = 1 << 0,
        ModelChanged = 1 << 1,
        DataChanged = 1 << 2
    }
}
