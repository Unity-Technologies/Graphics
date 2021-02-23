using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        bool                        m_FullScreenDebugPushed;
        DebugOverlay                m_DebugOverlay = new DebugOverlay();
        TextureHandle               m_DebugFullScreenTexture;
        ComputeBufferHandle         m_DebugFullScreenComputeBuffer;
        ShaderVariablesDebugDisplay m_ShaderVariablesDebugDisplayCB = new ShaderVariablesDebugDisplay();

        Material m_DebugViewMaterialGBuffer;
        Material m_DebugViewMaterialGBufferShadowMask;
        Material m_currentDebugViewMaterialGBuffer;
        Material m_DebugDisplayLatlong;
        Material m_DebugFullScreen;
        Material m_DebugColorPicker;
        Material m_DebugExposure;
        Material m_DebugViewTilesMaterial;
        Material m_DebugHDShadowMapMaterial;
        Material m_DebugDensityVolumeMaterial;
        Material m_DebugBlitMaterial;
#if ENABLE_VIRTUALTEXTURES
        Material m_VTDebugBlit;
#endif

        DebugDisplaySettings m_DebugDisplaySettings = new DebugDisplaySettings();

        /// <summary>
        /// Debug display settings.
        /// </summary>
        public DebugDisplaySettings debugDisplaySettings { get { return m_DebugDisplaySettings; } }
        static DebugDisplaySettings s_NeutralDebugDisplaySettings = new DebugDisplaySettings();
        internal DebugDisplaySettings m_CurrentDebugDisplaySettings;

        void InitializeDebug()
        {
            m_DebugViewMaterialGBuffer = CoreUtils.CreateEngineMaterial(defaultResources.shaders.debugViewMaterialGBufferPS);
            m_DebugViewMaterialGBufferShadowMask = CoreUtils.CreateEngineMaterial(defaultResources.shaders.debugViewMaterialGBufferPS);
            m_DebugViewMaterialGBufferShadowMask.EnableKeyword("SHADOWS_SHADOWMASK");
            m_DebugDisplayLatlong = CoreUtils.CreateEngineMaterial(defaultResources.shaders.debugDisplayLatlongPS);
            m_DebugFullScreen = CoreUtils.CreateEngineMaterial(defaultResources.shaders.debugFullScreenPS);
            m_DebugColorPicker = CoreUtils.CreateEngineMaterial(defaultResources.shaders.debugColorPickerPS);
            m_DebugExposure = CoreUtils.CreateEngineMaterial(defaultResources.shaders.debugExposurePS);
            m_DebugViewTilesMaterial = CoreUtils.CreateEngineMaterial(defaultResources.shaders.debugViewTilesPS);
            m_DebugHDShadowMapMaterial = CoreUtils.CreateEngineMaterial(defaultResources.shaders.debugHDShadowMapPS);
            m_DebugDensityVolumeMaterial = CoreUtils.CreateEngineMaterial(defaultResources.shaders.debugDensityVolumeAtlasPS);
            m_DebugBlitMaterial = CoreUtils.CreateEngineMaterial(defaultResources.shaders.debugBlitQuad);
#if ENABLE_VIRTUALTEXTURES
            m_VTDebugBlit = CoreUtils.CreateEngineMaterial(defaultResources.shaders.debugViewVirtualTexturingBlit);
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
            CoreUtils.Destroy(m_DebugViewTilesMaterial);
            CoreUtils.Destroy(m_DebugHDShadowMapMaterial);
            CoreUtils.Destroy(m_DebugDensityVolumeMaterial);
            CoreUtils.Destroy(m_DebugBlitMaterial);
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

        bool NeedsFullScreenDebugMode()
        {
            bool fullScreenDebugEnabled = m_CurrentDebugDisplaySettings.data.fullScreenDebugMode != FullScreenDebugMode.None;
            bool lightingDebugEnabled = m_CurrentDebugDisplaySettings.data.lightingDebugSettings.shadowDebugMode == ShadowMapDebugMode.SingleShadow;

            return fullScreenDebugEnabled || lightingDebugEnabled;
        }

        unsafe void ApplyDebugDisplaySettings(HDCamera hdCamera, CommandBuffer cmd)
        {
            // See ShaderPassForward.hlsl: for forward shaders, if DEBUG_DISPLAY is enabled and no DebugLightingMode or DebugMipMapMod
            // modes have been set, lighting is automatically skipped (To avoid some crashed due to lighting RT not set on console).
            // However debug mode like colorPickerModes and false color don't need DEBUG_DISPLAY and must work with the lighting.
            // So we will enabled DEBUG_DISPLAY independently

            bool debugDisplayEnabledOrSceneLightingDisabled = m_CurrentDebugDisplaySettings.IsDebugDisplayEnabled() || CoreUtils.IsSceneLightingDisabled(hdCamera.camera);
            // Enable globally the keyword DEBUG_DISPLAY on shader that support it with multi-compile
            CoreUtils.SetKeyword(cmd, "DEBUG_DISPLAY", debugDisplayEnabledOrSceneLightingDisabled);

            // Setting this all the time due to a strange bug that either reports a (globally) bound texture as not bound or where SetGlobalTexture doesn't behave as expected.
            // As a workaround we bind it regardless of debug display. Eventually with
            cmd.SetGlobalTexture(HDShaderIDs._DebugMatCapTexture, defaultResources.textures.matcapTex);

            m_ShaderVariablesGlobalCB._GlobalTessellationFactorMultiplier = (m_CurrentDebugDisplaySettings.data.fullScreenDebugMode == FullScreenDebugMode.QuadOverdraw) ? 0.0f : 1.0f;

            if (debugDisplayEnabledOrSceneLightingDisabled ||
                m_CurrentDebugDisplaySettings.data.colorPickerDebugSettings.colorPickerMode != ColorPickerDebugMode.None ||
                m_CurrentDebugDisplaySettings.IsDebugExposureModeEnabled())
            {
                // This is for texture streaming
                m_CurrentDebugDisplaySettings.UpdateMaterials();

                var lightingDebugSettings = m_CurrentDebugDisplaySettings.data.lightingDebugSettings;
                var materialDebugSettings = m_CurrentDebugDisplaySettings.data.materialDebugSettings;
                var debugAlbedo = new Vector4(lightingDebugSettings.overrideAlbedo ? 1.0f : 0.0f, lightingDebugSettings.overrideAlbedoValue.r, lightingDebugSettings.overrideAlbedoValue.g, lightingDebugSettings.overrideAlbedoValue.b);
                var debugSmoothness = new Vector4(lightingDebugSettings.overrideSmoothness ? 1.0f : 0.0f, lightingDebugSettings.overrideSmoothnessValue, 0.0f, 0.0f);
                var debugNormal = new Vector4(lightingDebugSettings.overrideNormal ? 1.0f : 0.0f, 0.0f, 0.0f, 0.0f);
                var debugAmbientOcclusion = new Vector4(lightingDebugSettings.overrideAmbientOcclusion ? 1.0f : 0.0f, lightingDebugSettings.overrideAmbientOcclusionValue, 0.0f, 0.0f);
                var debugSpecularColor = new Vector4(lightingDebugSettings.overrideSpecularColor ? 1.0f : 0.0f, lightingDebugSettings.overrideSpecularColorValue.r, lightingDebugSettings.overrideSpecularColorValue.g, lightingDebugSettings.overrideSpecularColorValue.b);
                var debugEmissiveColor = new Vector4(lightingDebugSettings.overrideEmissiveColor ? 1.0f : 0.0f, lightingDebugSettings.overrideEmissiveColorValue.r, lightingDebugSettings.overrideEmissiveColorValue.g, lightingDebugSettings.overrideEmissiveColorValue.b);
                var debugTrueMetalColor = new Vector4(materialDebugSettings.materialValidateTrueMetal ? 1.0f : 0.0f, materialDebugSettings.materialValidateTrueMetalColor.r, materialDebugSettings.materialValidateTrueMetalColor.g, materialDebugSettings.materialValidateTrueMetalColor.b);

                DebugLightingMode debugLightingMode = m_CurrentDebugDisplaySettings.GetDebugLightingMode();
                if (CoreUtils.IsSceneLightingDisabled(hdCamera.camera))
                {
                    debugLightingMode = DebugLightingMode.MatcapView;
                }

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

                cb._DebugLightingMode = (int)debugLightingMode;
                cb._DebugLightLayersMask = (int)m_CurrentDebugDisplaySettings.GetDebugLightLayersMask();
                cb._DebugShadowMapMode = (int)m_CurrentDebugDisplaySettings.GetDebugShadowMapMode();
                cb._DebugMipMapMode = (int)m_CurrentDebugDisplaySettings.GetDebugMipMapMode();
                cb._DebugMipMapModeTerrainTexture = (int)m_CurrentDebugDisplaySettings.GetDebugMipMapModeTerrainTexture();
                cb._ColorPickerMode = (int)m_CurrentDebugDisplaySettings.GetDebugColorPickerMode();
                cb._DebugFullScreenMode = (int)m_CurrentDebugDisplaySettings.data.fullScreenDebugMode;

#if UNITY_EDITOR
                cb._MatcapMixAlbedo = HDRenderPipelinePreferences.matcapViewMixAlbedo ? 1 : 0;
                cb._MatcapViewScale = HDRenderPipelinePreferences.matcapViewScale;
#else
                cb._MatcapMixAlbedo = 0;
                cb._MatcapViewScale = 1.0f;
#endif
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

                ConstantBuffer.PushGlobal(cmd, m_ShaderVariablesDebugDisplayCB, HDShaderIDs._ShaderVariablesDebugDisplay);

                cmd.SetGlobalTexture(HDShaderIDs._DebugFont, defaultResources.textures.debugFontTex);
            }
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

                    transparencyOverdrawOutput = builder.UseColorBuffer(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true) { name = "Transparency Overdraw", colorFormat = GetColorBufferFormat(), clearBuffer = true, clearColor = Color.black }), 0);

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
            public ComputeBufferHandle debugBuffer;
            public RendererListHandle rendererList;
        }

        void RenderFullScreenDebug(RenderGraph renderGraph, TextureHandle colorBuffer, TextureHandle depthBuffer, CullingResults cull, HDCamera hdCamera)
        {
            TextureHandle fullscreenDebugOutput = TextureHandle.nullHandle;
            ComputeBufferHandle fullscreenDebugBuffer = ComputeBufferHandle.nullHandle;

            using (var builder = renderGraph.AddRenderPass<FullScreenDebugPassData>("FullScreen Debug", out var passData))
            {
                fullscreenDebugOutput = builder.UseColorBuffer(colorBuffer, 0);
                builder.UseDepthBuffer(depthBuffer, DepthAccess.Read);

                passData.frameSettings = hdCamera.frameSettings;
                passData.debugBuffer = builder.WriteComputeBuffer(renderGraph.CreateComputeBuffer(new ComputeBufferDesc(hdCamera.actualWidth * hdCamera.actualHeight * hdCamera.viewCount, sizeof(uint))));
                passData.rendererList = builder.UseRendererList(renderGraph.CreateRendererList(CreateOpaqueRendererListDesc(cull, hdCamera.camera, m_FullScreenDebugPassNames, renderQueueRange: RenderQueueRange.all)));

                builder.SetRenderFunc(
                    (FullScreenDebugPassData data, RenderGraphContext ctx) =>
                    {
                        ctx.cmd.SetRandomWriteTarget(1, data.debugBuffer);
                        CoreUtils.DrawRendererList(ctx.renderContext, ctx.cmd, data.rendererList);
                        ctx.cmd.ClearRandomWriteTargets();
                    });

                fullscreenDebugBuffer = passData.debugBuffer;
            }

            m_DebugFullScreenComputeBuffer = fullscreenDebugBuffer;
            PushFullScreenDebugTexture(renderGraph, ResolveMSAAColor(renderGraph, hdCamera, fullscreenDebugOutput));
        }

        class ResolveFullScreenDebugPassData
        {
            public DebugDisplaySettings debugDisplaySettings;
            public Material debugFullScreenMaterial;
            public HDCamera hdCamera;
            public int depthPyramidMip;
            public ComputeBuffer depthPyramidOffsets;
            public TextureHandle output;
            public TextureHandle input;
            public TextureHandle depthPyramid;
            public ComputeBufferHandle fullscreenBuffer;
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
                passData.depthPyramidMip = (int)(m_CurrentDebugDisplaySettings.data.fullscreenDebugMip * GetDepthBufferMipChainInfo().mipLevelCount);
                passData.depthPyramidOffsets = GetDepthBufferMipChainInfo().GetOffsetBufferData(m_DepthPyramidMipLevelOffsetsBuffer);
                // On Vulkan, not binding the Random Write Target will result in an invalid drawcall.
                // To avoid that, if the compute buffer is invalid, we bind a dummy compute buffer anyway.
                if (m_DebugFullScreenComputeBuffer.IsValid())
                    passData.fullscreenBuffer = builder.ReadComputeBuffer(m_DebugFullScreenComputeBuffer);
                else
                    passData.fullscreenBuffer = builder.CreateTransientComputeBuffer(new ComputeBufferDesc(4, sizeof(uint)));
                passData.output = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                    { colorFormat = rtFormat, name = "ResolveFullScreenDebug" }));

                builder.SetRenderFunc(
                    (ResolveFullScreenDebugPassData data, RenderGraphContext ctx) =>
                    {
                        var mpb = ctx.renderGraphPool.GetTempMaterialPropertyBlock();
                        ComputeBuffer fullscreenBuffer = data.fullscreenBuffer;

                        mpb.SetTexture(HDShaderIDs._DebugFullScreenTexture, data.input);
                        mpb.SetTexture(HDShaderIDs._CameraDepthTexture, data.depthPyramid);
                        mpb.SetFloat(HDShaderIDs._FullScreenDebugMode, (float)data.debugDisplaySettings.data.fullScreenDebugMode);
                        if (data.debugDisplaySettings.data.enableDebugDepthRemap)
                            mpb.SetVector(HDShaderIDs._FullScreenDebugDepthRemap, new Vector4(data.debugDisplaySettings.data.fullScreenDebugDepthRemap.x, data.debugDisplaySettings.data.fullScreenDebugDepthRemap.y, data.hdCamera.camera.nearClipPlane, data.hdCamera.camera.farClipPlane));
                        else // Setup neutral value
                            mpb.SetVector(HDShaderIDs._FullScreenDebugDepthRemap, new Vector4(0.0f, 1.0f, 0.0f, 1.0f));
                        mpb.SetInt(HDShaderIDs._DebugDepthPyramidMip, data.depthPyramidMip);
                        mpb.SetBuffer(HDShaderIDs._DebugDepthPyramidOffsets, data.depthPyramidOffsets);
                        mpb.SetInt(HDShaderIDs._DebugContactShadowLightIndex, data.debugDisplaySettings.data.fullScreenContactShadowLightIndex);
                        mpb.SetFloat(HDShaderIDs._TransparencyOverdrawMaxPixelCost, (float)data.debugDisplaySettings.data.transparencyDebugSettings.maxPixelCost);
                        mpb.SetFloat(HDShaderIDs._QuadOverdrawMaxQuadCost, (float)data.debugDisplaySettings.data.maxQuadCost);
                        mpb.SetFloat(HDShaderIDs._VertexDensityMaxPixelCost, (float)data.debugDisplaySettings.data.maxVertexDensity);

                        if (fullscreenBuffer != null)
                            ctx.cmd.SetRandomWriteTarget(1, fullscreenBuffer);

                        HDUtils.DrawFullScreen(ctx.cmd, data.debugFullScreenMaterial, data.output, mpb, 0);

                        if (fullscreenBuffer != null)
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
                    { colorFormat = rtFormat, name = "ResolveColorPickerDebug" }));

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
            public DebugOverlay debugOverlay;
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

                        data.debugOverlay.SetViewport(ctx.cmd);
                        mpb.SetTexture(HDShaderIDs._InputCubemap, data.skyReflectionTexture);
                        mpb.SetFloat(HDShaderIDs._Mipmap, data.lightingDebugSettings.skyReflectionMipmap);
                        mpb.SetFloat(HDShaderIDs._ApplyExposure, 1.0f);
                        mpb.SetFloat(HDShaderIDs._SliceIndex, data.lightingDebugSettings.cubeArraySliceIndex);
                        ctx.cmd.DrawProcedural(Matrix4x4.identity, data.debugLatlongMaterial, 0, MeshTopology.Triangles, 3, 1, mpb);
                        data.debugOverlay.Next();
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

        void RenderAtlasDebugOverlay(RenderGraph renderGraph, TextureHandle colorBuffer, TextureHandle depthBuffer, Texture atlas, int mipLevel, bool applyExposure, string passName, HDProfileId profileID)
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
                        var mpb = ctx.renderGraphPool.GetTempMaterialPropertyBlock();
                        mpb.SetFloat(HDShaderIDs._ApplyExposure, applyExposure ? 1.0f : 0.0f);
                        mpb.SetFloat(HDShaderIDs._Mipmap, data.mipLevel);
                        mpb.SetTexture(HDShaderIDs._InputTexture, data.atlasTexture);
                        data.debugOverlay.SetViewport(ctx.cmd);
                        ctx.cmd.DrawProcedural(Matrix4x4.identity, data.debugBlitMaterial, 0, MeshTopology.Triangles, 3, 1, mpb);
                        data.debugOverlay.Next();
                    });
            }
        }

        class RenderDensityVolumeAtlasDebugOverlayPassData
            : DebugOverlayPassData
        {
            public float slice;
            public Texture3DAtlas atlas;
            public Material debugDensityVolumeMaterial;
            public bool useSelection;
        }

        void RenderDensityVolumeAtlasDebugOverlay(RenderGraph renderGraph, TextureHandle colorBuffer, TextureHandle depthBuffer)
        {
            if (!m_CurrentDebugDisplaySettings.data.lightingDebugSettings.displayDensityVolumeAtlas)
                return;

            using (var builder = renderGraph.AddRenderPass<RenderDensityVolumeAtlasDebugOverlayPassData>("RenderDensityVolumeAtlasOverlay" , out var passData, ProfilingSampler.Get(HDProfileId.DisplayDensityVolumeAtlas)))
            {
                passData.debugOverlay = m_DebugOverlay;
                passData.colorBuffer = builder.UseColorBuffer(colorBuffer, 0);
                passData.depthBuffer = builder.UseDepthBuffer(depthBuffer, DepthAccess.ReadWrite);
                passData.debugDensityVolumeMaterial = m_DebugDensityVolumeMaterial;
                passData.slice = (float)m_CurrentDebugDisplaySettings.data.lightingDebugSettings.densityVolumeAtlasSlice;
                passData.atlas = DensityVolumeManager.manager.volumeAtlas;
                passData.useSelection = m_CurrentDebugDisplaySettings.data.lightingDebugSettings.densityVolumeUseSelection;

                builder.SetRenderFunc(
                    (RenderDensityVolumeAtlasDebugOverlayPassData data, RenderGraphContext ctx) =>
                    {
                        var atlasTexture = data.atlas.GetAtlas();
                        var mpb = ctx.renderGraphPool.GetTempMaterialPropertyBlock();
                        mpb.SetTexture(HDShaderIDs._InputTexture, data.atlas.GetAtlas());
                        mpb.SetFloat("_Slice", (float)data.slice);
                        mpb.SetVector("_Offset", Vector3.zero);
                        mpb.SetVector("_TextureSize", new Vector3(atlasTexture.width, atlasTexture.height, atlasTexture.volumeDepth));

#if UNITY_EDITOR
                        if (data.useSelection)
                        {
                            var obj = UnityEditor.Selection.activeGameObject;

                            if (obj != null && obj.TryGetComponent<DensityVolume>(out var densityVolume))
                            {
                                var texture = densityVolume.parameters.volumeMask;

                                if (texture != null)
                                {
                                    float textureDepth = texture is RenderTexture rt ? rt.volumeDepth : texture is Texture3D t3D ? t3D.depth : 0;
                                    mpb.SetVector("_TextureSize", new Vector3(texture.width, texture.height, textureDepth));
                                    mpb.SetVector("_Offset", data.atlas.GetTextureOffset(texture));
                                }
                            }
                        }
#endif
                        data.debugOverlay.SetViewport(ctx.cmd);
                        ctx.cmd.DrawProcedural(Matrix4x4.identity, data.debugDensityVolumeMaterial, 0, MeshTopology.Triangles, 3, 1, mpb);
                        data.debugOverlay.Next();
                        data.debugOverlay.SetViewport(ctx.cmd);
                        ctx.cmd.DrawProcedural(Matrix4x4.identity, data.debugDensityVolumeMaterial, 1, MeshTopology.Triangles, 3, 1, mpb);
                        data.debugOverlay.Next();
                    });
            }
        }

        class RenderTileClusterDebugOverlayPassData
            : DebugOverlayPassData
        {
            public HDCamera hdCamera;
            public TextureHandle depthPyramidTexture;
            public ComputeBufferHandle tileList;
            public ComputeBufferHandle lightList;
            public ComputeBufferHandle perVoxelLightList;
            public ComputeBufferHandle dispatchIndirect;
            public Material debugViewTilesMaterial;
            public LightingDebugSettings lightingDebugSettings;
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
                passData.depthBuffer = builder.UseDepthBuffer(depthBuffer, DepthAccess.ReadWrite);
                passData.depthPyramidTexture = builder.ReadTexture(depthPyramidTexture);
                passData.tileList = builder.ReadComputeBuffer(lightLists.tileList);
                passData.lightList = builder.ReadComputeBuffer(lightLists.lightList);
                passData.perVoxelLightList = builder.ReadComputeBuffer(lightLists.perVoxelLightLists);
                passData.dispatchIndirect = builder.ReadComputeBuffer(lightLists.dispatchIndirectBuffer);
                passData.debugViewTilesMaterial = m_DebugViewTilesMaterial;
                passData.lightingDebugSettings = m_CurrentDebugDisplaySettings.data.lightingDebugSettings;

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
                                data.debugViewTilesMaterial.SetBuffer(HDShaderIDs.g_TileList, data.tileList);
                                data.debugViewTilesMaterial.SetBuffer(HDShaderIDs.g_DispatchIndirectBuffer, data.dispatchIndirect);
                                data.debugViewTilesMaterial.EnableKeyword("USE_FPTL_LIGHTLIST");
                                data.debugViewTilesMaterial.DisableKeyword("USE_CLUSTERED_LIGHTLIST");
                                data.debugViewTilesMaterial.DisableKeyword("SHOW_LIGHT_CATEGORIES");
                                data.debugViewTilesMaterial.EnableKeyword("SHOW_FEATURE_VARIANTS");
                                if (DeferredUseComputeAsPixel(data.hdCamera.frameSettings))
                                    data.debugViewTilesMaterial.EnableKeyword("IS_DRAWPROCEDURALINDIRECT");
                                else
                                    data.debugViewTilesMaterial.DisableKeyword("IS_DRAWPROCEDURALINDIRECT");
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
                            data.debugViewTilesMaterial.SetVector(HDShaderIDs._MousePixelCoord, HDUtils.GetMouseCoordinates(data.hdCamera));
                            data.debugViewTilesMaterial.SetVector(HDShaderIDs._MouseClickPixelCoord, HDUtils.GetMouseClickCoordinates(data.hdCamera));
                            data.debugViewTilesMaterial.SetBuffer(HDShaderIDs.g_vLightListGlobal, bUseClustered ? data.perVoxelLightList : data.lightList);
                            data.debugViewTilesMaterial.SetTexture(HDShaderIDs._CameraDepthTexture, data.depthPyramidTexture);
                            data.debugViewTilesMaterial.EnableKeyword(bUseClustered ? "USE_CLUSTERED_LIGHTLIST" : "USE_FPTL_LIGHTLIST");
                            data.debugViewTilesMaterial.DisableKeyword(!bUseClustered ? "USE_CLUSTERED_LIGHTLIST" : "USE_FPTL_LIGHTLIST");
                            data.debugViewTilesMaterial.EnableKeyword("SHOW_LIGHT_CATEGORIES");
                            data.debugViewTilesMaterial.DisableKeyword("SHOW_FEATURE_VARIANTS");
                            if (!bUseClustered && data.hdCamera.frameSettings.IsEnabled(FrameSettingsField.MSAA))
                                data.debugViewTilesMaterial.EnableKeyword("DISABLE_TILE_MODE");
                            else
                                data.debugViewTilesMaterial.DisableKeyword("DISABLE_TILE_MODE");

                            CoreUtils.DrawFullScreen(ctx.cmd, data.debugViewTilesMaterial, 0);
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
                passData.depthBuffer = builder.UseDepthBuffer(depthBuffer, DepthAccess.ReadWrite);
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
                                    data.shadowManager.DisplayShadowMap(data.shadowTextures, shadowIndex, ctx.cmd, data.debugShadowMapMaterial, data.debugOverlay.x, data.debugOverlay.y, data.debugOverlay.overlaySize, data.debugOverlay.overlaySize, lightingDebug.shadowMinValue, lightingDebug.shadowMaxValue, mpb);
                                    data.debugOverlay.Next();
                                }
                                break;
                            case ShadowMapDebugMode.VisualizePunctualLightAtlas:
                                data.shadowManager.DisplayShadowAtlas(data.shadowTextures.punctualShadowResult, ctx.cmd, data.debugShadowMapMaterial, data.debugOverlay.x, data.debugOverlay.y, data.debugOverlay.overlaySize, data.debugOverlay.overlaySize, lightingDebug.shadowMinValue, lightingDebug.shadowMaxValue, mpb);
                                data.debugOverlay.Next();
                                break;
                            case ShadowMapDebugMode.VisualizeCachedPunctualLightAtlas:
                                data.shadowManager.DisplayCachedPunctualShadowAtlas(data.shadowTextures.cachedPunctualShadowResult, ctx.cmd, data.debugShadowMapMaterial, data.debugOverlay.x, data.debugOverlay.y, data.debugOverlay.overlaySize, data.debugOverlay.overlaySize, lightingDebug.shadowMinValue, lightingDebug.shadowMaxValue, mpb);
                                data.debugOverlay.Next();
                                break;
                            case ShadowMapDebugMode.VisualizeDirectionalLightAtlas:
                                data.shadowManager.DisplayShadowCascadeAtlas(data.shadowTextures.directionalShadowResult, ctx.cmd, data.debugShadowMapMaterial, data.debugOverlay.x, data.debugOverlay.y, data.debugOverlay.overlaySize, data.debugOverlay.overlaySize, lightingDebug.shadowMinValue, lightingDebug.shadowMaxValue, mpb);
                                data.debugOverlay.Next();
                                break;
                            case ShadowMapDebugMode.VisualizeAreaLightAtlas:
                                data.shadowManager.DisplayAreaLightShadowAtlas(data.shadowTextures.areaShadowResult, ctx.cmd, data.debugShadowMapMaterial, data.debugOverlay.x, data.debugOverlay.y, data.debugOverlay.overlaySize, data.debugOverlay.overlaySize, lightingDebug.shadowMinValue, lightingDebug.shadowMaxValue, mpb);
                                data.debugOverlay.Next();
                                break;
                            case ShadowMapDebugMode.VisualizeCachedAreaLightAtlas:
                                data.shadowManager.DisplayCachedAreaShadowAtlas(data.shadowTextures.cachedAreaShadowResult, ctx.cmd, data.debugShadowMapMaterial, data.debugOverlay.x, data.debugOverlay.y, data.debugOverlay.overlaySize, data.debugOverlay.overlaySize, lightingDebug.shadowMinValue, lightingDebug.shadowMaxValue, mpb);
                                data.debugOverlay.Next();
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
            if (!m_CurrentDebugDisplaySettings.data.decalsDebugSettings.displayAtlas)
                return;

            using (var builder = renderGraph.AddRenderPass<RenderDecalOverlayPassData>("DecalOverlay", out var passData, ProfilingSampler.Get(HDProfileId.DisplayDebugDecalsAtlas)))
            {
                passData.debugOverlay = m_DebugOverlay;
                passData.colorBuffer = builder.UseColorBuffer(colorBuffer, 0);
                passData.depthBuffer = builder.UseDepthBuffer(depthBuffer, DepthAccess.ReadWrite);
                passData.mipLevel = (int)debugDisplaySettings.data.decalsDebugSettings.mipLevel;
                passData.hdCamera = hdCamera;

                builder.SetRenderFunc(
                    (RenderDecalOverlayPassData data, RenderGraphContext ctx) =>
                    {
                        DecalSystem.instance.RenderDebugOverlay(data.hdCamera, ctx.cmd, data.mipLevel, data.debugOverlay);
                    });
            }
        }

        void RenderDebugOverlays(RenderGraph    renderGraph,
            TextureHandle               colorBuffer,
            TextureHandle               depthBuffer,
            TextureHandle               depthPyramidTexture,
            TextureHandle               rayCountTexture,
            in BuildGPULightListOutput  lightLists,
            in ShadowResult             shadowResult,
            HDCamera                    hdCamera)
        {
            float overlayRatio = m_CurrentDebugDisplaySettings.data.debugOverlayRatio;
            int overlaySize = (int)(Math.Min(hdCamera.actualHeight, hdCamera.actualWidth) * overlayRatio);
            m_DebugOverlay.StartOverlay(HDUtils.GetRuntimeDebugPanelWidth(hdCamera), hdCamera.actualHeight - overlaySize, overlaySize, hdCamera.actualWidth);

            RenderSkyReflectionOverlay(renderGraph, colorBuffer, depthBuffer, hdCamera);
            RenderRayCountOverlay(renderGraph, hdCamera, colorBuffer, depthBuffer, rayCountTexture);

            if (m_CurrentDebugDisplaySettings.data.lightingDebugSettings.displayCookieAtlas)
                RenderAtlasDebugOverlay(renderGraph, colorBuffer, depthBuffer, m_TextureCaches.lightCookieManager.atlasTexture, (int)m_CurrentDebugDisplaySettings.data.lightingDebugSettings.cookieAtlasMipLevel, applyExposure: false, "RenderCookieAtlasOverlay", HDProfileId.DisplayCookieAtlas);

            if (m_CurrentDebugDisplaySettings.data.lightingDebugSettings.displayPlanarReflectionProbeAtlas)
                RenderAtlasDebugOverlay(renderGraph, colorBuffer, depthBuffer, m_TextureCaches.reflectionPlanarProbeCache.GetTexCache(), (int)m_CurrentDebugDisplaySettings.data.lightingDebugSettings.planarReflectionProbeMipLevel, applyExposure: true, "RenderPlanarProbeAtlasOverlay", HDProfileId.DisplayPlanarReflectionProbeAtlas);

            RenderDensityVolumeAtlasDebugOverlay(renderGraph, colorBuffer, depthBuffer);
            RenderTileClusterDebugOverlay(renderGraph, colorBuffer, depthBuffer, lightLists, depthPyramidTexture, hdCamera);
            RenderShadowsDebugOverlay(renderGraph, colorBuffer, depthBuffer, shadowResult);
            RenderDecalOverlay(renderGraph, colorBuffer, depthBuffer, hdCamera);
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
            public PostProcessSystem.DebugImageHistogramParameters parameters;
            public TextureHandle source;
        }

        void GenerateDebugImageHistogram(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle source)
        {
            if (m_CurrentDebugDisplaySettings.data.lightingDebugSettings.exposureDebugMode != ExposureDebugMode.FinalImageHistogramView)
                return;

            using (var builder = renderGraph.AddRenderPass<DebugImageHistogramData>("Generate Debug Image Histogram", out var passData, ProfilingSampler.Get(HDProfileId.FinalImageHistogram)))
            {
                passData.source = builder.ReadTexture(source);
                passData.parameters = m_PostProcessSystem.PrepareDebugImageHistogramParameters(hdCamera);
                builder.SetRenderFunc(
                    (DebugImageHistogramData data, RenderGraphContext ctx) =>
                    {
                        PostProcessSystem.GenerateDebugImageHistogram(data.parameters, ctx.cmd, data.source);
                    });
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
                m_PostProcessSystem.ComputeProceduralMeteringParams(hdCamera, out passData.proceduralMeteringParams1, out passData.proceduralMeteringParams2);

                passData.lightingDebugSettings = m_CurrentDebugDisplaySettings.data.lightingDebugSettings;
                passData.hdCamera = hdCamera;
                passData.debugExposureMaterial = m_DebugExposure;
                passData.colorBuffer = builder.ReadTexture(colorBuffer);
                passData.debugFullScreenTexture = builder.ReadTexture(m_DebugFullScreenTexture);
                passData.output = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                    { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, name = "ExposureDebug" }));
                passData.currentExposure = builder.ReadTexture(renderGraph.ImportTexture(m_PostProcessSystem.GetExposureTexture(hdCamera)));
                passData.previousExposure = builder.ReadTexture(renderGraph.ImportTexture(m_PostProcessSystem.GetPreviousExposureTexture(hdCamera)));
                passData.debugExposureData = builder.ReadTexture(renderGraph.ImportTexture(m_PostProcessSystem.GetExposureDebugData()));
                passData.customToneMapCurve = m_PostProcessSystem.GetCustomToneMapCurve();
                passData.lutSize = m_PostProcessSystem.GetLutSize();
                passData.histogramBuffer = passData.lightingDebugSettings.exposureDebugMode == ExposureDebugMode.FinalImageHistogramView ? m_PostProcessSystem.GetDebugImageHistogramBuffer() : m_PostProcessSystem.GetHistogramBuffer();

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
                            data.debugExposureMaterial.SetVector(HDShaderIDs._ExposureDebugParams, new Vector4(drawTonemapCurve ? 1.0f : 0.0f, (int)tonemappingMode, centerAroundMiddleGrey ? 1 : 0, 0));
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

        TextureHandle RenderDebug(RenderGraph                 renderGraph,
            HDCamera                    hdCamera,
            TextureHandle               colorBuffer,
            TextureHandle               depthBuffer,
            TextureHandle               depthPyramidTexture,
            TextureHandle               colorPickerDebugTexture,
            TextureHandle               rayCountTexture,
            in BuildGPULightListOutput  lightLists,
            in ShadowResult             shadowResult,
            CullingResults              cullResults,
            GraphicsFormat              colorFormat)
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
                m_DebugFullScreenComputeBuffer = ComputeBufferHandle.nullHandle;
            }

            if (NeedExposureDebugMode(m_CurrentDebugDisplaySettings))
                output = RenderExposureDebug(renderGraph, hdCamera, colorBuffer);

            if (NeedColorPickerDebug(m_CurrentDebugDisplaySettings))
                output = ResolveColorPickerDebug(renderGraph, colorPickerDebugTexture, hdCamera, colorFormat);

            RenderLightVolumes(renderGraph, output, depthBuffer, cullResults, hdCamera);

            RenderDebugOverlays(renderGraph, output, depthBuffer, depthPyramidTexture, rayCountTexture, lightLists, shadowResult, hdCamera);

            return output;
        }

        class DebugViewMaterialData
        {
            public TextureHandle outputColor;
            public TextureHandle outputDepth;
            public RendererListHandle opaqueRendererList;
            public RendererListHandle transparentRendererList;
            public Material debugGBufferMaterial;
            public FrameSettings frameSettings;

            public Texture clearColorTexture;
            public RenderTexture clearDepthTexture;
            public bool clearDepth;
        }

        TextureHandle RenderDebugViewMaterial(RenderGraph renderGraph, CullingResults cull, HDCamera hdCamera)
        {
            bool msaa = hdCamera.frameSettings.IsEnabled(FrameSettingsField.MSAA);

            var output = renderGraph.CreateTexture(
                new TextureDesc(Vector2.one, true, true)
                {
                    colorFormat = GetColorBufferFormat(),
                    enableRandomWrite = !msaa,
                    bindTextureMS = msaa,
                    enableMSAA = msaa,
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

                    builder.SetRenderFunc(
                        (DebugViewMaterialData data, RenderGraphContext context) =>
                        {
                            HDUtils.DrawFullScreen(context.cmd, data.debugGBufferMaterial, data.outputColor);
                        });
                }
            }
            else
            {
                using (var builder = renderGraph.AddRenderPass<DebugViewMaterialData>("DisplayDebug ViewMaterial", out var passData, ProfilingSampler.Get(HDProfileId.DisplayDebugViewMaterial)))
                {
                    passData.frameSettings = hdCamera.frameSettings;
                    passData.outputColor = builder.UseColorBuffer(output, 0);
                    passData.outputDepth = builder.UseDepthBuffer(CreateDepthBuffer(renderGraph, true, hdCamera.frameSettings.IsEnabled(FrameSettingsField.MSAA)), DepthAccess.ReadWrite);

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
                            DrawOpaqueRendererList(context, data.frameSettings, data.opaqueRendererList);
                            DrawTransparentRendererList(context, data.frameSettings, data.transparentRendererList);
                        });
                }
            }

            return output;
        }

        class PushFullScreenDebugPassData
        {
            public TextureHandle    input;
            public TextureHandle    output;
            public int              mipIndex;
            public bool             xrTexture;
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

        void PushFullScreenDebugTextureMip(RenderGraph renderGraph, TextureHandle input, int lodCount, Vector4 scaleBias, FullScreenDebugMode debugMode, GraphicsFormat colorFormat = GraphicsFormat.R16G16B16A16_SFloat)
        {
            if (debugMode == m_CurrentDebugDisplaySettings.data.fullScreenDebugMode)
            {
                var mipIndex = Mathf.FloorToInt(m_CurrentDebugDisplaySettings.data.fullscreenDebugMip * lodCount);

                PushFullScreenDebugTexture(renderGraph, input, colorFormat, mipIndex);
            }
        }

        void PushFullScreenDebugTexture(RenderGraph renderGraph, TextureHandle input, GraphicsFormat rtFormat = GraphicsFormat.R16G16B16A16_SFloat, int mipIndex = -1, bool xrTexture = true)
        {
            using (var builder = renderGraph.AddRenderPass<PushFullScreenDebugPassData>("Push Full Screen Debug", out var passData))
            {
                passData.mipIndex = mipIndex;
                passData.xrTexture = xrTexture;
                passData.input = builder.ReadTexture(input);
                passData.output = builder.UseColorBuffer(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                    { colorFormat = rtFormat, name = "DebugFullScreen" }), 0);

                builder.SetRenderFunc(
                    (PushFullScreenDebugPassData data, RenderGraphContext ctx) =>
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
                    { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, name = "DebugColorPicker" }), 0);

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
