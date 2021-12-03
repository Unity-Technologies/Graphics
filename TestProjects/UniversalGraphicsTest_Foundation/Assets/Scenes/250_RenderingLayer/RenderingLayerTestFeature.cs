using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;

public class RenderingLayerTestFeature : ScriptableRendererFeature
{
    private class RequestRenderingLayerPass : ScriptableRenderPass
    {
        private ProfilingSampler m_ProfilingSampler;
        private RenderTargetHandle m_Target;

        public RequestRenderingLayerPass(RenderPassEvent renderPassEvent)
        {
            m_ProfilingSampler = new ProfilingSampler("Draw Rendering Layer");
            this.renderPassEvent = RenderPassEvent.AfterRenderingSkybox;
            m_Target.Init("_RenderingLayerTestTexture");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                Blit(cmd, ref renderingData, m_Target.Identifier(), null);
            }

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            CommandBufferPool.Release(cmd);

        }
    }

    private class DrawRenderingLayerPass : ScriptableRenderPass
    {
        private static class ShaderPropertyId
        {
            public static readonly int scaleBias = Shader.PropertyToID("_ScaleBias");
        }

        private Material m_Material;
        private ProfilingSampler m_ProfilingSampler;
        private RenderTargetHandle m_Target;
        private Vector4[] m_RenderingLayerColors = new Vector4[32];

        public DrawRenderingLayerPass(RenderPassEvent renderPassEvent)
        {
            ConfigureInput(ScriptableRenderPassInput.RenderingLayer);
            m_ProfilingSampler = new ProfilingSampler("Draw Rendering Layer");
            this.renderPassEvent = renderPassEvent;
            m_Target.Init("_RenderingLayerTestTexture");
        }

        public void Setup(Material material)
        {
            m_Material = material;

            for (int i = 0; i < 32; i++)
                m_RenderingLayerColors[i] = Color.HSVToRGB(i / 32f, 1, 1);

        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var desc = renderingData.cameraData.cameraTargetDescriptor;
            desc.msaaSamples = 1;
            cmd.GetTemporaryRT(m_Target.id, desc);
            ConfigureTarget(m_Target.Identifier());
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                //Blit(cmd, ref renderingData, 0, m_Material);

                Render(cmd, renderingData.cameraData);
            }

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(m_Target.id);
        }

        private void Render(CommandBuffer cmd, in CameraData cameraData)
        {
            cmd.SetGlobalVectorArray("_RenderingLayerColors", m_RenderingLayerColors);

            cmd.SetGlobalVector(ShaderPropertyId.scaleBias, new Vector4(1, 1, 0, 0));
#if ENABLE_VR && ENABLE_XR_MODULE
            bool useDrawProcedural =  cameraData.xrRendering;
#else
            bool useDrawProcedural = false;
#endif

            cmd.SetViewProjectionMatrices(Matrix4x4.identity, Matrix4x4.identity); // Prepare for manual blit
            if (useDrawProcedural)
            {
                cmd.DrawProcedural(Matrix4x4.identity, m_Material, 0, MeshTopology.Quads, 4, 1, null);
            }
            else
            {
                cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, m_Material, 0, 0);
            }
            cmd.SetViewProjectionMatrices(cameraData.camera.worldToCameraMatrix, cameraData.camera.projectionMatrix);
        }
    }

    private const string k_ShaderName = "Hidden/Universal Render Pipeline/DrawRenderingLayer";

    [SerializeField]
    private Shader m_Shader;

    [SerializeField]
    private RenderPassEvent m_Event = RenderPassEvent.AfterRenderingPrePasses;

    private Material m_Material;
    private DrawRenderingLayerPass m_DrawRenderingLayerPass;
    private RequestRenderingLayerPass m_RequestRenderingLayerPass;

    /// <inheritdoc/>
    public override void Create()
    {
        m_DrawRenderingLayerPass = new DrawRenderingLayerPass(m_Event);
        m_RequestRenderingLayerPass = new RequestRenderingLayerPass(m_Event);

        GetMaterial();
    }

    private bool GetMaterial()
    {
        if (m_Material != null)
        {
            return true;
        }

        if (m_Shader == null)
        {
            m_Shader = Shader.Find(k_ShaderName);
            if (m_Shader == null)
            {
                return false;
            }
        }

        m_Material = CoreUtils.CreateEngineMaterial(m_Shader);

        return m_Material != null;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        Assert.IsNotNull(m_Material);
        m_DrawRenderingLayerPass.Setup(m_Material);
        renderer.EnqueuePass(m_DrawRenderingLayerPass);
        renderer.EnqueuePass(m_RequestRenderingLayerPass);
    }
}
