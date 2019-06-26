namespace UnityEditor.ShaderGraph
{
    enum GenerationMode
    {
        Preview,
        PreviewAndOptimized,
        ForReals,
        ForRealsAndOptimized,
    }

    static class GenerationModeExtensions
    {
        public static bool IsPreview(this GenerationMode mode) { return mode == GenerationMode.Preview || mode == GenerationMode.PreviewAndOptimized; }
        public static bool IsOptimized(this GenerationMode mode) { return mode == GenerationMode.ForRealsAndOptimized || mode == GenerationMode.PreviewAndOptimized; }
    }
}
