namespace UnityEditor.ShaderGraph
{
    [GenerationAPI]
    internal enum KeywordDefinition
    {
        ShaderFeature,      // adds #pragma shaderfeature for the keyword
        MultiCompile,       // adds #pragma multicompile for the keyword
        Predefined          // does not add ShaderFeature or MultiCompile pragmas, and is forced to be !exposed
    }
}
