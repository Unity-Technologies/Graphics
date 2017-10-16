namespace UnityEngine.MaterialGraph
{
    public interface IGeneratesBodyCode
    {
        void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode);
    }
}
