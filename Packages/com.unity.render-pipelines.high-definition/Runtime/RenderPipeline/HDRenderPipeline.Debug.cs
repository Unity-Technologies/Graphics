using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEditor;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        bool m_FullScreenDebugPushed;
        Rendering.DebugOverlay m_DebugOverlay = new Rendering.DebugOverlay();
        TextureHandle m_DebugFullScreenTexture;
        BufferHandle m_DebugFullScreenComputeBuffer;
        ShaderVariablesDebugDisplay m_ShaderVariablesDebugDisplayCB = new ShaderVariablesDebugDisplay();

        ComputeShader m_ClearFullScreenBufferCS;
        int m_ClearFullScreenBufferKernel;

        Material m_DebugViewMaterialGBuffer;
        Material m_DebugViewMaterialGBufferShadowMask;
        Material m_currentDebugViewMaterialGBuffer;
        Material m_DebugDisplayLatlong;
        Material m_DebugFullScreen;
        Material m_DebugColorPicker;
        Material m_DebugExposure;
        Material m_DebugHDROutput;
        Material m_DebugViewTilesMaterial;
        Material m_DebugHDShadowMapMaterial;
        Material m_DebugLocalVolumetricFogMaterial;
        Material m_DebugBlitMaterial;
        Material m_DebugDrawClustersBoundsMaterial;

        // Color monitors
        Material m_DebugVectorscope;
        Material m_DebugWaveform;

        ComputeShader m_ComputePositionNormal; // Used to write a pixel's position and normal in a compute buffer to debug probe sampling

#if ENABLE_VIRTUALTEXTURES
        Material m_VTDebugBlit;
