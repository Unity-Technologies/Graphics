#if URP_SCREEN_SPACE_REFLECTION
using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using static UnityEngine.Rendering.RenderGraphModule.Util.RenderGraphUtils;
using static UnityEngine.Rendering.Universal.ScreenSpaceReflectionVolumeSettings;

namespace UnityEngine.Rendering.Universal
{
    internal class ScreenSpaceReflectionPass : ScriptableRenderPass
    {
        public class SharedSSRData : ContextItem
        {
            // Used for transparency support, outputs from transparency depthnormal prepass
            public TextureHandle depthTransparentTexture = TextureHandle.nullHandle;
            public TextureHandle normalTransparentTexture = TextureHandle.nullHandle;

            public override void Reset()
            {
                depthTransparentTexture = TextureHandle.nullHandle;
                normalTransparentTexture = TextureHandle.nullHandle;
            }
        }

        // Enums
        private enum ShaderPasses
        {
            Reflection = 0, // Generate reflected color texture
            BlitAfterOpaque = 1, // Blit to screen (only when using After Opaque mode)
            BilinearUpscale = 2, // Single pass bilinear upscale
            BilateralUpscale = 3, // Single pass normal-weighted bilinear upscale
            TemporalFiltering = 4,
        }

        // Constants
        const string k_ScreenSpaceReflectionTextureName = "_ScreenSpaceReflectionTexture";
        const string k_HiZTrace = "_HIZ_TRACE";
        const string k_UseMotionVectors = "_USE_MOTION_VECTORS";
        const string k_RefineDepth = "_REFINE_DEPTH";

        // Statics
        internal static class ShaderConstants
        {
            internal static readonly int _ReflectionParam = Shader.PropertyToID("_ScreenSpaceReflectionParam");
            internal static readonly int _ReflectionParam2 = Shader.PropertyToID("_ScreenSpaceReflectionParam2");
            internal static readonly int _MaxRayLength = Shader.PropertyToID("_MaxRayLength");
            internal static readonly int _MaxRaySteps = Shader.PropertyToID("_MaxRaySteps");
            internal static readonly int _Downsample = Shader.PropertyToID("_Downsample");
            internal static readonly int _ThicknessScaleAndBias = Shader.PropertyToID("_ThicknessScaleAndBias");
            internal static readonly int _CameraProjections = Shader.PropertyToID("_CameraProjections");
            internal static readonly int _CameraInverseProjections = Shader.PropertyToID("_CameraInverseProjections");
            internal static readonly int _CameraInverseViewProjections = Shader.PropertyToID("_CameraInverseViewProjections");
            internal static readonly int _CameraViewProjections = Shader.PropertyToID("_CameraViewProjections");
            internal static readonly int _CameraViews = Shader.PropertyToID("_CameraViews");
            internal static readonly int _CameraColorTexture = Shader.PropertyToID("_CameraColorTexture");
            internal static readonly int _CameraDepthTexture = Shader.PropertyToID("_CameraDepthTexture");
            internal static readonly int _CameraNormalsTexture = Shader.PropertyToID("_CameraNormalsTexture");
            internal static readonly int _SmoothnessTexture = Shader.PropertyToID("_SmoothnessTexture");
            internal static readonly int _MotionVectorColorTexture = Shader.PropertyToID("_MotionVectorColorTexture");
            internal static readonly int _LastFrameCameraDepthTexture = Shader.PropertyToID("_LastFrameCameraDepthTexture");
            internal static readonly int _SsrDepthPyramidMaxMip = Shader.PropertyToID("_SsrDepthPyramidMaxMip");
            internal static readonly int _SsrDepthPyramid = Shader.PropertyToID("_DepthPyramid");
            internal static readonly int _SmoothnessAndStrengthAndClamp = Shader.PropertyToID("_SmoothnessAndStrengthAndClamp");
            internal static readonly int _ScreenEdgeFadeAndViewConeDot = Shader.PropertyToID("_ScreenEdgeFadeAndViewConeDot");
            internal static readonly int _ReflectSky = Shader.PropertyToID("_ReflectSky");
            internal static readonly int _HitRefinementSteps = Shader.PropertyToID("_HitRefinementSteps");
            internal static readonly int _DepthPyramidMipLevelOffsets = Shader.PropertyToID("_DepthPyramidMipLevelOffsets");
            internal static readonly int _SourceSize = Shader.PropertyToID("_SourceSize");
            internal static readonly int _CameraDeltaJitterOffset = Shader.PropertyToID("_CameraDeltaJitterOffset");
            internal static readonly int _ReflectionHistoryTexture = Shader.PropertyToID("_ReflectionHistoryTexture");
            internal static readonly int _BaseBlendFactor = Shader.PropertyToID("_BaseBlendFactor");
        }

        // Private Variables
        Material m_Material;
        Material m_BlitMaterial;
        bool m_AfterOpaque;

        readonly ProfilingSampler m_ProfilingSampler = URPProfilingSamplers.SSR;
        readonly ProfilingSampler m_DepthPyramidSampler = new("SSR - Depth Pyramid Generation");
        readonly ProfilingSampler m_UpscalingSampler = new("SSR - Upscaling");
        readonly ProfilingSampler m_FinalBlitSampler = new("SSR - Final Blit");

        MipGenerator m_MipGenerator = new();
        //We need to have this struct as a member in order to only allocate the arrays once.
        PackedMipChainInfo m_PackedMipChainInfo;

