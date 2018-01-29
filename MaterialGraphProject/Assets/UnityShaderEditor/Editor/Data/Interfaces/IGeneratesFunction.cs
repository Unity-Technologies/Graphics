namespace UnityEditor.ShaderGraph
{
    public interface IGeneratesFunction
    {
        void GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode);
    }
}
