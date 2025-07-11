using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
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
            ConfigureInput(ScriptableRenderPassInput.Depth);
            renderPassEvent = RenderPassEvent.AfterRenderingSkybox;
        }

        public void Setup(Material material)
        {
            m_Material = material;
        }

#if URP_COMPATIBILITY_MODE
        [Obsolete("This rendering path is for compatibility mode only (when Render Graph is disabled). Use Render Graph API instead.", false)]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get();

            ExecutePass(CommandBufferHelpers.GetRasterCommandBuffer(cmd), renderingData.cameraData);

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            CommandBufferPool.Release(cmd);
        }
#endif

        static void ExecutePass(RasterCommandBuffer cmd, in CameraData cameraData)
        {
            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                NormalReconstruction.SetupProperties(cmd, cameraData);

                int width = cameraData.cameraTargetDescriptor.width;
                int height = cameraData.cameraTargetDescriptor.height;
                var world = cameraData.camera.worldToCameraMatrix;
                var proj = cameraData.camera.projectionMatrix;
                ExecutePass(cmd, world, proj, width, height);
            }
        }

        static void ExecutePass(RasterCommandBuffer cmd, UniversalCameraData cameraData)
        {
            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                NormalReconstruction.SetupProperties(cmd, cameraData);

                int width = cameraData.cameraTargetDescriptor.width;
                int height = cameraData.cameraTargetDescriptor.height;
                var world = cameraData.camera.worldToCameraMatrix;
                var proj = cameraData.camera.projectionMatrix;
                ExecutePass(cmd, world, proj, width, height);
            }
        }
        static void ExecutePass(RasterCommandBuffer cmd, Matrix4x4 world, Matrix4x4 proj, int width, int height)
        {
            Render(cmd, world, proj, TapMode.Tap1, new Rect(0, 0, 0.5f, 0.5f), width, height);
            Render(cmd, world, proj, TapMode.Tap3, new Rect(0.5f, 0, 0.5f, 0.5f), width, height);
            Render(cmd, world, proj, TapMode.Tap5, new Rect(0, 0.5f, 0.5f, 0.5f), width, height);
            Render(cmd, world, proj, TapMode.Tap9, new Rect(0.5f, 0.5f, 0.5f, 0.5f), width, height);
        }

        static void Render(RasterCommandBuffer cmd, Matrix4x4 world, Matrix4x4 proj, TapMode tapMode, Rect viewport, int width, int height)
        {
            CoreUtils.SetKeyword(cmd, "_DRAW_NORMALS_TAP1", tapMode == TapMode.Tap1);
            CoreUtils.SetKeyword(cmd, "_DRAW_NORMALS_TAP3", tapMode == TapMode.Tap3);
            CoreUtils.SetKeyword(cmd, "_DRAW_NORMALS_TAP5", tapMode == TapMode.Tap5);
            CoreUtils.SetKeyword(cmd, "_DRAW_NORMALS_TAP9", tapMode == TapMode.Tap9);

            // Remove test case scaling as it adds noise.
            //cmd.SetGlobalVector(ShaderPropertyId.scaleBias, new Vector4(1f / viewport.width, 1f / viewport.height, width * -viewport.x * 2, height * -viewport.y * 2));
            cmd.SetGlobalVector(ShaderPropertyId.scaleBias, new Vector4(1, 1, 0, 0));
            cmd.SetViewport(new Rect(width * viewport.x, height * viewport.y, width * viewport.width, height * viewport.height));
            Blitter.BlitTexture(cmd,  Vector2.one, m_Material, 0);
            cmd.SetViewProjectionMatrices(world, proj);
        }

        internal class PassData
        {
            internal UniversalCameraData cameraData;
            internal TextureHandle color;
        }
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Normal Reconstruction Test Pass", out var passData, m_ProfilingSampler))
            {
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

                TextureHandle color = resourceData.activeColorTexture;
                passData.color = color;
                builder.SetRenderAttachment(color, 0, AccessFlags.Write);
                passData.cameraData = cameraData;
                builder.AllowGlobalStateModification(true);

                builder.UseTexture(resourceData.cameraDepthTexture);

                builder.SetRenderFunc((PassData data, RasterGraphContext rgContext) =>
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
