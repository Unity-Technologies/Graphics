namespace UnityEditor.Graphing
{
    public interface ISlot
    {
        string name { get; }
        string displayName { get; set; }
        bool isInputSlot { get; }
        bool isOutputSlot { get; }
    }
}
