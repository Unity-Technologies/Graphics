namespace UnityEditor.ShaderGraph
{
    public interface IMaterialSlotHasVaule<T>
    {
        T defaultValue { get; }
        T value { get; }
    }
}
