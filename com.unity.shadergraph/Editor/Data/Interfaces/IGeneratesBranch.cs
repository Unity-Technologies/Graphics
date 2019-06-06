namespace UnityEditor.ShaderGraph
{
    interface IGeneratesBranch
    {
        void CollectShaderKeywords(KeywordCollector keywords, GenerationMode generationMode);
    }
}
