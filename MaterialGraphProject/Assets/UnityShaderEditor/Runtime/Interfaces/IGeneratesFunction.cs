namespace UnityEngine.MaterialGraph
{
    public interface IGeneratesFunction
    {
        void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode);
    }
}
