namespace UnityEngine.Rendering.HighDefinition
{
    public class HDDebugUtils
    {
        public static DebugRenderer temporaryDebugRenderer { get; private set; }

        internal static void Initialize(RenderPipelineResources defaultResources)
        {
            temporaryDebugRenderer = new DebugRenderer(2048, defaultResources.shaders.debugRendererPS);
        }

        internal static void Cleanup()
        {
            temporaryDebugRenderer.Cleanup();
        }

        internal static void Render(CommandBuffer cmd, HDCamera hdCamera)
        {
            temporaryDebugRenderer.Render(cmd, hdCamera);
            temporaryDebugRenderer.ClearData();
        }
    }
}
