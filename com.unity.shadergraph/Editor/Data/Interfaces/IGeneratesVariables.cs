namespace UnityEditor.ShaderGraph
{
    interface IGeneratesVariables
    {
        void GenerateNodeVariables(VariableRegistry registry, GenerationMode generationMode);
    }
}
