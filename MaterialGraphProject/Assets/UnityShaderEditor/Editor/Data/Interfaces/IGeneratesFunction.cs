namespace UnityEditor.ShaderGraph
{
    public interface IGeneratesFunction
    {
        void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode);
    }
}
