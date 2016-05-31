namespace UnityEngine.Graphing
{
    public interface IEdge
    {
        SlotReference outputSlot { get; }
        SlotReference inputSlot { get; }
    }
}
