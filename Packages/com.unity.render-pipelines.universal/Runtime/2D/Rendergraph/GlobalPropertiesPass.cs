using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal.U2D.Profiler;

namespace UnityEngine.Rendering.Universal
{
    internal class GlobalPropertiesPass : ScriptableRenderPass
    {

        class PassData
        {
            internal Vector2Int screenParams;
        }

        internal static void Setup(RenderGraph graph, ContextContainer frameData, bool useLights)
        {
            Universal2DResourceData universal2DResourceData = frameData.Get<Universal2DResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            Renderer2DData rendererData = frameData.Get<Universal2DRenderingData>().renderingData;

            using (var builder = graph.AddRasterRenderPass<PassData>(ProfilerMarkers.s_SetGlobalProperties, out var passData, ProfilerMarkers.s_ProfilingSamplerSetGlobalProperties))
            {
                // Set screenParams when pixel perfect camera is used with the reference resolution
                passData.screenParams = Vector2Int.zero;
                cameraData.camera.TryGetComponent(out PixelPerfectCamera pixelPerfectCamera);
                if (pixelPerfectCamera != null && pixelPerfectCamera.enabled && pixelPerfectCamera.offscreenRTSize != Vector2Int.zero)
                    passData.screenParams = pixelPerfectCamera.offscreenRTSize;

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

                builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
                {
                    if (data.screenParams != Vector2Int.zero)
                    {
                        var cameraWidth = data.screenParams.x;
                        var cameraHeight = data.screenParams.y;
                        context.cmd.SetGlobalVector(ShaderPropertyId.screenParams, new Vector4(cameraWidth, cameraHeight, 1.0f + 1.0f / cameraWidth, 1.0f + 1.0f / cameraHeight));
                    }
                });
            }
        }
    }
}
