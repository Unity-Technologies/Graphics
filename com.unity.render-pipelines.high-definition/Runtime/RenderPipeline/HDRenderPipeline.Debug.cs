using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        DebugOverlay m_DebugOverlay = new DebugOverlay();
        TextureHandle m_DebugFullScreenTexture;
        ComputeBufferHandle m_DebugFullScreenComputeBuffer;


        static bool NeedColorPickerDebug(DebugDisplaySettings debugSettings)
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

        struct DebugParameters
        {
            public DebugDisplaySettings debugDisplaySettings;
            public HDCamera hdCamera;

            public DebugOverlay debugOverlay;

            public bool rayTracingSupported;
            public RayCountManager rayCountManager;

            // Lighting
            public LightLoopDebugOverlayParameters lightingOverlayParameters;

            // Exposure
            public bool exposureDebugEnabled;
            public Material debugExposureMaterial;
        }

        DebugParameters PrepareDebugParameters(HDCamera hdCamera, HDUtils.PackedMipChainInfo depthMipInfo)
        {
            var parameters = new DebugParameters();

            parameters.debugDisplaySettings = m_CurrentDebugDisplaySettings;
            parameters.hdCamera = hdCamera;

            parameters.lightingOverlayParameters = PrepareLightLoopDebugOverlayParameters();

            parameters.rayTracingSupported = hdCamera.frameSettings.IsEnabled(FrameSettingsField.RayTracing);
            parameters.rayCountManager = m_RayCountManager;

            parameters.exposureDebugEnabled = NeedExposureDebugMode(parameters.debugDisplaySettings);
            parameters.debugExposureMaterial = m_DebugExposure;

            parameters.debugOverlay = m_DebugOverlay;

            return parameters;
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

        TextureHandle ResolveFullScreenDebug(RenderGraph renderGraph, TextureHandle inputFullScreenDebug, TextureHandle depthPyramid, HDCamera hdCamera)
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
                    { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, name = "ResolveFullScreenDebug" }));

                builder.SetRenderFunc(
                    (ResolveFullScreenDebugPassData data, RenderGraphContext ctx) =>
                    {
                        var mpb = ctx.renderGraphPool.GetTempMaterialPropertyBlock();
                        ComputeBuffer fullscreenBuffer = data.fullscreenBuffer;

                        mpb.SetTexture(HDShaderIDs._DebugFullScreenTexture, inputFullScreenDebug);
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

        TextureHandle ResolveColorPickerDebug(RenderGraph renderGraph, TextureHandle inputColorPickerDebug, HDCamera hdCamera)
        {
            using (var builder = renderGraph.AddRenderPass<ResolveColorPickerDebugPassData>("ResolveColorPickerDebug", out var passData))
            {
                passData.hdCamera = hdCamera;
                passData.debugDisplaySettings = m_CurrentDebugDisplaySettings;
                passData.colorPickerMaterial = m_DebugColorPicker;
                passData.input = builder.ReadTexture(inputColorPickerDebug);
                passData.output = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                    { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, name = "ResolveColorPickerDebug" }));

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

        void RenderRayCountOverlay(RenderGraph renderGraph, in DebugParameters debugParameters, TextureHandle colorBuffer, TextureHandle depthBuffer, TextureHandle rayCountTexture)
        {
            if (!debugParameters.rayTracingSupported)
                return;

            debugParameters.rayCountManager.EvaluateRayCount(renderGraph, debugParameters.hdCamera, colorBuffer, depthBuffer, rayCountTexture);
        }

        class DebugLightLoopOverlayPassData
            : DebugOverlayPassData
        {
            public DebugParameters debugParameters;
            public TextureHandle depthPyramidTexture;
            public ComputeBufferHandle tileList;
            public ComputeBufferHandle lightList;
            public ComputeBufferHandle perVoxelLightList;
            public ComputeBufferHandle dispatchIndirect;
        }


        struct LightLoopDebugOverlayParameters
        {
            public Material debugViewTilesMaterial;
            public Material debugDensityVolumeMaterial;
            public Material debugBlitMaterial;
            public LightCookieManager cookieManager;
            public PlanarReflectionProbeCache planarProbeCache;
        }

        LightLoopDebugOverlayParameters PrepareLightLoopDebugOverlayParameters()
        {
            var parameters = new LightLoopDebugOverlayParameters();

            parameters.debugViewTilesMaterial = m_DebugViewTilesMaterial;

            parameters.debugDensityVolumeMaterial = m_DebugDensityVolumeMaterial;
            parameters.debugBlitMaterial = m_DebugBlitMaterial;
            parameters.cookieManager = m_TextureCaches.lightCookieManager;
            parameters.planarProbeCache = m_TextureCaches.reflectionPlanarProbeCache;

            return parameters;
        }

        static void RenderLightLoopDebugOverlay(in DebugParameters debugParameters,
            CommandBuffer cmd,
            ComputeBuffer tileBuffer,
            ComputeBuffer lightListBuffer,
            ComputeBuffer perVoxelLightListBuffer,
            ComputeBuffer dispatchIndirectBuffer,
            RTHandle depthTexture)
        {
            var hdCamera = debugParameters.hdCamera;
            var parameters = debugParameters.lightingOverlayParameters;
            LightingDebugSettings lightingDebug = debugParameters.debugDisplaySettings.data.lightingDebugSettings;
            if (lightingDebug.tileClusterDebug != TileClusterDebug.None)
            {
                using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.TileClusterLightingDebug)))
                {
                    int w = hdCamera.actualWidth;
                    int h = hdCamera.actualHeight;
                    int numTilesX = (w + 15) / 16;
                    int numTilesY = (h + 15) / 16;
                    int numTiles = numTilesX * numTilesY;

                    // Debug tiles
                    if (lightingDebug.tileClusterDebug == TileClusterDebug.MaterialFeatureVariants)
                    {
                        if (GetFeatureVariantsEnabled(hdCamera.frameSettings))
                        {
                            // featureVariants
                            parameters.debugViewTilesMaterial.SetInt(HDShaderIDs._NumTiles, numTiles);
                            parameters.debugViewTilesMaterial.SetInt(HDShaderIDs._ViewTilesFlags, (int)lightingDebug.tileClusterDebugByCategory);
                            parameters.debugViewTilesMaterial.SetVector(HDShaderIDs._MousePixelCoord, HDUtils.GetMouseCoordinates(hdCamera));
                            parameters.debugViewTilesMaterial.SetVector(HDShaderIDs._MouseClickPixelCoord, HDUtils.GetMouseClickCoordinates(hdCamera));
                            parameters.debugViewTilesMaterial.SetBuffer(HDShaderIDs.g_TileList, tileBuffer);
                            parameters.debugViewTilesMaterial.SetBuffer(HDShaderIDs.g_DispatchIndirectBuffer, dispatchIndirectBuffer);
                            parameters.debugViewTilesMaterial.EnableKeyword("USE_FPTL_LIGHTLIST");
                            parameters.debugViewTilesMaterial.DisableKeyword("USE_CLUSTERED_LIGHTLIST");
                            parameters.debugViewTilesMaterial.DisableKeyword("SHOW_LIGHT_CATEGORIES");
                            parameters.debugViewTilesMaterial.EnableKeyword("SHOW_FEATURE_VARIANTS");
                            if (DeferredUseComputeAsPixel(hdCamera.frameSettings))
                                parameters.debugViewTilesMaterial.EnableKeyword("IS_DRAWPROCEDURALINDIRECT");
                            else
                                parameters.debugViewTilesMaterial.DisableKeyword("IS_DRAWPROCEDURALINDIRECT");
                            cmd.DrawProcedural(Matrix4x4.identity, parameters.debugViewTilesMaterial, 0, MeshTopology.Triangles, numTiles * 6);
                        }
                    }
                    else // tile or cluster
                    {
                        bool bUseClustered = lightingDebug.tileClusterDebug == TileClusterDebug.Cluster;

                        // lightCategories
                        parameters.debugViewTilesMaterial.SetInt(HDShaderIDs._ViewTilesFlags, (int)lightingDebug.tileClusterDebugByCategory);
                        parameters.debugViewTilesMaterial.SetInt(HDShaderIDs._ClusterDebugMode, bUseClustered ? (int)lightingDebug.clusterDebugMode : (int)ClusterDebugMode.VisualizeOpaque);
                        parameters.debugViewTilesMaterial.SetFloat(HDShaderIDs._ClusterDebugDistance, lightingDebug.clusterDebugDistance);
                        parameters.debugViewTilesMaterial.SetVector(HDShaderIDs._MousePixelCoord, HDUtils.GetMouseCoordinates(hdCamera));
                        parameters.debugViewTilesMaterial.SetVector(HDShaderIDs._MouseClickPixelCoord, HDUtils.GetMouseClickCoordinates(hdCamera));
                        parameters.debugViewTilesMaterial.SetBuffer(HDShaderIDs.g_vLightListGlobal, bUseClustered ? perVoxelLightListBuffer : lightListBuffer);
                        parameters.debugViewTilesMaterial.SetTexture(HDShaderIDs._CameraDepthTexture, depthTexture);
                        parameters.debugViewTilesMaterial.EnableKeyword(bUseClustered ? "USE_CLUSTERED_LIGHTLIST" : "USE_FPTL_LIGHTLIST");
                        parameters.debugViewTilesMaterial.DisableKeyword(!bUseClustered ? "USE_CLUSTERED_LIGHTLIST" : "USE_FPTL_LIGHTLIST");
                        parameters.debugViewTilesMaterial.EnableKeyword("SHOW_LIGHT_CATEGORIES");
                        parameters.debugViewTilesMaterial.DisableKeyword("SHOW_FEATURE_VARIANTS");
                        if (!bUseClustered && hdCamera.frameSettings.IsEnabled(FrameSettingsField.MSAA))
                            parameters.debugViewTilesMaterial.EnableKeyword("DISABLE_TILE_MODE");
                        else
                            parameters.debugViewTilesMaterial.DisableKeyword("DISABLE_TILE_MODE");

                        CoreUtils.DrawFullScreen(cmd, parameters.debugViewTilesMaterial, 0);
                    }
                }
            }

            if (lightingDebug.displayCookieAtlas)
            {
                using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.DisplayCookieAtlas)))
                {
                    m_LightLoopDebugMaterialProperties.SetFloat(HDShaderIDs._ApplyExposure, 0.0f);
                    m_LightLoopDebugMaterialProperties.SetFloat(HDShaderIDs._Mipmap, lightingDebug.cookieAtlasMipLevel);
                    m_LightLoopDebugMaterialProperties.SetTexture(HDShaderIDs._InputTexture, parameters.cookieManager.atlasTexture);
                    debugParameters.debugOverlay.SetViewport(cmd);
                    cmd.DrawProcedural(Matrix4x4.identity, parameters.debugBlitMaterial, 0, MeshTopology.Triangles, 3, 1, m_LightLoopDebugMaterialProperties);
                    debugParameters.debugOverlay.Next();
                }
            }

            if (lightingDebug.clearPlanarReflectionProbeAtlas)
            {
                parameters.planarProbeCache.Clear(cmd);
            }

            if (lightingDebug.displayPlanarReflectionProbeAtlas)
            {
                using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.DisplayPlanarReflectionProbeAtlas)))
                {
                    m_LightLoopDebugMaterialProperties.SetFloat(HDShaderIDs._ApplyExposure, 1.0f);
                    m_LightLoopDebugMaterialProperties.SetFloat(HDShaderIDs._Mipmap, lightingDebug.planarReflectionProbeMipLevel);
                    m_LightLoopDebugMaterialProperties.SetTexture(HDShaderIDs._InputTexture, parameters.planarProbeCache.GetTexCache());
                    debugParameters.debugOverlay.SetViewport(cmd);
                    cmd.DrawProcedural(Matrix4x4.identity, parameters.debugBlitMaterial, 0, MeshTopology.Triangles, 3, 1, m_LightLoopDebugMaterialProperties);
                    debugParameters.debugOverlay.Next();
                }
            }

            if (lightingDebug.displayDensityVolumeAtlas)
            {
                using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.DisplayDensityVolumeAtlas)))
                {
                    var atlas = DensityVolumeManager.manager.volumeAtlas;
                    var atlasTexture = atlas.GetAtlas();
                    m_LightLoopDebugMaterialProperties.SetTexture(HDShaderIDs._InputTexture, atlasTexture);
                    m_LightLoopDebugMaterialProperties.SetFloat("_Slice", (float)lightingDebug.densityVolumeAtlasSlice);
                    m_LightLoopDebugMaterialProperties.SetVector("_Offset", Vector3.zero);
                    m_LightLoopDebugMaterialProperties.SetVector("_TextureSize", new Vector3(atlasTexture.width, atlasTexture.height, atlasTexture.volumeDepth));

#if UNITY_EDITOR
                    if (lightingDebug.densityVolumeUseSelection)
                    {
                        var obj = UnityEditor.Selection.activeGameObject;

                        if (obj != null && obj.TryGetComponent<DensityVolume>(out var densityVolume))
                        {
                            var texture = densityVolume.parameters.volumeMask;

                            if (texture != null)
                            {
                                float textureDepth = texture is RenderTexture rt ? rt.volumeDepth : texture is Texture3D t3D ? t3D.depth : 0;
                                m_LightLoopDebugMaterialProperties.SetVector("_TextureSize", new Vector3(texture.width, texture.height, textureDepth));
                                m_LightLoopDebugMaterialProperties.SetVector("_Offset", atlas.GetTextureOffset(texture));
                            }
                        }
                    }
#endif

                    debugParameters.debugOverlay.SetViewport(cmd);
                    cmd.DrawProcedural(Matrix4x4.identity, parameters.debugDensityVolumeMaterial, 0, MeshTopology.Triangles, 3, 1, m_LightLoopDebugMaterialProperties);
                    debugParameters.debugOverlay.Next();
                    debugParameters.debugOverlay.SetViewport(cmd);
                    cmd.DrawProcedural(Matrix4x4.identity, parameters.debugDensityVolumeMaterial, 1, MeshTopology.Triangles, 3, 1, m_LightLoopDebugMaterialProperties);
                    debugParameters.debugOverlay.Next();
                }
            }
        }

        void RenderLightLoopDebugOverlay(RenderGraph renderGraph, in DebugParameters debugParameters, TextureHandle colorBuffer, TextureHandle depthBuffer, in BuildGPULightListOutput lightLists, TextureHandle depthPyramidTexture)
        {
            var lightingDebug = debugParameters.debugDisplaySettings.data.lightingDebugSettings;
            if (lightingDebug.tileClusterDebug == TileClusterDebug.None
                && !lightingDebug.displayCookieAtlas
                && !lightingDebug.displayPlanarReflectionProbeAtlas
                && !lightingDebug.displayDensityVolumeAtlas)
                return;

            using (var builder = renderGraph.AddRenderPass<DebugLightLoopOverlayPassData>("RenderLightLoopDebugOverlay", out var passData))
            {
                passData.debugParameters = debugParameters;
                passData.colorBuffer = builder.UseColorBuffer(colorBuffer, 0);
                passData.depthBuffer = builder.UseDepthBuffer(depthBuffer, DepthAccess.ReadWrite);

                if (lightingDebug.tileClusterDebug != TileClusterDebug.None)
                {
                    passData.depthPyramidTexture = builder.ReadTexture(depthPyramidTexture);
                    passData.tileList = builder.ReadComputeBuffer(lightLists.tileList);
                    passData.lightList = builder.ReadComputeBuffer(lightLists.lightList);
                    passData.perVoxelLightList = builder.ReadComputeBuffer(lightLists.perVoxelLightLists);
                    passData.dispatchIndirect = builder.ReadComputeBuffer(lightLists.dispatchIndirectBuffer);
                }

                builder.SetRenderFunc(
                    (DebugLightLoopOverlayPassData data, RenderGraphContext ctx) =>
                    {
                        RenderLightLoopDebugOverlay(data.debugParameters, ctx.cmd, data.tileList, data.lightList, data.perVoxelLightList, data.dispatchIndirect, data.depthPyramidTexture);
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

        void RenderDebugOverlays(RenderGraph                renderGraph,
            in DebugParameters          debugParameters,
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
            RenderRayCountOverlay(renderGraph, debugParameters, colorBuffer, depthBuffer, rayCountTexture);
            RenderLightLoopDebugOverlay(renderGraph, debugParameters, colorBuffer, depthBuffer, lightLists, depthPyramidTexture);
            RenderShadowsDebugOverlay(renderGraph, colorBuffer, depthBuffer, shadowResult);
            RenderDecalOverlay(renderGraph, colorBuffer, depthBuffer, hdCamera);
        }

        class RenderLightVolumesPassData
        {
            public DebugLightVolumes.RenderLightVolumesParameters   parameters;
            // Render target that holds the light count in floating points
            public TextureHandle                                    lightCountBuffer;
            // Render target that holds the color accumulated value
            public TextureHandle                                    colorAccumulationBuffer;
            // The output texture of the debug
            public TextureHandle                                    debugLightVolumesTexture;
            // Required depth texture given that we render multiple render targets
            public TextureHandle                                    depthBuffer;
            public TextureHandle                                    destination;
        }

        static void RenderLightVolumes(RenderGraph renderGraph, in DebugParameters debugParameters, TextureHandle destination, TextureHandle depthBuffer, CullingResults cullResults)
        {
            using (var builder = renderGraph.AddRenderPass<RenderLightVolumesPassData>("LightVolumes", out var passData))
            {
                passData.parameters = s_lightVolumes.PrepareLightVolumeParameters(debugParameters.hdCamera, debugParameters.debugDisplaySettings.data.lightingDebugSettings, cullResults);
                passData.lightCountBuffer = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                    { colorFormat = GraphicsFormat.R32_SFloat, clearBuffer = true, clearColor = Color.black, name = "LightVolumeCount" });
                passData.colorAccumulationBuffer = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                    { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, clearBuffer = true, clearColor = Color.black, name = "LightVolumeColorAccumulation" });
                passData.debugLightVolumesTexture = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                    { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, clearBuffer = true, clearColor = Color.black, enableRandomWrite = true, name = "LightVolumeDebugLightVolumesTexture" });
                passData.depthBuffer = builder.UseDepthBuffer(depthBuffer, DepthAccess.ReadWrite);
                passData.destination = builder.WriteTexture(destination);

                builder.SetRenderFunc(
                    (RenderLightVolumesPassData data, RenderGraphContext ctx) =>
                    {
                        RenderTargetIdentifier[] mrt = ctx.renderGraphPool.GetTempArray<RenderTargetIdentifier>(2);
                        mrt[0] = data.lightCountBuffer;
                        mrt[1] = data.colorAccumulationBuffer;

                        DebugLightVolumes.RenderLightVolumes(ctx.cmd,
                            data.parameters,
                            mrt, data.lightCountBuffer,
                            data.colorAccumulationBuffer,
                            data.debugLightVolumesTexture,
                            data.depthBuffer,
                            data.destination,
                            ctx.renderGraphPool.GetTempMaterialPropertyBlock());
                    });
            }
        }

        class DebugImageHistogramData
        {
            public PostProcessSystem.DebugImageHistogramParameters parameters;
            public TextureHandle source;
        }

        void GenerateDebugImageHistogram(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle source)
        {
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
            public DebugParameters debugParameters;
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

        TextureHandle RenderExposureDebug(RenderGraph renderGraph, HDCamera hdCamera, DebugParameters debugParameters, TextureHandle colorBuffer)
        {
            using (var builder = renderGraph.AddRenderPass<DebugExposureData>("Debug Exposure", out var passData))
            {
                m_PostProcessSystem.ComputeProceduralMeteringParams(hdCamera, out passData.proceduralMeteringParams1, out passData.proceduralMeteringParams2);

                passData.debugParameters = debugParameters;
                passData.colorBuffer = builder.ReadTexture(colorBuffer);
                passData.debugFullScreenTexture = builder.ReadTexture(m_DebugFullScreenTexture);
                passData.output = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                    { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, name = "ExposureDebug" }));
                passData.currentExposure = builder.ReadTexture(renderGraph.ImportTexture(m_PostProcessSystem.GetExposureTexture(hdCamera)));
                passData.previousExposure = builder.ReadTexture(renderGraph.ImportTexture(m_PostProcessSystem.GetPreviousExposureTexture(hdCamera)));
                passData.debugExposureData = builder.ReadTexture(renderGraph.ImportTexture(m_PostProcessSystem.GetExposureDebugData()));
                passData.customToneMapCurve = m_PostProcessSystem.GetCustomToneMapCurve();
                passData.lutSize = m_PostProcessSystem.GetLutSize();
                passData.histogramBuffer = debugParameters.debugDisplaySettings.data.lightingDebugSettings.exposureDebugMode == ExposureDebugMode.FinalImageHistogramView ? m_PostProcessSystem.GetDebugImageHistogramBuffer() : m_PostProcessSystem.GetHistogramBuffer();

                builder.SetRenderFunc(
                    (DebugExposureData data, RenderGraphContext ctx) =>
                    {
                        RenderExposureDebug(data.debugParameters, data.colorBuffer, data.debugFullScreenTexture,
                            data.previousExposure,
                            data.currentExposure,
                            data.debugExposureData,
                            data.output,
                            data.customToneMapCurve,
                            data.lutSize,
                            data.proceduralMeteringParams1,
                            data.proceduralMeteringParams2,
                            data.histogramBuffer, ctx.cmd);
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
            CullingResults              cullResults)
        {
            // We don't want any overlay for these kind of rendering
            if (hdCamera.camera.cameraType == CameraType.Reflection || hdCamera.camera.cameraType == CameraType.Preview)
                return colorBuffer;

            TextureHandle output = colorBuffer;
            var debugParameters = PrepareDebugParameters(hdCamera, GetDepthBufferMipChainInfo());

            if (NeedsFullScreenDebugMode() && m_FullScreenDebugPushed)
            {
                output = ResolveFullScreenDebug(renderGraph, m_DebugFullScreenTexture, depthPyramidTexture, hdCamera);
                // If we have full screen debug, this is what we want color picked, so we replace color picker input texture with the new one.
                if (NeedColorPickerDebug(m_CurrentDebugDisplaySettings))
                    colorPickerDebugTexture = PushColorPickerDebugTexture(renderGraph, output);

                m_FullScreenDebugPushed = false;
                m_DebugFullScreenComputeBuffer = ComputeBufferHandle.nullHandle;
            }

            if (debugParameters.exposureDebugEnabled)
                output = RenderExposureDebug(renderGraph, hdCamera, debugParameters, colorBuffer);

            if (NeedColorPickerDebug(m_CurrentDebugDisplaySettings))
                output = ResolveColorPickerDebug(renderGraph, colorPickerDebugTexture, hdCamera);

            if (debugParameters.debugDisplaySettings.data.lightingDebugSettings.displayLightVolumes)
            {
                RenderLightVolumes(renderGraph, debugParameters, output, depthBuffer, cullResults);
            }

            RenderDebugOverlays(renderGraph, debugParameters, output, depthBuffer, depthPyramidTexture, rayCountTexture, lightLists, shadowResult, hdCamera);

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
                            stateBlock: m_DepthStateOpaque)));

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
        }

        void PushFullScreenLightingDebugTexture(RenderGraph renderGraph, TextureHandle input)
        {
            // In practice, this is only useful for the SingleShadow debug view.
            // TODO: See how we can make this nicer than a specific functions just for one case.
            if (NeedsFullScreenDebugMode() && m_FullScreenDebugPushed == false)
            {
                PushFullScreenDebugTexture(renderGraph, input);
            }
        }

        internal void PushFullScreenDebugTexture(RenderGraph renderGraph, TextureHandle input, FullScreenDebugMode debugMode)
        {
            if (debugMode == m_CurrentDebugDisplaySettings.data.fullScreenDebugMode)
            {
                PushFullScreenDebugTexture(renderGraph, input);
            }
        }

        void PushFullScreenDebugTextureMip(RenderGraph renderGraph, TextureHandle input, int lodCount, Vector4 scaleBias, FullScreenDebugMode debugMode)
        {
            if (debugMode == m_CurrentDebugDisplaySettings.data.fullScreenDebugMode)
            {
                var mipIndex = Mathf.FloorToInt(m_CurrentDebugDisplaySettings.data.fullscreenDebugMip * lodCount);

                PushFullScreenDebugTexture(renderGraph, input, mipIndex);
            }
        }

        void PushFullScreenDebugTexture(RenderGraph renderGraph, TextureHandle input, int mipIndex = -1)
        {
            using (var builder = renderGraph.AddRenderPass<PushFullScreenDebugPassData>("Push Full Screen Debug", out var passData))
            {
                passData.mipIndex = mipIndex;
                passData.input = builder.ReadTexture(input);
                passData.output = builder.UseColorBuffer(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                    { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, name = "DebugFullScreen" }), 0);

                builder.SetRenderFunc(
                    (PushFullScreenDebugPassData data, RenderGraphContext ctx) =>
                    {
                        if (data.mipIndex != -1)
                            HDUtils.BlitCameraTexture(ctx.cmd, data.input, data.output, data.mipIndex);
                        else
                            HDUtils.BlitCameraTexture(ctx.cmd, data.input, data.output);
                    });

                m_DebugFullScreenTexture = passData.output;
            }

            // We need this flag because otherwise if no full screen debug is pushed (like for example if the corresponding pass is disabled), when we render the result in RenderDebug m_DebugFullScreenTempBuffer will contain potential garbage
            m_FullScreenDebugPushed = true;
        }

        void PushFullScreenExposureDebugTexture(RenderGraph renderGraph, TextureHandle input)
        {
            if (m_CurrentDebugDisplaySettings.data.lightingDebugSettings.exposureDebugMode != ExposureDebugMode.None)
            {
                PushFullScreenDebugTexture(renderGraph, input);
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
