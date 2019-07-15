namespace UnityEditor.ShaderGraph
{
    interface IProceduralMaterialSlot : Graphing.ISlot
    {
        string shaderOutputName { get; }
        ConcreteSlotValueType concreteValueType { get; }
    }
}
