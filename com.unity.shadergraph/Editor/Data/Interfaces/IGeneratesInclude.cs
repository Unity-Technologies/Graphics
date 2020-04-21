namespace UnityEditor.ShaderGraph
{
    interface IGeneratesInclude
    {
        void GenerateNodeInclude(IncludeCollection registry, GenerationMode generationMode);
    }
}
