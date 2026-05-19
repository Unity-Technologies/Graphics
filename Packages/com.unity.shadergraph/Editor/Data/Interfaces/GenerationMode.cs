namespace UnityEditor.ShaderGraph
{
    enum GenerationMode
    {
        Preview,
        ForReals,
        PreviewForReals,    // Combines ForReals features (UITK macros) with Preview behavior (uniforms for slots)
        VFX
    }
    static class GenerationModeExtensions
    {
        public static bool IsPreview(this GenerationMode mode) { return mode == GenerationMode.Preview || mode == GenerationMode.PreviewForReals; }
    }
}