        public ScreenSpaceReflectionPass()
        {
            m_PackedMipChainInfo.Allocate();
        }

        public void Dispose()
        {
            m_MipGenerator.Release();
        }

        internal bool Setup(ScriptableRenderer renderer, Material material, Material blitMaterial, bool afterOpaque, UniversalRenderingData renderingData, CameraType cameraType)
        {
            m_AfterOpaque = afterOpaque;
            m_BlitMaterial = blitMaterial;

            if (m_Material != material)
            {
                m_Material = material;
                if (m_Material != null)
                {
                }
            }

            var settings = VolumeManager.instance.stack.GetComponent<ScreenSpaceReflectionVolumeSettings>();

            // Rendering after PrePasses is usually correct except when depth priming is in play:
            // then we rely on a depth resolve taking place after the PrePasses in order to have it ready for SSR.

            ScriptableRenderPassInput requiredInputs;
            if (renderer is UniversalRenderer { usesDeferredLighting: true })
            {
                renderPassEvent = m_AfterOpaque ? (settings.ShouldRenderTransparents() ? RenderPassEvent.AfterRenderingTransparents + 25 : RenderPassEvent.AfterRenderingSkybox + 25) : RenderPassEvent.AfterRenderingPrePasses + 5;

                requiredInputs = ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal;
            }
            else
            {
                renderPassEvent = m_AfterOpaque ? (settings.ShouldRenderTransparents() ? RenderPassEvent.AfterRenderingTransparents + 25 : RenderPassEvent.BeforeRenderingTransparents + 25) : RenderPassEvent.AfterRenderingPrePasses + 5;

                // Request DepthNormals texture.
                requiredInputs = ScriptableRenderPassInput.Depth | ScriptableRenderPassInput.Normal;
            }

            // Write smoothness to alpha of depth normals texture so we can sample it in SSR pass.
            renderingData.writesSmoothnessToDepthNormalsAlpha = true;

            // Motion vectors are needed for reprojection (before opaque) and temporal filtering.
            if ((!m_AfterOpaque || settings.temporalFiltering.value) && (cameraType == CameraType.VR || cameraType == CameraType.Game))
                requiredInputs |= ScriptableRenderPassInput.Motion;

            ConfigureInput(requiredInputs);

            return m_Material != null && m_BlitMaterial != null;
        }


        private class ScreenSpaceReflectionPassData
        {
            // Camera data. This is not expected to change during a frame.
            internal UniversalCameraData cameraData;
            internal Matrix4x4[] cameraInverseViewProjections = new Matrix4x4[2];
            internal Matrix4x4[] cameraViewProjections = new Matrix4x4[2];
            internal Matrix4x4[] cameraProjections = new Matrix4x4[2];
            internal Matrix4x4[] cameraInverseProjections = new Matrix4x4[2];
            internal Matrix4x4[] cameraViews = new Matrix4x4[2];
            internal Vector4[] depthPyramidMipOffsets = new Vector4[15];

            // Material containing SSR pass.
            internal Material material;

            // MipInfo for HiZ marching.
            internal PackedMipChainInfo mipsInfo;

            // Various camera settings
            internal Vector2 previousJitter;

            // Required textures.
            internal TextureHandle cameraColor;          // Camera target texture.
            internal TextureHandle cameraDepth;          // Camera depth target texture.
            internal TextureHandle cameraNormalsTexture; // Normals.
            internal TextureHandle smoothnessTexture;    // Texture containing smoothness in the alpha channel.
            internal TextureHandle ssrTexture;           // Output texture containing reflected colors.
            internal TextureHandle blackTexture;         // A black fallback texture.

            // Optional textures.
            internal TextureHandle lastFrameCameraDepth; // Camera depth texture from last frame (only needed if AfterOpaque=false). May contain transparents if transparency support is on.
            internal TextureHandle lastFrameCameraColor; // Camera target texture from last frame (only needed if AfterOpaque=false).
            internal TextureHandle motionVectorColor;    // Motion vectors (only needed if AfterOpaque=false).
            internal TextureHandle depthPyramidTexture;  // Depth pyramid texture for HiZ marching (only needed if LinearMarching=false).

            // Settings.
            internal float minimumSmoothness;
            internal float smoothnessFadeStart;
            internal float normalFade;
            internal float screenEdgeFade;
            internal float maxRayLength;
            internal float rayLengthFade;
            internal float thicknessScale;
            internal float thicknessBias;
            internal float thicknessScaleFine;
            internal float thicknessBiasFine;
            internal int hitRefinementSteps;
            internal int maxRaySteps;
            internal int resolutionScale;
            internal float roughnessScale;
            internal float reflectionStrength;
            internal float clampValue;
            internal bool reflectSky;
            internal bool afterOpaque;
            internal bool linearMarching;
            internal bool useGaussianBlur;
        }

        private class TemporalFilteringPassData
        {
            internal Material material;
            internal TextureHandle ssrTexture;
            internal TextureHandle reflectionHistory;
            internal TextureHandle motionVectors;
            internal float baseBlendFactor;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var settings = VolumeManager.instance.stack.GetComponent<ScreenSpaceReflectionVolumeSettings>();

            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

