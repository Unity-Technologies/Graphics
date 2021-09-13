using System;

namespace UnityEditor.Graphing
{
    interface IEdge : IEquatable<IEdge>
    {
        SlotReference outputSlot { get; }
        SlotReference inputSlot { get; }
    }
}
