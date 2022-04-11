namespace UnityEditor.ShaderGraph
{
    interface IGeneratesBodyCode
    {
        void GenerateNodeCode(ShaderStringBuilder sb, GenerationMode generationMode);
    }
}
