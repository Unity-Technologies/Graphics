using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition.Compositor
{
    // A custom clear pass that is used internally in the compositor. The functionality includes:
    // - Always clears the stencil buffer
    // - Clears the alpha channel if desired
    // - Clears the RGB channel to the color of a texture using a specified stretching mode
    [HideInInspector]
    internal class CustomClear : CustomPass
    {
        internal class ShaderIDs
        {
            public static readonly int k_BlitScaleBiasRt = Shader.PropertyToID("_BlitScaleBiasRt");
            public static readonly int k_BlitScaleBias = Shader.PropertyToID("_BlitScaleBias");
            public static readonly int k_BlitTexture = Shader.PropertyToID("_BlitTexture");
            public static readonly int k_ClearAlpha = Shader.PropertyToID("_ClearAlpha");
        }

        Material m_FullscreenPassMaterial;
        int m_ClearColorAndStencilPassIndex;
        int m_DrawTextureAndClearStencilPassIndex;

        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in an performance manner.
        protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            if (!HDRenderPipeline.isReady)
                return;

            var runtimeShaders = GraphicsSettings.GetRenderPipelineSettings<HDRenderPipelineRuntimeShaders>();

            // Setup code here
            if (string.IsNullOrEmpty(name)) name = "CustomClear";

            m_FullscreenPassMaterial = CoreUtils.CreateEngineMaterial(runtimeShaders.customClearPS);
            m_ClearColorAndStencilPassIndex = m_FullscreenPassMaterial.FindPass("ClearColorAndStencil");
            m_DrawTextureAndClearStencilPassIndex = m_FullscreenPassMaterial.FindPass("DrawTextureAndClearStencil");
        }

        protected override void Execute(CustomPassContext ctx)
        {
            // Executed every frame for all the camera inside the pass volume
            AdditionalCompositorData layerData = null;
            ctx.hdCamera.camera.gameObject.TryGetComponent<AdditionalCompositorData>(out layerData);
            if (layerData == null || layerData.clearColorTexture == null)
            {
                return;
            }
            else
            {
                float cameraAspectRatio = (float)ctx.hdCamera.actualWidth / ctx.hdCamera.actualHeight;
                float imageAspectRatio = (float)layerData.clearColorTexture.width / layerData.clearColorTexture.height;

                var scaleBiasRt = new Vector4(1.0f, 1.0f, 0.0f, 0.0f);
                if (layerData.imageFitMode == BackgroundFitMode.FitHorizontally)
                {
                    scaleBiasRt.y = cameraAspectRatio / imageAspectRatio;
                    scaleBiasRt.w = (1 - scaleBiasRt.y) / 2.0f;
                }
                else if (layerData.imageFitMode == BackgroundFitMode.FitVertically)
                {
                    scaleBiasRt.x = imageAspectRatio / cameraAspectRatio;
                    scaleBiasRt.z = (1 - scaleBiasRt.x) / 2.0f;
                }
                //else stretch (nothing to do)

                // The texture might not cover the entire screen (letter boxing), so in this case clear first to the background color (and stencil)
                if (scaleBiasRt.x < 1.0f || scaleBiasRt.y < 1.0f)
                {
                    m_FullscreenPassMaterial.SetVector(ShaderIDs.k_BlitScaleBiasRt, new Vector4(1.0f, 1.0f, 0.0f, 0.0f));
                    m_FullscreenPassMaterial.SetVector(ShaderIDs.k_BlitScaleBias, new Vector4(1.0f, 1.0f, 0.0f, 0.0f));
                    ctx.cmd.DrawProcedural(Matrix4x4.identity, m_FullscreenPassMaterial, m_ClearColorAndStencilPassIndex, MeshTopology.Quads, 4, 1);
                }

                m_FullscreenPassMaterial.SetTexture(ShaderIDs.k_BlitTexture, layerData.clearColorTexture);
                m_FullscreenPassMaterial.SetVector(ShaderIDs.k_BlitScaleBiasRt, scaleBiasRt);
                m_FullscreenPassMaterial.SetVector(ShaderIDs.k_BlitScaleBias, new Vector4(1.0f, 1.0f, 0.0f, 0.0f));
                m_FullscreenPassMaterial.SetInt(ShaderIDs.k_ClearAlpha, layerData.clearAlpha ? 1 : 0);

                // draw a quad (not Triangle), to support letter boxing and stretching
                ctx.cmd.DrawProcedural(Matrix4x4.identity, m_FullscreenPassMaterial, m_DrawTextureAndClearStencilPassIndex, MeshTopology.Quads, 4, 1);
            }
        }

        protected override void Cleanup()
        {
            // Cleanup code
            CoreUtils.Destroy(m_FullscreenPassMaterial);
        }
    }
}
