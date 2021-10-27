namespace UnityEditor.ShaderGraph
{
    enum GenerationMode
    {
        Preview,
        ForReals,
        VFX
    }
    static class GenerationModeExtensions
    {
        public static bool IsPreview(this GenerationMode mode) { return mode == GenerationMode.Preview; }
    }
}
