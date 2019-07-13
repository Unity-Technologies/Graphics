namespace UnityEditor.ShaderGraph
{
    interface IGeneratesProceduralCode
    {
        void GenerateProceduralCode(ShaderStringBuilder sb, GraphContext graphContext, GenerationMode generationMode);
    }
}