            // Create the texture handles...
            CreateRenderTextureHandles(renderGraph,
                resourceData,
                settings,
                out TextureHandle ssrTexture,
                out TextureHandle upscaleTexture,
                out TextureHandle mipGenTexture,
                out TextureHandle finalTexture,
                out TextureHandle depthPyramidTexture);

            // Get input resources.
            SharedSSRData ssrData = frameData.GetOrCreate<SharedSSRData>();
            TextureHandle cameraDepthTexture = resourceData.cameraDepthTexture;
            TextureHandle cameraNormalsTexture = resourceData.cameraNormalsTexture;
            TextureHandle cameraColorTexture = resourceData.cameraColor;
            TextureHandle motionVectorColorTexture = resourceData.motionVectorColor;
            TextureHandle smoothnessTexture = settings.ShouldRenderTransparents() ? ssrData.normalTransparentTexture : cameraNormalsTexture;
            if (settings.ShouldRenderTransparents())
            {
                cameraDepthTexture = ssrData.depthTransparentTexture;
                cameraNormalsTexture = ssrData.normalTransparentTexture;
            }

            using (new RenderGraphProfilingScope(renderGraph, m_ProfilingSampler))
            {
                if (!settings.ShouldUseLinearMarching())
                {
                    using (new RenderGraphProfilingScope(renderGraph, m_DepthPyramidSampler))
                    {
                        var blitParam = new BlitMaterialParameters(cameraDepthTexture, depthPyramidTexture, new(1f, 2f), Vector2.zero, m_BlitMaterial, 0);
                        renderGraph.AddBlitPass(blitParam, passName: "Copy depth to mip 0");
                        m_PackedMipChainInfo.ComputePackedMipChainInfo(new Vector2Int(cameraData.cameraTargetDescriptor.width / (int)settings.resolution.value, cameraData.cameraTargetDescriptor.height / (int)settings.resolution.value), 0);
                        m_MipGenerator.RenderMinDepthPyramid(renderGraph, depthPyramidTexture, ref m_PackedMipChainInfo);
                    }
                }

                using (var builder = renderGraph.AddRasterRenderPass<ScreenSpaceReflectionPassData>("SSR - Main Pass", out var passData))
                {
                    // Shader keyword changes are considered as global state modifications
                    builder.AllowGlobalStateModification(true);

                    // Set required pass data.
                    passData.cameraData = cameraData;
                    passData.previousJitter = Vector2.zero;
                    passData.afterOpaque = m_AfterOpaque;
                    passData.linearMarching = settings.ShouldUseLinearMarching();
                    passData.useGaussianBlur = settings.ShouldUseGaussianBlurRoughness();
                    passData.minimumSmoothness = settings.minimumSmoothness.value;
                    passData.smoothnessFadeStart = settings.smoothnessFadeStart.value;
                    passData.normalFade = settings.normalFade.value;
                    passData.screenEdgeFade = Mathf.Max(0.001f, settings.screenEdgeFadeDistance.value * 0.5f);
                    passData.reflectSky = settings.reflectSky.value;
                    passData.hitRefinementSteps = settings.hitRefinementSteps.value;
                    passData.maxRayLength = settings.maxRayLength.value;
                    passData.rayLengthFade = settings.rayLengthFade.value;
                    passData.maxRaySteps = settings.maxRaySteps.value;
                    passData.resolutionScale = (int)settings.resolution.value;
                    passData.roughnessScale = settings.roughnessScale.value;
                    passData.reflectionStrength = settings.reflectionStrength.value;
                    // 65504 is the max representable FP16 value - use it to avoid an additional branch in shader.
                    passData.clampValue = settings.clampReflectedColor.value ? settings.maxColorValue.value : 65504.0f;
                    passData.material = m_Material;
                    passData.mipsInfo = m_PackedMipChainInfo;
                    passData.cameraColor = cameraColorTexture;
                    passData.cameraDepth = cameraDepthTexture;
                    passData.ssrTexture = ssrTexture;
                    passData.cameraNormalsTexture = cameraNormalsTexture;
                    passData.smoothnessTexture = smoothnessTexture;
                    passData.blackTexture = (TextureXR.slices > 1 && TextureXR.initialized) ? renderGraph.defaultResources.blackTextureXR : renderGraph.defaultResources.blackTexture;
                    CalculateThicknessScaleAndBias(cameraData.camera.nearClipPlane, cameraData.camera.farClipPlane, settings.objectThickness.value,
                        out passData.thicknessScale, out passData.thicknessBias);
                    CalculateThicknessScaleAndBias(cameraData.camera.nearClipPlane, cameraData.camera.farClipPlane, settings.objectThickness.value * settings.finalThicknessMultiplier.value,
                        out passData.thicknessScaleFine, out passData.thicknessBiasFine);

                    // Set optional input textures to black by default.
                    passData.lastFrameCameraColor = passData.blackTexture;
                    passData.lastFrameCameraDepth = passData.blackTexture;
                    passData.motionVectorColor = passData.blackTexture;
                    passData.depthPyramidTexture = passData.blackTexture;

                    // Declare required input textures.
                    builder.SetRenderAttachment(passData.ssrTexture, 0, AccessFlags.ReadWrite);
                    builder.UseTexture(passData.cameraDepth);
                    builder.UseTexture(passData.cameraColor);
                    builder.UseTexture(passData.cameraNormalsTexture);
                    builder.UseTexture(passData.smoothnessTexture);
                    builder.UseTexture(passData.blackTexture);

                    // If we are using HiZ marching, set the depth pyramid texture.
                    if (!settings.ShouldUseLinearMarching())
                    {
                        passData.depthPyramidTexture = depthPyramidTexture;
                        builder.UseTexture(passData.depthPyramidTexture);
                    }

                    // If AfterOpaque=false, set the motion vector and last frame color textures.
                    // When AfterOpaque=true, we use the current frame color instead, and need no motion vectors.
                    if (!passData.afterOpaque && cameraData.historyManager != null)
                    {
                        int multipassId = 0;
#if ENABLE_VR && ENABLE_XR_MODULE
                        multipassId = cameraData.xr.multipassId;
#endif

                        // If we are rendering transparents in SSR, we want the last frame color to include transparents.
                        // Otherwise, we want last frame color before rendering transparents.
                        RTHandle historyTexture;
                        if (settings.ShouldRenderTransparents())
                        {
                            cameraData.historyManager.RequestAccess<RawColorHistory>();
                            RawColorHistory history = cameraData.historyManager.GetHistoryForRead<RawColorHistory>();
                            historyTexture = history?.GetPreviousTexture(multipassId);
                        }
                        else
                        {
                            cameraData.historyManager.RequestAccess<BeforeTransparentsColorHistory>();
                            BeforeTransparentsColorHistory history = cameraData.historyManager.GetHistoryForRead<BeforeTransparentsColorHistory>();
                            historyTexture = history?.GetPreviousTexture(multipassId);
                        }

                        if (historyTexture != null)
                        {
                            passData.lastFrameCameraColor = renderGraph.ImportTexture(historyTexture);
                            builder.UseTexture(passData.lastFrameCameraColor);
                        }

                        // We also need motion vectors to reproject.
                        if (input.HasFlag(ScriptableRenderPassInput.Motion))
                        {
                            passData.motionVectorColor = motionVectorColorTexture;
                            builder.UseTexture(passData.motionVectorColor);
                        }

                        // We also need depth from last frame to reject our reprojected positions.
                        {
                            cameraData.historyManager.RequestAccess<ScreenSpaceReflectionDepthHistory>();
                            ScreenSpaceReflectionDepthHistory history = cameraData.historyManager.GetHistoryForRead<ScreenSpaceReflectionDepthHistory>();
                            var depthHistoryTexture = history?.GetPreviousTexture(multipassId);
                            if (depthHistoryTexture != null)
                            {
                                passData.lastFrameCameraDepth = renderGraph.ImportTexture(depthHistoryTexture);
                                builder.UseTexture(passData.lastFrameCameraDepth);

                                passData.previousJitter = history.GetPreviousJitter(multipassId);
                            }
                        }
                    }

                    builder.SetRenderFunc<ScreenSpaceReflectionPassData>(static (ssrData, rgContext) =>
                    {
                        SetupKeywordsAndParameters(ref ssrData);

                        var cmd = rgContext.cmd;
                        Vector4 finalTextureSourceSize = PostProcessUtils.CalcShaderSourceSize(ssrData.cameraColor);
                        ssrData.material.SetVector(ShaderConstants._SourceSize, finalTextureSourceSize);

                        ssrData.material.SetTexture(ShaderConstants._CameraDepthTexture, ssrData.cameraDepth);

                        if (ssrData.afterOpaque)
                            ssrData.material.SetTexture(ShaderConstants._CameraColorTexture, ssrData.cameraColor);
                        // Somehow this texture can be null even when TextureHandle.IsValid() is true, guard against that.
                        else if (((RTHandle)ssrData.lastFrameCameraColor)?.rt == null)
                            ssrData.material.SetTexture(ShaderConstants._CameraColorTexture, ssrData.blackTexture);
                        else
                            ssrData.material.SetTexture(ShaderConstants._CameraColorTexture, ssrData.lastFrameCameraColor);

                        if (ssrData.afterOpaque)
                            ssrData.material.SetTexture(ShaderConstants._LastFrameCameraDepthTexture, ssrData.blackTexture);
                        else if (((RTHandle)ssrData.lastFrameCameraDepth)?.rt == null)
                            ssrData.material.SetTexture(ShaderConstants._LastFrameCameraDepthTexture, ssrData.blackTexture);
                        else
                            ssrData.material.SetTexture(ShaderConstants._LastFrameCameraDepthTexture, ssrData.lastFrameCameraDepth);

                        ssrData.material.SetTexture(ShaderConstants._CameraNormalsTexture, ssrData.cameraNormalsTexture);
                        ssrData.material.SetTexture(ShaderConstants._SmoothnessTexture, ssrData.smoothnessTexture);
                        ssrData.material.SetTexture(ShaderConstants._MotionVectorColorTexture, ssrData.motionVectorColor);

                        // Main SSR Pass
                        Blitter.BlitTexture(cmd, ssrData.ssrTexture, new Vector4(1, 1, 0, 0), ssrData.material, (int)ShaderPasses.Reflection);

                        cmd.SetGlobalVector(ShaderConstants._ReflectionParam, new Vector4(1f, ssrData.minimumSmoothness, ssrData.smoothnessFadeStart, 0));

                        // This handles asymetric projections as well as orthographic ones. We only take into account the first eye as both eyes have the same "width", even
                        // if the projection is "flipped"
                        Vector3 right = ssrData.cameraInverseProjections[0].MultiplyPoint(new Vector3(1, 0, 0));
                        Vector3 left = ssrData.cameraInverseProjections[0].MultiplyPoint(new Vector3(-1, 0, 0));

                        float finalTextureLastMipIndex = Mathf.Log(finalTextureSourceSize.x) / Mathf.Log(2);
                        float finalTextureLastValidMipIndex = finalTextureLastMipIndex;
                        // With gaussian blurring, we do not compute mips that are under k_MinimumResolutionGaussian pixels wide.
                        // See MipGenerator.RenderColorGaussianPyramid
                        if (ssrData.useGaussianBlur)
                        {
                            float mipOffset = Mathf.Log((float)MipGenerator.k_MinimumResolutionGaussian) / Mathf.Log(2);
                            finalTextureLastValidMipIndex = Mathf.Max(0, finalTextureLastValidMipIndex - mipOffset);
                        }

                        const float k_SSRBlurReferenceLastMipIndex = 10.0f;
                        float finalTextureMipOffset = finalTextureLastMipIndex - k_SSRBlurReferenceLastMipIndex;

                        // Multiply by the ratio between a reference screen width and the actual width so we are independent of field of view.
                        const float k_SSRBlurReferenceScreenHalfWidth = 1.0f;
                        const float k_BlurrinessBase = 100f;
                        float screenHalfWidthAtOne = 0.5f * Math.Abs(left.x / left.z - right.x / right.z);
                        float blurriness = k_BlurrinessBase * Mathf.Pow(2f, ssrData.roughnessScale) * k_SSRBlurReferenceScreenHalfWidth / Math.Max(0.01f, screenHalfWidthAtOne);
                        cmd.SetGlobalVector(ShaderConstants._ReflectionParam2, new Vector4(blurriness, finalTextureMipOffset, finalTextureLastValidMipIndex, 0));
                    });
                }

                // Temporal filtering pass.
                if (settings.temporalFiltering.value &&
                    cameraData.historyManager != null &&
                    input.HasFlag(ScriptableRenderPassInput.Motion))
                {
                    int multipassId = 0;
                    bool xrMultipassEnabled = false;
#if ENABLE_VR && ENABLE_XR_MODULE
                    xrMultipassEnabled = cameraData.xr.enabled && !cameraData.xr.singlePassEnabled;
                    multipassId = cameraData.xr.multipassId;
#endif
                    cameraData.historyManager.RequestAccess<ScreenSpaceReflectionColorHistory>();
                    var ssrHistory = cameraData.historyManager.GetHistoryForWrite<ScreenSpaceReflectionColorHistory>();
                    if (ssrHistory != null)
                    {
                        // Get history descriptor.
                        TextureDesc ssrHistoryDesc = ssrTexture.GetDescriptor(renderGraph);
                        var ssrDesc = cameraData.cameraTargetDescriptor;
                        ssrDesc.width = ssrHistoryDesc.width;
                        ssrDesc.height = ssrHistoryDesc.height;
                        ssrDesc.graphicsFormat = ssrHistoryDesc.format;
                        ssrDesc.depthStencilFormat = GraphicsFormat.None;
                        ssrDesc.msaaSamples = 1;
                        ssrDesc.useMipMap = false;
                        ssrDesc.autoGenerateMips = false;
                        ssrDesc.enableRandomWrite = false;

                        // Apply temporal filtering resolve.
                        bool isNewAlloc = ssrHistory.Update(cameraData, xrMultipassEnabled, ssrDesc);
                        RTHandle prevRT = ssrHistory.GetPreviousTexture(multipassId);
                        RTHandle currRT = ssrHistory.GetCurrentTexture(multipassId);
                        if (prevRT != null && currRT != null)
                        {
                            TextureHandle historyTexture = renderGraph.ImportTexture(prevRT);
                            TextureHandle currentHistoryTexture = renderGraph.ImportTexture(currRT);

                            using (var builder = renderGraph.AddRasterRenderPass<TemporalFilteringPassData>("SSR - Temporal Filtering", out var passData))
                            {
                                passData.material = m_Material;
                                passData.ssrTexture = ssrTexture;
                                passData.reflectionHistory = historyTexture;
                                passData.motionVectors = motionVectorColorTexture;
                                passData.baseBlendFactor = isNewAlloc ? 0.0f : settings.baseBlendFactor.value;

                                builder.SetRenderAttachment(currentHistoryTexture, 0);
                                builder.UseTexture(ssrTexture);
                                builder.UseTexture(historyTexture);
                                builder.UseTexture(motionVectorColorTexture);

                                builder.SetRenderFunc<TemporalFilteringPassData>(static (data, ctx) =>
                                {
                                    data.material.SetTexture(ShaderConstants._ReflectionHistoryTexture, data.reflectionHistory);
                                    data.material.SetTexture(ShaderConstants._MotionVectorColorTexture, data.motionVectors);
                                    data.material.SetFloat(ShaderConstants._BaseBlendFactor, data.baseBlendFactor);
                                    Blitter.BlitTexture(ctx.cmd, data.ssrTexture, Vector2.one, data.material, (int)ShaderPasses.TemporalFiltering);
                                });
                            }
                            ssrTexture = currentHistoryTexture;
                        }
                    }
                }

                // Upscale pass.
                TextureHandle fullResSSRTexture = ssrTexture;
                using (new RenderGraphProfilingScope(renderGraph, m_UpscalingSampler))
                {
                    if (settings.resolution != ScreenSpaceReflectionVolumeSettings.Resolution.Full)
                    {
                        fullResSSRTexture = upscaleTexture;

                        if (settings.upscalingMethod == UpscalingMethod.Bilinear)
                        {
                            var blitParam = new BlitMaterialParameters(ssrTexture, upscaleTexture, Vector2.one, Vector2.zero, m_Material, (int)ShaderPasses.BilinearUpscale);
                            renderGraph.AddBlitPass(blitParam, passName: "BilinearUpscale");
                        }
                        else if (settings.upscalingMethod == UpscalingMethod.Bilateral)
                        {
                            var blitParam = new BlitMaterialParameters(ssrTexture, upscaleTexture, Vector2.one, Vector2.zero, m_Material, (int)ShaderPasses.BilateralUpscale);
                            renderGraph.AddBlitPass(blitParam, passName: "BilateralUpscale");
                        }
                    }
                }

                // Final blit pass.
                using (new RenderGraphProfilingScope(renderGraph, m_FinalBlitSampler))
                {
                    var viewportSizeWithScale = new Vector2Int(cameraData.cameraTargetDescriptor.width, cameraData.cameraTargetDescriptor.height);
                    if (m_AfterOpaque)
                    {
                        TextureHandle textureToBlit = fullResSSRTexture;
                        if (settings.ShouldUseGaussianBlurRoughness())
                        {
                            textureToBlit = mipGenTexture;
                            m_MipGenerator.RenderColorGaussianPyramid(renderGraph, viewportSizeWithScale, fullResSSRTexture, mipGenTexture);
                        }
                        var blitParam = new BlitMaterialParameters(textureToBlit, finalTexture, Vector2.one, Vector2.zero, m_Material, (int)ShaderPasses.BlitAfterOpaque);
                        renderGraph.AddBlitPass(blitParam, passName: "Final blit");
                    }
                    else
                    {
                        if (settings.ShouldUseGaussianBlurRoughness())
                            m_MipGenerator.RenderColorGaussianPyramid(renderGraph, viewportSizeWithScale, fullResSSRTexture, finalTexture);
                        else
                        {
                            renderGraph.AddCopyPass(fullResSSRTexture, finalTexture, passName: "Final blit");
                        }
                    }
                }
            }

