using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.Universal.Internal;

public class NormalReconstructionTestFeature : ScriptableRendererFeature
{
    private enum TapMode
    {
        Tap1,
        Tap3,
        Tap5,
        Tap9,
    }

    private class DrawNormalPass : ScriptableRenderPass
    {
        private static class ShaderPropertyId
        {
            public static readonly int scaleBias = Shader.PropertyToID("_ScaleBias");
        }

        private static Material m_Material;
        private static ProfilingSampler m_ProfilingSampler;

        public DrawNormalPass()
        {
            m_ProfilingSampler = new ProfilingSampler("Render Normals");
            renderPassEvent = RenderPassEvent.AfterRenderingSkybox;
        }

        public void Setup(Material material)
        {
            m_Material = material;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get();

            ExecutePass(cmd, renderingData.cameraData);

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            CommandBufferPool.Release(cmd);
        }

        static void ExecutePass(CommandBuffer cmd, CameraData cameraData)
        {
            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                NormalReconstruction.SetupProperties(cmd, cameraData);

                int width = cameraData.cameraTargetDescriptor.width;
                int height = cameraData.cameraTargetDescriptor.height;

                Render(cmd, cameraData, TapMode.Tap1, new Rect(0, 0, 0.5f, 0.5f), width, height);
                Render(cmd, cameraData, TapMode.Tap3, new Rect(0.5f, 0, 0.5f, 0.5f), width, height);
                Render(cmd, cameraData, TapMode.Tap5, new Rect(0, 0.5f, 0.5f, 0.5f), width, height);
                Render(cmd, cameraData, TapMode.Tap9, new Rect(0.5f, 0.5f, 0.5f, 0.5f), width, height);
            }
        }

        static void Render(CommandBuffer cmd, in CameraData cameraData, TapMode tapMode, Rect viewport, int width, int height)
        {
            CoreUtils.SetKeyword(cmd, "_DRAW_NORMALS_TAP1", tapMode == TapMode.Tap1);
            CoreUtils.SetKeyword(cmd, "_DRAW_NORMALS_TAP3", tapMode == TapMode.Tap3);
            CoreUtils.SetKeyword(cmd, "_DRAW_NORMALS_TAP5", tapMode == TapMode.Tap5);
            CoreUtils.SetKeyword(cmd, "_DRAW_NORMALS_TAP9", tapMode == TapMode.Tap9);

            cmd.SetGlobalVector(ShaderPropertyId.scaleBias, new Vector4(1f / viewport.width, 1f / viewport.height, width * -viewport.x * 2, height * -viewport.y * 2));
            cmd.SetViewport(new Rect(width * viewport.x, height * viewport.y, width * viewport.width, height * viewport.height));

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

        internal class PassData
        {
            internal CameraData cameraData;
        }
        public override void RecordRenderGraph(RenderGraph renderGraph, FrameResources frameResources, ref RenderingData renderingData)
        {
            using (var builder = renderGraph.AddRenderPass<PassData>("Normal Reconstruction Test Pass", out var passData, m_ProfilingSampler))
            {
                UniversalRenderer renderer = (UniversalRenderer) renderingData.cameraData.renderer;

                TextureHandle color = renderer.activeColorTexture;
                builder.UseColorBuffer(color, 0);
                passData.cameraData = renderingData.cameraData;

                builder.SetRenderFunc((PassData data, RenderGraphContext rgContext) =>
                {
                    ExecutePass(rgContext.cmd, data.cameraData);
                });
            }
        }
    }

    private const string k_ShaderName = "Hidden/Universal Render Pipeline/DrawNormals";

    [SerializeField]
    private Shader m_Shader;

    private Material m_Material;
    private DrawNormalPass m_DrawNormalPass;

    /// <inheritdoc/>
    public override void Create()
    {
        m_DrawNormalPass = new DrawNormalPass();

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
        m_DrawNormalPass.Setup(m_Material);
        renderer.EnqueuePass(m_DrawNormalPass);
    }
}
