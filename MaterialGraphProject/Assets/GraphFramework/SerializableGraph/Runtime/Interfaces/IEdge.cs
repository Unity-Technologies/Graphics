using System;

namespace UnityEngine.Graphing
{
    public interface IEdge : IEquatable<IEdge>
    {
        SlotReference outputSlot { get; }
        SlotReference inputSlot { get; }
    }
}
