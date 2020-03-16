namespace UnityEditor.ShaderGraph
{
    interface IGeneratesInclude
    {
        void GenerateNodeInclude(IncludeRegistry registry, GenerationMode generationMode);
    }
}
