using System;

namespace UnityEditor.Graphing
{
    [Flags]
    public enum GUIModificationType
    {
        None = 0,
        // just repaint this node and it's dependencies
        Repaint = 1 << 0,
        // something structurally changed, rebuild the graph from scratch!
        ModelChanged = 1 << 1,
        // some data internally to the node was modified 
        // that dependent nodes may use.
        DataChanged = 1 << 2
    }
}
