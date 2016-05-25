namespace UnityEditor.Graphing
{
    public interface IEdge
    {
        SlotReference outputSlot { get; }
        SlotReference inputSlot { get; }
    }
}
