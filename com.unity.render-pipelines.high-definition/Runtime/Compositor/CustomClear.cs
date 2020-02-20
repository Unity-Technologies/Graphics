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
    internal class CustomClear : CustomPass
    {
        internal class ShaderIDs
        {
            public static readonly int _BlitScaleBiasRt = Shader.PropertyToID("_BlitScaleBiasRt");
            public static readonly int _BlitScaleBias = Shader.PropertyToID("_BlitScaleBias");
            public static readonly int _BlitTexture = Shader.PropertyToID("_BlitTexture");
            public static readonly int _ClearAlpha = Shader.PropertyToID("_ClearAlpha");
        }

        enum PassType
        {
            ClearColorAndStencil = 0,
            DrawTextureAndClearStencil = 1
        };
        Material fullscreenPassMaterial;

        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in an performance manner.
        protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
        {
            // Setup code here
            if (name == "") name = "CustomClear";

            fullscreenPassMaterial = CoreUtils.CreateEngineMaterial("Hidden/HDRP/CustomClear");
        }

        protected override void Execute(ScriptableRenderContext renderContext, CommandBuffer cmd, HDCamera camera, CullingResults cullingResult)
        {
            // Executed every frame for all the camera inside the pass volume
            AdditionalCompositorData layerData = camera.camera.gameObject.GetComponent<AdditionalCompositorData>();
            if (layerData == null || layerData.m_clearColorTexture == false)
            {
                return;
            }
            else
            {
                float cameraAspectRatio = (float)camera.actualWidth / camera.actualHeight;
                float imageAspectRatio = (float)layerData.m_clearColorTexture.width / layerData.m_clearColorTexture.height;

                var scaleBiasRt = new Vector4(1.0f, 1.0f, 0.0f, 0.0f);
                if (layerData.m_imageFitMode == BackgroundFitMode.FitHorizontally)
                {
                    scaleBiasRt.y = cameraAspectRatio / imageAspectRatio;
                    scaleBiasRt.w = (1 - scaleBiasRt.y) / 2.0f;
                }
                else if (layerData.m_imageFitMode == BackgroundFitMode.FitVertically)
                {
                    scaleBiasRt.x = imageAspectRatio / cameraAspectRatio;
                    scaleBiasRt.z = (1 - scaleBiasRt.x) / 2.0f;
                }
                //else stretch (nothing to do)

                // The texture might not cover the entire screen (letter boxing), so in this case clear first to the background color (and stencil)
                if (scaleBiasRt.x < 1.0f || scaleBiasRt.y < 1.0f)
                {
                    fullscreenPassMaterial.SetVector(ShaderIDs._BlitScaleBiasRt, new Vector4(1.0f, 1.0f, 0.0f, 0.0f));
                    fullscreenPassMaterial.SetVector(ShaderIDs._BlitScaleBias, new Vector4(1.0f, 1.0f, 0.0f, 0.0f));
                    cmd.DrawProcedural(Matrix4x4.identity, fullscreenPassMaterial, (int)PassType.ClearColorAndStencil, MeshTopology.Quads, 4, 1);
                }

                fullscreenPassMaterial.SetTexture(ShaderIDs._BlitTexture, layerData.m_clearColorTexture);
                fullscreenPassMaterial.SetVector(ShaderIDs._BlitScaleBiasRt, scaleBiasRt);
                fullscreenPassMaterial.SetVector(ShaderIDs._BlitScaleBias, new Vector4(1.0f, 1.0f, 0.0f, 0.0f));
                fullscreenPassMaterial.SetInt(ShaderIDs._ClearAlpha, layerData.m_clearAlpha ? 1 : 0);

                // draw a quad (not Triangle), to support letter boxing and stretching 
                cmd.DrawProcedural(Matrix4x4.identity, fullscreenPassMaterial, (int)PassType.DrawTextureAndClearStencil, MeshTopology.Quads, 4, 1);
            }
        }

        protected override void Cleanup()
        {
            // Cleanup code
            CoreUtils.Destroy(fullscreenPassMaterial);
        }
    }
}
