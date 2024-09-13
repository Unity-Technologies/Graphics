using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal
{
    internal class GlobalLightTexturePass : ScriptableRenderPass
    {
        static readonly string k_SetGlobalLightTexture = "SetGlobalLightTextures";
        private static readonly ProfilingSampler m_SetGlobalLightTextureProfilingSampler = new ProfilingSampler(k_SetGlobalLightTexture);

#if UNITY_EDITOR
        private static readonly int k_DefaultWhiteTextureID = Shader.PropertyToID("_DefaultWhiteTex");
#endif

        class PassData
        {
        }

        internal static void SetGlobals(RenderGraph graph)
        {
            using (var builder = graph.AddRasterRenderPass<PassData>(k_SetGlobalLightTexture, out var passData, m_SetGlobalLightTextureProfilingSampler))
            {
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
                });
            }
        }
    }
}