#endif

        private readonly DebugDisplaySettingsUI m_DebugDisplaySettingsUI = new DebugDisplaySettingsUI();
        DebugDisplaySettings m_DebugDisplaySettings = new DebugDisplaySettings();

        /// <summary>
        /// Debug display settings.
        /// </summary>
        public DebugDisplaySettings debugDisplaySettings { get { return m_DebugDisplaySettings; } }
        static DebugDisplaySettings s_NeutralDebugDisplaySettings = new DebugDisplaySettings();
        internal DebugDisplaySettings m_CurrentDebugDisplaySettings;

        void InitializeDebug()
        {
            m_ComputePositionNormal = runtimeShaders.probeVolumeSamplingDebugComputeShader;
            m_DebugViewMaterialGBuffer           = CoreUtils.CreateEngineMaterial(runtimeShaders.debugViewMaterialGBufferPS);
            m_DebugViewMaterialGBufferShadowMask = CoreUtils.CreateEngineMaterial(runtimeShaders.debugViewMaterialGBufferPS);
            m_DebugViewMaterialGBufferShadowMask.EnableKeyword("SHADOWS_SHADOWMASK");

            m_DebugDisplayLatlong             = CoreUtils.CreateEngineMaterial(runtimeShaders.debugDisplayLatlongPS);
            m_DebugFullScreen                 = CoreUtils.CreateEngineMaterial(runtimeShaders.debugFullScreenPS);
            m_DebugColorPicker                = CoreUtils.CreateEngineMaterial(runtimeShaders.debugColorPickerPS);
            m_DebugExposure                   = CoreUtils.CreateEngineMaterial(runtimeShaders.debugExposurePS);
            m_DebugHDROutput                  = CoreUtils.CreateEngineMaterial(runtimeShaders.debugHDRPS);
            m_DebugViewTilesMaterial          = CoreUtils.CreateEngineMaterial(runtimeShaders.debugViewTilesPS);
            m_DebugHDShadowMapMaterial        = CoreUtils.CreateEngineMaterial(runtimeShaders.debugHDShadowMapPS);
            m_DebugLocalVolumetricFogMaterial = CoreUtils.CreateEngineMaterial(runtimeShaders.debugLocalVolumetricFogAtlasPS);
            m_DebugBlitMaterial               = CoreUtils.CreateEngineMaterial(runtimeShaders.debugBlitQuad);
            m_DebugWaveform                   = CoreUtils.CreateEngineMaterial(runtimeShaders.debugWaveformPS);
            m_DebugVectorscope                = CoreUtils.CreateEngineMaterial(runtimeShaders.debugVectorscopePS);

            m_ClearFullScreenBufferCS        = runtimeShaders.clearDebugBufferCS;
            m_ClearFullScreenBufferKernel    = m_ClearFullScreenBufferCS.FindKernel("clearMain");

#if ENABLE_VIRTUALTEXTURES
            m_VTDebugBlit = CoreUtils.CreateEngineMaterial(runtimeShaders.debugViewVirtualTexturingBlit);
#endif
        }

        void CleanupDebug()
        {
            CoreUtils.Destroy(m_DebugViewMaterialGBuffer);
            CoreUtils.Destroy(m_DebugViewMaterialGBufferShadowMask);
            CoreUtils.Destroy(m_DebugDisplayLatlong);
            CoreUtils.Destroy(m_DebugFullScreen);
            CoreUtils.Destroy(m_DebugColorPicker);
            CoreUtils.Destroy(m_DebugExposure);
            CoreUtils.Destroy(m_DebugHDROutput);
            CoreUtils.Destroy(m_DebugViewTilesMaterial);
            CoreUtils.Destroy(m_DebugHDShadowMapMaterial);
            CoreUtils.Destroy(m_DebugLocalVolumetricFogMaterial);
            CoreUtils.Destroy(m_DebugBlitMaterial);
            CoreUtils.Destroy(m_DebugWaveform);
            CoreUtils.Destroy(m_DebugVectorscope);
#if ENABLE_VIRTUALTEXTURES
            CoreUtils.Destroy(m_VTDebugBlit);
#endif
        }

        internal bool showCascade
        {
            get => m_DebugDisplaySettings.GetDebugLightingMode() == DebugLightingMode.VisualizeCascade;
            set
            {
                if (value)
                    m_DebugDisplaySettings.SetDebugLightingMode(DebugLightingMode.VisualizeCascade);
                else
                    m_DebugDisplaySettings.SetDebugLightingMode(DebugLightingMode.None);
            }
        }

        internal bool NeedDebugDisplay()
        {
             return m_CurrentDebugDisplaySettings.IsDebugDisplayEnabled();
        }

        bool NeedColorPickerDebug(DebugDisplaySettings debugSettings)
        {
            return debugSettings.data.colorPickerDebugSettings.colorPickerMode != ColorPickerDebugMode.None
                || debugSettings.data.falseColorDebugSettings.falseColor
                || debugSettings.data.lightingDebugSettings.debugLightingMode == DebugLightingMode.LuminanceMeter;
        }

        bool NeedExposureDebugMode(DebugDisplaySettings debugSettings)
        {
            return debugSettings.data.lightingDebugSettings.exposureDebugMode != ExposureDebugMode.None;
        }

        bool NeedHDRDebugMode(DebugDisplaySettings debugSettings)
        {
            return debugSettings.data.lightingDebugSettings.hdrDebugMode != HDRDebugMode.None;
        }

        bool NeedsFullScreenDebugMode()
        {
            bool fullScreenDebugEnabled = m_CurrentDebugDisplaySettings.data.fullScreenDebugMode != FullScreenDebugMode.None;
            bool lightingDebugEnabled = m_CurrentDebugDisplaySettings.data.lightingDebugSettings.shadowDebugMode == ShadowMapDebugMode.SingleShadow;
            bool historyBufferViewEnabled = m_CurrentDebugDisplaySettings.data.historyBuffersView != -1;
            bool mipmapDebuggingEnabled = m_CurrentDebugDisplaySettings.data.mipMapDebugSettings.debugMipMapMode != DebugMipMapMode.None;

            return fullScreenDebugEnabled || lightingDebugEnabled || historyBufferViewEnabled || mipmapDebuggingEnabled;
        }

        unsafe void ApplyDebugDisplaySettings(HDCamera hdCamera, CommandBuffer cmd, bool aovOutput)
        {
            // See ShaderPassForward.hlsl: for forward shaders, if DEBUG_DISPLAY is enabled and no DebugLightingMode or DebugMipMapMod
            // modes have been set, lighting is automatically skipped (To avoid some crashed due to lighting RT not set on console).
            // However debug mode like colorPickerModes and false color don't need DEBUG_DISPLAY and must work with the lighting.
            // So we will enabled DEBUG_DISPLAY independently

            bool isSceneLightingDisabled = CoreUtils.IsSceneLightingDisabled(hdCamera.camera);
            bool debugDisplayEnabledOrSceneLightingDisabled = m_CurrentDebugDisplaySettings.IsDebugDisplayEnabled() || isSceneLightingDisabled;

            // Enable globally the keyword DEBUG_DISPLAY on shader that support it with multi-compile
            CoreUtils.SetKeyword(cmd, "DEBUG_DISPLAY", debugDisplayEnabledOrSceneLightingDisabled);

            // Setting this all the time due to a strange bug that either reports a (globally) bound texture as not bound or where SetGlobalTexture doesn't behave as expected.
            // As a workaround we bind it regardless of debug display. Eventually with
            cmd.SetGlobalTexture(HDShaderIDs._DebugMatCapTexture, runtimeTextures.matcapTex);

            m_ShaderVariablesGlobalCB._GlobalTessellationFactorMultiplier = (m_CurrentDebugDisplaySettings.data.fullScreenDebugMode == FullScreenDebugMode.QuadOverdraw) ? 0.0f : 1.0f;

            if (debugDisplayEnabledOrSceneLightingDisabled ||
                m_CurrentDebugDisplaySettings.data.colorPickerDebugSettings.colorPickerMode != ColorPickerDebugMode.None ||
                m_CurrentDebugDisplaySettings.IsDebugExposureModeEnabled())
            {
                var lightingDebugSettings = m_CurrentDebugDisplaySettings.data.lightingDebugSettings;
                var materialDebugSettings = m_CurrentDebugDisplaySettings.data.materialDebugSettings;

                var linearAlbedo = lightingDebugSettings.overrideAlbedoValue.linear;
                var linearSpecularColor = lightingDebugSettings.overrideSpecularColorValue.linear;

                var debugAlbedo = new Vector4(lightingDebugSettings.overrideAlbedo ? 1.0f : 0.0f, linearAlbedo.r, linearAlbedo.g, linearAlbedo.b);
                var debugSmoothness = new Vector4(lightingDebugSettings.overrideSmoothness ? 1.0f : 0.0f, lightingDebugSettings.overrideSmoothnessValue, 0.0f, 0.0f);
                var debugNormal = new Vector4(lightingDebugSettings.overrideNormal ? 1.0f : 0.0f, 0.0f, 0.0f, 0.0f);
                var debugAmbientOcclusion = new Vector4(lightingDebugSettings.overrideAmbientOcclusion ? 1.0f : 0.0f, lightingDebugSettings.overrideAmbientOcclusionValue, 0.0f, 0.0f);
                var debugSpecularColor = new Vector4(lightingDebugSettings.overrideSpecularColor ? 1.0f : 0.0f, linearSpecularColor.r, linearSpecularColor.g, linearSpecularColor.b);
                var debugEmissiveColor = new Vector4(lightingDebugSettings.overrideEmissiveColor ? 1.0f : 0.0f, lightingDebugSettings.overrideEmissiveColorValue.r, lightingDebugSettings.overrideEmissiveColorValue.g, lightingDebugSettings.overrideEmissiveColorValue.b);
                var debugTrueMetalColor = new Vector4(materialDebugSettings.materialValidateTrueMetal ? 1.0f : 0.0f, materialDebugSettings.materialValidateTrueMetalColor.r, materialDebugSettings.materialValidateTrueMetalColor.g, materialDebugSettings.materialValidateTrueMetalColor.b);

                ref var cb = ref m_ShaderVariablesDebugDisplayCB;

                var debugMaterialIndices = m_CurrentDebugDisplaySettings.GetDebugMaterialIndexes();
                for (int i = 0; i < 11; ++i)
                {
                    cb._DebugViewMaterialArray[i * 4] = (uint)debugMaterialIndices[i]; // Only x component is used.
                }
                for (int i = 0; i < 32; ++i)
                {
                    for (int j = 0; j < 4; ++j)
                        cb._DebugRenderingLayersColors[i * 4 + j] = m_CurrentDebugDisplaySettings.data.lightingDebugSettings.debugRenderingLayersColors[i][j];
                }

                if (apvIsEnabled)
                {
                    var subdivColors = ProbeReferenceVolume.instance.subdivisionDebugColors;
                    for (int i = 0; i < 7; ++i)
                    {
                        for (int j = 0; j < 4; ++j)
                            cb._DebugAPVSubdivColors[i * 4 + j] = subdivColors[i][j];
                    }
                }

                DebugLightingMode debugLightingMode = m_CurrentDebugDisplaySettings.GetDebugLightingMode();

                // Mat Cap Mode Logic
                {
                    bool matCapMixAlbedo = false;
                    float matCapMixScale = 1.0f;

                    if (debugLightingMode == DebugLightingMode.MatcapView)
                    {
                        matCapMixAlbedo = m_CurrentDebugDisplaySettings.data.lightingDebugSettings.matCapMixAlbedo;
                        matCapMixScale = m_CurrentDebugDisplaySettings.data.lightingDebugSettings.matCapMixScale;
                    }
#if UNITY_EDITOR
                    else if (isSceneLightingDisabled)
                    {
                        // Forcing the MatCap Mode when scene view lighting is disabled. Also use the default values
                        debugLightingMode = DebugLightingMode.MatcapView;
                        matCapMixAlbedo = HDRenderPipelinePreferences.matCapMode.mixAlbedo.value;
                        matCapMixScale = HDRenderPipelinePreferences.matCapMode.viewScale.value;
                    }
#endif
                    cb._MatcapMixAlbedo = matCapMixAlbedo ? 1 : 0;
                    cb._MatcapViewScale = matCapMixScale;
                }

                cb._DebugLightingMode = (int)debugLightingMode;
                cb._DebugLightLayersMask = (int)m_CurrentDebugDisplaySettings.GetDebugLightLayersMask();
                cb._DebugShadowMapMode = (int)m_CurrentDebugDisplaySettings.GetDebugShadowMapMode();
                cb._DebugMipMapMode = (int)m_CurrentDebugDisplaySettings.GetDebugMipMapMode();
                cb._DebugMipMapOpacity = m_CurrentDebugDisplaySettings.GetDebugMipMapOpacity();
                cb._DebugMipMapStatusMode = (int) m_CurrentDebugDisplaySettings.GetDebugMipMapStatusMode();
                cb._DebugMipMapShowStatusCode = m_CurrentDebugDisplaySettings.GetDebugMipMapShowStatusCode() ? 1 : 0;
                cb._DebugMipMapRecentlyUpdatedCooldown = m_CurrentDebugDisplaySettings.GetDebugMipMapRecentlyUpdatedCooldown();
                cb._DebugIsLitShaderModeDeferred = hdCamera.frameSettings.litShaderMode == LitShaderMode.Deferred ? 1 : 0;
                cb._DebugMipMapModeTerrainTexture = (int)m_CurrentDebugDisplaySettings.GetDebugMipMapModeTerrainTexture();
                cb._ColorPickerMode = (int)m_CurrentDebugDisplaySettings.GetDebugColorPickerMode();
                cb._DebugFullScreenMode = (int)m_CurrentDebugDisplaySettings.data.fullScreenDebugMode;

                cb._DebugViewportSize = hdCamera.screenSize;
                cb._DebugLightingAlbedo = debugAlbedo;
                cb._DebugLightingSmoothness = debugSmoothness;
                cb._DebugLightingNormal = debugNormal;
                cb._DebugLightingAmbientOcclusion = debugAmbientOcclusion;
                cb._DebugLightingSpecularColor = debugSpecularColor;
                cb._DebugLightingEmissiveColor = debugEmissiveColor;
                cb._DebugLightingMaterialValidateHighColor = materialDebugSettings.materialValidateHighColor;
                cb._DebugLightingMaterialValidateLowColor = materialDebugSettings.materialValidateLowColor;
                cb._DebugLightingMaterialValidatePureMetalColor = debugTrueMetalColor;

                cb._MousePixelCoord = HDUtils.GetMouseCoordinates(hdCamera);
                cb._MouseClickPixelCoord = HDUtils.GetMouseClickCoordinates(hdCamera);

                cb._DebugSingleShadowIndex = m_CurrentDebugDisplaySettings.data.lightingDebugSettings.shadowDebugUseSelection ? m_DebugSelectedLightShadowIndex : (int)m_CurrentDebugDisplaySettings.data.lightingDebugSettings.shadowMapIndex;

                cb._DebugAOVOutput = aovOutput ? 1 : 0;

#if UNITY_EDITOR
                cb._DebugCurrentRealTime = (float) EditorApplication.timeSinceStartup;
#else
                cb._DebugCurrentRealTime = Time.realtimeSinceStartup;
#endif

                ConstantBuffer.PushGlobal(cmd, m_ShaderVariablesDebugDisplayCB, HDShaderIDs._ShaderVariablesDebugDisplay);

                cmd.SetGlobalTexture(HDShaderIDs._DebugFont, runtimeTextures.debugFontTex);
            }
        }

        class MonitorsPassData
        {
            public float         sizeRatio;
            public Material      blitMaterial;
            public TextureHandle colorTexture;
            public TextureHandle downsampledInput;
            public Vector2Int    downsampledSize;
            public Vector2Int    inputSize;
            public MonitorsDebugSettings settings;
            public int runtimeDebugPannelWidth;

            // Waveform data
            public ComputeShader       waveformCS;
            public BufferHandle waveformBuffer;
            public TextureHandle       waveformTexture;
            public Material            waveformMaterial;
            public int                 waveformClearKernel;
            public int                 waveformGatherKernel;

            // Vectorscope data
            public ComputeShader       vectorscopeCS;
            public BufferHandle vectorscopeBuffer;
            public TextureHandle       vectorscopeTexture;
            public Material            vectorscopeMaterial;
            public Vector2Int          vectorscopeSize;
            public int                 vectorscopeBufferSize;
            public int                 vectorscopeGatherKernel;
            public int                 vectorscopeClearKernel;
        }

        void RenderMonitorsOverlay(RenderGraph renderGraph, TextureHandle colorBuffer, HDCamera hdCamera)
        {
            MonitorsDebugSettings settings = m_CurrentDebugDisplaySettings.data.monitorsDebugSettings;

            // If no monitor is enabled, skipping the pass
            if (!settings.vectorscopeToggle && !settings.waveformToggle)
                return;

            using RenderGraphBuilder builder = renderGraph.AddRenderPass("Monitors overlay", out MonitorsPassData data);

            // Filling in pass data
            data.runtimeDebugPannelWidth = HDUtils.GetRuntimeDebugPanelWidth(hdCamera);
            data.blitMaterial            = m_DebugBlitMaterial;
            data.sizeRatio               = settings.monitorsSize;
            data.colorTexture            = builder.ReadWriteTexture(colorBuffer);
            data.settings                = m_CurrentDebugDisplaySettings.data.monitorsDebugSettings;
            data.inputSize               = new Vector2Int(hdCamera.actualWidth, hdCamera.actualHeight);

            // Downsampled input
            data.downsampledSize  = new Vector2Int(hdCamera.actualWidth / 2, hdCamera.actualHeight / 2);
            data.downsampledInput = builder.CreateTransientTexture(new TextureDesc(data.downsampledSize.x, data.downsampledSize.y) {
                format = GraphicsFormat.R16G16B16A16_UNorm,
                name              = "Downsampled color buffer"
            });

            FillWaveformData   (data, builder);
            FillVectorscopeData(data, (int)(hdCamera.actualHeight * data.sizeRatio), builder);

            builder.SetRenderFunc(static (MonitorsPassData passData, RenderGraphContext ctx) =>
            {
                // Down-sampling the input
                HDUtils.BlitCameraTexture(ctx.cmd, passData.colorTexture, passData.downsampledInput, 0f, true);

                if (passData.settings.waveformToggle)
                    RenderWaveformDebug(passData, ctx);
                if (passData.settings.vectorscopeToggle)
                    RenderVectorScopeDebug(passData, ctx);

                // Finally composing the monitors on the color buffer
                const int spacing          = 5;
                int       horizontalOffset = passData.runtimeDebugPannelWidth + spacing;

                if (passData.settings.vectorscopeToggle)
                {
                    ctx.cmd.SetGlobalTexture(HDShaderIDs._InputTexture, passData.vectorscopeTexture);
                    ctx.cmd.SetRenderTarget (passData.colorTexture);
                    ctx.cmd.SetViewport     (new Rect(horizontalOffset, spacing, passData.vectorscopeSize.x, passData.vectorscopeSize.y));
                    ctx.cmd.DrawProcedural  (Matrix4x4.identity, passData.blitMaterial, 0, MeshTopology.Triangles, 3, 1, null);

                    horizontalOffset += passData.vectorscopeSize.x + spacing;
                }

                if (passData.settings.waveformToggle)
                {
                    ctx.cmd.SetGlobalTexture(HDShaderIDs._InputTexture, passData.waveformTexture);
                    ctx.cmd.SetRenderTarget (passData.colorTexture);
                    ctx.cmd.SetViewport     (new Rect(horizontalOffset, spacing, passData.inputSize.x * passData.sizeRatio, passData.inputSize.y * passData.sizeRatio));
                    ctx.cmd.DrawProcedural  (Matrix4x4.identity, passData.blitMaterial, 0, MeshTopology.Triangles, 3, 1, null);
                }
            });
        }

        void FillWaveformData(MonitorsPassData data, RenderGraphBuilder builder)
        {
            data.waveformCS           = runtimeShaders.debugWaveformCS;
            data.waveformMaterial     = m_DebugWaveform;
            data.waveformClearKernel  = data.waveformCS.FindKernel("KWaveformClear");
            data.waveformGatherKernel = data.waveformCS.FindKernel("KWaveformGather");

            data.waveformBuffer = builder.CreateTransientBuffer(
                new BufferDesc(data.downsampledSize.x * data.downsampledSize.y, sizeof(uint) * 4) {
                    name = "Waveform Debug Buffer"
                }
            );

            data.waveformTexture = builder.CreateTransientTexture(
                new TextureDesc(data.downsampledSize.x, data.downsampledSize.y) {
                    enableRandomWrite = true,
                    format  = GraphicsFormat.B10G11R11_UFloatPack32,
                    name              = "Waveform Debug Texture"
                }
            );
        }

        void FillVectorscopeData(MonitorsPassData data, int size, RenderGraphBuilder builder)
        {
            data.vectorscopeCS           = runtimeShaders.debugVectorscopeCS;
            data.vectorscopeSize         = new Vector2Int(size, size);
            data.vectorscopeMaterial     = m_DebugVectorscope;
            data.vectorscopeBufferSize   = data.vectorscopeSize.x * data.vectorscopeSize.x;
            data.vectorscopeClearKernel  = data.vectorscopeCS.FindKernel("KVectorscopeClear");
            data.vectorscopeGatherKernel = data.vectorscopeCS.FindKernel("KVectorscopeGather");

            data.vectorscopeTexture = builder.CreateTransientTexture(new TextureDesc(data.vectorscopeSize.x, data.vectorscopeSize.y) {
                enableRandomWrite = true,
                format            = GetColorBufferFormat(),
                name              = "Vectorscope Debug Texture"
            });

            data.vectorscopeBuffer = builder.CreateTransientBuffer(new BufferDesc(data.vectorscopeBufferSize, sizeof(uint) * 4) {
                name = "Vectorscope Debug Buffer"
            });
        }

        static void RenderVectorScopeDebug(MonitorsPassData data, RenderGraphContext ctx)
        {
            const float kThreadGroupSizeX = 16f;
            const float kThreadGroupSizeY = 16f;

            var parameters = new Vector4(data.vectorscopeSize.x, data.vectorscopeSize.y, data.settings.vectorscopeExposure);

            // Clearing the vectorscope buffer
            ctx.cmd.SetComputeBufferParam(data.vectorscopeCS, data.vectorscopeClearKernel, HDShaderIDs._VectorscopeBuffer, data.vectorscopeBuffer);
            ctx.cmd.SetComputeIntParam(data.vectorscopeCS, HDShaderIDs._BufferSize, data.vectorscopeSize.x);
            ctx.cmd.DispatchCompute(data.vectorscopeCS, data.vectorscopeClearKernel,
                Mathf.CeilToInt(data.vectorscopeSize.x / kThreadGroupSizeX),
                Mathf.CeilToInt(data.vectorscopeSize.y / kThreadGroupSizeY), 1);

            // Gather all pixels and fill our vectorscope
            ctx.cmd.SetComputeBufferParam (data.vectorscopeCS, data.vectorscopeGatherKernel, HDShaderIDs._VectorscopeBuffer, data.vectorscopeBuffer);
            ctx.cmd.SetComputeTextureParam(data.vectorscopeCS, data.vectorscopeGatherKernel, HDShaderIDs._Source,            data.downsampledInput);
            ctx.cmd.DispatchCompute(data.vectorscopeCS, data.vectorscopeGatherKernel,
                Mathf.CeilToInt(data.downsampledSize.x / kThreadGroupSizeX),
                Mathf.CeilToInt(data.downsampledSize.y / kThreadGroupSizeY), 1);

            // Generate our vectorscope texture
            data.vectorscopeMaterial.SetBuffer(HDShaderIDs._VectorscopeBuffer, data.vectorscopeBuffer);
            data.vectorscopeMaterial.SetVector(HDShaderIDs._VectorscopeParameters, parameters);
            ctx.cmd.SetRenderTarget(data.vectorscopeTexture);
            ctx.cmd.DrawProcedural(Matrix4x4.identity, data.vectorscopeMaterial, 0, MeshTopology.Triangles, 3, 1, null);
        }

        static void RenderWaveformDebug(MonitorsPassData data, RenderGraphContext ctx)
        {
            const float kThreadGroupSizeX = 16f;
            const float kThreadGroupSizeY = 16f;

            var parameters = new Vector4(data.downsampledSize.x, data.downsampledSize.y, data.settings.waveformExposure, data.settings.waveformParade ? 1f : 0f);

            // Clearing the waveform buffer
            ctx.cmd.SetComputeBufferParam(data.waveformCS, data.waveformClearKernel, HDShaderIDs._WaveformBuffer, data.waveformBuffer);
            ctx.cmd.SetComputeVectorParam(data.waveformCS, HDShaderIDs._Params, parameters);
            ctx.cmd.DispatchCompute(data.waveformCS, data.waveformClearKernel,
                Mathf.CeilToInt(data.downsampledSize.x / kThreadGroupSizeX),
                Mathf.CeilToInt(data.downsampledSize.y / kThreadGroupSizeY), 1);

            // Gather all pixels and fill in our waveform
            ctx.cmd.SetComputeTextureParam(data.waveformCS, data.waveformGatherKernel, HDShaderIDs._Source        , data.downsampledInput);
            ctx.cmd.SetComputeBufferParam (data.waveformCS, data.waveformGatherKernel, HDShaderIDs._WaveformBuffer, data.waveformBuffer);
            ctx.cmd.DispatchCompute(data.waveformCS, data.waveformGatherKernel,
                Mathf.CeilToInt(data.downsampledSize.x / kThreadGroupSizeX),
                Mathf.CeilToInt(data.downsampledSize.y / kThreadGroupSizeY), 1);

            // Filling the waveform texture from the buffer
            data.waveformMaterial.SetBuffer(HDShaderIDs._WaveformBuffer, data.waveformBuffer);
            data.waveformMaterial.SetVector(HDShaderIDs._WaveformParameters, parameters);
            ctx.cmd.SetRenderTarget(data.waveformTexture);
            ctx.cmd.DrawProcedural(Matrix4x4.identity, data.waveformMaterial, 0, MeshTopology.Triangles, 3, 1, null);
        }

        class TransparencyOverdrawPassData
        {
            public FrameSettings frameSettings;
            public ShaderVariablesDebugDisplay constantBuffer;

            public RendererListHandle transparencyRL;
            public RendererListHandle transparencyAfterPostRL;
            public RendererListHandle transparencyLowResRL;
        }

        void RenderTransparencyOverdraw(RenderGraph renderGraph, TextureHandle depthBuffer, CullingResults cull, HDCamera hdCamera)
        {
            if (m_CurrentDebugDisplaySettings.IsDebugDisplayEnabled() && m_CurrentDebugDisplaySettings.data.fullScreenDebugMode == FullScreenDebugMode.TransparencyOverdraw)
            {
                TextureHandle transparencyOverdrawOutput = TextureHandle.nullHandle;
                using (var builder = renderGraph.AddRenderPass<TransparencyOverdrawPassData>("Transparency Overdraw", out var passData))
                {
                    var passNames = m_Asset.currentPlatformRenderPipelineSettings.supportTransparentBackface ? m_AllTransparentPassNames : m_TransparentNoBackfaceNames;
                    var stateBlock = new RenderStateBlock
                    {
                        mask = RenderStateMask.Blend,
                        blendState = new BlendState
                        {
                            blendState0 = new RenderTargetBlendState
                            {
                                destinationColorBlendMode = BlendMode.One,
                                sourceColorBlendMode = BlendMode.One,
                                destinationAlphaBlendMode = BlendMode.One,
                                sourceAlphaBlendMode = BlendMode.One,
                                colorBlendOperation = BlendOp.Add,
                                alphaBlendOperation = BlendOp.Add,
                                writeMask = ColorWriteMask.All
                            }
                        }
                    };

                    passData.frameSettings = hdCamera.frameSettings;
                    passData.constantBuffer = m_ShaderVariablesDebugDisplayCB;
                    builder.UseDepthBuffer(depthBuffer, DepthAccess.Read);
                    passData.transparencyRL = builder.UseRendererList(renderGraph.CreateRendererList(
                        CreateTransparentRendererListDesc(cull, hdCamera.camera, passNames, stateBlock: stateBlock)));
                    passData.transparencyAfterPostRL = builder.UseRendererList(
                        renderGraph.CreateRendererList(CreateTransparentRendererListDesc(cull, hdCamera.camera, passNames, renderQueueRange: HDRenderQueue.k_RenderQueue_AfterPostProcessTransparent, stateBlock: stateBlock)));
                    passData.transparencyLowResRL = builder.UseRendererList(
                        renderGraph.CreateRendererList(CreateTransparentRendererListDesc(cull, hdCamera.camera, passNames, renderQueueRange: HDRenderQueue.k_RenderQueue_LowTransparent, stateBlock: stateBlock)));

                    transparencyOverdrawOutput = builder.UseColorBuffer(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true) { name = "Transparency Overdraw", format = GetColorBufferFormat(), clearBuffer = true, clearColor = Color.black }), 0);

                    builder.SetRenderFunc(
                        (TransparencyOverdrawPassData data, RenderGraphContext ctx) =>
                        {
                            data.constantBuffer._DebugTransparencyOverdrawWeight = 1.0f;
                            ConstantBuffer.PushGlobal(ctx.cmd, data.constantBuffer, HDShaderIDs._ShaderVariablesDebugDisplay);

                            DrawTransparentRendererList(ctx.renderContext, ctx.cmd, data.frameSettings, data.transparencyRL);
                            DrawTransparentRendererList(ctx.renderContext, ctx.cmd, data.frameSettings, data.transparencyAfterPostRL);

                            data.constantBuffer._DebugTransparencyOverdrawWeight = 0.25f;
                            ConstantBuffer.PushGlobal(ctx.cmd, data.constantBuffer, HDShaderIDs._ShaderVariablesDebugDisplay);
                            DrawTransparentRendererList(ctx.renderContext, ctx.cmd, data.frameSettings, data.transparencyLowResRL);
                        });
                }

                PushFullScreenDebugTexture(renderGraph, transparencyOverdrawOutput, FullScreenDebugMode.TransparencyOverdraw);
            }
        }

        class FullScreenDebugPassData
        {
            public FrameSettings frameSettings;
            public BufferHandle debugBuffer;
            public RendererListHandle rendererList;
            public ComputeShader clearBufferCS;
            public int clearBufferCSKernel;
            public int width;
            public int height;
            public int viewCount;
        }

        void RenderFullScreenDebug(RenderGraph renderGraph, TextureHandle colorBuffer, TextureHandle depthBuffer, CullingResults cull, HDCamera hdCamera)
        {
            using (var builder = renderGraph.AddRenderPass<FullScreenDebugPassData>("FullScreen Debug", out var passData))
            {
                builder.UseColorBuffer(colorBuffer, 0);
                builder.UseDepthBuffer(depthBuffer, DepthAccess.Read);

                m_DebugFullScreenComputeBuffer = renderGraph.CreateBuffer(new BufferDesc(hdCamera.actualWidth * hdCamera.actualHeight * hdCamera.viewCount, sizeof(uint)));

                passData.frameSettings = hdCamera.frameSettings;
                passData.debugBuffer = builder.WriteBuffer(m_DebugFullScreenComputeBuffer);
                passData.rendererList = builder.UseRendererList(renderGraph.CreateRendererList(CreateOpaqueRendererListDesc(cull, hdCamera.camera, m_FullScreenDebugPassNames, renderQueueRange: RenderQueueRange.all)));
                passData.clearBufferCS = m_ClearFullScreenBufferCS;
                passData.clearBufferCSKernel = m_ClearFullScreenBufferKernel;
                passData.width = hdCamera.actualWidth;
                passData.height = hdCamera.actualHeight;
                passData.viewCount = hdCamera.viewCount;

                builder.SetRenderFunc(
                    (FullScreenDebugPassData data, RenderGraphContext ctx) =>
                    {
                        ctx.cmd.SetComputeVectorParam(data.clearBufferCS, HDShaderIDs._QuadOverdrawClearBuffParams, new Vector4(data.width, data.height, 0.0f, 0.0f));
                        ctx.cmd.SetComputeBufferParam(data.clearBufferCS, data.clearBufferCSKernel, HDShaderIDs._FullScreenDebugBuffer, data.debugBuffer);
                        ctx.cmd.DispatchCompute(data.clearBufferCS, data.clearBufferCSKernel, HDUtils.DivRoundUp(data.width, 16), HDUtils.DivRoundUp(data.height, 16), data.viewCount);

                        ctx.cmd.SetRandomWriteTarget(1, data.debugBuffer);
                        CoreUtils.DrawRendererList(ctx.renderContext, ctx.cmd, data.rendererList);
                        ctx.cmd.ClearRandomWriteTargets();
                    });
            }

            // This is not useful in theory but its just to register there is a fullscreen debug active
            PushFullScreenDebugTexture(renderGraph, ResolveMSAAColor(renderGraph, hdCamera, colorBuffer));
        }

        class ResolveFullScreenDebugPassData
        {
            public DebugDisplaySettings debugDisplaySettings;
            public Material debugFullScreenMaterial;
            public HDCamera hdCamera;
            public Vector4 depthPyramidParams;
            public TextureHandle output;
            public TextureHandle input;
            public TextureHandle depthPyramid;
            public TextureHandle thickness;
            public BufferHandle thicknessReindex;
            public BufferHandle fullscreenBuffer;
        }

        TextureHandle ResolveFullScreenDebug(RenderGraph renderGraph, TextureHandle inputFullScreenDebug, TextureHandle depthPyramid, HDCamera hdCamera, GraphicsFormat rtFormat = GraphicsFormat.R16G16B16A16_SFloat)
        {
            using (var builder = renderGraph.AddRenderPass<ResolveFullScreenDebugPassData>("ResolveFullScreenDebug", out var passData))
            {
                passData.hdCamera = hdCamera;
                passData.debugDisplaySettings = m_CurrentDebugDisplaySettings;
                passData.debugFullScreenMaterial = m_DebugFullScreen;
                passData.input = builder.ReadTexture(inputFullScreenDebug);
                passData.depthPyramid = builder.ReadTexture(depthPyramid);
                {
                    int mipCount = hdCamera.depthBufferMipChainInfo.mipLevelCount;
                    int mipIndex = Mathf.Min(Mathf.FloorToInt(m_CurrentDebugDisplaySettings.data.fullscreenDebugMip * mipCount), mipCount - 1);
                    Vector2Int mipOffset = hdCamera.depthBufferMipChainInfo.mipLevelOffsets[mipIndex];
                    if (m_CurrentDebugDisplaySettings.data.depthPyramidView == DepthPyramidDebugView.CheckerboardDepth && hdCamera.depthBufferMipChainInfo.mipLevelCountCheckerboard != 0)
                    {
                        mipIndex = Mathf.Min(mipIndex, hdCamera.depthBufferMipChainInfo.mipLevelCountCheckerboard - 1);
                        mipOffset = hdCamera.depthBufferMipChainInfo.mipLevelOffsetsCheckerboard[mipIndex];
                    }
                    passData.depthPyramidParams = new Vector4(mipIndex, mipOffset.x, mipOffset.y, 0.0f);
                }

                if (IsComputeThicknessNeeded(hdCamera))
                    passData.thickness = builder.ReadTexture(HDComputeThickness.Instance.GetThicknessTextureArray());
                else
                    passData.thickness = builder.ReadTexture(renderGraph.defaultResources.blackTextureArrayXR);
                passData.thicknessReindex = builder.ReadBuffer(renderGraph.ImportBuffer(HDComputeThickness.Instance.GetReindexMap()));

                // On Vulkan, not binding the Random Write Target will result in an invalid drawcall.
                // To avoid that, if the compute buffer is invalid, we bind a dummy compute buffer anyway.
                if (m_DebugFullScreenComputeBuffer.IsValid())
                    passData.fullscreenBuffer = builder.ReadBuffer(m_DebugFullScreenComputeBuffer);
                else
                    passData.fullscreenBuffer = builder.CreateTransientBuffer(new BufferDesc(4, sizeof(uint)));
                passData.output = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, false /* we dont want DRS on this output target*/, true /*We want XR support on this output target*/)
                    { format = rtFormat, name = "ResolveFullScreenDebug" }));

                builder.SetRenderFunc(
                    (ResolveFullScreenDebugPassData data, RenderGraphContext ctx) =>
                    {
                        var mpb = ctx.renderGraphPool.GetTempMaterialPropertyBlock();
                        ComputeVolumetricFogSliceCountAndScreenFraction(data.hdCamera.volumeStack.GetComponent<Fog>(), out var volumetricSliceCount, out _);

                        BindGlobalThicknessBuffers(data.thickness, data.thicknessReindex, ctx.cmd);

                        mpb.SetTexture(HDShaderIDs._DebugFullScreenTexture, data.input);
                        mpb.SetTexture(HDShaderIDs._CameraDepthTexture, data.depthPyramid);
                        mpb.SetFloat(HDShaderIDs._FullScreenDebugMode, (float)data.debugDisplaySettings.data.fullScreenDebugMode);
                        mpb.SetFloat(HDShaderIDs._ApplyExposure, data.debugDisplaySettings.data.SupportsExposure() && data.debugDisplaySettings.data.applyExposure ? 1 : 0);
                        if (data.debugDisplaySettings.data.enableDebugDepthRemap)
                            mpb.SetVector(HDShaderIDs._FullScreenDebugDepthRemap, new Vector4(data.debugDisplaySettings.data.fullScreenDebugDepthRemap.x, data.debugDisplaySettings.data.fullScreenDebugDepthRemap.y, data.hdCamera.camera.nearClipPlane, data.hdCamera.camera.farClipPlane));
                        else // Setup neutral value
                            mpb.SetVector(HDShaderIDs._FullScreenDebugDepthRemap, new Vector4(0.0f, 1.0f, 0.0f, 1.0f));
                        mpb.SetVector(HDShaderIDs._DebugDepthPyramidParams, data.depthPyramidParams);
                        mpb.SetInt(HDShaderIDs._DebugContactShadowLightIndex, data.debugDisplaySettings.data.fullScreenContactShadowLightIndex);
                        mpb.SetFloat(HDShaderIDs._TransparencyOverdrawMaxPixelCost, (float)data.debugDisplaySettings.data.transparencyDebugSettings.maxPixelCost);
                        mpb.SetFloat(HDShaderIDs._FogVolumeOverdrawMaxValue, (float)volumetricSliceCount);
                        mpb.SetFloat(HDShaderIDs._QuadOverdrawMaxQuadCost, (float)data.debugDisplaySettings.data.maxQuadCost);
                        mpb.SetFloat(HDShaderIDs._VertexDensityMaxPixelCost, (float)data.debugDisplaySettings.data.maxVertexDensity);
                        mpb.SetFloat(HDShaderIDs._MinMotionVector, data.debugDisplaySettings.data.minMotionVectorLength);
                        mpb.SetVector(HDShaderIDs._MotionVecIntensityParams, new Vector4(data.debugDisplaySettings.data.motionVecVisualizationScale, data.debugDisplaySettings.data.motionVecIntensityHeat ? 1 : 0, 0, 0));
                        mpb.SetInt(HDShaderIDs._ComputeThicknessLayerIndex, (int)data.debugDisplaySettings.data.computeThicknessLayerIndex);
                        mpb.SetInt(HDShaderIDs._ComputeThicknessShowOverlapCount, data.debugDisplaySettings.data.computeThicknessShowOverlapCount ? 1 : 0);
                        mpb.SetFloat(HDShaderIDs._ComputeThicknessScale, data.debugDisplaySettings.data.computeThicknessScale);
                        mpb.SetInt(HDShaderIDs._VolumetricCloudsDebugMode, (int)data.debugDisplaySettings.data.volumetricCloudDebug);

                        ctx.cmd.SetRandomWriteTarget(1, data.fullscreenBuffer);
                        HDUtils.DrawFullScreen(ctx.cmd, data.debugFullScreenMaterial, data.output, mpb, 0);
                        ctx.cmd.ClearRandomWriteTargets();
                    });

                return passData.output;
            }
        }

        class ResolveColorPickerDebugPassData
        {
            public HDCamera hdCamera;
            public DebugDisplaySettings debugDisplaySettings;
            public Material colorPickerMaterial;
            public TextureHandle output;
            public TextureHandle input;
        }

        TextureHandle ResolveColorPickerDebug(RenderGraph renderGraph, TextureHandle inputColorPickerDebug, HDCamera hdCamera, GraphicsFormat rtFormat = GraphicsFormat.R16G16B16A16_SFloat)
        {
            using (var builder = renderGraph.AddRenderPass<ResolveColorPickerDebugPassData>("ResolveColorPickerDebug", out var passData))
            {
                passData.hdCamera = hdCamera;
                passData.debugDisplaySettings = m_CurrentDebugDisplaySettings;
                passData.colorPickerMaterial = m_DebugColorPicker;
                passData.input = builder.ReadTexture(inputColorPickerDebug);
                passData.output = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                    { format = rtFormat, name = "ResolveColorPickerDebug" }));

                builder.SetRenderFunc(
                    (ResolveColorPickerDebugPassData data, RenderGraphContext ctx) =>
                    {
                        var falseColorDebugSettings = data.debugDisplaySettings.data.falseColorDebugSettings;
                        var colorPickerDebugSettings = data.debugDisplaySettings.data.colorPickerDebugSettings;
                        var falseColorThresholds = new Vector4(falseColorDebugSettings.colorThreshold0, falseColorDebugSettings.colorThreshold1, falseColorDebugSettings.colorThreshold2, falseColorDebugSettings.colorThreshold3);

                        // Here we have three cases:
                        // - Material debug is enabled, this is the buffer we display
                        // - Otherwise we display the HDR buffer before postprocess and distortion
                        // - If fullscreen debug is enabled we always use it
                        data.colorPickerMaterial.SetTexture(HDShaderIDs._DebugColorPickerTexture, data.input);
                        data.colorPickerMaterial.SetColor(HDShaderIDs._ColorPickerFontColor, colorPickerDebugSettings.fontColor);
                        data.colorPickerMaterial.SetInt(HDShaderIDs._FalseColorEnabled, falseColorDebugSettings.falseColor ? 1 : 0);
                        data.colorPickerMaterial.SetVector(HDShaderIDs._FalseColorThresholds, falseColorThresholds);
                        data.colorPickerMaterial.SetVector(HDShaderIDs._MousePixelCoord, HDUtils.GetMouseCoordinates(data.hdCamera));
                        data.colorPickerMaterial.SetVector(HDShaderIDs._MouseClickPixelCoord, HDUtils.GetMouseClickCoordinates(data.hdCamera));

                        // The material display debug perform sRGBToLinear conversion as the final blit currently hardcodes a linearToSrgb conversion. As when we read with color picker this is not done,
                        // we perform it inside the color picker shader. But we shouldn't do it for HDR buffer.
                        data.colorPickerMaterial.SetFloat(HDShaderIDs._ApplyLinearToSRGB, data.debugDisplaySettings.IsDebugMaterialDisplayEnabled() ? 1.0f : 0.0f);

                        HDUtils.DrawFullScreen(ctx.cmd, data.colorPickerMaterial, data.output);
                    });

                return passData.output;
            }
        }

        class DebugOverlayPassData
        {
            public Rendering.DebugOverlay debugOverlay;
            public TextureHandle colorBuffer;
            public TextureHandle depthBuffer;
        }

        class SkyReflectionOverlayPassData
            : DebugOverlayPassData
        {
            public LightingDebugSettings lightingDebugSettings;
            public Material debugLatlongMaterial;
            public Texture skyReflectionTexture;
        }

        void RenderSkyReflectionOverlay(RenderGraph renderGraph, TextureHandle colorBuffer, TextureHandle depthBuffer, HDCamera hdCamera)
        {
            if (!m_CurrentDebugDisplaySettings.data.lightingDebugSettings.displaySkyReflection)
                return;

            using (var builder = renderGraph.AddRenderPass<SkyReflectionOverlayPassData>("SkyReflectionOverlay", out var passData))
            {
                passData.debugOverlay = m_DebugOverlay;
                passData.colorBuffer = builder.UseColorBuffer(colorBuffer, 0);
                passData.depthBuffer = builder.UseDepthBuffer(depthBuffer, DepthAccess.ReadWrite);
                passData.lightingDebugSettings = m_CurrentDebugDisplaySettings.data.lightingDebugSettings;
                passData.skyReflectionTexture = m_SkyManager.GetSkyReflection(hdCamera);
                passData.debugLatlongMaterial = m_DebugDisplayLatlong;

                builder.SetRenderFunc(
                    (SkyReflectionOverlayPassData data, RenderGraphContext ctx) =>
                    {
                        var mpb = ctx.renderGraphPool.GetTempMaterialPropertyBlock();

                        ctx.cmd.SetViewport(data.debugOverlay.Next());
                        mpb.SetTexture(HDShaderIDs._InputCubemap, data.skyReflectionTexture);
                        mpb.SetFloat(HDShaderIDs._Mipmap, data.lightingDebugSettings.skyReflectionMipmap);
                        mpb.SetFloat(HDShaderIDs._ApplyExposure, 1.0f);
                        mpb.SetFloat(HDShaderIDs._SliceIndex, data.lightingDebugSettings.cubeArraySliceIndex);
                        ctx.cmd.DrawProcedural(Matrix4x4.identity, data.debugLatlongMaterial, 0, MeshTopology.Triangles, 3, 1, mpb);
                    });
            }
        }

        void RenderRayCountOverlay(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle colorBuffer, TextureHandle depthBuffer, TextureHandle rayCountTexture)
        {
            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.RayTracing))
                return;

            m_RayCountManager.EvaluateRayCount(renderGraph, hdCamera, colorBuffer, depthBuffer, rayCountTexture);
        }

        class RenderAtlasDebugOverlayPassData
            : DebugOverlayPassData
        {
            public Texture atlasTexture;
            public int mipLevel;
            public Material debugBlitMaterial;
        }

        void RenderAtlasDebugOverlay(RenderGraph renderGraph, TextureHandle colorBuffer, TextureHandle depthBuffer, Texture atlas, int slice, int mipLevel, bool applyExposure, string passName, HDProfileId profileID)
        {
            using (var builder = renderGraph.AddRenderPass<RenderAtlasDebugOverlayPassData>(passName, out var passData, ProfilingSampler.Get(profileID)))
            {
                passData.debugOverlay = m_DebugOverlay;
                passData.colorBuffer = builder.UseColorBuffer(colorBuffer, 0);
                passData.depthBuffer = builder.UseDepthBuffer(depthBuffer, DepthAccess.ReadWrite);
                passData.debugBlitMaterial = m_DebugBlitMaterial;
                passData.mipLevel = mipLevel;
                passData.atlasTexture = atlas;

                builder.SetRenderFunc(
                    (RenderAtlasDebugOverlayPassData data, RenderGraphContext ctx) =>
                    {
                        Debug.Assert(data.atlasTexture.dimension == TextureDimension.Tex2D || data.atlasTexture.dimension == TextureDimension.Tex2DArray);

                        ctx.cmd.SetViewport(data.debugOverlay.Next((float)data.atlasTexture.width / data.atlasTexture.height));

                        int shaderPass;
                        var mpb = ctx.renderGraphPool.GetTempMaterialPropertyBlock();
                        mpb.SetFloat(HDShaderIDs._ApplyExposure, applyExposure ? 1.0f : 0.0f);
                        mpb.SetFloat(HDShaderIDs._Mipmap, data.mipLevel);
                        if (data.atlasTexture.dimension == TextureDimension.Tex2D)
                        {
                            shaderPass = 0;
                            mpb.SetTexture(HDShaderIDs._InputTexture, data.atlasTexture);
                        }
                        else
                        {
                            shaderPass = 1;
                            mpb.SetTexture(HDShaderIDs._InputTextureArray, data.atlasTexture);
                            mpb.SetInt(HDShaderIDs._ArrayIndex, slice);
                        }

                        ctx.cmd.DrawProcedural(Matrix4x4.identity, data.debugBlitMaterial, shaderPass, MeshTopology.Triangles, 3, 1, mpb);
                    });
            }
        }

        class RenderTileClusterDebugOverlayPassData
            : DebugOverlayPassData
        {
            public HDCamera hdCamera;
            public TextureHandle depthPyramidTexture;
            public BufferHandle tileList;
            public BufferHandle lightList;
            public BufferHandle perVoxelLightList;
            public BufferHandle dispatchIndirect;
            public Material debugViewTilesMaterial;
            public LightingDebugSettings lightingDebugSettings;
            public Vector4 lightingViewportSize;
        }

        void RenderTileClusterDebugOverlay(RenderGraph renderGraph, TextureHandle colorBuffer, TextureHandle depthBuffer, in BuildGPULightListOutput lightLists, TextureHandle depthPyramidTexture, HDCamera hdCamera)
        {
            // Depending on the debug mode enabled we may not be building the light lists so the buffers would not be valid in this case.
            if (!lightLists.tileList.IsValid())
                return;

            if (m_CurrentDebugDisplaySettings.data.lightingDebugSettings.tileClusterDebug == TileClusterDebug.None)
                return;

            using (var builder = renderGraph.AddRenderPass<RenderTileClusterDebugOverlayPassData>("RenderTileAndClusterDebugOverlay", out var passData, ProfilingSampler.Get(HDProfileId.TileClusterLightingDebug)))
            {
                passData.hdCamera = hdCamera;
                passData.debugOverlay = m_DebugOverlay;
                passData.colorBuffer = builder.UseColorBuffer(colorBuffer, 0);
                passData.depthPyramidTexture = builder.ReadTexture(depthPyramidTexture);
                passData.tileList = builder.ReadBuffer(lightLists.tileList);
                passData.lightList = builder.ReadBuffer(lightLists.lightList);
                passData.perVoxelLightList = builder.ReadBuffer(lightLists.perVoxelLightLists);
                passData.dispatchIndirect = builder.ReadBuffer(lightLists.dispatchIndirectBuffer);
                passData.debugViewTilesMaterial = m_DebugViewTilesMaterial;
                passData.lightingDebugSettings = m_CurrentDebugDisplaySettings.data.lightingDebugSettings;
                passData.lightingViewportSize = new Vector4(hdCamera.actualWidth, hdCamera.actualHeight, 1.0f / (float)hdCamera.actualWidth, 1.0f / (float)hdCamera.actualHeight);

                builder.SetRenderFunc(
                    (RenderTileClusterDebugOverlayPassData data, RenderGraphContext ctx) =>
                    {
                        int w = data.hdCamera.actualWidth;
                        int h = data.hdCamera.actualHeight;
                        int numTilesX = (w + 15) / 16;
                        int numTilesY = (h + 15) / 16;
                        int numTiles = numTilesX * numTilesY;

                        var lightingDebug = data.lightingDebugSettings;

                        // Debug tiles
                        if (lightingDebug.tileClusterDebug == TileClusterDebug.MaterialFeatureVariants)
                        {
                            if (GetFeatureVariantsEnabled(data.hdCamera.frameSettings))
                            {
                                // featureVariants
                                data.debugViewTilesMaterial.SetInt(HDShaderIDs._NumTiles, numTiles);
                                data.debugViewTilesMaterial.SetInt(HDShaderIDs._ViewTilesFlags, (int)lightingDebug.tileClusterDebugByCategory);
                                data.debugViewTilesMaterial.SetVector(HDShaderIDs._MousePixelCoord, HDUtils.GetMouseCoordinates(data.hdCamera));
                                data.debugViewTilesMaterial.SetVector(HDShaderIDs._MouseClickPixelCoord, HDUtils.GetMouseClickCoordinates(data.hdCamera));
                                data.debugViewTilesMaterial.SetVector(HDShaderIDs._ClusterDebugLightViewportSize, data.lightingViewportSize);
                                data.debugViewTilesMaterial.SetBuffer(HDShaderIDs.g_TileList, data.tileList);
                                data.debugViewTilesMaterial.SetBuffer(HDShaderIDs.g_DispatchIndirectBuffer, data.dispatchIndirect);
                                CoreUtils.SetKeyword(ctx.cmd, "USE_FPTL_LIGHTLIST", true);
                                CoreUtils.SetKeyword(ctx.cmd, "USE_CLUSTERED_LIGHTLIST", false);

                                data.debugViewTilesMaterial.DisableKeyword("SHOW_LIGHT_CATEGORIES");
                                data.debugViewTilesMaterial.EnableKeyword("SHOW_FEATURE_VARIANTS");
                                ctx.cmd.DrawProcedural(Matrix4x4.identity, data.debugViewTilesMaterial, 0, MeshTopology.Triangles, numTiles * 6);
                            }
                        }
                        else // tile or cluster
                        {
                            bool bUseClustered = lightingDebug.tileClusterDebug == TileClusterDebug.Cluster;

                            // lightCategories
                            data.debugViewTilesMaterial.SetInt(HDShaderIDs._ViewTilesFlags, (int)lightingDebug.tileClusterDebugByCategory);
                            data.debugViewTilesMaterial.SetInt(HDShaderIDs._ClusterDebugMode, bUseClustered ? (int)lightingDebug.clusterDebugMode : (int)ClusterDebugMode.VisualizeOpaque);
                            data.debugViewTilesMaterial.SetFloat(HDShaderIDs._ClusterDebugDistance, lightingDebug.clusterDebugDistance);
                            data.debugViewTilesMaterial.SetVector(HDShaderIDs._ClusterDebugLightViewportSize, data.lightingViewportSize);
                            data.debugViewTilesMaterial.SetVector(HDShaderIDs._MousePixelCoord, HDUtils.GetMouseCoordinates(data.hdCamera));
                            data.debugViewTilesMaterial.SetVector(HDShaderIDs._MouseClickPixelCoord, HDUtils.GetMouseClickCoordinates(data.hdCamera));
                            data.debugViewTilesMaterial.SetBuffer(HDShaderIDs.g_vLightListTile, data.lightList);
                            data.debugViewTilesMaterial.SetBuffer(HDShaderIDs.g_vLightListCluster, data.perVoxelLightList);

                            data.debugViewTilesMaterial.SetTexture(HDShaderIDs._CameraDepthTexture, data.depthPyramidTexture);
                            CoreUtils.SetKeyword(ctx.cmd, "USE_FPTL_LIGHTLIST", !bUseClustered);
                            CoreUtils.SetKeyword(ctx.cmd, "USE_CLUSTERED_LIGHTLIST", bUseClustered);

                            data.debugViewTilesMaterial.EnableKeyword("SHOW_LIGHT_CATEGORIES");
                            data.debugViewTilesMaterial.DisableKeyword("SHOW_FEATURE_VARIANTS");
                            if (!bUseClustered && data.hdCamera.msaaEnabled)
                                data.debugViewTilesMaterial.EnableKeyword("DISABLE_TILE_MODE");
                            else
                                data.debugViewTilesMaterial.DisableKeyword("DISABLE_TILE_MODE");

                            HDUtils.DrawFullScreen(ctx.cmd, data.debugViewTilesMaterial, data.colorBuffer);
                        }
                    });
            }
        }

        class RenderShadowsDebugOverlayPassData
            : DebugOverlayPassData
        {
            public LightingDebugSettings lightingDebugSettings;
            public ShadowResult shadowTextures;
            public HDShadowManager shadowManager;
            public int debugSelectedLightShadowIndex;
            public int debugSelectedLightShadowCount;
            public Material debugShadowMapMaterial;
        }

        void RenderShadowsDebugOverlay(RenderGraph renderGraph, TextureHandle colorBuffer, TextureHandle depthBuffer, in ShadowResult shadowResult)
        {
            if (HDRenderPipeline.currentAsset.currentPlatformRenderPipelineSettings.hdShadowInitParams.maxShadowRequests == 0
                || m_CurrentDebugDisplaySettings.data.lightingDebugSettings.shadowDebugMode == ShadowMapDebugMode.None)
                return;

            using (var builder = renderGraph.AddRenderPass<RenderShadowsDebugOverlayPassData>("RenderShadowsDebugOverlay", out var passData, ProfilingSampler.Get(HDProfileId.DisplayShadows)))
            {
                passData.debugOverlay = m_DebugOverlay;
                passData.colorBuffer = builder.UseColorBuffer(colorBuffer, 0);
                passData.depthBuffer = builder.UseDepthBuffer(depthBuffer, DepthAccess.Write);
                passData.lightingDebugSettings = m_CurrentDebugDisplaySettings.data.lightingDebugSettings;
                passData.shadowTextures = HDShadowManager.ReadShadowResult(shadowResult, builder);
                passData.shadowManager = m_ShadowManager;
                passData.debugSelectedLightShadowIndex = m_DebugSelectedLightShadowIndex;
                passData.debugSelectedLightShadowCount = m_DebugSelectedLightShadowCount;
                passData.debugShadowMapMaterial = m_DebugHDShadowMapMaterial;

                builder.SetRenderFunc(
                    (RenderShadowsDebugOverlayPassData data, RenderGraphContext ctx) =>
                    {
                        var lightingDebug = data.lightingDebugSettings;
                        var mpb = ctx.renderGraphPool.GetTempMaterialPropertyBlock();

                        Rect rect;

                        switch (lightingDebug.shadowDebugMode)
                        {
                            case ShadowMapDebugMode.VisualizeShadowMap:
                                int startShadowIndex = (int)lightingDebug.shadowMapIndex;
                                int shadowRequestCount = 1;

#if UNITY_EDITOR
                                if (lightingDebug.shadowDebugUseSelection)
                                {
                                    if (data.debugSelectedLightShadowIndex != -1 && data.debugSelectedLightShadowCount != 0)
                                    {
                                        startShadowIndex = data.debugSelectedLightShadowIndex;
                                        shadowRequestCount = data.debugSelectedLightShadowCount;
                                    }
                                    else
                                    {
                                        // We don't display any shadow map if the selected object is not a light
                                        shadowRequestCount = 0;
                                    }
                                }
#endif
                                for (int shadowIndex = startShadowIndex; shadowIndex < startShadowIndex + shadowRequestCount; shadowIndex++)
                                {
                                    rect = data.debugOverlay.Next();
                                    data.shadowManager.DisplayShadowMap(data.shadowTextures, shadowIndex, ctx.cmd, data.debugShadowMapMaterial, rect.x, rect.y, rect.width, rect.height, lightingDebug.shadowMinValue, lightingDebug.shadowMaxValue, mpb);
                                }
                                break;
                            case ShadowMapDebugMode.VisualizePunctualLightAtlas:
                                rect = data.debugOverlay.Next();
                                data.shadowManager.DisplayShadowAtlas(data.shadowTextures.punctualShadowResult, ctx.cmd, data.debugShadowMapMaterial, rect.x, rect.y, rect.width, rect.height, lightingDebug.shadowMinValue, lightingDebug.shadowMaxValue, mpb);
                                break;
                            case ShadowMapDebugMode.VisualizeCachedPunctualLightAtlas:
                                rect = data.debugOverlay.Next();
                                data.shadowManager.DisplayCachedPunctualShadowAtlas(data.shadowTextures.cachedPunctualShadowResult, ctx.cmd, data.debugShadowMapMaterial, rect.x, rect.y, rect.width, rect.height, lightingDebug.shadowMinValue, lightingDebug.shadowMaxValue, mpb);
                                break;
                            case ShadowMapDebugMode.VisualizeDirectionalLightAtlas:
                                rect = data.debugOverlay.Next();
                                data.shadowManager.DisplayShadowCascadeAtlas(data.shadowTextures.directionalShadowResult, ctx.cmd, data.debugShadowMapMaterial, rect.x, rect.y, rect.width, rect.height, lightingDebug.shadowMinValue, lightingDebug.shadowMaxValue, mpb);
                                break;
                            case ShadowMapDebugMode.VisualizeAreaLightAtlas:
                                rect = data.debugOverlay.Next();
                                data.shadowManager.DisplayAreaLightShadowAtlas(data.shadowTextures.areaShadowResult, ctx.cmd, data.debugShadowMapMaterial, rect.x, rect.y, rect.width, rect.height, lightingDebug.shadowMinValue, lightingDebug.shadowMaxValue, mpb);
                                break;
                            case ShadowMapDebugMode.VisualizeCachedAreaLightAtlas:
                                rect = data.debugOverlay.Next();
                                data.shadowManager.DisplayCachedAreaShadowAtlas(data.shadowTextures.cachedAreaShadowResult, ctx.cmd, data.debugShadowMapMaterial, rect.x, rect.y, rect.width, rect.height, lightingDebug.shadowMinValue, lightingDebug.shadowMaxValue, mpb);
                                break;
                            default:
                                break;
                        }
                    });
            }
        }

        class RenderDecalOverlayPassData
            : DebugOverlayPassData
        {
            public int mipLevel;
            public HDCamera hdCamera;
        }

        void RenderDecalOverlay(RenderGraph renderGraph, TextureHandle colorBuffer, TextureHandle depthBuffer, HDCamera hdCamera)
        {
            if (!HDDebugDisplaySettings.Instance.decalSettings.displayAtlas)
                return;

            using (var builder = renderGraph.AddRenderPass<RenderDecalOverlayPassData>("DecalOverlay", out var passData, ProfilingSampler.Get(HDProfileId.DisplayDebugDecalsAtlas)))
            {
                passData.debugOverlay = m_DebugOverlay;
                passData.colorBuffer = builder.UseColorBuffer(colorBuffer, 0);
                passData.depthBuffer = builder.UseDepthBuffer(depthBuffer, DepthAccess.ReadWrite);
                passData.mipLevel = (int)HDDebugDisplaySettings.Instance.decalSettings.mipLevel;
                passData.hdCamera = hdCamera;

                builder.SetRenderFunc(
                    (RenderDecalOverlayPassData data, RenderGraphContext ctx) =>
                    {
                        DecalSystem.instance.RenderDebugOverlay(data.hdCamera, ctx.cmd, data.mipLevel, data.debugOverlay);
                    });
            }
        }

        void RenderOcclusionOverlay(RenderGraph renderGraph, TextureHandle colorBuffer, HDCamera hdCamera)
        {
            GPUResidentDrawer.RenderDebugOcclusionTestOverlay(
                renderGraph,
                HDDebugDisplaySettings.Instance?.gpuResidentDrawerSettings ?? null,
                hdCamera.camera.GetInstanceID(),
                colorBuffer);
        }

        void RenderOccluderDebugOverlay(RenderGraph renderGraph, TextureHandle colorBuffer, HDCamera hdCamera)
        {
            var debugSettings = HDDebugDisplaySettings.Instance?.gpuResidentDrawerSettings ?? null;
            if (debugSettings != null && debugSettings.occluderDebugViewEnable)
            {
                Rect rect = m_DebugOverlay.Next();
                GPUResidentDrawer.RenderDebugOccluderOverlay(
                    renderGraph,
                    debugSettings,
                    new Vector2(rect.x, rect.y), rect.height,
                    colorBuffer);
            }
        }

        void RenderDebugOverlays(RenderGraph renderGraph,
            TextureHandle                    colorBuffer,
            TextureHandle                    depthBuffer,
            TextureHandle                    depthPyramidTexture,
            TextureHandle                    rayCountTexture,
            in BuildGPULightListOutput       lightLists,
            in ShadowResult                  shadowResult,
            HDCamera                         hdCamera)
        {
            float overlayRatio = m_CurrentDebugDisplaySettings.data.debugOverlayRatio;
            int viewportWidth = (int)hdCamera.finalViewport.width;
            int viewportHeight = (int)hdCamera.finalViewport.height;
            int overlaySize = (int)((float)Math.Min(viewportWidth, viewportHeight) * overlayRatio);
            m_DebugOverlay.StartOverlay(HDUtils.GetRuntimeDebugPanelWidth(hdCamera), viewportHeight - overlaySize, overlaySize, viewportWidth);

            RenderSkyReflectionOverlay(renderGraph, colorBuffer, depthBuffer, hdCamera);
            RenderRayCountOverlay(renderGraph, hdCamera, colorBuffer, depthBuffer, rayCountTexture);

            if (m_CurrentDebugDisplaySettings.data.lightingDebugSettings.displayCookieAtlas)
                RenderAtlasDebugOverlay(renderGraph, colorBuffer, depthBuffer, m_TextureCaches.lightCookieManager.atlasTexture, 0, (int)m_CurrentDebugDisplaySettings.data.lightingDebugSettings.cookieAtlasMipLevel, applyExposure: false, "RenderCookieAtlasOverlay", HDProfileId.DisplayCookieAtlas);

            if (m_CurrentDebugDisplaySettings.data.lightingDebugSettings.displayReflectionProbeAtlas)
                RenderAtlasDebugOverlay(renderGraph, colorBuffer, depthBuffer, m_TextureCaches.reflectionProbeTextureCache.GetAtlasTexture(), (int)m_CurrentDebugDisplaySettings.data.lightingDebugSettings.reflectionProbeSlice,
                    (int)m_CurrentDebugDisplaySettings.data.lightingDebugSettings.reflectionProbeMipLevel, applyExposure: m_CurrentDebugDisplaySettings.data.lightingDebugSettings.reflectionProbeApplyExposure, "RenderReflectionProbeAtlasOverlay", HDProfileId.DisplayReflectionProbeAtlas);

            RenderTileClusterDebugOverlay(renderGraph, colorBuffer, depthBuffer, lightLists, depthPyramidTexture, hdCamera);
            RenderShadowsDebugOverlay(renderGraph, colorBuffer, depthBuffer, shadowResult);
            RenderDecalOverlay(renderGraph, colorBuffer, depthBuffer, hdCamera);
            RenderMonitorsOverlay(renderGraph, colorBuffer, hdCamera);

            ProbeReferenceVolume.instance.RenderFragmentationOverlay(renderGraph, colorBuffer, depthBuffer, m_DebugOverlay);

            RenderOcclusionOverlay(renderGraph, colorBuffer, hdCamera);
            RenderOccluderDebugOverlay(renderGraph, colorBuffer, hdCamera);
        }

        void RenderLightVolumes(RenderGraph renderGraph, TextureHandle destination, TextureHandle depthBuffer, CullingResults cullResults, HDCamera hdCamera)
        {
            if (m_CurrentDebugDisplaySettings.data.lightingDebugSettings.displayLightVolumes)
            {
                s_lightVolumes.RenderLightVolumes(renderGraph, m_CurrentDebugDisplaySettings.data.lightingDebugSettings, destination, depthBuffer, cullResults, hdCamera);
            }
        }

        class DebugImageHistogramData
        {
            public ComputeShader debugImageHistogramCS;
            public ComputeBuffer imageHistogram;

            public int debugImageHistogramKernel;
            public int cameraWidth;
            public int cameraHeight;

            public TextureHandle source;
        }

        void GenerateDebugImageHistogram(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle source)
        {
            if (m_CurrentDebugDisplaySettings.data.lightingDebugSettings.exposureDebugMode != ExposureDebugMode.FinalImageHistogramView)
                return;

            using (var builder = renderGraph.AddRenderPass<DebugImageHistogramData>("Generate Debug Image Histogram", out var passData, ProfilingSampler.Get(HDProfileId.FinalImageHistogram)))
            {
                ValidateComputeBuffer(ref m_DebugImageHistogramBuffer, k_DebugImageHistogramBins, 4 * sizeof(uint));
                m_DebugImageHistogramBuffer.SetData(m_EmptyDebugImageHistogram);    // Clear the histogram

                passData.debugImageHistogramCS = runtimeShaders.debugImageHistogramCS;
                passData.debugImageHistogramKernel = passData.debugImageHistogramCS.FindKernel("KHistogramGen");
                passData.imageHistogram = m_DebugImageHistogramBuffer;
                passData.cameraWidth = postProcessViewportSize.x;
                passData.cameraHeight = postProcessViewportSize.y;
                passData.source = builder.ReadTexture(source);

                builder.SetRenderFunc(
                    (DebugImageHistogramData data, RenderGraphContext ctx) =>
                    {
                        ctx.cmd.SetComputeTextureParam(data.debugImageHistogramCS, data.debugImageHistogramKernel, HDShaderIDs._SourceTexture, data.source);
                        ctx.cmd.SetComputeBufferParam(data.debugImageHistogramCS, data.debugImageHistogramKernel, HDShaderIDs._HistogramBuffer, data.imageHistogram);

                        int threadGroupSizeX = 16;
                        int threadGroupSizeY = 16;
                        int dispatchSizeX = HDUtils.DivRoundUp(data.cameraWidth / 2, threadGroupSizeX);
                        int dispatchSizeY = HDUtils.DivRoundUp(data.cameraHeight / 2, threadGroupSizeY);
                        int totalPixels = data.cameraWidth * data.cameraHeight;
                        ctx.cmd.DispatchCompute(data.debugImageHistogramCS, data.debugImageHistogramKernel, dispatchSizeX, dispatchSizeY, 1);
                    });
            }
        }

        class GenerateHDRDebugData
        {
            public ComputeShader generateXYMappingCS;
            public TextureHandle xyBuffer;

            public int debugXYGenKernel;
            public int cameraWidth;
            public int cameraHeight;
            public Vector4 debugParameters;

            public TextureHandle source;
        }

        TextureHandle GenerateDebugHDRxyMapping(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle source)
        {
            if (m_CurrentDebugDisplaySettings.data.lightingDebugSettings.hdrDebugMode == HDRDebugMode.None)
                return TextureHandle.nullHandle;

            using (var builder = renderGraph.AddRenderPass<GenerateHDRDebugData>("Generate HDR debug data", out var passData, ProfilingSampler.Get(HDProfileId.HDRDebugData)))
            {
                passData.generateXYMappingCS = runtimeShaders.debugHDRxyMappingCS;
                passData.debugXYGenKernel = passData.generateXYMappingCS.FindKernel("KCIExyGen");
                passData.cameraWidth = postProcessViewportSize.x;
                passData.cameraHeight = postProcessViewportSize.y;
                passData.source = builder.ReadTexture(source);

                passData.xyBuffer = builder.ReadWriteTexture(renderGraph.CreateTexture(new TextureDesc(k_SizeOfHDRXYMapping, k_SizeOfHDRXYMapping, true, true)
                    { format = GraphicsFormat.R32_SFloat, enableRandomWrite = true, clearBuffer = true, name = "HDR_xyMapping" }));

                ColorGamut gamut = HDROutputActiveForCameraType(hdCamera) ? HDRDisplayColorGamutForCamera(hdCamera) : ColorGamut.Rec709;
                HDROutputUtils.ConfigureHDROutput(passData.generateXYMappingCS, gamut, HDROutputUtils.Operation.ColorConversion);
                passData.debugParameters = new Vector4(k_SizeOfHDRXYMapping, k_SizeOfHDRXYMapping, 0, 0);

                builder.SetRenderFunc(
                    (GenerateHDRDebugData data, RenderGraphContext ctx) =>
                    {
                        ctx.cmd.SetComputeTextureParam(data.generateXYMappingCS, data.debugXYGenKernel, HDShaderIDs._SourceTexture, data.source);
                        ctx.cmd.SetComputeVectorParam(data.generateXYMappingCS, HDShaderIDs._HDRxyBufferDebugParams, data.debugParameters);
                        ctx.cmd.SetComputeTextureParam(data.generateXYMappingCS, data.debugXYGenKernel, HDShaderIDs._xyBuffer, data.xyBuffer);

                        int threadGroupSizeX = 8;
                        int threadGroupSizeY = 8;
                        int dispatchSizeX = HDUtils.DivRoundUp(data.cameraWidth, threadGroupSizeX);
                        int dispatchSizeY = HDUtils.DivRoundUp(data.cameraHeight, threadGroupSizeY);
                        int totalPixels = data.cameraWidth * data.cameraHeight;
                        ctx.cmd.DispatchCompute(data.generateXYMappingCS, data.debugXYGenKernel, dispatchSizeX, dispatchSizeY, 1);
                    });

                source = passData.xyBuffer;
            }

            return source;
        }

        class DebugHDRData
        {
            public LightingDebugSettings lightingDebugSettings;
            public Material debugHDRMaterial;
            public Vector4 hdrOutputParams;
            public Vector4 hdrOutputParams2;
            public Vector4 hdrDebugParams;
            public int debugPass;

            public BufferHandle  xyMappingBuffer;
            public TextureHandle colorBuffer;
            public TextureHandle xyTexture;

            public TextureHandle debugFullScreenTexture;
            public TextureHandle output;
        }

        TextureHandle RenderHDRDebug(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle colorBuffer, TextureHandle xyBuff)
        {
            using (var builder = renderGraph.AddRenderPass<DebugHDRData>("Debug HDR", out var passData))
            {
                passData.debugHDRMaterial = m_DebugHDROutput;
                passData.lightingDebugSettings = m_CurrentDebugDisplaySettings.data.lightingDebugSettings;
                if (HDROutputActiveForCameraType(hdCamera))
                    GetHDROutputParameters(HDRDisplayInformationForCamera(hdCamera), HDRDisplayColorGamutForCamera(hdCamera), hdCamera.volumeStack.GetComponent<Tonemapping>(), out passData.hdrOutputParams, out passData.hdrOutputParams2);
                else
                    passData.hdrOutputParams.z = 1.0f;

                passData.debugPass = (int)m_CurrentDebugDisplaySettings.data.lightingDebugSettings.hdrDebugMode - 1;
                passData.colorBuffer = builder.ReadTexture(colorBuffer);
                passData.debugFullScreenTexture = builder.ReadTexture(m_DebugFullScreenTexture);
                passData.output = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                    { format = GraphicsFormat.R16G16B16A16_SFloat, name = "HDRDebug" }));

                passData.hdrDebugParams = new Vector4(k_SizeOfHDRXYMapping, k_SizeOfHDRXYMapping, 0, 0);
                passData.xyTexture = builder.ReadTexture(xyBuff);

                passData.debugHDRMaterial.enabledKeywords = null;
                if (HDROutputActiveForCameraType(hdCamera))
                {
                    HDROutputUtils.ConfigureHDROutput(passData.debugHDRMaterial, HDRDisplayColorGamutForCamera(hdCamera), HDROutputUtils.Operation.ColorConversion);
                }

                builder.SetRenderFunc(
                    (DebugHDRData data, RenderGraphContext ctx) =>
                    {
                        data.debugHDRMaterial.SetTexture(HDShaderIDs._DebugFullScreenTexture, data.debugFullScreenTexture);
                        data.debugHDRMaterial.SetTexture(HDShaderIDs._xyBuffer, data.xyTexture);

                        data.debugHDRMaterial.SetVector(HDShaderIDs._HDROutputParams, data.hdrOutputParams);
                        data.debugHDRMaterial.SetVector(HDShaderIDs._HDROutputParams2, data.hdrOutputParams2);
                        data.debugHDRMaterial.SetVector(HDShaderIDs._HDRDebugParams, data.hdrDebugParams);

                        HDUtils.DrawFullScreen(ctx.cmd, data.debugHDRMaterial, data.output, null, data.debugPass);
                    });

                return passData.output;
            }
        }

        class DebugExposureData
        {
            public LightingDebugSettings lightingDebugSettings;
            public HDCamera hdCamera;
            public Material debugExposureMaterial;

            public Vector4 proceduralMeteringParams1;
            public Vector4 proceduralMeteringParams2;
            public TextureHandle colorBuffer;
            public TextureHandle debugFullScreenTexture;
            public TextureHandle output;
            public TextureHandle currentExposure;
            public TextureHandle previousExposure;
            public TextureHandle debugExposureData;
            public HableCurve customToneMapCurve;
            public int lutSize;
            public ComputeBuffer histogramBuffer;
        }

        TextureHandle RenderExposureDebug(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle colorBuffer)
        {
            using (var builder = renderGraph.AddRenderPass<DebugExposureData>("Debug Exposure", out var passData))
            {
                ComputeProceduralMeteringParams(hdCamera, out passData.proceduralMeteringParams1, out passData.proceduralMeteringParams2);

                passData.lightingDebugSettings = m_CurrentDebugDisplaySettings.data.lightingDebugSettings;
                passData.hdCamera = hdCamera;
                passData.debugExposureMaterial = m_DebugExposure;
                passData.colorBuffer = builder.ReadTexture(colorBuffer);
                passData.debugFullScreenTexture = builder.ReadTexture(m_DebugFullScreenTexture);
                passData.output = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                    { format = GraphicsFormat.R16G16B16A16_SFloat, name = "ExposureDebug" }));
                passData.currentExposure = builder.ReadTexture(renderGraph.ImportTexture(GetExposureTexture(hdCamera)));
                passData.previousExposure = builder.ReadTexture(renderGraph.ImportTexture(GetPreviousExposureTexture(hdCamera)));
                passData.debugExposureData = builder.ReadTexture(renderGraph.ImportTexture(GetExposureDebugData()));
                passData.customToneMapCurve = GetCustomToneMapCurve();
                passData.lutSize = GetLutSize();
                passData.histogramBuffer = passData.lightingDebugSettings.exposureDebugMode == ExposureDebugMode.FinalImageHistogramView ? GetDebugImageHistogramBuffer() : GetHistogramBuffer();

                builder.SetRenderFunc(
                    (DebugExposureData data, RenderGraphContext ctx) =>
                    {
                        // Grab exposure parameters
                        var exposureSettings = data.hdCamera.volumeStack.GetComponent<Exposure>();

                        Vector4 exposureParams = new Vector4(exposureSettings.compensation.value + data.lightingDebugSettings.debugExposure, exposureSettings.limitMin.value,
                            exposureSettings.limitMax.value, 0f);

                        Vector4 exposureVariants = new Vector4(1.0f, (int)exposureSettings.meteringMode.value, (int)exposureSettings.adaptationMode.value, 0.0f);
                        Vector2 histogramFraction = exposureSettings.histogramPercentages.value / 100.0f;
                        float evRange = exposureSettings.limitMax.value - exposureSettings.limitMin.value;
                        float histScale = 1.0f / Mathf.Max(1e-5f, evRange);
                        float histBias = -exposureSettings.limitMin.value * histScale;
                        Vector4 histogramParams = new Vector4(histScale, histBias, histogramFraction.x, histogramFraction.y);

                        data.debugExposureMaterial.SetVector(HDShaderIDs._ProceduralMaskParams, data.proceduralMeteringParams1);
                        data.debugExposureMaterial.SetVector(HDShaderIDs._ProceduralMaskParams2, data.proceduralMeteringParams2);

                        data.debugExposureMaterial.SetVector(HDShaderIDs._HistogramExposureParams, histogramParams);
                        data.debugExposureMaterial.SetVector(HDShaderIDs._Variants, exposureVariants);
                        data.debugExposureMaterial.SetVector(HDShaderIDs._ExposureParams, exposureParams);
                        data.debugExposureMaterial.SetVector(HDShaderIDs._ExposureParams2, new Vector4(0.0f, 0.0f, ColorUtils.lensImperfectionExposureScale, ColorUtils.s_LightMeterCalibrationConstant));
                        data.debugExposureMaterial.SetVector(HDShaderIDs._MousePixelCoord, HDUtils.GetMouseCoordinates(data.hdCamera));
                        data.debugExposureMaterial.SetTexture(HDShaderIDs._SourceTexture, data.colorBuffer);
                        data.debugExposureMaterial.SetTexture(HDShaderIDs._DebugFullScreenTexture, data.debugFullScreenTexture);
                        data.debugExposureMaterial.SetTexture(HDShaderIDs._PreviousExposureTexture, data.previousExposure);
                        data.debugExposureMaterial.SetTexture(HDShaderIDs._ExposureTexture, data.currentExposure);
                        data.debugExposureMaterial.SetTexture(HDShaderIDs._ExposureWeightMask, exposureSettings.weightTextureMask.value);
                        data.debugExposureMaterial.SetBuffer(HDShaderIDs._HistogramBuffer, data.histogramBuffer);


                        int passIndex = 0;
                        if (data.lightingDebugSettings.exposureDebugMode == ExposureDebugMode.MeteringWeighted)
                        {
                            passIndex = 1;
                            data.debugExposureMaterial.SetVector(HDShaderIDs._ExposureDebugParams, new Vector4(data.lightingDebugSettings.displayMaskOnly ? 1 : 0, 0, 0, 0));
                        }
                        if (data.lightingDebugSettings.exposureDebugMode == ExposureDebugMode.HistogramView)
                        {
                            data.debugExposureMaterial.SetTexture(HDShaderIDs._ExposureDebugTexture, data.debugExposureData);
                            var tonemappingSettings = data.hdCamera.volumeStack.GetComponent<Tonemapping>();

                            bool toneMapIsEnabled = data.hdCamera.frameSettings.IsEnabled(FrameSettingsField.Tonemapping);
                            var tonemappingMode = toneMapIsEnabled ? tonemappingSettings.mode.value : TonemappingMode.None;

                            bool drawTonemapCurve = tonemappingMode != TonemappingMode.None &&
                                data.lightingDebugSettings.showTonemapCurveAlongHistogramView;

                            bool centerAroundMiddleGrey = data.lightingDebugSettings.centerHistogramAroundMiddleGrey;
                            bool displayOverlay = data.lightingDebugSettings.displayOnSceneOverlay;
                            data.debugExposureMaterial.SetVector(HDShaderIDs._ExposureDebugParams, new Vector4(drawTonemapCurve ? 1.0f : 0.0f, (int)tonemappingMode, centerAroundMiddleGrey ? 1 : 0, displayOverlay ? 1 : 0));
                            if (drawTonemapCurve)
                            {
                                if (tonemappingMode == TonemappingMode.Custom)
                                {
                                    data.debugExposureMaterial.SetVector(HDShaderIDs._CustomToneCurve, data.customToneMapCurve.uniforms.curve);
                                    data.debugExposureMaterial.SetVector(HDShaderIDs._ToeSegmentA, data.customToneMapCurve.uniforms.toeSegmentA);
                                    data.debugExposureMaterial.SetVector(HDShaderIDs._ToeSegmentB, data.customToneMapCurve.uniforms.toeSegmentB);
                                    data.debugExposureMaterial.SetVector(HDShaderIDs._MidSegmentA, data.customToneMapCurve.uniforms.midSegmentA);
                                    data.debugExposureMaterial.SetVector(HDShaderIDs._MidSegmentB, data.customToneMapCurve.uniforms.midSegmentB);
                                    data.debugExposureMaterial.SetVector(HDShaderIDs._ShoSegmentA, data.customToneMapCurve.uniforms.shoSegmentA);
                                    data.debugExposureMaterial.SetVector(HDShaderIDs._ShoSegmentB, data.customToneMapCurve.uniforms.shoSegmentB);
                                }
                            }
                            else if (tonemappingMode == TonemappingMode.External)
                            {
                                data.debugExposureMaterial.SetTexture(HDShaderIDs._LogLut3D, tonemappingSettings.lutTexture.value);
                                data.debugExposureMaterial.SetVector(HDShaderIDs._LogLut3D_Params, new Vector4(1f / data.lutSize, data.lutSize - 1f, tonemappingSettings.lutContribution.value, 0f));
                            }
                            passIndex = 2;
                        }
                        if (data.lightingDebugSettings.exposureDebugMode == ExposureDebugMode.FinalImageHistogramView)
                        {
                            bool finalImageRGBHisto = data.lightingDebugSettings.displayFinalImageHistogramAsRGB;

                            data.debugExposureMaterial.SetVector(HDShaderIDs._ExposureDebugParams, new Vector4(0, 0, 0, finalImageRGBHisto ? 1 : 0));
                            data.debugExposureMaterial.SetBuffer(HDShaderIDs._FullImageHistogram, data.histogramBuffer);
                            passIndex = 3;
                        }


                        HDUtils.DrawFullScreen(ctx.cmd, data.debugExposureMaterial, data.output, null, passIndex);
                    });

                return passData.output;
            }
        }

        TextureHandle RenderDebug(RenderGraph renderGraph,
            HDCamera hdCamera,
            TextureHandle colorBuffer,
            TextureHandle depthBuffer,
            TextureHandle depthPyramidTexture,
            TextureHandle colorPickerDebugTexture,
            TextureHandle rayCountTexture,
            TextureHandle xyBufferMapping,
            in BuildGPULightListOutput lightLists,
            in ShadowResult shadowResult,
            CullingResults cullResults,
            GraphicsFormat colorFormat)
        {
            // We don't want any overlay for these kind of rendering
            if (hdCamera.camera.cameraType == CameraType.Reflection || hdCamera.camera.cameraType == CameraType.Preview)
                return colorBuffer;

            TextureHandle output = colorBuffer;

            if (NeedsFullScreenDebugMode() && m_FullScreenDebugPushed)
            {
                output = ResolveFullScreenDebug(renderGraph, m_DebugFullScreenTexture, depthPyramidTexture, hdCamera, colorFormat);

                // If we have full screen debug, this is what we want color picked, so we replace color picker input texture with the new one.
                if (NeedColorPickerDebug(m_CurrentDebugDisplaySettings))
                    colorPickerDebugTexture = PushColorPickerDebugTexture(renderGraph, output);

                m_FullScreenDebugPushed = false;
                m_DebugFullScreenComputeBuffer = BufferHandle.nullHandle;
            }

            if (NeedExposureDebugMode(m_CurrentDebugDisplaySettings))
                output = RenderExposureDebug(renderGraph, hdCamera, colorBuffer);

            if (NeedHDRDebugMode(m_CurrentDebugDisplaySettings))
                output = RenderHDRDebug(renderGraph, hdCamera, colorBuffer, xyBufferMapping);

            if (NeedColorPickerDebug(m_CurrentDebugDisplaySettings))
                output = ResolveColorPickerDebug(renderGraph, colorPickerDebugTexture, hdCamera, colorFormat);

            RenderLightVolumes(renderGraph, output, depthBuffer, cullResults, hdCamera);

            RenderDebugOverlays(renderGraph, output, depthBuffer, depthPyramidTexture, rayCountTexture, lightLists, shadowResult, hdCamera);

            return output;
        }

        void RenderProbeVolumeDebug(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle depthPyramidBuffer, TextureHandle normalBuffer)
        {
            if (apvIsEnabled && ProbeReferenceVolume.instance.GetProbeSamplingDebugResources(hdCamera.camera, out var resultBuffer, out Vector2 coords))
                WriteApvPositionNormalDebugBuffer(renderGraph, resultBuffer, coords, depthPyramidBuffer, normalBuffer);
        }

        class WriteApvData
        {
            public ComputeShader computeShader;
            public BufferHandle resultBuffer;
            public Vector2 clickCoordinates;
            public TextureHandle depthBuffer;
            public TextureHandle normalBuffer;
        }

        // Compute worldspace position and normal at given screenspace clickCoordinates, and write it into given ResultBuffer.
        void WriteApvPositionNormalDebugBuffer(RenderGraph renderGraph, GraphicsBuffer resultBuffer, Vector2 clickCoordinates, TextureHandle depthBuffer, TextureHandle normalBuffer)
        {
            using (var builder = renderGraph.AddRenderPass<WriteApvData>("APV Debug Sampling", out var passData, ProfilingSampler.Get(HDProfileId.APVSamplingDebug)))
            {
                passData.resultBuffer = builder.WriteBuffer(renderGraph.ImportBuffer(resultBuffer));
                passData.clickCoordinates = clickCoordinates;
                passData.depthBuffer = builder.ReadTexture(depthBuffer);
                passData.normalBuffer = builder.ReadTexture(normalBuffer);
                passData.computeShader = m_ComputePositionNormal;

                builder.SetRenderFunc(
                    (WriteApvData data, RenderGraphContext ctx) =>
                    {
                        int kernelHandle = data.computeShader.FindKernel("ComputePositionNormal");
                        ctx.cmd.SetComputeTextureParam(data.computeShader, kernelHandle, "_CameraDepthTexture", data.depthBuffer);
                        ctx.cmd.SetComputeTextureParam(data.computeShader, kernelHandle, "_NormalBufferTexture", data.normalBuffer);
                        ctx.cmd.SetComputeVectorParam(data.computeShader, "_positionSS", new Vector4(data.clickCoordinates.x, data.clickCoordinates.y, 0.0f, 0.0f));
                        ctx.cmd.SetComputeBufferParam(data.computeShader, kernelHandle, "_ResultBuffer", data.resultBuffer);
                        ctx.cmd.DispatchCompute(data.computeShader, kernelHandle, 1, 1, 1);
                    });
            }

        }

        class DebugViewMaterialData
        {
            public TextureHandle outputColor;
            public TextureHandle outputDepth;
            public RendererListHandle opaqueRendererList;
            public RendererListHandle transparentRendererList;
            public Material debugGBufferMaterial;
            public FrameSettings frameSettings;

            public bool decalsEnabled;
            public BufferHandle  perVoxelOffset;
            public DBufferOutput dbuffer;
            public GBufferOutput gbuffer;
            public TextureHandle depthBuffer;

            public Texture clearColorTexture;
            public RenderTexture clearDepthTexture;
            public bool clearDepth;
        }

        TextureHandle RenderDebugViewMaterial(RenderGraph renderGraph, CullingResults cull, HDCamera hdCamera, BuildGPULightListOutput lightLists, DBufferOutput dbuffer, GBufferOutput gbuffer, TextureHandle depthBuffer, TextureHandle vtFeedbackBuffer)
        {
            bool msaa = hdCamera.msaaEnabled;

            var output = renderGraph.CreateTexture(
                new TextureDesc(Vector2.one, true, true)
                {
                    format = GetColorBufferFormat(),
                    enableRandomWrite = !msaa,
                    bindTextureMS = msaa,
                    msaaSamples = hdCamera.msaaSamples,
                    clearBuffer = true,
                    clearColor = Color.clear,
                    name = msaa ? "CameraColorMSAA" : "CameraColor"
                });

            if (m_CurrentDebugDisplaySettings.data.materialDebugSettings.IsDebugGBufferEnabled() && hdCamera.frameSettings.litShaderMode == LitShaderMode.Deferred)
            {
                using (var builder = renderGraph.AddRenderPass<DebugViewMaterialData>("DebugViewMaterialGBuffer", out var passData, ProfilingSampler.Get(HDProfileId.DebugViewMaterialGBuffer)))
                {
                    passData.debugGBufferMaterial = m_currentDebugViewMaterialGBuffer;
                    passData.outputColor = builder.WriteTexture(output);
                    passData.gbuffer = ReadGBuffer(gbuffer, builder);
                    passData.depthBuffer = builder.ReadTexture(depthBuffer);

                    builder.SetRenderFunc(
                        (DebugViewMaterialData data, RenderGraphContext context) =>
                        {
                            var gbufferHandles = data.gbuffer;
                            for (int i = 0; i < gbufferHandles.gBufferCount; ++i)
                            {
                                data.debugGBufferMaterial.SetTexture(HDShaderIDs._GBufferTexture[i], gbufferHandles.mrt[i]);
                            }
                            data.debugGBufferMaterial.SetTexture(HDShaderIDs._CameraDepthTexture, data.depthBuffer);

                            HDUtils.DrawFullScreen(context.cmd, data.debugGBufferMaterial, data.outputColor);
                        });
                }
            }
            else
            {
                // Create the depth texture that will be used for the display debug
                TextureHandle depth = CreateDepthBuffer(renderGraph, true, hdCamera.msaaSamples);

                // Render the debug water
                m_WaterSystem.RenderWaterDebug(renderGraph, hdCamera, output, depth);

                // Render the debug lines.
                RenderLines(renderGraph, depthBuffer, hdCamera, lightLists);
                ComposeLines(renderGraph, hdCamera, output, depth, TextureHandle.nullHandle, -1);

                using (var builder = renderGraph.AddRenderPass<DebugViewMaterialData>("DisplayDebug ViewMaterial", out var passData, ProfilingSampler.Get(HDProfileId.DisplayDebugViewMaterial)))
                {
                    passData.frameSettings = hdCamera.frameSettings;
                    passData.outputColor = builder.UseColorBuffer(output, 0);
#if ENABLE_VIRTUALTEXTURES
                    builder.UseColorBuffer(vtFeedbackBuffer, 1);
#endif
                    passData.outputDepth = builder.UseDepthBuffer(depth, DepthAccess.ReadWrite);

                    // When rendering debug material we shouldn't rely on a depth prepass for optimizing the alpha clip test. As it is control on the material inspector side
                    // we must override the state here.
                    passData.opaqueRendererList = builder.UseRendererList(
                        renderGraph.CreateRendererList(CreateOpaqueRendererListDesc(cull, hdCamera.camera, m_AllForwardOpaquePassNames,
                            rendererConfiguration: m_CurrentRendererConfigurationBakedLighting,
                            stateBlock: m_DepthStateOpaque)));
                    passData.transparentRendererList = builder.UseRendererList(
                        renderGraph.CreateRendererList(CreateTransparentRendererListDesc(cull, hdCamera.camera, m_AllTransparentPassNames,
                            rendererConfiguration: m_CurrentRendererConfigurationBakedLighting,
                            stateBlock: m_DepthStateNoWrite)));

                    passData.decalsEnabled = (hdCamera.frameSettings.IsEnabled(FrameSettingsField.Decals)) && (DecalSystem.m_DecalDatasCount > 0);
                    passData.perVoxelOffset = builder.ReadBuffer(lightLists.perVoxelOffset);
                    passData.dbuffer = ReadDBuffer(dbuffer, builder);

                    passData.clearColorTexture = Compositor.CompositionManager.GetClearTextureForStackedCamera(hdCamera);   // returns null if is not a stacked camera
                    passData.clearDepthTexture = Compositor.CompositionManager.GetClearDepthForStackedCamera(hdCamera);     // returns null if is not a stacked camera
                    passData.clearDepth = hdCamera.clearDepth;

                    builder.SetRenderFunc(
                        (DebugViewMaterialData data, RenderGraphContext context) =>
                        {
                            // If we are doing camera stacking, then we want to clear the debug color and depth buffer using the data from the previous camera on the stack
                            // Note: Ideally here we would like to draw directly on the same buffers as the previous camera, but currently the compositor is not using
                            // Texture Arrays so this would not work. We might need to revise this in the future.
                            if (data.clearColorTexture != null)
                            {
                                HDUtils.BlitColorAndDepth(context.cmd, data.clearColorTexture, data.clearDepthTexture, new Vector4(1, 1, 0, 0), 0, !data.clearDepth);
                            }

                            BindDefaultTexturesLightingBuffers(context.defaultResources, context.cmd);

                            BindDBufferGlobalData(data.dbuffer, context);
                            DrawOpaqueRendererList(context, data.frameSettings, data.opaqueRendererList);

                            if (data.decalsEnabled)
                                DecalSystem.instance.SetAtlas(context.cmd); // for clustered decals
                            if (data.perVoxelOffset.IsValid())
                                context.cmd.SetGlobalBuffer(HDShaderIDs.g_vLayeredOffsetsBuffer, data.perVoxelOffset);
                            DrawTransparentRendererList(context, data.frameSettings, data.transparentRendererList);
                        });
                }
            }

            return output;
        }

        class PushFullScreenDebugPassData
        {
            public TextureHandle input;
            public TextureHandle output;
            public int mipIndex;
            public bool xrTexture;
            public bool useCustomScaleBias;
            public Vector4 customScaleBias;
        }

        void PushFullScreenLightingDebugTexture(RenderGraph renderGraph, TextureHandle input, GraphicsFormat colorFormat = GraphicsFormat.R16G16B16A16_SFloat)
        {
            // In practice, this is only useful for the SingleShadow debug view.
            // TODO: See how we can make this nicer than a specific functions just for one case.
            if (NeedsFullScreenDebugMode() && m_FullScreenDebugPushed == false)
            {
                PushFullScreenDebugTexture(renderGraph, input, colorFormat);
            }
        }

        internal void PushFullScreenDebugTexture(RenderGraph renderGraph, TextureHandle input, FullScreenDebugMode debugMode, GraphicsFormat colorFormat = GraphicsFormat.R16G16B16A16_SFloat, bool xrTexture = true)
        {
            if (debugMode == m_CurrentDebugDisplaySettings.data.fullScreenDebugMode)
            {
                PushFullScreenDebugTexture(renderGraph, input, colorFormat, xrTexture: xrTexture);
            }
        }

        internal void PushFullScreenDebugTexture(RenderGraph renderGraph, TextureHandle input, Vector2 scales, FullScreenDebugMode debugMode, GraphicsFormat colorFormat = GraphicsFormat.R16G16B16A16_SFloat, bool xrTexture = true)
        {
            if (debugMode == m_CurrentDebugDisplaySettings.data.fullScreenDebugMode)
            {
                PushFullScreenDebugTexture(renderGraph, input, true, scales, colorFormat, xrTexture: xrTexture);
            }
        }

        void PushFullScreenDebugTextureMip(RenderGraph renderGraph, TextureHandle input, int lodCount, Vector4 scaleBias, FullScreenDebugMode debugMode, GraphicsFormat colorFormat = GraphicsFormat.R16G16B16A16_SFloat)
        {
            if (debugMode == m_CurrentDebugDisplaySettings.data.fullScreenDebugMode)
            {
                var mipIndex = Mathf.FloorToInt(m_CurrentDebugDisplaySettings.data.fullscreenDebugMip * lodCount);

                PushFullScreenDebugTexture(renderGraph, input, colorFormat, mipIndex);
            }
        }

        void PushFullScreenHistoryBuffer(RenderGraph renderGraph, TextureHandle input, HDCameraFrameHistoryType historyType, GraphicsFormat colorFormat = GraphicsFormat.R16G16B16A16_SFloat)
        {
            PushFullScreenDebugTexture(renderGraph, input, colorFormat);
        }

        void PushFullScreenDebugTexture(RenderGraph renderGraph, TextureHandle input, GraphicsFormat rtFormat = GraphicsFormat.R16G16B16A16_SFloat, int mipIndex = -1, bool xrTexture = true)
        {
            PushFullScreenDebugTexture(renderGraph, input, false, new Vector2(1.0f, 1.0f), rtFormat, mipIndex, xrTexture);
        }

        void PushFullScreenDebugTexture(RenderGraph renderGraph, TextureHandle input, bool useCustomScaleBias, Vector2 customScales, GraphicsFormat rtFormat = GraphicsFormat.R16G16B16A16_SFloat, int mipIndex = -1, bool xrTexture = true)
        {
            using (var builder = renderGraph.AddRenderPass<PushFullScreenDebugPassData>("Push Full Screen Debug", out var passData))
            {
                passData.mipIndex = mipIndex;
                passData.xrTexture = xrTexture;
                passData.input = builder.ReadTexture(input);
                passData.useCustomScaleBias = false;
                if (useCustomScaleBias)
                {
                    passData.useCustomScaleBias = true;
                    passData.customScaleBias = new Vector4(customScales.x, customScales.y, 0.0f, 0.0f);
                }

                passData.output = builder.UseColorBuffer(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                    { format = rtFormat, name = "DebugFullScreen" }), 0);

                builder.SetRenderFunc(
                    (PushFullScreenDebugPassData data, RenderGraphContext ctx) =>
                    {
                        if (data.useCustomScaleBias)
                        {
                            if (data.xrTexture)
                            {
                                if (data.mipIndex != -1)
                                    HDUtils.BlitCameraTexture(ctx.cmd, data.input, data.output, data.customScaleBias, data.mipIndex);
                                else
                                    HDUtils.BlitCameraTexture(ctx.cmd, data.input, data.output, data.customScaleBias);
                            }
                            else
                            {
                                CoreUtils.SetRenderTarget(ctx.cmd, data.output);
                                if (data.mipIndex != -1)
                                    HDUtils.BlitTexture2D(ctx.cmd, data.input, data.customScaleBias, data.mipIndex, false);
                                else
                                    HDUtils.BlitTexture2D(ctx.cmd, data.input, data.customScaleBias, 0.0f, false);
                            }
                        }
                        else
                        {
                            if (data.xrTexture)
                            {
                                if (data.mipIndex != -1)
                                    HDUtils.BlitCameraTexture(ctx.cmd, data.input, data.output, data.mipIndex);
                                else
                                    HDUtils.BlitCameraTexture(ctx.cmd, data.input, data.output);
                            }
                            else
                            {
                                if (data.mipIndex != -1)
                                    HDUtils.BlitCameraTexture2D(ctx.cmd, data.input, data.output, data.mipIndex);
                                else
                                    HDUtils.BlitCameraTexture2D(ctx.cmd, data.input, data.output);
                            }
                        }
                    });

                m_DebugFullScreenTexture = passData.output;
            }

            // We need this flag because otherwise if no full screen debug is pushed (like for example if the corresponding pass is disabled), when we render the result in RenderDebug m_DebugFullScreenTempBuffer will contain potential garbage
            m_FullScreenDebugPushed = true;
        }

        void PushFullScreenExposureDebugTexture(RenderGraph renderGraph, TextureHandle input, GraphicsFormat colorFormat = GraphicsFormat.R16G16B16A16_SFloat)
        {
            if (m_CurrentDebugDisplaySettings.data.lightingDebugSettings.exposureDebugMode != ExposureDebugMode.None)
            {
                PushFullScreenDebugTexture(renderGraph, input, colorFormat);
            }
        }

        void PushFullScreenHDRDebugTexture(RenderGraph renderGraph, TextureHandle input, GraphicsFormat colorFormat = GraphicsFormat.R16G16B16A16_SFloat)
        {
            if (m_CurrentDebugDisplaySettings.data.lightingDebugSettings.hdrDebugMode != HDRDebugMode.None)
            {
                PushFullScreenDebugTexture(renderGraph, input, colorFormat);
            }
        }

