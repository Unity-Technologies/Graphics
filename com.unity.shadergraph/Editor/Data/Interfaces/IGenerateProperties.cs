namespace UnityEditor.ShaderGraph
{
    interface IGenerateProperties
    {
        void CollectShaderProperties(PropertyCollector properties, GenerationMode generationMode);
    }
}
