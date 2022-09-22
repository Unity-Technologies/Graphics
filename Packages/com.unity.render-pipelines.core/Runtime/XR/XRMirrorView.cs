using System;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering
{
    internal static class XRMirrorView
    {
        static readonly MaterialPropertyBlock s_MirrorViewMaterialProperty = new MaterialPropertyBlock();
        static readonly ProfilingSampler k_MirrorViewProfilingSampler = new ProfilingSampler("XR Mirror View");

        static readonly int k_SourceTex = Shader.PropertyToID("_SourceTex");
        static readonly int k_SourceTexArraySlice = Shader.PropertyToID("_SourceTexArraySlice");
        static readonly int k_ScaleBias = Shader.PropertyToID("_ScaleBias");
        static readonly int k_ScaleBiasRt = Shader.PropertyToID("_ScaleBiasRt");
        static readonly int k_SRGBRead = Shader.PropertyToID("_SRGBRead");
        static readonly int k_SRGBWrite = Shader.PropertyToID("_SRGBWrite");

#if ENABLE_VR && ENABLE_XR_MODULE
        internal static void RenderMirrorView(CommandBuffer cmd, Camera camera, Material mat, UnityEngine.XR.XRDisplaySubsystem display)
        {
            // XRTODO : remove this check when the Quest plugin is fixed
            if (Application.platform == RuntimePlatform.Android && !XRGraphicsAutomatedTests.running)
                return;

            if (display == null || !display.running || mat == null)
                return;

            int mirrorBlitMode = display.GetPreferredMirrorBlitMode();
            if (display.GetMirrorViewBlitDesc(null, out var blitDesc, mirrorBlitMode))
            {
                using (new ProfilingScope(cmd, k_MirrorViewProfilingSampler))
                {
                    cmd.SetRenderTarget(camera.targetTexture != null ? camera.targetTexture : new RenderTargetIdentifier(BuiltinRenderTextureType.CameraTarget));

                    if (blitDesc.nativeBlitAvailable)
                    {
                        display.AddGraphicsThreadMirrorViewBlit(cmd, blitDesc.nativeBlitInvalidStates, mirrorBlitMode);
                    }
                    else
                    {
                        for (int i = 0; i < blitDesc.blitParamsCount; ++i)
                        {
                            blitDesc.GetBlitParameter(i, out var blitParam);

                            Vector4 scaleBias = new Vector4(blitParam.srcRect.width, blitParam.srcRect.height, blitParam.srcRect.x, blitParam.srcRect.y);
                            Vector4 scaleBiasRt = new Vector4(blitParam.destRect.width, blitParam.destRect.height, blitParam.destRect.x, blitParam.destRect.y);

                            // Deal with y-flip
                            if (camera.targetTexture != null || camera.cameraType == CameraType.SceneView || camera.cameraType == CameraType.Preview)
                            {
                                scaleBias.y = -scaleBias.y;
                                scaleBias.w += blitParam.srcRect.height;
                            }

                            // Eye textures are always gamma corrected : use explicit sRGB read in shader only if the source is not using sRGB format.
                            s_MirrorViewMaterialProperty.SetFloat(k_SRGBRead, blitParam.srcTex.sRGB ? 0.0f : 1.0f);

                            // Perform explicit sRGB write in shader if color space is gamma
                            s_MirrorViewMaterialProperty.SetFloat(k_SRGBWrite, (QualitySettings.activeColorSpace == ColorSpace.Linear) ? 0.0f : 1.0f);

                            s_MirrorViewMaterialProperty.SetTexture(k_SourceTex, blitParam.srcTex);
                            s_MirrorViewMaterialProperty.SetVector(k_ScaleBias, scaleBias);
                            s_MirrorViewMaterialProperty.SetVector(k_ScaleBiasRt, scaleBiasRt);
                            s_MirrorViewMaterialProperty.SetFloat(k_SourceTexArraySlice, blitParam.srcTexArraySlice);

                            if (XRSystem.foveatedRenderingCaps.HasFlag(FoveatedRenderingCaps.NonUniformRaster) && blitParam.foveatedRenderingInfo != IntPtr.Zero)
                            {
                                cmd.ConfigureFoveatedRendering(blitParam.foveatedRenderingInfo);
                                cmd.EnableShaderKeyword("_FOVEATED_RENDERING_NON_UNIFORM_RASTER");
                            }

                            int shaderPass = (blitParam.srcTex.dimension == TextureDimension.Tex2DArray) ? 1 : 0;
                            cmd.DrawProcedural(Matrix4x4.identity, mat, shaderPass, MeshTopology.Quads, 4, 1, s_MirrorViewMaterialProperty);
                        }
                    }
                }
            }

            if (XRSystem.foveatedRenderingCaps.HasFlag(FoveatedRenderingCaps.NonUniformRaster))
            {
                cmd.DisableShaderKeyword("_FOVEATED_RENDERING_NON_UNIFORM_RASTER");
                cmd.ConfigureFoveatedRendering(IntPtr.Zero);
            }
        }

#endif
    }
}
