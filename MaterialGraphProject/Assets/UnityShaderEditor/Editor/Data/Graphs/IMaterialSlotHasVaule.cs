namespace UnityEngine.MaterialGraph
{
    public interface IMaterialSlotHasVaule<T>
    {
        T defaultValue { get; }
        T value { get; }
    }
}