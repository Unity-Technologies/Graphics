namespace UnityEditor.ShaderGraph
{
    interface IGeneratesFunction
    {
        void GenerateNodeFunction(FunctionRegistry registry, GenerationMode generationMode);
    }
}
