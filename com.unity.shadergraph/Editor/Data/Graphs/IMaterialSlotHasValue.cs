namespace UnityEditor.ShaderGraph
{
    interface IMaterialSlotHasValue<T>
    {
        T defaultValue { get; }
        T value { get; }
    }
}
