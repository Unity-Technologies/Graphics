using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal
{
    internal class GlobalPropertiesPass : ScriptableRenderPass
    {
        static readonly string k_SetGlobalProperties = "SetGlobalProperties";
        private static readonly ProfilingSampler m_SetGlobalPropertiesProfilingSampler = new ProfilingSampler(k_SetGlobalProperties);

#if UNITY_EDITOR
        private static readonly int k_DefaultWhiteTextureID = Shader.PropertyToID("_DefaultWhiteTex");
#endif

        class PassData
        {
            internal Vector2Int screenParams;
        }

        internal static void Setup(RenderGraph graph, UniversalCameraData cameraData)
        {
            using (var builder = graph.AddRasterRenderPass<PassData>(k_SetGlobalProperties, out var passData, m_SetGlobalPropertiesProfilingSampler))
            {
                // Set screenParams when pixel perfect camera is used with the reference resolution
                passData.screenParams = Vector2Int.zero;
                cameraData.camera.TryGetComponent(out PixelPerfectCamera pixelPerfectCamera);
                if (pixelPerfectCamera != null && pixelPerfectCamera.enabled && pixelPerfectCamera.offscreenRTSize != Vector2Int.zero)
                    passData.screenParams = pixelPerfectCamera.offscreenRTSize;

                // Set light lookup and fall off textures as global
                var lightLookupTexture = graph.ImportTexture(Light2DLookupTexture.GetLightLookupTexture_Rendergraph());
                var fallOffTexture = graph.ImportTexture(Light2DLookupTexture.GetFallOffLookupTexture_Rendergraph());

                builder.SetGlobalTextureAfterPass(lightLookupTexture, Light2DLookupTexture.k_LightLookupID);
                builder.SetGlobalTextureAfterPass(fallOffTexture, Light2DLookupTexture.k_FalloffLookupID);
#if UNITY_EDITOR
                builder.SetGlobalTextureAfterPass(graph.defaultResources.whiteTexture, k_DefaultWhiteTextureID);
#endif

                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
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
