namespace UnityEditor.ShaderGraph.Internal
{
    interface IConditionalShaderString
    {
        string value { get; }
        FieldCondition[] fieldConditions { get; }
    }
}
