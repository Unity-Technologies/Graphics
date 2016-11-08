namespace UnityEngine.Graphing
{
    public interface ISlot
    {
        int id { get; }
        string displayName { get; set; }
        bool isInputSlot { get; }
        bool isOutputSlot { get; }
        int priority { get; set; }
        SlotReference slotReference { get; }
        INode owner { get; set; }
    }

    public interface IGenerateDefaultInput
    {
        INode defaultNode { get; }
        int defaultSlotID { get; }
    }
}
