using System;

namespace UnityEngine.MaterialGraph
{
    [Flags]
    public enum NeededCoordinateSpace
    {
        None = 0,
        Object = 1<<0,
        View = 1<<1,
        World = 1<<2,
        Tangent = 1<<3
    }
}