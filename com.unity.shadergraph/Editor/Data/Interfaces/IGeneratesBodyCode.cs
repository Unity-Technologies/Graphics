namespace UnityEditor.ShaderGraph
{
    interface IGeneratesBodyCode
    {
        void GenerateNodeCode(ShaderStringBuilder sb, GraphContext graphContext, GenerationMode generationMode);
    }
}
