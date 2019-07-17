namespace UnityEditor.ShaderGraph
{
    interface IProceduralMaterialSlot : Graphing.ISlot
    {
        string shaderOutputName { get; }
        ShaderStageCapability stageCapability { get; }
        ConcreteSlotValueType concreteValueType { get; }
    }
}
