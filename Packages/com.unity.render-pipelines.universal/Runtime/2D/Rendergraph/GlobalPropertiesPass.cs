using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal.U2D.Profiler;

namespace UnityEngine.Rendering.Universal
{
    internal class GlobalPropertiesPass : ScriptableRenderPass
    {
        class PassData {}

        internal static void Setup(RenderGraph graph, ContextContainer frameData, bool useLights)
        {
            Universal2DResourceData universal2DResourceData = frameData.Get<Universal2DResourceData>();
            Renderer2DData rendererData = frameData.Get<Universal2DRenderingData>().renderingData;

            using (var builder = graph.AddRasterRenderPass<PassData>(ProfilerMarkers.s_SetGlobalProperties, out _, ProfilerMarkers.s_ProfilingSamplerSetGlobalProperties))
            {
                if (useLights)
                {
                    // Set light lookup and fall off textures as global
                    var lightLookupTexture = graph.ImportTexture(Light2DLookupTexture.GetLightLookupTexture_Rendergraph());
                    var fallOffTexture = graph.ImportTexture(Light2DLookupTexture.GetFallOffLookupTexture_Rendergraph());

                    builder.SetGlobalTextureAfterPass(lightLookupTexture, Light2DLookupTexture.k_LightLookupID);
                    builder.SetGlobalTextureAfterPass(fallOffTexture, Light2DLookupTexture.k_FalloffLookupID);
                }

                if (rendererData.useCameraSortingLayerTexture)
                    builder.SetGlobalTextureAfterPass(universal2DResourceData.cameraSortingLayerTexture, CopyCameraSortingLayerPass.k_CameraSortingLayerTextureId);

                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc(static (PassData data, RasterGraphContext context) => {});
            }
        }
    }
}
