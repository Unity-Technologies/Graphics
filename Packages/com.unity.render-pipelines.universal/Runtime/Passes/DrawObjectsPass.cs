using System;
using System.Collections.Generic;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal.Internal
{
    /// <summary>
    /// Draw  objects into the given color and depth target
    ///
    /// You can use this pass to render objects that have a material and/or shader
    /// with the pass names UniversalForward or SRPDefaultUnlit.
    /// </summary>
    public partial class DrawObjectsPass : ScriptableRenderPass
    {
        FilteringSettings m_FilteringSettings;
        RenderStateBlock m_RenderStateBlock;
        List<ShaderTagId> m_ShaderTagIdList = new List<ShaderTagId>();

        bool m_IsOpaque;

        /// <summary>
        /// Used to indicate whether transparent objects should receive shadows or not.
        /// </summary>
        public bool m_ShouldTransparentsReceiveShadows;

        static readonly int s_DrawObjectPassDataPropID = Shader.PropertyToID("_DrawObjectPassData");

        /// <summary>
        /// Creates a new <c>DrawObjectsPass</c> instance.
        /// </summary>
        /// <param name="profilerTag">The profiler tag used with the pass.</param>
        /// <param name="shaderTagIds"></param>
        /// <param name="opaque">Marks whether the objects are opaque or transparent.</param>
        /// <param name="evt">The <c>RenderPassEvent</c> to use.</param>
        /// <param name="renderQueueRange">The <c>RenderQueueRange</c> to use for creating filtering settings that control what objects get rendered.</param>
        /// <param name="layerMask">The layer mask to use for creating filtering settings that control what objects get rendered.</param>
        /// <param name="stencilState">The stencil settings to use with this poss.</param>
        /// <param name="stencilReference">The stencil reference value to use with this pass.</param>
        /// <seealso cref="ShaderTagId"/>
        /// <seealso cref="RenderPassEvent"/>
        /// <seealso cref="RenderQueueRange"/>
        /// <seealso cref="LayerMask"/>
        /// <seealso cref="StencilState"/>
        public DrawObjectsPass(string profilerTag, ShaderTagId[] shaderTagIds, bool opaque, RenderPassEvent evt, RenderQueueRange renderQueueRange, LayerMask layerMask, StencilState stencilState, int stencilReference)
        {
            Init(opaque, evt, renderQueueRange, layerMask, stencilState, stencilReference, shaderTagIds);

            profilingSampler = new ProfilingSampler(profilerTag);
        }

        /// <summary>
        /// Creates a new <c>DrawObjectsPass</c> instance.
        /// </summary>
        /// <param name="profilerTag">The profiler tag used with the pass.</param>
        /// <param name="opaque">Marks whether the objects are opaque or transparent.</param>
        /// <param name="evt">The <c>RenderPassEvent</c> to use.</param>
        /// <param name="renderQueueRange">The <c>RenderQueueRange</c> to use for creating filtering settings that control what objects get rendered.</param>
        /// <param name="layerMask">The layer mask to use for creating filtering settings that control what objects get rendered.</param>
        /// <param name="stencilState">The stencil settings to use with this poss.</param>
        /// <param name="stencilReference">The stencil reference value to use with this pass.</param>
        /// <seealso cref="RenderPassEvent"/>
        /// <seealso cref="RenderQueueRange"/>
        /// <seealso cref="LayerMask"/>
        /// <seealso cref="StencilState"/>
        public DrawObjectsPass(string profilerTag, bool opaque, RenderPassEvent evt, RenderQueueRange renderQueueRange, LayerMask layerMask, StencilState stencilState, int stencilReference)
            : this(profilerTag, null, opaque, evt, renderQueueRange, layerMask, stencilState, stencilReference)
        {
        }

        internal DrawObjectsPass(URPProfileId profileId, bool opaque, RenderPassEvent evt, RenderQueueRange renderQueueRange, LayerMask layerMask, StencilState stencilState, int stencilReference)
        {
            Init(opaque, evt, renderQueueRange, layerMask, stencilState, stencilReference);

            profilingSampler = ProfilingSampler.Get(profileId);
        }

        internal void Init(bool opaque, RenderPassEvent evt, RenderQueueRange renderQueueRange, LayerMask layerMask, StencilState stencilState, int stencilReference, ShaderTagId[] shaderTagIds = null)
        {
            if (shaderTagIds == null)
                shaderTagIds = new ShaderTagId[] { new ShaderTagId("SRPDefaultUnlit"), new ShaderTagId("UniversalForward"), new ShaderTagId("UniversalForwardOnly") };

            foreach (ShaderTagId sid in shaderTagIds)
                m_ShaderTagIdList.Add(sid);
            renderPassEvent = evt;
            m_FilteringSettings = new FilteringSettings(renderQueueRange, layerMask);
            m_RenderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);
            m_IsOpaque = opaque;
            m_ShouldTransparentsReceiveShadows = false;

            if (stencilState.enabled)
            {
                m_RenderStateBlock.stencilReference = stencilReference;
                m_RenderStateBlock.mask = RenderStateMask.Stencil;
                m_RenderStateBlock.stencilState = stencilState;
            }
        }

        internal static void ExecutePass(RasterCommandBuffer cmd, PassData data, RendererList rendererList, RendererList objectsWithErrorRendererList, bool yFlip)
        {
            // Global render pass data containing various settings.
            // x,y,z are currently unused
            // w is used for knowing whether the object is opaque(1) or alpha blended(0)
            Vector4 drawObjectPassData = new Vector4(0.0f, 0.0f, 0.0f, (data.isOpaque) ? 1.0f : 0.0f);
            cmd.SetGlobalVector(s_DrawObjectPassDataPropID, drawObjectPassData);

            if (data.cameraData.xr.enabled && data.isActiveTargetBackBuffer)
            {
                cmd.SetViewport(data.cameraData.xr.GetViewport());
            }

            bool useScreenSpaceIrradiance = data.screenSpaceIrradianceHdl.IsValid();
            cmd.SetKeyword(ShaderGlobalKeywords.ScreenSpaceIrradiance, useScreenSpaceIrradiance);
            if (useScreenSpaceIrradiance)
            {
                cmd.SetGlobalTexture(ShaderPropertyId.screenSpaceIrradiance, data.screenSpaceIrradianceHdl);
            }

            // scaleBias.x = flipSign
            // scaleBias.y = scale
            // scaleBias.z = bias
            // scaleBias.w = unused
            float flipSign = yFlip ? -1.0f : 1.0f;
            Vector4 scaleBias = (flipSign < 0.0f)
                ? new Vector4(flipSign, 1.0f, -1.0f, 1.0f)
                : new Vector4(flipSign, 0.0f, 1.0f, 1.0f);
            cmd.SetGlobalVector(ShaderPropertyId.scaleBiasRt, scaleBias);

            // Set a value that can be used by shaders to identify when AlphaToMask functionality may be active
            // The material shader alpha clipping logic requires this value in order to function correctly in all cases.
            float alphaToMaskAvailable = ((data.cameraData.cameraTargetDescriptor.msaaSamples > 1) && data.isOpaque) ? 1.0f : 0.0f;
            cmd.SetGlobalFloat(ShaderPropertyId.alphaToMaskAvailable, alphaToMaskAvailable);

            var activeDebugHandler = GetActiveDebugHandler(data.cameraData);
            if (activeDebugHandler != null)
            {
                data.debugRendererLists.DrawWithRendererList(cmd);
            }
            else
            {
                cmd.DrawRendererList(rendererList);
                // Render objects that did not match any shader pass with error shader
                RenderingUtils.DrawRendererListObjectsWithError(cmd, ref objectsWithErrorRendererList);
            }
        }

        /// <summary>
        /// Shared pass data
        /// </summary>
        internal class PassData
        {
            internal TextureHandle albedoHdl;
            internal TextureHandle depthHdl;
            internal TextureHandle screenSpaceIrradianceHdl;

            internal UniversalCameraData cameraData;
            internal bool isOpaque;
            internal bool shouldTransparentsReceiveShadows;
            internal uint batchLayerMask;
            internal bool isActiveTargetBackBuffer;
            internal RendererListHandle rendererListHdl;
            internal RendererListHandle objectsWithErrorRendererListHdl;
            internal DebugRendererLists debugRendererLists;

            // Required for code sharing purpose between RG and non-RG.
            internal RendererList rendererList;
            internal RendererList objectsWithErrorRendererList;
        }

        /// <summary>
        /// Initialize the shared pass data.
        /// </summary>
        /// <param name="passData"></param>
        internal void InitPassData(UniversalCameraData cameraData, ref PassData passData, uint batchLayerMask, bool isActiveTargetBackBuffer = false)
        {
            passData.cameraData = cameraData;
            passData.isOpaque = m_IsOpaque;
            passData.shouldTransparentsReceiveShadows = m_ShouldTransparentsReceiveShadows;
            passData.batchLayerMask = batchLayerMask;
            passData.isActiveTargetBackBuffer = isActiveTargetBackBuffer;
        }

        internal void InitRendererLists(UniversalRenderingData renderingData, UniversalCameraData cameraData, UniversalLightData lightData, ref PassData passData, RenderGraph renderGraph, bool zWriteOff)
        {
            ref Camera camera = ref cameraData.camera;
            var sortFlags = (m_IsOpaque) ? cameraData.defaultOpaqueSortFlags : SortingCriteria.CommonTransparent;
            if (cameraData.renderer.useDepthPriming && m_IsOpaque && (cameraData.renderType == CameraRenderType.Base || cameraData.clearDepth))
                sortFlags = SortingCriteria.SortingLayer | SortingCriteria.RenderQueue | SortingCriteria.OptimizeStateChanges | SortingCriteria.CanvasOrder;

            var filterSettings = m_FilteringSettings;
            filterSettings.batchLayerMask = passData.batchLayerMask;
#if UNITY_EDITOR
            // When rendering the preview camera, we want the layer mask to be forced to Everything
            if (cameraData.isPreviewCamera)
            {
                filterSettings.layerMask = -1;
            }
#endif
            DrawingSettings drawSettings = RenderingUtils.CreateDrawingSettings(m_ShaderTagIdList, renderingData, cameraData, lightData, sortFlags);

            if (zWriteOff)
            {
                m_RenderStateBlock.depthState = new DepthState(false, CompareFunction.Equal);
                m_RenderStateBlock.mask |= RenderStateMask.Depth;
            }
            else 
            {
                m_RenderStateBlock.depthState = DepthState.defaultValue;
                m_RenderStateBlock.mask &= ~RenderStateMask.Depth;
            }

            var activeDebugHandler = GetActiveDebugHandler(cameraData);
            if (activeDebugHandler != null)
            {
                passData.debugRendererLists = activeDebugHandler.CreateRendererListsWithDebugRenderState(renderGraph, ref renderingData.cullResults, ref drawSettings, ref filterSettings, ref m_RenderStateBlock);
            }
            else
            {
                RenderingUtils.CreateRendererListWithRenderStateBlock(renderGraph, ref renderingData.cullResults, drawSettings, filterSettings, m_RenderStateBlock, ref passData.rendererListHdl);
                RenderingUtils.CreateRendererListObjectsWithError(renderGraph, ref renderingData.cullResults, camera, filterSettings, sortFlags, ref passData.objectsWithErrorRendererListHdl);
            }
        }

        internal static bool CanDisableZWrite(UniversalCameraData cameraData, bool isOpaque)
        {
            return cameraData.renderer.useDepthPriming && isOpaque && (cameraData.renderType == CameraRenderType.Base || cameraData.clearDepth);
        }

        internal void Render(RenderGraph renderGraph, ContextContainer frameData, in TextureHandle colorTarget, in TextureHandle depthTarget, in TextureHandle mainShadowsTexture, in TextureHandle additionalShadowsTexture, uint batchLayerMask = uint.MaxValue, bool isMainOpaquePass = false)
        {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();

            bool disableZWrite = CanDisableZWrite(cameraData, m_IsOpaque);

            using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData, profilingSampler))
            {
                builder.UseAllGlobalTextures(true);

                InitPassData(cameraData, ref passData, batchLayerMask, resourceData.isActiveTargetBackBuffer);

                if (colorTarget.IsValid())
                {
                    passData.albedoHdl = colorTarget;
                    builder.SetRenderAttachment(colorTarget, 0, AccessFlags.Write);
                }

                if (depthTarget.IsValid())
                {
                    var depthAccessFlags = (disableZWrite) ? AccessFlags.Read : AccessFlags.ReadWrite;
                    passData.depthHdl = depthTarget;
                    builder.SetRenderAttachmentDepth(depthTarget, depthAccessFlags);
                }

                if (mainShadowsTexture.IsValid())
                    builder.UseTexture(mainShadowsTexture, AccessFlags.Read);
                if (additionalShadowsTexture.IsValid())
                    builder.UseTexture(additionalShadowsTexture, AccessFlags.Read);

                TextureHandle ssaoTexture = resourceData.ssaoTexture;
                if (ssaoTexture.IsValid())
                    builder.UseTexture(ssaoTexture, AccessFlags.Read);

                TextureHandle irradianceTexture = resourceData.irradianceTexture;
                if (irradianceTexture.IsValid())
                {
                    passData.screenSpaceIrradianceHdl = irradianceTexture;
                    builder.UseTexture(irradianceTexture, AccessFlags.Read);
                }

                RenderGraphUtils.UseDBufferIfValid(builder, resourceData);

                InitRendererLists(renderingData, cameraData, lightData, ref passData, renderGraph, disableZWrite);

                var activeDebugHandler = GetActiveDebugHandler(cameraData);
                if (activeDebugHandler != null)
                {
                    passData.debugRendererLists.PrepareRendererListForRasterPass(builder);
                }
                else
                {
                    builder.UseRendererList(passData.rendererListHdl);
                    builder.UseRendererList(passData.objectsWithErrorRendererListHdl);
                }

                builder.AllowGlobalStateModification(true);
                if (cameraData.xr.enabled)
                {
                    bool passSupportsFoveation = cameraData.xrUniversal.canFoveateIntermediatePasses || resourceData.isActiveTargetBackBuffer;
                    builder.EnableFoveatedRasterization(cameraData.xr.supportsFoveatedRendering && passSupportsFoveation);
                    // Apply MultiviewRenderRegionsCompatible flag only to the peripheral view in Quad Views
                    if (cameraData.xr.multipassId == 0)
                    {
                        builder.SetExtendedFeatureFlags(ExtendedFeatureFlags.MultiviewRenderRegionsCompatible);
                    }
#if ENABLE_VR && ENABLE_XR_MODULE && PLATFORM_ANDROID
                    if (isMainOpaquePass)
                    {
                        builder.SetExtendedFeatureFlags(ExtendedFeatureFlags.TileProperties);
                    }
#endif
                }

                builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
                {
                    // Currently we only need to call this additional pass when the user
                    // doesn't want transparent objects to receive shadows
                    if (!data.isOpaque && !data.shouldTransparentsReceiveShadows)
                        TransparentSettingsPass.ExecutePass(context.cmd);

                    bool yFlip = RenderingUtils.IsHandleYFlipped(context, in (data.albedoHdl.IsValid() ? ref data.albedoHdl : ref data.depthHdl));

                    bool useScreenSpaceIrradiance = data.screenSpaceIrradianceHdl.IsValid();
                    context.cmd.SetKeyword(ShaderGlobalKeywords.ScreenSpaceIrradiance, useScreenSpaceIrradiance);
                    if (useScreenSpaceIrradiance)
                    {
                        context.cmd.SetGlobalTexture(ShaderPropertyId.screenSpaceIrradiance, data.screenSpaceIrradianceHdl);
                    }

                    ExecutePass(context.cmd, data, data.rendererListHdl, data.objectsWithErrorRendererListHdl, yFlip);
                });
            }
        }
    }

    /// <summary>
    /// Extension of DrawObjectPass that also output Rendering Layers Texture as second render target.
    /// </summary>
    internal class DrawObjectsWithRenderingLayersPass : DrawObjectsPass
    {
        /// <summary>
        /// Creates a new <c>DrawObjectsWithRenderingLayersPass</c> instance.
        /// </summary>
        /// <param name="profilerTag">The profiler tag used with the pass.</param>
        /// <param name="opaque">Marks whether the objects are opaque or transparent.</param>
        /// <param name="evt">The <c>RenderPassEvent</c> to use.</param>
        /// <param name="renderQueueRange">The <c>RenderQueueRange</c> to use for creating filtering settings that control what objects get rendered.</param>
        /// <param name="layerMask">The layer mask to use for creating filtering settings that control what objects get rendered.</param>
        /// <param name="stencilState">The stencil settings to use with this poss.</param>
        /// <param name="stencilReference">The stencil reference value to use with this pass.</param>
        public DrawObjectsWithRenderingLayersPass(URPProfileId profilerTag, bool opaque, RenderPassEvent evt, RenderQueueRange renderQueueRange, LayerMask layerMask, StencilState stencilState,
            int stencilReference) :
            base(profilerTag, opaque, evt, renderQueueRange, layerMask, stencilState, stencilReference)
        {
        }

        private class RenderingLayersPassData
        {
            internal PassData basePassData;
            internal RenderingLayerUtils.MaskSize maskSize;

            public RenderingLayersPassData()
            {
                basePassData = new PassData();
            }
        }

        internal void Render(RenderGraph renderGraph, ContextContainer frameData, in TextureHandle colorTarget, in TextureHandle renderingLayersTexture, in TextureHandle depthTarget,
            in TextureHandle mainShadowsTexture, in TextureHandle additionalShadowsTexture, RenderingLayerUtils.MaskSize maskSize, uint batchLayerMask = uint.MaxValue)
        {
            using (var builder = renderGraph.AddRasterRenderPass<RenderingLayersPassData>(passName, out var passData, profilingSampler))
            {
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
                UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
                UniversalLightData lightData = frameData.Get<UniversalLightData>();                

                InitPassData(cameraData, ref passData.basePassData, batchLayerMask);

                passData.maskSize = maskSize;

                passData.basePassData.albedoHdl = colorTarget;
                builder.SetRenderAttachment(colorTarget, 0, AccessFlags.Write);
                builder.SetRenderAttachment(renderingLayersTexture, 1, AccessFlags.Write);

                bool disableZWrite = CanDisableZWrite(cameraData, passData.basePassData.isOpaque);
                var depthAccessFlags = (disableZWrite) ? AccessFlags.Read : AccessFlags.ReadWrite;
                passData.basePassData.depthHdl = depthTarget;
                builder.SetRenderAttachmentDepth(depthTarget, depthAccessFlags);

                if (mainShadowsTexture.IsValid())
                    builder.UseTexture(mainShadowsTexture, AccessFlags.Read);
                if (additionalShadowsTexture.IsValid())
                    builder.UseTexture(additionalShadowsTexture, AccessFlags.Read);

                UniversalRenderer renderer = cameraData.renderer as UniversalRenderer;
                if (renderer != null)
                {
                    TextureHandle ssaoTexture = resourceData.ssaoTexture;
                    if (ssaoTexture.IsValid())
                        builder.UseTexture(ssaoTexture, AccessFlags.Read);

                    RenderGraphUtils.UseDBufferIfValid(builder, resourceData);
                }

                InitRendererLists(renderingData, cameraData, lightData, ref passData.basePassData, renderGraph, disableZWrite);

                var activeDebugHandler = GetActiveDebugHandler(cameraData);
                if (activeDebugHandler != null)
                {
                    passData.basePassData.debugRendererLists.PrepareRendererListForRasterPass(builder);
                }
                else
                {
                    builder.UseRendererList(passData.basePassData.rendererListHdl);
                    builder.UseRendererList(passData.basePassData.objectsWithErrorRendererListHdl);
                }

                // Required here because of RenderingLayerUtils.SetupProperties
                builder.AllowGlobalStateModification(true);

                if (cameraData.xr.enabled)
                {
                    bool passSupportsFoveation = cameraData.xrUniversal.canFoveateIntermediatePasses || resourceData.isActiveTargetBackBuffer;
                    builder.EnableFoveatedRasterization(cameraData.xr.supportsFoveatedRendering && passSupportsFoveation);
                    // Apply MultiviewRenderRegionsCompatible flag only to the peripheral view in Quad Views
                    if (cameraData.xr.multipassId == 0)
                    {
                        builder.SetExtendedFeatureFlags(ExtendedFeatureFlags.MultiviewRenderRegionsCompatible);
                    }
                }

                builder.SetRenderFunc(static (RenderingLayersPassData data, RasterGraphContext context) =>
                {
                    // Enable Rendering Layers
                    context.cmd.SetKeyword(ShaderGlobalKeywords.WriteRenderingLayers, true);

                    RenderingLayerUtils.SetupProperties(context.cmd, data.maskSize);

                    // Currently we only need to call this additional pass when the user
                    // doesn't want transparent objects to receive shadows
                    if (!data.basePassData.isOpaque && !data.basePassData.shouldTransparentsReceiveShadows)
                        TransparentSettingsPass.ExecutePass(context.cmd);

                    bool yFlip = RenderingUtils.IsHandleYFlipped(context, in (data.basePassData.albedoHdl.IsValid() ? ref data.basePassData.albedoHdl : ref data.basePassData.depthHdl));

                    // Execute
                    ExecutePass(context.cmd, data.basePassData, data.basePassData.rendererListHdl, data.basePassData.objectsWithErrorRendererListHdl, yFlip);

                    // Clean up
                    context.cmd.SetKeyword(ShaderGlobalKeywords.WriteRenderingLayers, false);
                });
            }
        }
    }
}