            RenderDepthHistory(renderGraph, cameraData, cameraDepthTexture);

            // Set global texture so subsequent passes can read it.
            if (m_AfterOpaque)
                resourceData.ssrTexture = TextureHandle.nullHandle;
            else
                resourceData.ssrTexture = finalTexture;
        }

        static void SetupKeywordsAndParameters(ref ScreenSpaceReflectionPassData data)
        {
            UniversalCameraData cameraData = data.cameraData;
#if ENABLE_VR && ENABLE_XR_MODULE
            int eyeCount = cameraData.xr.enabled && cameraData.xr.singlePassEnabled ? 2 : 1;
#else
            int eyeCount = 1;
#endif
            for (int eyeIndex = 0; eyeIndex < eyeCount; eyeIndex++)
            {
                Matrix4x4 view = cameraData.GetViewMatrix(eyeIndex);
                Matrix4x4 proj = cameraData.GetGPUProjectionMatrix(true, eyeIndex);
                data.cameraProjections[eyeIndex] = proj;
                data.cameraInverseProjections[eyeIndex] = proj.inverse;
                data.cameraViews[eyeIndex] = view;
                data.cameraViewProjections[eyeIndex] = proj * view;
                data.cameraInverseViewProjections[eyeIndex] = data.cameraViewProjections[eyeIndex].inverse;
            }

            // Send the difference between the current and previous jitter values so we can reproject taking jittering
            // into account. Divide by 2 because the jittering values are in NDC space (-1 to 1) and we need them in texture space (0 to 1).
            // We use the current jitter from the cameraData as the one in the history is not yet up to date because the depth history update is done later on in the frame.
            data.material.SetVector(ShaderConstants._CameraDeltaJitterOffset, (Vector4)(cameraData.jitter - data.previousJitter) * 0.5f);
            data.material.SetMatrixArray(ShaderConstants._CameraProjections, data.cameraProjections);
            data.material.SetMatrixArray(ShaderConstants._CameraInverseProjections, data.cameraInverseProjections);
            data.material.SetMatrixArray(ShaderConstants._CameraViews, data.cameraViews);
            data.material.SetMatrixArray(ShaderConstants._CameraInverseViewProjections, data.cameraInverseViewProjections);
            data.material.SetMatrixArray(ShaderConstants._CameraViewProjections, data.cameraViewProjections);
            data.material.SetVector(ShaderConstants._SmoothnessAndStrengthAndClamp, new Vector4(data.minimumSmoothness, data.smoothnessFadeStart, data.reflectionStrength, data.clampValue));
            data.material.SetVector(ShaderConstants._ScreenEdgeFadeAndViewConeDot, new Vector4(data.screenEdgeFade, 1.0f - data.screenEdgeFade, 2.0f * data.normalFade - 1.0f));
            data.material.SetInteger(ShaderConstants._ReflectSky, data.reflectSky ? 1 : 0);
            data.material.SetInteger(ShaderConstants._HitRefinementSteps, data.hitRefinementSteps);
            data.material.SetVector(ShaderConstants._MaxRayLength, new Vector4(data.maxRayLength, data.maxRayLength - Math.Max(0.01f, data.rayLengthFade)));
            data.material.SetInteger(ShaderConstants._MaxRaySteps, data.maxRaySteps);
            data.material.SetInteger(ShaderConstants._Downsample, data.resolutionScale);
            data.material.SetVector(ShaderConstants._ThicknessScaleAndBias, new Vector4(data.thicknessScale, data.thicknessBias, data.thicknessScaleFine, data.thicknessBiasFine));

            if (!data.linearMarching)
            {
                for (int i = 0; i < data.mipsInfo.mipLevelCount; i++)
                {
                    data.depthPyramidMipOffsets[i] = new Vector4(data.mipsInfo.mipLevelOffsets[i].x, data.mipsInfo.mipLevelOffsets[i].y, 0, 0);
                }

                data.material.SetVectorArray(ShaderConstants._DepthPyramidMipLevelOffsets, data.depthPyramidMipOffsets);
                data.material.SetInteger(ShaderConstants._SsrDepthPyramidMaxMip, data.mipsInfo.mipLevelCount);
                data.material.SetTexture(ShaderConstants._SsrDepthPyramid, data.depthPyramidTexture);
            }

            CoreUtils.SetKeyword(data.material, k_HiZTrace, !data.linearMarching);
            CoreUtils.SetKeyword(data.material, k_RefineDepth, data.hitRefinementSteps > 0);
            CoreUtils.SetKeyword(data.material, k_UseMotionVectors, !data.afterOpaque && !cameraData.isSceneViewCamera);
        }

