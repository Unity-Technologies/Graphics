using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal
{
    internal static class SSAOUtils
    {
        // Constants
        internal const string k_SSAOTextureName = "_ScreenSpaceOcclusionTexture";

        // Shared Shader Property IDs
        internal static class ShaderConstants
        {
            public static readonly int _AmbientOcclusionParam = Shader.PropertyToID("_AmbientOcclusionParam");
            public static readonly int _SSAOParams = Shader.PropertyToID("_SSAOParams");
            public static readonly int _SSAOBlueNoiseParams = Shader.PropertyToID("_SSAOBlueNoiseParams");
            public static readonly int _BlueNoiseTexture = Shader.PropertyToID("_BlueNoiseTexture");
            public static readonly int _SSAOFinalTexture = Shader.PropertyToID(k_SSAOTextureName);
            public static readonly int _CameraViewXExtent = Shader.PropertyToID("_CameraViewXExtent");
            public static readonly int _CameraViewYExtent = Shader.PropertyToID("_CameraViewYExtent");
            public static readonly int _CameraViewZExtent = Shader.PropertyToID("_CameraViewZExtent");
            public static readonly int _ProjectionParams2 = Shader.PropertyToID("_ProjectionParams2");
            public static readonly int _CameraViewProjections = Shader.PropertyToID("_CameraViewProjections");
            public static readonly int _CameraViewTopLeftCorner = Shader.PropertyToID("_CameraViewTopLeftCorner");
            public static readonly int _CameraNormalsTexture = Shader.PropertyToID("_CameraNormalsTexture");
            public static readonly int _BlitScaleBias = Shader.PropertyToID("_BlitScaleBias");
            public static readonly int _BlitTexture = Shader.PropertyToID("_BlitTexture");
            public static readonly int _SourceSize = Shader.PropertyToID("_SourceSize");

#if MODERN_SSAO
            public static readonly int _SSAOParams2 = Shader.PropertyToID("_SSAOParams2");
            public static readonly int _AODepthToViewParams = Shader.PropertyToID("_AODepthToViewParams");
#endif
        }

        // Enums
        internal enum BlurTypes
        {
            Bilateral,
            Gaussian,
            Kawase,
        }

        internal enum ShaderPasses
        {
            AmbientOcclusion = 0,

            BilateralBlurHorizontal = 1,
            BilateralBlurVertical = 2,
            BilateralBlurFinal = 3,
            BilateralAfterOpaque = 4,

            GaussianBlurHorizontal = 5,
            GaussianBlurVertical = 6,
            GaussianAfterOpaque = 7,

            KawaseBlur = 8,
            KawaseAfterOpaque = 9,
        }

        // Camera view data aggregated into a struct to avoid scattered arrays.
        internal struct CameraViewData
        {
            public Vector4[] topLeftCorner;
            public Vector4[] xExtent;
            public Vector4[] yExtent;
            public Vector4[] zExtent;
            public Matrix4x4[] viewProjections;

            public static CameraViewData Create()
            {
                return new CameraViewData
                {
                    topLeftCorner = new Vector4[2],
                    xExtent = new Vector4[2],
                    yExtent = new Vector4[2],
                    zExtent = new Vector4[2],
                    viewProjections = new Matrix4x4[2],
                };
            }
        }

        // Structs
        internal readonly struct SSAOMaterialParams
        {
            internal readonly bool orthographicCamera;
            internal readonly bool aoBlueNoise;
            internal readonly bool aoInterleavedGradient;
            internal readonly bool sampleCountHigh;
            internal readonly bool sampleCountMedium;
            internal readonly bool sampleCountLow;
            internal readonly bool sourceDepthNormals;
            internal readonly bool sourceDepthHigh;
            internal readonly bool sourceDepthMedium;
            internal readonly bool sourceDepthLow;
            internal readonly Vector4 ssaoParams;
#if MODERN_SSAO
            internal readonly Vector4 ssaoParams2;
            internal readonly Vector4 depthToViewParams;
            internal readonly bool isGTAOMode;
#endif

            internal SSAOMaterialParams(ScreenSpaceAmbientOcclusionSettings settings, UniversalCameraData cameraData, in TextureDesc cameraColorDesc)
            {
                float radius = CalculateRadius(settings);
                orthographicCamera = cameraData.camera.orthographic;
                sampleCountHigh = settings.Samples == ScreenSpaceAmbientOcclusionSettings.AOSampleOption.High;
                sampleCountMedium = settings.Samples == ScreenSpaceAmbientOcclusionSettings.AOSampleOption.Medium;
                sampleCountLow = settings.Samples == ScreenSpaceAmbientOcclusionSettings.AOSampleOption.Low;

#if MODERN_SSAO
                isGTAOMode = settings.Mode != ScreenSpaceAmbientOcclusionMode.Standard;

                if (isGTAOMode)
                {
                    // GTAO always uses depth normals
                    sourceDepthNormals = true;

                    aoBlueNoise = settings.AOMethod == ScreenSpaceAmbientOcclusionSettings.AOMethodOptions.BlueNoise;
                    aoInterleavedGradient = settings.AOMethod == ScreenSpaceAmbientOcclusionSettings.AOMethodOptions.InterleavedGradient;
                    sourceDepthHigh = false;
                    sourceDepthMedium = false;
                    sourceDepthLow = false;

                    GTAOPass.CalculateGTAOViewParams(settings, cameraData, orthographicCamera, radius, cameraColorDesc, out ssaoParams2, out depthToViewParams);
                }
                else
                {
#endif
                    // Standard mode
                    bool isUsingDepthNormals = settings.Source == ScreenSpaceAmbientOcclusionSettings.DepthSource.DepthNormals;
                    aoBlueNoise = settings.AOMethod == ScreenSpaceAmbientOcclusionSettings.AOMethodOptions.BlueNoise;
                    aoInterleavedGradient = settings.AOMethod == ScreenSpaceAmbientOcclusionSettings.AOMethodOptions.InterleavedGradient;
                    sourceDepthNormals = settings.Source == ScreenSpaceAmbientOcclusionSettings.DepthSource.DepthNormals;
                    sourceDepthHigh = !isUsingDepthNormals && settings.NormalSamples == ScreenSpaceAmbientOcclusionSettings.NormalQuality.High;
                    sourceDepthMedium = !isUsingDepthNormals && settings.NormalSamples == ScreenSpaceAmbientOcclusionSettings.NormalQuality.Medium;
                    sourceDepthLow = !isUsingDepthNormals && settings.NormalSamples == ScreenSpaceAmbientOcclusionSettings.NormalQuality.Low;
#if MODERN_SSAO
                    ssaoParams2 = Vector4.zero;
                    depthToViewParams = Vector4.zero;
                }
#endif
                ssaoParams = CalculateCommonParams(settings, radius);
            }

            internal bool Equals(in SSAOMaterialParams other)
            {
                return orthographicCamera == other.orthographicCamera
                       && aoBlueNoise == other.aoBlueNoise
                       && aoInterleavedGradient == other.aoInterleavedGradient
                       && sampleCountHigh == other.sampleCountHigh
                       && sampleCountMedium == other.sampleCountMedium
                       && sampleCountLow == other.sampleCountLow
                       && sourceDepthNormals == other.sourceDepthNormals
                       && sourceDepthHigh == other.sourceDepthHigh
                       && sourceDepthMedium == other.sourceDepthMedium
                       && sourceDepthLow == other.sourceDepthLow
                       && ssaoParams == other.ssaoParams
#if MODERN_SSAO
                       && ssaoParams2 == other.ssaoParams2
                       && depthToViewParams == other.depthToViewParams
                       && isGTAOMode == other.isGTAOMode
#endif
                    ;
            }
        }

        // ---- Static Utility Methods ----

        internal static float CalculateRadius(ScreenSpaceAmbientOcclusionSettings settings)
        {
            float radiusMultiplier = settings.AOMethod == ScreenSpaceAmbientOcclusionSettings.AOMethodOptions.BlueNoise ? 1.5f : 1.0f;
            return settings.Radius * radiusMultiplier;
        }

        internal static Vector4 CalculateCommonParams(ScreenSpaceAmbientOcclusionSettings settings, float radius)
        {
            return new Vector4(settings.Intensity, radius, 1.0f / (settings.Downsample ? 2 : 1), settings.Falloff);
        }

        internal static Vector4 CalculateProjectionParams2(UniversalCameraData cameraData)
        {
            return new Vector4(1.0f / cameraData.camera.nearClipPlane, 0.0f, 0.0f, 0.0f);
        }

        internal static Vector2 GetR2Offset()
        {
            // R2 sequence from https://extremelearning.com.au/unreasonable-effectiveness-of-quasirandom-sequences/
            // x_n = (n * (1/phi_2)) mod 1, y_n = (n * (1/phi_2^2)) mod 1
            //
            // Use uint multiplication so wrap-around handles "mod 1" exactly,
            // regardless of how large Time.frameCount grows. Constants are the
            // irrational R2 ratios scaled by 2^32 and rounded:
            //   round(2^32 * 0.7548776662466927) = 3242174889
            //   round(2^32 * 0.5698402909980532) = 2447445413
            const uint k_R2SequenceX = 3242174889u;
            const uint k_R2SequenceY = 2447445413u;
            const float k_InvScale = 1.0f / 4294967296.0f; // 1 / 2^32

            uint sampleIndex = (uint)(Time.frameCount + 1);
            uint offsetX = sampleIndex * k_R2SequenceX; // wraps mod 2^32 ⇔ mod 1
            uint offsetY = sampleIndex * k_R2SequenceY;
            return new Vector2(offsetX * k_InvScale, offsetY * k_InvScale);
        }

        internal static Vector4 CalculateBlueNoiseParams(UniversalCameraData cameraData, Texture2D noiseTexture)
        {
#if UNITY_INCLUDE_TESTS
            return new Vector4(
                cameraData.pixelWidth / (float)noiseTexture.width,
                cameraData.pixelHeight / (float)noiseTexture.height,
                1.0f,
                1.0f);
#else
            Vector2 blueNoiseOffset = GetR2Offset();
            return new Vector4(
                cameraData.pixelWidth / (float)noiseTexture.width,
                cameraData.pixelHeight / (float)noiseTexture.height,
                blueNoiseOffset.x,
                blueNoiseOffset.y);
#endif
        }

        internal static Texture2D GetBlueNoiseTexture(Texture2D[] blueNoiseTextures, int index)
        {
            if (blueNoiseTextures == null || blueNoiseTextures.Length == 0)
                return null;

            return blueNoiseTextures[index % blueNoiseTextures.Length];
        }

        internal static int AdvanceBlueNoiseIndex(ScreenSpaceAmbientOcclusionSettings settings, Texture2D[] blueNoiseTextures, int currentIndex)
        {
            if (settings.AOMethod != ScreenSpaceAmbientOcclusionSettings.AOMethodOptions.BlueNoise ||
                blueNoiseTextures == null || blueNoiseTextures.Length == 0)
            {
                return currentIndex;
            }

#if UNITY_INCLUDE_TESTS
            return 0;
#else
            return (currentIndex + 1) % blueNoiseTextures.Length;
#endif
        }

        internal static BlurTypes GetBlurType(ScreenSpaceAmbientOcclusionSettings.BlurQualityOptions quality)
        {
            return quality switch
            {
                ScreenSpaceAmbientOcclusionSettings.BlurQualityOptions.High => BlurTypes.Bilateral,
                ScreenSpaceAmbientOcclusionSettings.BlurQualityOptions.Medium => BlurTypes.Gaussian,
                ScreenSpaceAmbientOcclusionSettings.BlurQualityOptions.Low => BlurTypes.Kawase,
                _ => throw new ArgumentOutOfRangeException(nameof(quality))
            };
        }

        internal static void SetupCameraViewMatrices(UniversalCameraData cameraData, ref CameraViewData viewData)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            int eyeCount = cameraData.xr.enabled && cameraData.xr.singlePassEnabled ? 2 : 1;
#else
            int eyeCount = 1;
#endif

            for (int eyeIndex = 0; eyeIndex < eyeCount; eyeIndex++)
            {
                Matrix4x4 view = cameraData.GetViewMatrix(eyeIndex);
                Matrix4x4 proj = cameraData.GetProjectionMatrix(eyeIndex);
                viewData.viewProjections[eyeIndex] = proj * view;

                // camera view space without translation, used by SSAO.hlsl ReconstructViewPos() to calculate view vector.
                Matrix4x4 camView = view;
                camView.SetColumn(3, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
                Matrix4x4 camViewProj = proj * camView;
                Matrix4x4 camViewProjInv = camViewProj.inverse;

                Vector4 topLeftCorner = camViewProjInv.MultiplyPoint(new Vector4(-1, 1, -1, 1));
                Vector4 topRightCorner = camViewProjInv.MultiplyPoint(new Vector4(1, 1, -1, 1));
                Vector4 bottomLeftCorner = camViewProjInv.MultiplyPoint(new Vector4(-1, -1, -1, 1));
                Vector4 farCentre = camViewProjInv.MultiplyPoint(new Vector4(0, 0, 1, 1));
                viewData.topLeftCorner[eyeIndex] = topLeftCorner;
                viewData.xExtent[eyeIndex] = topRightCorner - topLeftCorner;
                viewData.yExtent[eyeIndex] = bottomLeftCorner - topLeftCorner;
                viewData.zExtent[eyeIndex] = farCentre;
            }
        }

        internal static bool IsYFlip(RasterGraphContext ctx, in TextureHandle srcTexture, in TextureHandle dstTexture)
        {
            return ctx.GetTextureUVOrigin(srcTexture) != ctx.GetTextureUVOrigin(dstTexture);
        }

        internal static Vector4 ComputeScaleBias(in TextureHandle source, bool yFlip)
        {
            RTHandle srcRTHandle = source;
            Vector2 viewportScale;
            if (srcRTHandle is { useScaling: true })
            {
                var scale = srcRTHandle.rtHandleProperties.rtHandleScale;
                viewportScale.x = scale.x;
                viewportScale.y = scale.y;
            }
            else
            {
                viewportScale = Vector2.one;
            }

            if (yFlip)
                return new Vector4(viewportScale.x, -viewportScale.y, 0, viewportScale.y);
            else
                return new Vector4(viewportScale.x, viewportScale.y, 0, 0);
        }

        // ---- Shared Raster Recording Methods ----

        internal static void SetupKeywordsAndParameters(
            Material material,
            ScreenSpaceAmbientOcclusionSettings settings,
            UniversalCameraData cameraData,
            in TextureDesc cameraColorDesc,
            ref CameraViewData viewData,
            Texture2D[] blueNoiseTextures,
            int blueNoiseTextureIndex,
            ref SSAOMaterialParams prevParams)
        {
            material.SetVector(ShaderConstants._ProjectionParams2, CalculateProjectionParams2(cameraData));
            material.SetMatrixArray(ShaderConstants._CameraViewProjections, viewData.viewProjections);
            material.SetVectorArray(ShaderConstants._CameraViewTopLeftCorner, viewData.topLeftCorner);
            material.SetVectorArray(ShaderConstants._CameraViewXExtent, viewData.xExtent);
            material.SetVectorArray(ShaderConstants._CameraViewYExtent, viewData.yExtent);
            material.SetVectorArray(ShaderConstants._CameraViewZExtent, viewData.zExtent);

            if (settings.AOMethod == ScreenSpaceAmbientOcclusionSettings.AOMethodOptions.BlueNoise)
            {
                Texture2D noiseTexture = GetBlueNoiseTexture(blueNoiseTextures, blueNoiseTextureIndex);
                Debug.Assert(noiseTexture != null, "Blue noise texture is null. Blue noise mode requires a valid blue noise texture.");
                Vector4 blueNoiseParams = CalculateBlueNoiseParams(cameraData, noiseTexture);

                material.SetTexture(ShaderConstants._BlueNoiseTexture, noiseTexture);
                material.SetVector(ShaderConstants._SSAOBlueNoiseParams, blueNoiseParams);
            }
            else
            {
                material.SetVector(ShaderConstants._SSAOBlueNoiseParams, Vector4.zero);
            }

            // Setting keywords can be somewhat expensive on low-end platforms.
            // Previous params are cached to avoid setting the same keywords every frame.
            SSAOMaterialParams matParams = new SSAOMaterialParams(settings, cameraData, cameraColorDesc);
            bool ssaoParamsDirty = !prevParams.Equals(in matParams);
            bool isParamsPropertySet = material.HasProperty(ShaderConstants._SSAOParams);
            if (!ssaoParamsDirty && isParamsPropertySet)
                return;

            prevParams = matParams;

            CoreUtils.SetKeyword(material, ScreenSpaceAmbientOcclusionKeywords.k_SampleCountHighKeyword,       matParams.sampleCountHigh);
            CoreUtils.SetKeyword(material, ScreenSpaceAmbientOcclusionKeywords.k_SampleCountMediumKeyword,     matParams.sampleCountMedium);
            CoreUtils.SetKeyword(material, ScreenSpaceAmbientOcclusionKeywords.k_SampleCountLowKeyword,        matParams.sampleCountLow);
            CoreUtils.SetKeyword(material, ScreenSpaceAmbientOcclusionKeywords.k_SourceDepthNormalsKeyword,    matParams.sourceDepthNormals);
            CoreUtils.SetKeyword(material, ScreenSpaceAmbientOcclusionKeywords.k_SourceDepthHighKeyword,       matParams.sourceDepthHigh);
            CoreUtils.SetKeyword(material, ScreenSpaceAmbientOcclusionKeywords.k_SourceDepthMediumKeyword,     matParams.sourceDepthMedium);
            CoreUtils.SetKeyword(material, ScreenSpaceAmbientOcclusionKeywords.k_SourceDepthLowKeyword,        matParams.sourceDepthLow);
            CoreUtils.SetKeyword(material, ScreenSpaceAmbientOcclusionKeywords.k_OrthographicCameraKeyword,    matParams.orthographicCamera);
            CoreUtils.SetKeyword(material, ScreenSpaceAmbientOcclusionKeywords.k_AOBlueNoiseKeyword,           matParams.aoBlueNoise);
            CoreUtils.SetKeyword(material, ScreenSpaceAmbientOcclusionKeywords.k_AOInterleavedGradientKeyword, matParams.aoInterleavedGradient);
            material.SetVector(ShaderConstants._SSAOParams, matParams.ssaoParams);
#if MODERN_SSAO
            CoreUtils.SetKeyword(material, ScreenSpaceAmbientOcclusionKeywords.k_GTAOModeKeyword, matParams.isGTAOMode);
            if (matParams.isGTAOMode)
            {
                material.SetVector(ShaderConstants._SSAOParams2, matParams.ssaoParams2);
                material.SetVector(ShaderConstants._AODepthToViewParams, matParams.depthToViewParams);
            }
#endif
        }

        // Pass data classes for shared raster recording
        internal class RasterAOPassData
        {
            internal bool afterOpaque;
            internal ScreenSpaceAmbientOcclusionSettings.BlurQualityOptions blurQuality;
            internal Material material;
            internal float directLightingStrength;
            internal Vector4 sourceSize;
            internal TextureHandle aoTexture;
            internal TextureHandle finalTexture;
            internal TextureHandle blurTexture;
            internal TextureHandle cameraNormalsTexture;
        }

        internal class BlurPassData
        {
            internal TextureHandle srcTexture;
            internal TextureHandle dstTexture;
            internal Material material;
            internal UniversalCameraData cameraData;
            internal int pass;
            internal ScreenSpaceAmbientOcclusionSettings.BlurQualityOptions blurQuality;
            internal bool afterOpaque;
        }

        internal class CleanupPassData
        {
            internal float directLightingStrength;
        }

        internal static void RecordRasterAOPass(
            RenderGraph renderGraph,
            ProfilingSampler profilingSampler,
            Material material,
            ScreenSpaceAmbientOcclusionSettings settings,
            TextureHandle aoTexture,
            TextureHandle cameraDepthTexture,
            TextureHandle cameraNormalsTexture,
            in TextureDesc cameraColorDesc)
        {
            using (var builder = renderGraph.AddRasterRenderPass<RasterAOPassData>("Blit SSAO", out var passData, profilingSampler))
            {
                builder.AllowGlobalStateModification(true);

                passData.material = material;
                passData.blurQuality = settings.BlurQuality;
                passData.afterOpaque = settings.AfterOpaque;
                passData.directLightingStrength = settings.DirectLightingStrength;
                passData.sourceSize = PostProcessUtils.CalcShaderSourceSize(cameraColorDesc.width, cameraColorDesc.height, cameraColorDesc.useDynamicScale);
                passData.aoTexture = aoTexture;

                builder.SetRenderAttachment(passData.aoTexture, 0, AccessFlags.WriteAll);

                Debug.Assert(cameraDepthTexture.IsValid(), "Camera depth texture is invalid. SSAO raster AO pass requires a depth texture.");
                builder.UseTexture(cameraDepthTexture, AccessFlags.Read);

                if (settings.Source == ScreenSpaceAmbientOcclusionSettings.DepthSource.DepthNormals)
                {
                    Debug.Assert(cameraNormalsTexture.IsValid(), "Camera normals texture is invalid. SSAO raster AO pass requires a normals texture when Source is DepthNormals.");
                    builder.UseTexture(cameraNormalsTexture, AccessFlags.Read);
                    passData.cameraNormalsTexture = cameraNormalsTexture;
                }

                builder.SetRenderFunc(static (RasterAOPassData data, RasterGraphContext ctx) =>
                {
                    ctx.cmd.SetGlobalVector(ShaderConstants._SourceSize, data.sourceSize);

                    if (data.cameraNormalsTexture.IsValid())
                        data.material.SetTexture(ShaderConstants._CameraNormalsTexture, data.cameraNormalsTexture);

                    Vector4 viewScaleBias = new(1, 1, 0, 0);
                    Blitter.BlitTexture(ctx.cmd, viewScaleBias, data.material, (int)ShaderPasses.AmbientOcclusion);
                });
            }
        }

        internal static void RecordBlurStep(
            RenderGraph renderGraph,
            ProfilingSampler profilingSampler,
            Material material,
            ScreenSpaceAmbientOcclusionSettings settings,
            UniversalCameraData cameraData,
            string blurPassName,
            in TextureHandle src,
            in TextureHandle dst,
            int pass,
            bool isLastPass)
        {
            using (var builder = renderGraph.AddRasterRenderPass<BlurPassData>(blurPassName, out var passData, profilingSampler))
            {
                passData.material = material;
                passData.blurQuality = settings.BlurQuality;
                passData.afterOpaque = settings.AfterOpaque;
                passData.srcTexture = src;
                passData.dstTexture = dst;
                passData.cameraData = cameraData;
                passData.pass = pass;

                builder.UseTexture(passData.srcTexture);

                AccessFlags finalDstAccess = passData.afterOpaque && isLastPass ? AccessFlags.Write : AccessFlags.WriteAll;
                builder.SetRenderAttachment(passData.dstTexture, 0, finalDstAccess);

                builder.SetRenderFunc(static (BlurPassData data, RasterGraphContext ctx) =>
                {
                    Vector4 viewScaleBias = ComputeScaleBias(data.srcTexture, IsYFlip(ctx, in data.srcTexture, in data.dstTexture));
                    Blitter.BlitTexture(ctx.cmd, data.srcTexture, viewScaleBias, data.material, data.pass);
                });
            }
        }

        internal static void RecordBlurChain(
            RenderGraph renderGraph,
            ProfilingSampler profilingSampler,
            Material material,
            ScreenSpaceAmbientOcclusionSettings settings,
            UniversalCameraData cameraData,
            TextureHandle aoTexture,
            TextureHandle blurTexture,
            TextureHandle finalTexture)
        {
            switch (settings.BlurQuality)
            {
                case ScreenSpaceAmbientOcclusionSettings.BlurQualityOptions.High:
                    RecordBlurStep(renderGraph, profilingSampler, material, settings, cameraData, "Blur SSAO Horizontal (High)", aoTexture, blurTexture, (int)ShaderPasses.BilateralBlurHorizontal, false);
                    RecordBlurStep(renderGraph, profilingSampler, material, settings, cameraData, "Blur SSAO Vertical (High)", blurTexture, aoTexture, (int)ShaderPasses.BilateralBlurVertical, false);
                    RecordBlurStep(renderGraph, profilingSampler, material, settings, cameraData, "Blur SSAO Final (High)", aoTexture, finalTexture, (int)(settings.AfterOpaque ? ShaderPasses.BilateralAfterOpaque : ShaderPasses.BilateralBlurFinal), true);
                    break;
                case ScreenSpaceAmbientOcclusionSettings.BlurQualityOptions.Medium:
                    RecordBlurStep(renderGraph, profilingSampler, material, settings, cameraData, "Blur SSAO Horizontal (Medium)", aoTexture, blurTexture, (int)ShaderPasses.GaussianBlurHorizontal, false);
                    RecordBlurStep(renderGraph, profilingSampler, material, settings, cameraData, "Blur SSAO Final (Medium)", blurTexture, finalTexture, (int)(settings.AfterOpaque ? ShaderPasses.GaussianAfterOpaque : ShaderPasses.GaussianBlurVertical), true);
                    break;
                case ScreenSpaceAmbientOcclusionSettings.BlurQualityOptions.Low:
                    RecordBlurStep(renderGraph, profilingSampler, material, settings, cameraData, "Blur SSAO (Low)", aoTexture, finalTexture, (int)(settings.AfterOpaque ? ShaderPasses.KawaseAfterOpaque : ShaderPasses.KawaseBlur), true);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        internal static void RecordCleanupPass(
            RenderGraph renderGraph,
            ProfilingSampler profilingSampler,
            float directLightingStrength,
            TextureHandle finalTexture)
        {
            // Add cleanup pass to:
            // - Set global keywords for next passes
            // - Set global texture as there is a limitation in Render Graph where an input texture cannot be set as a global texture after the pass runs
            // A Raster pass is used so it can be merged easily with the blur passes.
            using (var builder = renderGraph.AddRasterRenderPass<CleanupPassData>("Cleanup SSAO", out var passData, profilingSampler))
            {
                passData.directLightingStrength = directLightingStrength;

                builder.AllowGlobalStateModification(true);

                builder.UseTexture(finalTexture, AccessFlags.Read);
                builder.SetGlobalTextureAfterPass(finalTexture, ShaderConstants._SSAOFinalTexture);

                builder.SetRenderFunc(static (CleanupPassData data, RasterGraphContext ctx) =>
                {
                    // We only want URP shaders to sample SSAO if After Opaque is disabled...
                    ctx.cmd.SetKeyword(ShaderGlobalKeywords.ScreenSpaceOcclusion, true);
                    ctx.cmd.SetGlobalVector(ShaderConstants._AmbientOcclusionParam, new Vector4(1f, 0f, 0f, data.directLightingStrength));
                });
            }
        }

        // ---- Texture Creation ----
        internal static TextureDesc GetCameraColorDescriptor(RenderGraph renderGraph, in TextureHandle source, bool useDynamicScale)
        {
            var info = renderGraph.GetRenderTargetInfo(source);
            return new TextureDesc(info.width, info.height)
            {
                format = info.format,
                slices = info.volumeDepth,
                dimension = info.volumeDepth > 1 ? TextureDimension.Tex2DArray : TextureDimension.Tex2D,
                bindTextureMS = info.bindMS,
                useDynamicScale = useDynamicScale,
            };
        }

        internal static void CreateRenderTextureHandles(
            RenderGraph renderGraph,
            UniversalResourceData resourceData,
            in TextureDesc cameraColorDesc,
            ScreenSpaceAmbientOcclusionSettings settings,
            bool supportsR8,
            BlurTypes blurType,
            bool enableRandomWrite,
            out TextureHandle aoTexture,
            out TextureHandle blurTexture,
            out TextureHandle temporalTexture,
            out TextureHandle finalTexture)
        {
            // Descriptor for the final blur pass
            TextureDesc finalTextureDescriptor = PostProcessUtils.GetCompatibleDescriptor(
                cameraColorDesc,
                cameraColorDesc.width,
                cameraColorDesc.height,
                supportsR8 ? GraphicsFormat.R8_UNorm : GraphicsFormat.R8G8B8A8_UNorm);
            finalTextureDescriptor.enableRandomWrite = enableRandomWrite;

            // Descriptor for the AO and Blur passes
            int downsampleDivider = settings.Downsample ? 2 : 1;
            bool useRedComponentOnly = supportsR8 && blurType > BlurTypes.Bilateral;

            TextureDesc aoBlurDescriptor = PostProcessUtils.GetCompatibleDescriptor(
                cameraColorDesc,
                cameraColorDesc.width / downsampleDivider,
                cameraColorDesc.height / downsampleDivider,
                useRedComponentOnly ? GraphicsFormat.R8_UNorm : GraphicsFormat.R8G8B8A8_UNorm);
            aoBlurDescriptor.enableRandomWrite = enableRandomWrite;

            // Handles
            aoTexture = UniversalRenderer.CreateRenderGraphTexture(renderGraph, aoBlurDescriptor, "_SSAO_OcclusionTexture0", false, Color.clear, FilterMode.Bilinear);
            finalTexture = settings.AfterOpaque ? resourceData.activeColorTexture : UniversalRenderer.CreateRenderGraphTexture(renderGraph, finalTextureDescriptor, k_SSAOTextureName, false, Color.clear, FilterMode.Bilinear);

            if (settings.BlurQuality != ScreenSpaceAmbientOcclusionSettings.BlurQualityOptions.Low)
                blurTexture = UniversalRenderer.CreateRenderGraphTexture(renderGraph, aoBlurDescriptor, "_SSAO_OcclusionTexture1", false, Color.clear, FilterMode.Bilinear);
            else
                blurTexture = TextureHandle.nullHandle;

#if MODERN_SSAO
            if (settings.IsTemporalFilterActive)
                temporalTexture = UniversalRenderer.CreateRenderGraphTexture(renderGraph, aoBlurDescriptor, "_SSAO_TemporalTexture", false, Color.clear, FilterMode.Bilinear);
            else
                temporalTexture = TextureHandle.nullHandle;
#else
            temporalTexture = TextureHandle.nullHandle;
#endif

            if (!settings.AfterOpaque)
                resourceData.ssaoTexture = finalTexture;
        }
    }
}
