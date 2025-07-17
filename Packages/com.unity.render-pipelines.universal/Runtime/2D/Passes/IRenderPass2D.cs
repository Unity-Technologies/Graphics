#if URP_COMPATIBILITY_MODE
namespace UnityEngine.Rendering.Universal
{
    internal interface IRenderPass2D
    {
        Renderer2DData rendererData { get; }
    }
}
#endif