        // Instead of calculating the 'floor' depth by adding a constant thickness to the linear depth from the depth buffer, we treat thickness as a multiplier
        // on the linear depth, i.e. linearFloorDepth = (1.0 + thickness) * linearDepth. To avoid converting to/from linear depth during marching, we precalculate
        // a scale and bias on the device depth which lets us do the comparison on it directly.
        private void CalculateThicknessScaleAndBias(float nearClip, float farClip, float thickness, out float thicknessScale, out float thicknessBias)
        {
            // Derivation below. 'b' is the floor device depth, 'd' is device depth, 'f' and 'n' are the far and near clip, and 'k_s', 'k_b' are the scale and bias.
            //   b = DeviceDepth((1 + thickness) * LinearDepth(d))
            //   b = ((f - n) * d + n * (1 - (1 + thickness))) / ((f - n) * (1 + thickness))
            //   b = ((f - n) * d - n * thickness) / ((f - n) * (1 + thickness))
            //   b = d / (1 + thickness) - n / (f - n) * (thickness / (1 + thickness))
            //   b = d * k_s + k_b

            // For non-reversed-Z (OpenGL), the derivation of thicknessScale is the same, but the derivation of thicknessBias becomes:
            //   thicknessBiasOpenGL = farClip / (farClip - nearClip) * (thickness * thicknessScale)

            thicknessScale = 1.0f / (1.0f + thickness);
            if (SystemInfo.usesReversedZBuffer)
                thicknessBias = -nearClip / (farClip - nearClip) * (thickness * thicknessScale);
            else
                thicknessBias = farClip / (farClip - nearClip) * (thickness * thicknessScale);
        }

