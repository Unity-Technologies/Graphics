using System;
using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class OutputTextureFeature : ScriptableRendererFeature
{
    public Shader shader;
    public ScriptableRenderPassInput inputRequirement;
    public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRendering;
    public int renderPassEventAdjustment = 0;
    public Vector4 outputAdjustParams = new Vector4(0, 0, 1, 1);

    private Material m_Material;
    private OutputTexturePass m_OutputTexturePassPass;


    /// <inheritdoc/>
    public override void Create()
    {
        m_OutputTexturePassPass = new OutputTexturePass(name);
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (shader == null)
        {
            return;
        }

        if (m_Material == null)
        {
            m_Material = new Material(shader);
        }
        m_OutputTexturePassPass.renderPassEvent = renderPassEvent + renderPassEventAdjustment;
        m_OutputTexturePassPass.Setup(renderer, m_Material, inputRequirement, outputAdjustParams);
        renderer.EnqueuePass(m_OutputTexturePassPass);
    }

    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(m_Material);
    }

    class OutputTexturePass : ScriptableRenderPass
    {
        private Material m_Material;
        private ScriptableRenderer m_Renderer;
        private ProfilingSampler m_ProfilingSampler;
        private PassData m_PassData;
        public Vector4 m_OutputAdjustParams;

        public OutputTexturePass(string profilerTag)
        {
            m_ProfilingSampler = new ProfilingSampler(profilerTag);
            m_PassData = new PassData();
        }

        public void Setup(ScriptableRenderer renderer, Material material, ScriptableRenderPassInput inputRequirement, Vector4 outputAdjustParams)
        {
            m_Material = material;
            m_Renderer = renderer;
            m_OutputAdjustParams = outputAdjustParams;
            ConfigureInput(inputRequirement);
        }

        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in a performant manner.
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
        }

        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get();

            CoreUtils.SetRenderTarget(cmd, m_Renderer.cameraColorTargetHandle, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, ClearFlag.None, Color.clear);
            m_PassData.profilingSampler = m_ProfilingSampler;
            m_PassData.material = m_Material;
            m_PassData.outputAdjust = m_OutputAdjustParams;
            ExecutePass(m_PassData, CommandBufferHelpers.GetRasterCommandBuffer(cmd));

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        private class PassData
        {
            internal ProfilingSampler profilingSampler;
            internal TextureHandle cameraNormalsTexture;
            internal Material material;
            internal Vector4 outputAdjust;
        }

        static readonly int s_OutputAdjustParamsID = Shader.PropertyToID("_OutputAdjustParams");
        static readonly int s_CameraNormalsTextureID = Shader.PropertyToID("_CameraNormalsTexture");
        private static void ExecutePass(PassData passData, RasterCommandBuffer cmd)
        {
            using (new ProfilingScope(cmd, passData.profilingSampler))
            {
                if (passData.cameraNormalsTexture.IsValid())
                {
                    passData.material.SetTexture(s_CameraNormalsTextureID, passData.cameraNormalsTexture);
                }

                passData.material.SetVector(s_OutputAdjustParamsID, passData.outputAdjust);
                Blitter.BlitTexture(cmd, Vector2.one, passData.material, 0);
            }
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalRenderer renderer = cameraData.renderer as UniversalRenderer;

            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Output Texture Pass", out var passData, m_ProfilingSampler))
            {
                builder.UseAllGlobalTextures(true);

                if (renderer.renderingModeActual == RenderingMode.Deferred)
                {
                    builder.UseTexture(resourceData.gBuffer[renderer.deferredLights.GBufferNormalSmoothnessIndex]);
                    passData.cameraNormalsTexture =
                        resourceData.gBuffer[renderer.deferredLights.GBufferNormalSmoothnessIndex];
                }


                builder.SetRenderAttachment(resourceData.activeColorTexture, 0, AccessFlags.ReadWrite);
                builder.AllowPassCulling(false);

                passData.profilingSampler = m_ProfilingSampler;
                passData.material = m_Material;
                passData.outputAdjust = m_OutputAdjustParams;

                builder.SetRenderFunc((PassData data, RasterGraphContext rgContext) =>
                {
                    ExecutePass(data, rgContext.cmd);
                });
            }
        }

        // Cleanup any allocated resources that were created during the execution of this render pass.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
        }
    }
}
