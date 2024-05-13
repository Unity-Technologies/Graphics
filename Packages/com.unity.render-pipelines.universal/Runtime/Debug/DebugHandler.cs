using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEditor;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal
{
    class DebugHandler : IDebugDisplaySettingsQuery
    {
        #region Property Id Constants

        static readonly int k_DebugColorInvalidModePropertyId = Shader.PropertyToID("_DebugColorInvalidMode");
        static readonly int k_DebugCurrentRealTimeId = Shader.PropertyToID("_DebugCurrentRealTime");

        static readonly int k_DebugColorPropertyId = Shader.PropertyToID("_DebugColor");
        static readonly int k_DebugTexturePropertyId = Shader.PropertyToID("_DebugTexture");
        static readonly int k_DebugFontId = Shader.PropertyToID("_DebugFont");
        static readonly int k_DebugTextureNoStereoPropertyId = Shader.PropertyToID("_DebugTextureNoStereo");
        static readonly int k_DebugTextureDisplayRect = Shader.PropertyToID("_DebugTextureDisplayRect");
        static readonly int k_DebugRenderTargetSupportsStereo = Shader.PropertyToID("_DebugRenderTargetSupportsStereo");
        static readonly int k_DebugRenderTargetRangeRemap = Shader.PropertyToID("_DebugRenderTargetRangeRemap");

        // Material settings...
        static readonly int k_DebugMaterialModeId = Shader.PropertyToID("_DebugMaterialMode");
        static readonly int k_DebugVertexAttributeModeId = Shader.PropertyToID("_DebugVertexAttributeMode");
        static readonly int k_DebugMaterialValidationModeId = Shader.PropertyToID("_DebugMaterialValidationMode");

        // Rendering settings...
        static readonly int k_DebugMipInfoModeId = Shader.PropertyToID("_DebugMipInfoMode");
        static readonly int k_DebugMipMapStatusModeId = Shader.PropertyToID("_DebugMipMapStatusMode");
        static readonly int k_DebugMipMapShowStatusCodeId = Shader.PropertyToID("_DebugMipMapShowStatusCode");
        static readonly int k_DebugMipMapOpacityId = Shader.PropertyToID("_DebugMipMapOpacity");
        static readonly int k_DebugMipMapRecentlyUpdatedCooldownId = Shader.PropertyToID("_DebugMipMapRecentlyUpdatedCooldown");
        static readonly int k_DebugMipMapTerrainTextureModeId = Shader.PropertyToID("_DebugMipMapTerrainTextureMode");
        static readonly int k_DebugSceneOverrideModeId = Shader.PropertyToID("_DebugSceneOverrideMode");
        static readonly int k_DebugFullScreenModeId = Shader.PropertyToID("_DebugFullScreenMode");
        static readonly int k_DebugValidationModeId = Shader.PropertyToID("_DebugValidationMode");
        static readonly int k_DebugValidateBelowMinThresholdColorPropertyId = Shader.PropertyToID("_DebugValidateBelowMinThresholdColor");
        static readonly int k_DebugValidateAboveMaxThresholdColorPropertyId = Shader.PropertyToID("_DebugValidateAboveMaxThresholdColor");
        static readonly int k_DebugMaxPixelCost = Shader.PropertyToID("_DebugMaxPixelCost");


        // Lighting settings...
        static readonly int k_DebugLightingModeId = Shader.PropertyToID("_DebugLightingMode");
        static readonly int k_DebugLightingFeatureFlagsId = Shader.PropertyToID("_DebugLightingFeatureFlags");

        static readonly int k_DebugValidateAlbedoMinLuminanceId = Shader.PropertyToID("_DebugValidateAlbedoMinLuminance");
        static readonly int k_DebugValidateAlbedoMaxLuminanceId = Shader.PropertyToID("_DebugValidateAlbedoMaxLuminance");
        static readonly int k_DebugValidateAlbedoSaturationToleranceId = Shader.PropertyToID("_DebugValidateAlbedoSaturationTolerance");
        static readonly int k_DebugValidateAlbedoHueToleranceId = Shader.PropertyToID("_DebugValidateAlbedoHueTolerance");
        static readonly int k_DebugValidateAlbedoCompareColorId = Shader.PropertyToID("_DebugValidateAlbedoCompareColor");

        static readonly int k_DebugValidateMetallicMinValueId = Shader.PropertyToID("_DebugValidateMetallicMinValue");
        static readonly int k_DebugValidateMetallicMaxValueId = Shader.PropertyToID("_DebugValidateMetallicMaxValue");

        static readonly int k_ValidationChannelsId = Shader.PropertyToID("_ValidationChannels");
        static readonly int k_RangeMinimumId = Shader.PropertyToID("_RangeMinimum");
        static readonly int k_RangeMaximumId = Shader.PropertyToID("_RangeMaximum");

        #endregion

        #region Pass Data

        private static readonly ProfilingSampler s_DebugSetupSampler = new ProfilingSampler(nameof(Setup));
        private static readonly ProfilingSampler s_DebugFinalValidationSampler = new ProfilingSampler(nameof(UpdateShaderGlobalPropertiesForFinalValidationPass));

        DebugSetupPassData s_DebugSetupPassData = new DebugSetupPassData();
        DebugFinalValidationPassData s_DebugFinalValidationPassData = new DebugFinalValidationPassData();

        #endregion

        readonly Material m_ReplacementMaterial;
        readonly Material m_HDRDebugViewMaterial;

        HDRDebugViewPass m_HDRDebugViewPass;
        RTHandle m_DebugScreenColorHandle;
        RTHandle m_DebugScreenDepthHandle;

        readonly UniversalRenderPipelineRuntimeTextures m_RuntimeTextures;

        bool m_HasDebugRenderTarget;
        bool m_DebugRenderTargetSupportsStereo;
        Vector4 m_DebugRenderTargetPixelRect;
        Vector4 m_DebugRenderTargetRangeRemap;
        RTHandle m_DebugRenderTarget;

        RTHandle m_DebugFontTexture;

        readonly UniversalRenderPipelineDebugDisplaySettings m_DebugDisplaySettings;

        DebugDisplaySettingsLighting LightingSettings => m_DebugDisplaySettings.lightingSettings;
        DebugDisplaySettingsMaterial MaterialSettings => m_DebugDisplaySettings.materialSettings;
        DebugDisplaySettingsRendering RenderingSettings => m_DebugDisplaySettings.renderingSettings;

        #region IDebugDisplaySettingsQuery

        /// <inheritdoc/>
        public bool AreAnySettingsActive => m_DebugDisplaySettings.AreAnySettingsActive;

        /// <inheritdoc/>
        public bool IsPostProcessingAllowed => m_DebugDisplaySettings.IsPostProcessingAllowed;

        /// <inheritdoc/>
        public bool IsLightingActive => m_DebugDisplaySettings.IsLightingActive;

        // These modes would require putting custom data into gbuffer, so instead we just disable deferred mode.
        internal bool IsActiveModeUnsupportedForDeferred =>
            m_DebugDisplaySettings.lightingSettings.lightingDebugMode != DebugLightingMode.None ||
            m_DebugDisplaySettings.lightingSettings.lightingFeatureFlags != DebugLightingFeatureFlags.None ||
            m_DebugDisplaySettings.renderingSettings.sceneOverrideMode != DebugSceneOverrideMode.None ||
            m_DebugDisplaySettings.materialSettings.materialDebugMode != DebugMaterialMode.None ||
            m_DebugDisplaySettings.materialSettings.vertexAttributeDebugMode != DebugVertexAttributeMode.None ||
            m_DebugDisplaySettings.materialSettings.materialValidationMode != DebugMaterialValidationMode.None ||
            m_DebugDisplaySettings.renderingSettings.mipInfoMode != DebugMipInfoMode.None;

        /// <inheritdoc/>
        public bool TryGetScreenClearColor(ref Color color)
        {
            return m_DebugDisplaySettings.TryGetScreenClearColor(ref color);
        }

        #endregion

        internal Material ReplacementMaterial => m_ReplacementMaterial;
        internal UniversalRenderPipelineDebugDisplaySettings DebugDisplaySettings => m_DebugDisplaySettings;
        internal ref RTHandle DebugScreenColorHandle => ref m_DebugScreenColorHandle;
        internal ref RTHandle DebugScreenDepthHandle => ref m_DebugScreenDepthHandle;
        internal HDRDebugViewPass hdrDebugViewPass => m_HDRDebugViewPass;

        internal bool HDRDebugViewIsActive(bool resolveFinalTarget)
        {
            // HDR debug views should only apply to the last camera in the stack
            return DebugDisplaySettings.lightingSettings.hdrDebugMode != HDRDebugMode.None && resolveFinalTarget;
        }

        internal bool WriteToDebugScreenTexture(bool resolveFinalTarget)
        {
            return HDRDebugViewIsActive(resolveFinalTarget);
        }

        internal bool IsScreenClearNeeded
        {
            get
            {
                Color color = Color.black;

                return TryGetScreenClearColor(ref color);
            }
        }

        internal bool IsRenderPassSupported
        {
            get
            {
                return RenderingSettings.sceneOverrideMode == DebugSceneOverrideMode.None || RenderingSettings.sceneOverrideMode == DebugSceneOverrideMode.Overdraw;
            }
        }

        internal int stpDebugViewIndex { get { return RenderingSettings.stpDebugViewIndex; } }

        internal DebugHandler()
        {
            m_DebugDisplaySettings = UniversalRenderPipelineDebugDisplaySettings.Instance;

            if (GraphicsSettings.TryGetRenderPipelineSettings<UniversalRenderPipelineDebugShaders>(out var shaders))
            {
                m_ReplacementMaterial = (shaders.debugReplacementPS != null) ? CoreUtils.CreateEngineMaterial(shaders.debugReplacementPS) : null;
                m_HDRDebugViewMaterial = (shaders.hdrDebugViewPS != null) ? CoreUtils.CreateEngineMaterial(shaders.hdrDebugViewPS) : null;
            }

            m_HDRDebugViewPass = new HDRDebugViewPass(m_HDRDebugViewMaterial);

            m_RuntimeTextures = GraphicsSettings.GetRenderPipelineSettings<UniversalRenderPipelineRuntimeTextures>();
            if (m_RuntimeTextures != null)
            {
                m_DebugFontTexture = RTHandles.Alloc(m_RuntimeTextures.debugFontTexture);
            }
        }

        public void Dispose()
        {
            m_HDRDebugViewPass.Dispose();
            m_DebugScreenColorHandle?.Release();
            m_DebugScreenDepthHandle?.Release();
            m_DebugFontTexture?.Release();
            CoreUtils.Destroy(m_HDRDebugViewMaterial);
            CoreUtils.Destroy(m_ReplacementMaterial);
        }

        internal bool IsActiveForCamera(bool isPreviewCamera)
        {
            return !isPreviewCamera && AreAnySettingsActive;
        }

        internal bool TryGetFullscreenDebugMode(out DebugFullScreenMode debugFullScreenMode)
        {
            return TryGetFullscreenDebugMode(out debugFullScreenMode, out _);
        }

        internal bool TryGetFullscreenDebugMode(out DebugFullScreenMode debugFullScreenMode, out int textureHeightPercent)
        {
            debugFullScreenMode = RenderingSettings.fullScreenDebugMode;
            textureHeightPercent = RenderingSettings.fullScreenDebugModeOutputSizeScreenPercent;
            return debugFullScreenMode != DebugFullScreenMode.None;
        }

        internal static void ConfigureColorDescriptorForDebugScreen(ref RenderTextureDescriptor descriptor, int cameraWidth, int cameraHeight)
        {
            descriptor.width = cameraWidth;
            descriptor.height = cameraHeight;
            descriptor.useMipMap = false;
            descriptor.autoGenerateMips = false;
            descriptor.useDynamicScale = true;
            descriptor.depthStencilFormat = GraphicsFormat.None;
        }

        internal static void ConfigureDepthDescriptorForDebugScreen(ref RenderTextureDescriptor descriptor, GraphicsFormat depthStencilFormat, int cameraWidth, int cameraHeight)
        {
            descriptor.width = cameraWidth;
            descriptor.height = cameraHeight;
            descriptor.useMipMap = false;
            descriptor.autoGenerateMips = false;
            descriptor.useDynamicScale = true;
            descriptor.depthStencilFormat = depthStencilFormat;
        }

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        internal void SetupShaderProperties(RasterCommandBuffer cmd, int passIndex = 0)
        {
            if (LightingSettings.lightingDebugMode == DebugLightingMode.ShadowCascades)
            {
                // we disable cubemap reflections, too distracting (in TemplateLWRP for ex.)
                cmd.EnableShaderKeyword("_DEBUG_ENVIRONMENTREFLECTIONS_OFF");
            }
            else
            {
                cmd.DisableShaderKeyword("_DEBUG_ENVIRONMENTREFLECTIONS_OFF");
            }

            switch (RenderingSettings.sceneOverrideMode)
            {
                case DebugSceneOverrideMode.Overdraw:
                {
                    // Target texture can contains only a value between 0 and 1
                    // So we encode the number of overdraw in the range (0, max displayed overdaw count)
                    // The value will be clamped by the GPU driver
                    var value = 1 / (float)RenderingSettings.maxOverdrawCount;
                    cmd.SetGlobalColor(k_DebugColorPropertyId, new Color(value, value, value, 1));
                    break;
                }

                case DebugSceneOverrideMode.Wireframe:
                {
                    cmd.SetGlobalColor(k_DebugColorPropertyId, Color.black);
                    break;
                }

                case DebugSceneOverrideMode.SolidWireframe:
                {
                    cmd.SetGlobalColor(k_DebugColorPropertyId, (passIndex == 0) ? Color.white : Color.black);
                    break;
                }

                case DebugSceneOverrideMode.ShadedWireframe:
                {
                    if (passIndex == 0)
                    {
                        cmd.SetKeyword(ShaderGlobalKeywords.DEBUG_DISPLAY, false);
                    }
                    else if (passIndex == 1)
                    {
                        cmd.SetGlobalColor(k_DebugColorPropertyId, Color.black);
                        cmd.SetKeyword(ShaderGlobalKeywords.DEBUG_DISPLAY, true);
                    }

                    break;
                }
            }

            switch (MaterialSettings.materialValidationMode)
            {
                case DebugMaterialValidationMode.Albedo:
                    cmd.SetGlobalFloat(k_DebugValidateAlbedoMinLuminanceId, MaterialSettings.albedoMinLuminance);
                    cmd.SetGlobalFloat(k_DebugValidateAlbedoMaxLuminanceId, MaterialSettings.albedoMaxLuminance);
                    cmd.SetGlobalFloat(k_DebugValidateAlbedoSaturationToleranceId, MaterialSettings.albedoSaturationTolerance);
                    cmd.SetGlobalFloat(k_DebugValidateAlbedoHueToleranceId, MaterialSettings.albedoHueTolerance);
                    cmd.SetGlobalColor(k_DebugValidateAlbedoCompareColorId, MaterialSettings.albedoCompareColor.linear);
                    break;

                case DebugMaterialValidationMode.Metallic:
                    cmd.SetGlobalFloat(k_DebugValidateMetallicMinValueId, MaterialSettings.metallicMinValue);
                    cmd.SetGlobalFloat(k_DebugValidateMetallicMaxValueId, MaterialSettings.metallicMaxValue);
                    break;
            }
        }

        internal void SetDebugRenderTarget(RTHandle renderTarget, Rect displayRect, bool supportsStereo, Vector4 dataRangeRemap)
        {
            m_HasDebugRenderTarget = true;
            m_DebugRenderTargetSupportsStereo = supportsStereo;
            m_DebugRenderTarget = renderTarget;
            m_DebugRenderTargetPixelRect = new Vector4(displayRect.x, displayRect.y, displayRect.width, displayRect.height);
            m_DebugRenderTargetRangeRemap = dataRangeRemap;
        }

        internal void ResetDebugRenderTarget()
        {
            m_HasDebugRenderTarget = false;
        }

        class DebugFinalValidationPassData
        {
            public bool isFinalPass;
            public bool resolveFinalTarget;
            public bool isActiveForCamera;
            public bool hasDebugRenderTarget;

            public TextureHandle debugRenderTargetHandle;
            public int debugTexturePropertyId;

            public Vector4 debugRenderTargetPixelRect;
            public int debugRenderTargetSupportsStereo;
            public Vector4 debugRenderTargetRangeRemap;

            public TextureHandle debugFontTextureHandle;

            // NOTE: The settings are references.
            // It's assumed they're the same for the whole frame. Build timeline != execution timeline!
            // Ideally these would be copied without any allocs.
            public DebugDisplaySettingsRendering renderingSettings;
        }

        DebugFinalValidationPassData InitDebugFinalValidationPassData(DebugFinalValidationPassData passData, UniversalCameraData cameraData, bool isFinalPass)
        {
            passData.isFinalPass = isFinalPass;
            passData.resolveFinalTarget = cameraData.resolveFinalTarget;
            passData.isActiveForCamera = IsActiveForCamera(cameraData.isPreviewCamera);
            passData.hasDebugRenderTarget = m_HasDebugRenderTarget;

            passData.debugRenderTargetHandle = TextureHandle.nullHandle;
            passData.debugTexturePropertyId = m_DebugRenderTargetSupportsStereo ? k_DebugTexturePropertyId : k_DebugTextureNoStereoPropertyId;

            passData.debugRenderTargetPixelRect = m_DebugRenderTargetPixelRect;
            passData.debugRenderTargetSupportsStereo = m_DebugRenderTargetSupportsStereo ? 1 : 0;
            passData.debugRenderTargetRangeRemap = m_DebugRenderTargetRangeRemap;

            passData.debugFontTextureHandle = TextureHandle.nullHandle;

            passData.renderingSettings = RenderingSettings;

            return passData;
        }

        static void UpdateShaderGlobalPropertiesForFinalValidationPass(RasterCommandBuffer cmd, DebugFinalValidationPassData data)
        {
            // Ensure final validation & fullscreen debug modes are only done once in the very final pass, for the last camera on the stack.
            bool isFinal = data.isFinalPass && data.resolveFinalTarget;
            if (!isFinal)
            {
                cmd.SetKeyword(ShaderGlobalKeywords.DEBUG_DISPLAY, false);
                return;
            }

            if (data.isActiveForCamera)
            {
                cmd.SetKeyword(ShaderGlobalKeywords.DEBUG_DISPLAY, true);
            }
            else
            {
                cmd.SetKeyword(ShaderGlobalKeywords.DEBUG_DISPLAY, false);
            }

            if (data.hasDebugRenderTarget)
            {
                if(data.debugRenderTargetHandle.IsValid())
                    cmd.SetGlobalTexture(data.debugTexturePropertyId,  data.debugRenderTargetHandle);

                cmd.SetGlobalVector(k_DebugTextureDisplayRect, data.debugRenderTargetPixelRect);
                cmd.SetGlobalInteger(k_DebugRenderTargetSupportsStereo, data.debugRenderTargetSupportsStereo);
                cmd.SetGlobalVector(k_DebugRenderTargetRangeRemap, data.debugRenderTargetRangeRemap);
            }

            var renderingSettings = data.renderingSettings;
            if (renderingSettings.validationMode == DebugValidationMode.HighlightOutsideOfRange)
            {
                cmd.SetGlobalInteger(k_ValidationChannelsId, (int)renderingSettings.validationChannels);
                cmd.SetGlobalFloat(k_RangeMinimumId, renderingSettings.validationRangeMin);
                cmd.SetGlobalFloat(k_RangeMaximumId, renderingSettings.validationRangeMax);
            }

            if (renderingSettings.mipInfoMode != DebugMipInfoMode.None)
            {
                // some (not all) of these need text rendering
                cmd.SetGlobalTexture(k_DebugFontId, data.debugFontTextureHandle);
            }
        }

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        internal void UpdateShaderGlobalPropertiesForFinalValidationPass(CommandBuffer cmd, UniversalCameraData cameraData, bool isFinalPass)
        {
            UpdateShaderGlobalPropertiesForFinalValidationPass(CommandBufferHelpers.GetRasterCommandBuffer(cmd), InitDebugFinalValidationPassData(s_DebugFinalValidationPassData, cameraData, isFinalPass));
            cmd.SetGlobalTexture(s_DebugFinalValidationPassData.debugTexturePropertyId,  m_DebugRenderTarget);
            if (RenderingSettings.mipInfoMode != DebugMipInfoMode.None)
            {
                // some (not all) of these need text rendering
                cmd.SetGlobalTexture(k_DebugFontId, m_RuntimeTextures.debugFontTexture);
            }
        }

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        internal void UpdateShaderGlobalPropertiesForFinalValidationPass(RenderGraph renderGraph, UniversalCameraData cameraData, bool isFinalPass)
        {
            using (var builder = renderGraph.AddRasterRenderPass<DebugFinalValidationPassData>(nameof(UpdateShaderGlobalPropertiesForFinalValidationPass), out var passData, s_DebugFinalValidationSampler))
            {
                InitDebugFinalValidationPassData(passData, cameraData, isFinalPass);
                passData.debugRenderTargetHandle = renderGraph.ImportTexture(m_DebugRenderTarget);
                passData.debugFontTextureHandle = renderGraph.ImportTexture(m_DebugFontTexture);

                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);
                builder.SetGlobalTextureAfterPass(passData.debugRenderTargetHandle, passData.debugTexturePropertyId);
                builder.SetGlobalTextureAfterPass(passData.debugFontTextureHandle, k_DebugFontId);
                builder.UseTexture(passData.debugRenderTargetHandle);
                builder.UseTexture(passData.debugFontTextureHandle);
                builder.SetRenderFunc(static (DebugFinalValidationPassData data, RasterGraphContext context) =>
                {
                    UpdateShaderGlobalPropertiesForFinalValidationPass(context.cmd, data);
                });
            }
        }

        class DebugSetupPassData
        {
            public bool isActiveForCamera;

            // NOTE: The settings are references.
            // It's assumed they're the same for the whole frame. Build timeline != execution timeline!
            // Ideally these would be copied without any allocs.
            public DebugDisplaySettingsMaterial  materialSettings;
            public DebugDisplaySettingsRendering renderingSettings;
            public DebugDisplaySettingsLighting  lightingSettings;
        }

        DebugSetupPassData InitDebugSetupPassData(DebugSetupPassData passData, bool isPreviewCamera)
        {
            passData.isActiveForCamera = IsActiveForCamera(isPreviewCamera);
            passData.materialSettings  = MaterialSettings;
            passData.renderingSettings = RenderingSettings;
            passData.lightingSettings  = LightingSettings;
            return passData;
        }

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        static void Setup(RasterCommandBuffer cmd, DebugSetupPassData passData)
        {
            if (passData.isActiveForCamera)
            {
                cmd.SetKeyword(ShaderGlobalKeywords.DEBUG_DISPLAY, true);

                // Material settings...
                cmd.SetGlobalFloat(k_DebugMaterialModeId, (int)passData.materialSettings.materialDebugMode);
                cmd.SetGlobalFloat(k_DebugVertexAttributeModeId, (int)passData.materialSettings.vertexAttributeDebugMode);

                cmd.SetGlobalInteger(k_DebugMaterialValidationModeId, (int)passData.materialSettings.materialValidationMode);

                // Rendering settings...
                cmd.SetGlobalInteger(k_DebugMipInfoModeId, (int)passData.renderingSettings.mipInfoMode);
                cmd.SetGlobalInteger(k_DebugMipMapStatusModeId, (int)passData.renderingSettings.mipDebugStatusMode);
                cmd.SetGlobalInteger(k_DebugMipMapShowStatusCodeId, passData.renderingSettings.mipDebugStatusShowCode ? 1 : 0);
                cmd.SetGlobalFloat(k_DebugMipMapOpacityId, passData.renderingSettings.mipDebugOpacity);
                cmd.SetGlobalFloat(k_DebugMipMapRecentlyUpdatedCooldownId, passData.renderingSettings.mipDebugRecentUpdateCooldown);
                cmd.SetGlobalFloat(k_DebugMipMapTerrainTextureModeId, (int)passData.renderingSettings.mipDebugTerrainTexture);
                cmd.SetGlobalInteger(k_DebugSceneOverrideModeId, (int)passData.renderingSettings.sceneOverrideMode);
                cmd.SetGlobalInteger(k_DebugFullScreenModeId, (int)passData.renderingSettings.fullScreenDebugMode);
                cmd.SetGlobalInteger(k_DebugMaxPixelCost, (int)passData.renderingSettings.maxOverdrawCount);
                cmd.SetGlobalInteger(k_DebugValidationModeId, (int)passData.renderingSettings.validationMode);
                cmd.SetGlobalColor(k_DebugValidateBelowMinThresholdColorPropertyId, Color.red);
                cmd.SetGlobalColor(k_DebugValidateAboveMaxThresholdColorPropertyId, Color.blue);

                // Lighting settings...
                cmd.SetGlobalFloat(k_DebugLightingModeId, (int)passData.lightingSettings.lightingDebugMode);
                cmd.SetGlobalInteger(k_DebugLightingFeatureFlagsId, (int)passData.lightingSettings.lightingFeatureFlags);

                // Set-up any other persistent properties...
                cmd.SetGlobalColor(k_DebugColorInvalidModePropertyId, Color.red);
#if UNITY_EDITOR
                cmd.SetGlobalFloat(k_DebugCurrentRealTimeId, (float)EditorApplication.timeSinceStartup);
#else
                cmd.SetGlobalFloat(k_DebugCurrentRealTimeId, Time.realtimeSinceStartup);
#endif
            }
            else
            {
                cmd.SetKeyword(ShaderGlobalKeywords.DEBUG_DISPLAY, false);
            }
        }

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        internal void Setup(CommandBuffer cmd, bool isPreviewCamera)
        {
            Setup(CommandBufferHelpers.GetRasterCommandBuffer(cmd), InitDebugSetupPassData(s_DebugSetupPassData, isPreviewCamera));
        }

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        internal void Setup(RenderGraph renderGraph, bool isPreviewCamera)
        {
            using (var builder = renderGraph.AddRasterRenderPass<DebugSetupPassData>(nameof(Setup), out var passData, s_DebugSetupSampler))
            {
                InitDebugSetupPassData(passData, isPreviewCamera);
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);
                builder.SetRenderFunc(static (DebugSetupPassData data, RasterGraphContext context) =>
                {
                    Setup(context.cmd, data);
                });
            }
        }

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        internal void Render(RenderGraph renderGraph, UniversalCameraData cameraData, TextureHandle srcColor, TextureHandle overlayTexture, TextureHandle dstColor)
        {
            if (IsActiveForCamera(cameraData.isPreviewCamera) && HDRDebugViewIsActive(cameraData.resolveFinalTarget))
            {
                m_HDRDebugViewPass.RenderHDRDebug(renderGraph, cameraData, srcColor, overlayTexture, dstColor, LightingSettings.hdrDebugMode);
            }
        }

        #region DebugRendererLists

        internal DebugRendererLists CreateRendererListsWithDebugRenderState(
             ScriptableRenderContext context,
             ref CullingResults cullResults,
             ref DrawingSettings drawingSettings,
             ref FilteringSettings filteringSettings,
             ref RenderStateBlock renderStateBlock)
        {
            DebugRendererLists debug = new DebugRendererLists(this, filteringSettings);
            debug.CreateRendererListsWithDebugRenderState(context, ref cullResults, ref drawingSettings, ref filteringSettings, ref renderStateBlock);
            return debug;
        }

        internal DebugRendererLists CreateRendererListsWithDebugRenderState(
            RenderGraph renderGraph,
            ref CullingResults cullResults,
            ref DrawingSettings drawingSettings,
            ref FilteringSettings filteringSettings,
            ref RenderStateBlock renderStateBlock)
        {
            DebugRendererLists debug = new DebugRendererLists(this, filteringSettings);
            debug.CreateRendererListsWithDebugRenderState(renderGraph, ref cullResults, ref drawingSettings, ref filteringSettings, ref renderStateBlock);
            return debug;
        }
        #endregion
    }

    internal class DebugRendererLists
    {
        private readonly DebugHandler m_DebugHandler;
        readonly FilteringSettings m_FilteringSettings;
        List<DebugRenderSetup> m_DebugRenderSetups = new List<DebugRenderSetup>(2);
        List<RendererList> m_ActiveDebugRendererList = new List<RendererList>(2);
        List<RendererListHandle> m_ActiveDebugRendererListHdl = new List<RendererListHandle>(2);

        public DebugRendererLists(DebugHandler debugHandler,
            FilteringSettings filteringSettings)
        {
            m_DebugHandler = debugHandler;
            m_FilteringSettings = filteringSettings;
        }

        private void CreateDebugRenderSetups(FilteringSettings filteringSettings)
        {
            var sceneOverrideMode = m_DebugHandler.DebugDisplaySettings.renderingSettings.sceneOverrideMode;
            var numIterations = ((sceneOverrideMode == DebugSceneOverrideMode.SolidWireframe) || (sceneOverrideMode == DebugSceneOverrideMode.ShadedWireframe)) ? 2 : 1;
            for (var i = 0; i < numIterations; i++)
                m_DebugRenderSetups.Add(new DebugRenderSetup(m_DebugHandler, i, filteringSettings));
        }

        void DisposeDebugRenderLists()
        {
            foreach (var debugRenderSetup in m_DebugRenderSetups)
            {
                debugRenderSetup.Dispose();
            }
            m_DebugRenderSetups.Clear();
            m_ActiveDebugRendererList.Clear();
            m_ActiveDebugRendererListHdl.Clear();
        }

        internal void CreateRendererListsWithDebugRenderState(
             ScriptableRenderContext context,
             ref CullingResults cullResults,
             ref DrawingSettings drawingSettings,
             ref FilteringSettings filteringSettings,
             ref RenderStateBlock renderStateBlock)
        {
            CreateDebugRenderSetups(filteringSettings);
            foreach (DebugRenderSetup debugRenderSetup in m_DebugRenderSetups)
            {
                DrawingSettings debugDrawingSettings = debugRenderSetup.CreateDrawingSettings(drawingSettings);
                RenderStateBlock debugRenderStateBlock = debugRenderSetup.GetRenderStateBlock(renderStateBlock);
                RendererList rendererList = new RendererList();
                RenderingUtils.CreateRendererListWithRenderStateBlock(context, ref cullResults, debugDrawingSettings, filteringSettings, debugRenderStateBlock, ref rendererList);
                m_ActiveDebugRendererList.Add((rendererList));
            }
        }

        internal void CreateRendererListsWithDebugRenderState(
            RenderGraph renderGraph,
            ref CullingResults cullResults,
            ref DrawingSettings drawingSettings,
            ref FilteringSettings filteringSettings,
            ref RenderStateBlock renderStateBlock)
        {
            CreateDebugRenderSetups(filteringSettings);
            foreach (DebugRenderSetup debugRenderSetup in m_DebugRenderSetups)
            {
                DrawingSettings debugDrawingSettings = debugRenderSetup.CreateDrawingSettings(drawingSettings);
                RenderStateBlock debugRenderStateBlock = debugRenderSetup.GetRenderStateBlock(renderStateBlock);
                RendererListHandle rendererListHdl = new RendererListHandle();
                RenderingUtils.CreateRendererListWithRenderStateBlock(renderGraph, ref cullResults, debugDrawingSettings, filteringSettings, debugRenderStateBlock, ref rendererListHdl);
                m_ActiveDebugRendererListHdl.Add((rendererListHdl));
            }
        }

        internal void PrepareRendererListForRasterPass(IRasterRenderGraphBuilder builder)
        {
            foreach (RendererListHandle rendererListHdl in m_ActiveDebugRendererListHdl)
            {
                builder.UseRendererList(rendererListHdl);
            }
        }

        internal void DrawWithRendererList(RasterCommandBuffer cmd)
        {
            foreach (DebugRenderSetup debugRenderSetup in m_DebugRenderSetups)
            {
                debugRenderSetup.Begin(cmd);
                RendererList rendererList = new RendererList();
                if (m_ActiveDebugRendererList.Count > 0)
                {
                    rendererList = m_ActiveDebugRendererList[debugRenderSetup.GetIndex()];
                }
                else if(m_ActiveDebugRendererListHdl.Count > 0)
                {
                    rendererList = m_ActiveDebugRendererListHdl[debugRenderSetup.GetIndex()];
                }

                debugRenderSetup.DrawWithRendererList(cmd, ref rendererList);
                debugRenderSetup.End(cmd);
            }

            DisposeDebugRenderLists();
        }
    }
}