        private void CreateRenderTextureHandles(
            RenderGraph renderGraph,
            UniversalResourceData resourceData,
            ScreenSpaceReflectionVolumeSettings settings,
            out TextureHandle ssrTexture,
            out TextureHandle upscaleTexture,
            out TextureHandle mipGenTexture,
            out TextureHandle finalTexture,
            out TextureHandle depthPyramidTexture)
        {
            bool needRoughnessMips = settings.roughnessFilter.value != RoughReflectionsQuality.Disabled;
            bool boxBlurRoughness = settings.roughnessFilter.value == RoughReflectionsQuality.BoxBlur;

            TextureDesc cameraDesc = resourceData.cameraColor.GetDescriptor(renderGraph);
            bool useHdrRendering = GraphicsFormatUtility.IsHDRFormat(cameraDesc.format);
            GraphicsFormat colorFormat = useHdrRendering ? GraphicsFormat.R16G16B16A16_SFloat : GraphicsFormat.R8G8B8A8_UNorm;

            TextureDesc fullResWithMips = cameraDesc;
            fullResWithMips.format = colorFormat;
            fullResWithMips.msaaSamples = MSAASamples.None;
            fullResWithMips.useMipMap = needRoughnessMips;
            fullResWithMips.autoGenerateMips = boxBlurRoughness;
            fullResWithMips.enableRandomWrite = SystemInfo.supportsComputeShaders;

            TextureDesc fullResNoMips = fullResWithMips;
            fullResNoMips.useMipMap = false;

            TextureDesc lowResWithMips = cameraDesc;
            lowResWithMips.format = colorFormat;
            lowResWithMips.msaaSamples = MSAASamples.None;
            lowResWithMips.useMipMap = needRoughnessMips;
            lowResWithMips.autoGenerateMips = boxBlurRoughness;
            lowResWithMips.enableRandomWrite = false;
            lowResWithMips.width /= (int)settings.resolution.value;
            lowResWithMips.height /= (int)settings.resolution.value;

            TextureDesc lowResNoMips = lowResWithMips;
            lowResNoMips.useMipMap = false;

            // Main output texture for SSR pass. Potentially low res. Only needs mips if we are relying on automips rather than gaussian mipchain, so there is no mipGenTexture.
            TextureDesc ssrTextureDescriptor = boxBlurRoughness ? lowResWithMips : lowResNoMips;
            ssrTexture = UniversalRenderer.CreateRenderGraphTexture(renderGraph, ssrTextureDescriptor, "_SSR_ReflectionTexture", false, Color.clear, FilterMode.Bilinear);

            // Temporary texture storing output of upscale. Only needs mips if we are relying on automips rather than gaussian mipchain, so there is no mipGenTexture.
            TextureDesc upscaleTextureDescriptor = boxBlurRoughness ? fullResWithMips : fullResNoMips;
            if (settings.resolution != ScreenSpaceReflectionVolumeSettings.Resolution.Full)
                upscaleTexture = UniversalRenderer.CreateRenderGraphTexture(renderGraph, upscaleTextureDescriptor, "_SSR_UpscaleTexture", false, Color.clear, FilterMode.Bilinear);
            else
                upscaleTexture = TextureHandle.nullHandle;

            // Temporary texture storing mipchain from color pyramid generator for rough reflections. Full res, with mips.
            // Only needed in AfterOpaque, otherwise mips generated directly to final texture.
            if (settings.ShouldUseGaussianBlurRoughness() && m_AfterOpaque)
                mipGenTexture = UniversalRenderer.CreateRenderGraphTexture(renderGraph, fullResWithMips, "_SSR_MipGenTexture", false, Color.clear, FilterMode.Bilinear);
            else
                mipGenTexture = TextureHandle.nullHandle;

            // Final texture. If after opaque, this is the screen target, otherwise a persistent texture with mips.
            finalTexture = m_AfterOpaque ? resourceData.activeColorTexture : UniversalRenderer.CreateRenderGraphTexture(renderGraph, fullResWithMips, k_ScreenSpaceReflectionTextureName, false, Color.clear, FilterMode.Bilinear);

            // Depth pyramid for Hi-Z tracing.
            if (!settings.ShouldUseLinearMarching())
            {
                // Base format on precision of input depth.
                // If the selected format isn't supported as UAV, try to fall back to a format with wider support.
                TextureDesc depthDesc = resourceData.cameraDepthTexture.GetDescriptor(renderGraph);
                GraphicsFormat depthFormat = depthDesc.format;
                if (GraphicsFormatUtility.IsDepthFormat(depthDesc.format))
                    depthFormat = GraphicsFormatUtility.GetDepthBits(depthDesc.format) > 16 ? GraphicsFormat.R32_SFloat : GraphicsFormat.R16_SFloat;
                if (!SystemInfo.IsFormatSupported(depthFormat, GraphicsFormatUsage.LoadStore))
                    depthFormat = GraphicsFormat.R32_SFloat;
                if (!SystemInfo.IsFormatSupported(depthFormat, GraphicsFormatUsage.LoadStore))
                    depthFormat = GraphicsFormat.R32G32B32A32_SFloat;

                TextureDesc depthPyramidDesc = cameraDesc;
                depthPyramidDesc.height *= 2;
                depthPyramidDesc.format = depthFormat;
                depthPyramidDesc.msaaSamples = MSAASamples.None;
                depthPyramidDesc.useMipMap = false;
                depthPyramidDesc.autoGenerateMips = false;
                depthPyramidDesc.enableRandomWrite = true;
                depthPyramidDesc.width /= (int)settings.resolution.value;
                depthPyramidDesc.height /= (int)settings.resolution.value;
                depthPyramidTexture = UniversalRenderer.CreateRenderGraphTexture(renderGraph, depthPyramidDesc, "_SSR_DepthPyramid", false, Color.clear, FilterMode.Point);
            }
            else
                depthPyramidTexture = TextureHandle.nullHandle;
        }