#if ENABLE_VIRTUALTEXTURES
        class PushFullScreenVTDebugPassData
        {
            public TextureHandle    input;
            public TextureHandle    output;
            public Material         material;
            public bool             msaa;
        }

        void PushFullScreenVTFeedbackDebugTexture(RenderGraph renderGraph, TextureHandle input, bool msaa)
        {
            if (FullScreenDebugMode.RequestedVirtualTextureTiles == m_CurrentDebugDisplaySettings.data.fullScreenDebugMode)
            {
                using (var builder = renderGraph.AddRenderPass<PushFullScreenVTDebugPassData>("Push Full Screen Debug", out var passData))
                {
                    passData.material = m_VTDebugBlit;
                    passData.msaa = msaa;
                    passData.input = builder.ReadTexture(input);
                    passData.output = builder.UseColorBuffer(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                        { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, name = "DebugFullScreen" }), 0);

                    builder.SetRenderFunc(
                        (PushFullScreenVTDebugPassData data, RenderGraphContext ctx) =>
                        {
                            CoreUtils.SetRenderTarget(ctx.cmd, data.output);
                            data.material.SetTexture(data.msaa ? HDShaderIDs._BlitTextureMSAA : HDShaderIDs._BlitTexture, data.input);
                            ctx.cmd.DrawProcedural(Matrix4x4.identity, data.material, data.msaa ? 1 : 0, MeshTopology.Triangles, 3, 1);
                        });

                    m_DebugFullScreenTexture = passData.output;
                }

                m_FullScreenDebugPushed = true;
            }
        }

#endif

        TextureHandle PushColorPickerDebugTexture(RenderGraph renderGraph, TextureHandle input)
        {
            using (var builder = renderGraph.AddRenderPass<PushFullScreenDebugPassData>("Push To Color Picker", out var passData))
            {
                passData.input = builder.ReadTexture(input);
                passData.output = builder.UseColorBuffer(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                    { format = GraphicsFormat.R16G16B16A16_SFloat, name = "DebugColorPicker" }), 0);

                builder.SetRenderFunc(
                    (PushFullScreenDebugPassData data, RenderGraphContext ctx) =>
                    {
                        HDUtils.BlitCameraTexture(ctx.cmd, data.input, data.output);
                    });

                return passData.output;
            }
        }
    }
}
