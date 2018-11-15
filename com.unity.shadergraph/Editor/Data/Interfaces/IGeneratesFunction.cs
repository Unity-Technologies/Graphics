namespace UnityEditor.ShaderGraph
{
    interface IGeneratesFunction
    {
        void GenerateNodeFunction(FunctionRegistry registry, GraphContext graphContext, GenerationMode generationMode);
    }
}