        private static void RenderDepthHistory(RenderGraph renderGraph, UniversalCameraData cameraData, TextureHandle cameraDepthTexture)
        {
            if (cameraData.historyManager != null)
            {
                UniversalCameraHistory history = cameraData.historyManager;

                bool xrMultipassEnabled = false;
                int multipassId = 0;
#if ENABLE_VR && ENABLE_XR_MODULE
                xrMultipassEnabled = cameraData.xr.enabled && !cameraData.xr.singlePassEnabled;
                multipassId = cameraData.xr.multipassId;
#endif
                if (history.IsAccessRequested<ScreenSpaceReflectionDepthHistory>() && cameraDepthTexture.IsValid())
                {
                    var depthHistory = history.GetHistoryForWrite<ScreenSpaceReflectionDepthHistory>();
                    if (depthHistory != null)
                    {
                        var tempColorDepthDesc = cameraData.cameraTargetDescriptor;
                        tempColorDepthDesc.graphicsFormat = GraphicsFormat.R32_SFloat;
                        tempColorDepthDesc.depthStencilFormat = GraphicsFormat.None;

                        depthHistory.Update(cameraData, xrMultipassEnabled, tempColorDepthDesc);

                        if (depthHistory.GetCurrentTexture(multipassId) != null)
                        {
                            var depthHistoryTarget = renderGraph.ImportTexture(depthHistory.GetCurrentTexture(multipassId));
                            renderGraph.AddBlitPass(cameraDepthTexture, depthHistoryTarget, Vector2.one, Vector2.zero, filterMode: BlitFilterMode.ClampNearest);
                        }
                    }
                }
            }
        }
    }
}
#endif
