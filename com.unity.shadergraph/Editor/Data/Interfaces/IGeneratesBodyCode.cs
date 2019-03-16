namespace UnityEditor.ShaderGraph
{
    interface IGeneratesBodyCode
    {
        void GenerateNodeCode(ShaderGenerator visitor, GraphContext graphContext, GenerationMode generationMode);
    }
}
