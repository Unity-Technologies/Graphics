namespace UnityEngine.MaterialGraph
{
    public interface IRequiresTime
    {
    }

    public enum GenerationMode
    {
        Preview,
        ForReals
    }

    public static class GenerationModeExtensions
    {
        public static bool IsPreview(this GenerationMode mode) { return mode == GenerationMode.Preview; }
    }

    public interface IGeneratesBodyCode
    {
        void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode);
    }

    public interface IGeneratesFunction
    {
        void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode);
    }
    
    public interface IGenerateProperties
    {
        void GeneratePropertyBlock(PropertyGenerator visitor, GenerationMode generationMode);
        void GeneratePropertyUsages(ShaderGenerator visitor, GenerationMode generationMode);
    }
}
