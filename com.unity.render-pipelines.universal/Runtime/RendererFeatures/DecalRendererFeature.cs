using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;

/*
 *
 */

namespace UnityEngine.Rendering.Universal
{
    public class DBufferPreparePass : ScriptableRenderPass
    {
        ProfilingSampler m_ProfilingSampler;

        public DBufferPreparePass()
        {
            this.renderPassEvent = RenderPassEvent.BeforeRendering;

            m_ProfilingSampler = new ProfilingSampler("DBuffer Prepare");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                cmd.EnableShaderKeyword("_DECAL");

                // decal system needs to be updated with current camera, it needs it to set up culling and light list generation parameters
                DecalSystem.CullRequest decalCullRequest = GenericPool<DecalSystem.CullRequest>.Get();
                DecalSystem.instance.CurrentCamera = renderingData.cameraData.camera;
                DecalSystem.instance.BeginCull(decalCullRequest);


                DecalSystem.CullResult decalCullResult = GenericPool<DecalSystem.CullResult>.Get();
                DecalSystem.instance.EndCull(decalCullRequest, decalCullResult);

                if (decalCullRequest != null)
                {
                    decalCullRequest.Clear();
                    GenericPool<DecalSystem.CullRequest>.Release(decalCullRequest);
                }

                // TODO: update singleton with DecalCullResults
                DecalSystem.instance.CurrentCamera = renderingData.cameraData.camera; // Singletons are extremely dangerous...
                DecalSystem.instance.LoadCullResults(decalCullResult);
                DecalSystem.instance.UpdateCachedMaterialData(); // textures, alpha or fade distances could've changed
                DecalSystem.instance.CreateDrawData();          // prepare data is separate from draw
                //DecalSystem.instance.UpdateTextureAtlas(cmd);   // as this is only used for transparent pass, would've been nice not to have to do this if no transparent renderers are visible, needs to happen after CreateDrawData

                if (decalCullResult != null)
                {
                    decalCullResult.Clear();
                    GenericPool<DecalSystem.CullResult>.Release(decalCullResult);
                }
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }
    }

    public class DecalRendererFeature : ScriptableRendererFeature
    {
        [Reload("Shaders/Utils/CopyDepth.shader")]
        public Shader copyDepthPS;

        DBufferRenderPass renderObjectsPass;
        DBufferPreparePass preparePass;

        CopyDepthPass copyDepthPass;

        public override void Create()
        {
#if UNITY_EDITOR
            ResourceReloader.TryReloadAllNullIn(this, UniversalRenderPipelineAsset.packagePath);
#endif
            var copyDepthMaterial = CoreUtils.CreateEngineMaterial(copyDepthPS);
            copyDepthPass = new CopyDepthPass(RenderPassEvent.AfterRenderingPrePasses, copyDepthMaterial);
            renderObjectsPass = new DBufferRenderPass("DBuffer Render", RenderPassEvent.BeforeRenderingPrePasses, DecalSystem.s_MaterialDecalPassNames);
            preparePass = new DBufferPreparePass();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            copyDepthPass.Setup(
                new RenderTargetHandle(new RenderTargetIdentifier("_CameraDepthTexture")),
                new RenderTargetHandle(new RenderTargetIdentifier("DBufferDepth"))
            );
            renderer.EnqueuePass(copyDepthPass);
            renderer.EnqueuePass(preparePass);
            renderer.EnqueuePass(renderObjectsPass);
        }

        protected override void Dispose(bool disposing)
        {
        }
    }
}
