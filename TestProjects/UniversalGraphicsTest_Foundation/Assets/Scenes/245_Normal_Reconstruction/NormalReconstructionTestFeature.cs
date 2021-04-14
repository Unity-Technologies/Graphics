using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

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

        private Material m_Material;
        private ProfilingSampler m_ProfilingSampler;

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
            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                NormalReconstruction.SetupProperties(cmd, renderingData.cameraData);

                int width = renderingData.cameraData.cameraTargetDescriptor.width;
                int height = renderingData.cameraData.cameraTargetDescriptor.height;

                Render(cmd, TapMode.Tap1, new Rect(0, 0, 0.5f, 0.5f), width, height);
                Render(cmd, TapMode.Tap3, new Rect(0.5f, 0, 0.5f, 0.5f), width, height);
                Render(cmd, TapMode.Tap5, new Rect(0, 0.5f, 0.5f, 0.5f), width, height);
                Render(cmd, TapMode.Tap9, new Rect(0.5f, 0.5f, 0.5f, 0.5f), width, height);
            }

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            CommandBufferPool.Release(cmd);
        }

        private void Render(CommandBuffer cmd, TapMode tapMode, Rect viewport, int width, int height)
        {
            CoreUtils.SetKeyword(cmd, "_DRAW_NORMALS_TAP1", tapMode == TapMode.Tap1);
            CoreUtils.SetKeyword(cmd, "_DRAW_NORMALS_TAP3", tapMode == TapMode.Tap3);
            CoreUtils.SetKeyword(cmd, "_DRAW_NORMALS_TAP5", tapMode == TapMode.Tap5);
            CoreUtils.SetKeyword(cmd, "_DRAW_NORMALS_TAP9", tapMode == TapMode.Tap9);

            cmd.SetGlobalVector(ShaderPropertyId.scaleBias, new Vector4(1f / viewport.width, 1f / viewport.height, width * -viewport.x * 2, height * -viewport.y * 2));
            cmd.SetViewport(new Rect(width * viewport.x, height * viewport.y, width * viewport.width, height * viewport.height));
            cmd.DrawProcedural(Matrix4x4.identity, m_Material, 0, MeshTopology.Quads, 4, 1, null);
        }
    }

    [SerializeField]
    private Shader m_Shader;

    private Material m_Material;
    private DrawNormalPass m_DrawNormalPass;

    /// <inheritdoc/>
    public override void Create()
    {
        m_Material = CoreUtils.CreateEngineMaterial(m_Shader);
        m_DrawNormalPass = new DrawNormalPass();
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        m_DrawNormalPass.Setup(m_Material);
        renderer.EnqueuePass(m_DrawNormalPass);
    }
}
