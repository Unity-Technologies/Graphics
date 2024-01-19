using System;
using UnityEngine.Rendering;

#if ENABLE_VR && ENABLE_XR_MODULE
using UnityEngine.XR;
#endif

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
        static readonly int k_MaxNits = Shader.PropertyToID("_MaxNits");
        static readonly int k_SourceMaxNits = Shader.PropertyToID("_SourceMaxNits");
        static readonly int k_SourceHDREncoding = Shader.PropertyToID("_SourceHDREncoding");
        static readonly int k_ColorTransform = Shader.PropertyToID("_ColorTransform");

#if ENABLE_VR && ENABLE_XR_MODULE
        internal static void RenderMirrorView(CommandBuffer cmd, Camera camera, Material mat, XRDisplaySubsystem display)
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

                            HDROutputSettings mainDisplayHdrSettings = HDROutputSettings.main;

                            // If we are writing to a HDR surface or reading from one we use the conversion shader to handle both
                            if (blitParam.srcHdrEncoded || mainDisplayHdrSettings.active)
                            {
                                ColorGamut mainDisplayColorGamut = mainDisplayHdrSettings.active ? mainDisplayHdrSettings.displayColorGamut
                                    : ColorGamut.sRGB;
                                ColorGamut xrDisplayColorGamut = blitParam.srcHdrEncoded ? blitParam.srcHdrColorGamut
                                    : ColorGamut.sRGB;

                                ColorPrimaries mainDisplayColorPrimaries = ColorGamutUtility.GetColorPrimaries(mainDisplayColorGamut);
                                ColorPrimaries xrDisplayColorPrimaries = ColorGamutUtility.GetColorPrimaries(xrDisplayColorGamut);

                                // Use the material? And use the passes?
                                HDROutputUtils.ConfigureHDROutput(s_MirrorViewMaterialProperty, mainDisplayColorGamut);
                                HDROutputUtils.ConfigureHDROutput(mat, HDROutputUtils.Operation.ColorConversion | HDROutputUtils.Operation.ColorEncoding);
                                int sourceHdrEncoding;
                                HDROutputUtils.GetColorEncodingForGamut(xrDisplayColorGamut, out sourceHdrEncoding);
                                s_MirrorViewMaterialProperty.SetInteger(k_SourceHDREncoding, sourceHdrEncoding);

                                Matrix4x4 sourceToRec2020 = Matrix4x4.identity;
                                sourceToRec2020.m33 = 0.0f; // 3x3 identity, not 4x4 identity.
                                if (xrDisplayColorPrimaries == ColorPrimaries.Rec709)
                                    sourceToRec2020 = ColorSpaceUtils.Rec709ToRec2020Mat;
                                else if (xrDisplayColorPrimaries == ColorPrimaries.P3)
                                    sourceToRec2020 = ColorSpaceUtils.P3D65ToRec2020Mat;

                                Matrix4x4 rec2020ToDest = Matrix4x4.identity;
                                rec2020ToDest.m33 = 0.0f; // 3x3 identity, not 4x4 identity.
                                if (mainDisplayColorPrimaries == ColorPrimaries.Rec709)
                                    rec2020ToDest = ColorSpaceUtils.Rec2020ToRec709Mat;
                                else if (mainDisplayColorPrimaries == ColorPrimaries.P3)
                                    rec2020ToDest = ColorSpaceUtils.Rec2020ToP3D65Mat;

                                Matrix4x4 m = sourceToRec2020 * rec2020ToDest;
                                s_MirrorViewMaterialProperty.SetMatrix(k_ColorTransform, m);

                                s_MirrorViewMaterialProperty.SetFloat(k_MaxNits, mainDisplayHdrSettings.active ? mainDisplayHdrSettings.maxToneMapLuminance : 160.0f);
                                s_MirrorViewMaterialProperty.SetFloat(k_SourceMaxNits, blitParam.srcHdrEncoded ? blitParam.srcHdrMaxLuminance : 160.0f);
                            }

                            // For 8888 formats we always gamma correct eye textures : use explicit sRGB read in shader only if the source is not using sRGB format.
                            bool manualSRGBRead = !blitParam.srcTex.sRGB &&
                                                  (blitParam.srcTex.graphicsFormat == GraphicsFormat.R8G8B8A8_UNorm ||
                                                   blitParam.srcTex.graphicsFormat == GraphicsFormat.B8G8R8A8_UNorm);
                            s_MirrorViewMaterialProperty.SetFloat(k_SRGBRead, manualSRGBRead ? 1.0f : 0.0f);

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
