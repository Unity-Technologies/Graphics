namespace UnityEditor.ShaderGraph
{
    // A ISplatLoopNode creates a new loop of all splats because it depends on the result of all splats.
    interface ISplatLoopNode
    {
        void GenerateSetupCode(ShaderStringBuilder sb, GraphContext graphContext, GenerationMode generationMode);
    }
}
