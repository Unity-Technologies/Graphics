namespace UnityEngine.MaterialGraph
{
    public interface IGenerateProperties
    {
        void GeneratePropertyBlock(PropertyGenerator visitor, GenerationMode generationMode);
        void GeneratePropertyUsages(ShaderGenerator visitor, GenerationMode generationMode);
    }
}
