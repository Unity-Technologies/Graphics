namespace UnityEditor.MaterialGraph
{
    public interface IRequiresTime
    {
    }

    public enum GenerationMode
    {
        Preview2D,
        Preview3D,
        SurfaceShader
    }

    public static class GenerationModeExtensions
    {
        public static bool IsPreview(this GenerationMode mode) { return mode == GenerationMode.Preview2D || mode == GenerationMode.Preview3D; }
        public static bool Is2DPreview(this GenerationMode mode) { return mode == GenerationMode.Preview2D; }
        public static bool Is3DPreview(this GenerationMode mode) { return mode == GenerationMode.Preview3D; }
    }

    public interface IGeneratesBodyCode
    {
        void GenerateNodeCode(ShaderGenerator visitor, GenerationMode generationMode);
    }

    public interface IGeneratesVertexToFragmentBlock
    {
        void GenerateVertexToFragmentBlock(ShaderGenerator visitor, GenerationMode generationMode);
    }

    public interface IGeneratesFunction
    {
        void GenerateNodeFunction(ShaderGenerator visitor, GenerationMode generationMode);
    }

    public interface IGeneratesVertexShaderBlock
    {
        void GenerateVertexShaderBlock(ShaderGenerator visitor, GenerationMode generationMode);
    }

    public interface IGenerateProperties
    {
        void GeneratePropertyBlock(PropertyGenerator visitor, GenerationMode generationMode);
        void GeneratePropertyUsages(ShaderGenerator visitor, GenerationMode generationMode, ConcreteSlotValueType slotValueType);
    }
}
