namespace UnityEditor.ShaderGraph
{
    public interface IGeneratesBodyCode
    {
        void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode);
    }
}
