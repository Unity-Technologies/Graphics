namespace UnityEngine.Rendering
{
    // These Shader methods are internal, and the engine's InternalsVisibleTo list includes SRP Core's runtime assembly
    // but not URP's: URP calls them through this class, via Unity.RenderPipelines.Core.Runtime.Shared.
    internal static class ConstantBufferBackedPropertiesBridge
    {
        public static void SetBackedByConstantBuffer(int nameID, string warning)
        {
            Shader.SetGlobalShaderPropertyBackedByConstantBuffer(nameID, warning);
        }

        public static void ClearWarnings()
        {
            Shader.ClearConstantBufferBackedPropertyWarnings();
        }

        public static void EnableWarnings(bool enable)
        {
            Shader.EnableConstantBufferBackedPropertyWarnings(enable);
        }
    }
}
