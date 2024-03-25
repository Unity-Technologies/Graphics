using System;
using Unity.Collections;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal
{
    sealed class MotionVectorRenderPass : ScriptableRenderPass
    {
        #region Fields
        internal const string k_MotionVectorTextureName = "_MotionVectorTexture";
        internal const string k_MotionVectorDepthTextureName = "_MotionVectorDepthTexture";

        internal const GraphicsFormat k_TargetFormat = GraphicsFormat.R16G16_SFloat;

        public const string k_MotionVectorsLightModeTag = "MotionVectors";
        static readonly string[] s_ShaderTags = new string[] { k_MotionVectorsLightModeTag };

        RTHandle m_Color;
        RTHandle m_Depth;
        readonly Material m_CameraMaterial;

        private PassData m_PassData;
        #endregion

        #region Constructors
        internal MotionVectorRenderPass(RenderPassEvent evt, Material cameraMaterial)
        {
            renderPassEvent = evt;
            m_CameraMaterial = cameraMaterial;
            m_PassData = new PassData();
            base.profilingSampler = ProfilingSampler.Get(URPProfileId.MotionVectors);

            ConfigureInput(ScriptableRenderPassInput.Depth);
        }

        #endregion

        #region State
        internal void Setup(RTHandle color, RTHandle depth)
        {
            m_Color = color;
            m_Depth = depth;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            cmd.SetGlobalTexture(m_Color.name, m_Color.nameID);
            cmd.SetGlobalTexture(m_Depth.name, m_Depth.nameID);
            ConfigureTarget(m_Color, m_Depth);
            ConfigureClear(ClearFlag.Color | ClearFlag.Depth, Color.black);

            // Can become a Store based on 'StoreActionsOptimization.Auto' and/or if a user RendererFeature is added.
            // We need to keep the MotionVecDepth in case of a user wants to extend the motion vectors
            // using a custom RendererFeature.
            ConfigureDepthStoreAction(RenderBufferStoreAction.DontCare);
        }

        #endregion

        #region Execution
        private static void ExecutePass(RasterCommandBuffer cmd, PassData passData, RendererList rendererList, ref RenderingData renderingData)
        {
            var cameraMaterial = passData.cameraMaterial;

            if (cameraMaterial == null)
                return;

            // Get data
            ref var cameraData = ref renderingData.cameraData;
            Camera camera = cameraData.camera;

            // Never draw in Preview
            if (camera.cameraType == CameraType.Preview)
                return;

            // Profiling command
            using (new ProfilingScope(cmd, ProfilingSampler.Get(URPProfileId.MotionVectors)))
            {
                // These flags are still required in SRP or the engine won't compute previous model matrices...
                // If the flag hasn't been set yet on this camera, motion vectors will skip a frame.
                camera.depthTextureMode |= DepthTextureMode.MotionVectors | DepthTextureMode.Depth;

                // TODO: add option to only draw either one?
                DrawCameraMotionVectors(cmd, ref renderingData, cameraMaterial);
                DrawObjectMotionVectors(cmd, ref renderingData, ref rendererList);
            }
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            InitPassData(ref renderingData, ref m_PassData);

            InitRendererLists(ref renderingData, ref m_PassData, context, default(RenderGraph), false);
            ExecutePass(CommandBufferHelpers.GetRasterCommandBuffer(renderingData.commandBuffer), m_PassData, m_PassData.rendererList, ref renderingData);
        }

        private static DrawingSettings GetDrawingSettings(ref RenderingData renderingData)
        {
            var camera = renderingData.cameraData.camera;
            var sortingSettings = new SortingSettings(camera) { criteria = SortingCriteria.CommonOpaque };
            var drawingSettings = new DrawingSettings(ShaderTagId.none, sortingSettings)
            {
                perObjectData = PerObjectData.MotionVectors,
                enableDynamicBatching = renderingData.supportsDynamicBatching,
                enableInstancing = true,
            };

            for (int i = 0; i < s_ShaderTags.Length; ++i)
            {
                drawingSettings.SetShaderPassName(i, new ShaderTagId(s_ShaderTags[i]));
            }

            return drawingSettings;
        }

        // NOTE: depends on camera depth to reconstruct static geometry positions
        private static void DrawCameraMotionVectors(RasterCommandBuffer cmd, ref RenderingData renderingData, Material cameraMaterial)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            bool foveatedRendering = renderingData.cameraData.xr.supportsFoveatedRendering;
            bool nonUniformFoveatedRendering = foveatedRendering && XRSystem.foveatedRenderingCaps.HasFlag(FoveatedRenderingCaps.NonUniformRaster);
            if (foveatedRendering)
            {
                if (nonUniformFoveatedRendering)
                    // This is a screen-space pass, make sure foveated rendering is disabled for non-uniform renders
                    cmd.SetFoveatedRenderingMode(FoveatedRenderingMode.Disabled);
                else
                    cmd.SetFoveatedRenderingMode(FoveatedRenderingMode.Enabled);
            }
#endif
            // Draw fullscreen quad
            cmd.DrawProcedural(Matrix4x4.identity, cameraMaterial, 0, MeshTopology.Triangles, 3, 1);

#if ENABLE_VR && ENABLE_XR_MODULE
            if (foveatedRendering && !nonUniformFoveatedRendering)
                cmd.SetFoveatedRenderingMode(FoveatedRenderingMode.Disabled);
#endif
        }

        private static void DrawObjectMotionVectors(RasterCommandBuffer cmd, ref RenderingData renderingData, ref RendererList rendererList)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            bool foveatedRendering = renderingData.cameraData.xr.supportsFoveatedRendering;
            if (foveatedRendering)
                // This is a geometry pass, enable foveated rendering (we need to disable it after)
                cmd.SetFoveatedRenderingMode(FoveatedRenderingMode.Enabled);
#endif
            cmd.DrawRendererList(rendererList);

#if ENABLE_VR && ENABLE_XR_MODULE
            if (foveatedRendering)
                cmd.SetFoveatedRenderingMode(FoveatedRenderingMode.Disabled);
#endif
        }
        #endregion

        /// <summary>
        /// Shared pass data
        /// </summary>
        private class PassData
        {
            internal TextureHandle motionVectorColor;
            internal TextureHandle motionVectorDepth;
            internal TextureHandle cameraDepth;
            internal RenderingData renderingData;
            internal Material cameraMaterial;
            internal RendererListHandle rendererListHdl;

            // Required for code sharing purpose between RG and non-RG.
            internal RendererList rendererList;
        }
        /// <summary>
        /// Initialize the shared pass data.
        /// </summary>
        /// <param name="passData"></param>
        private void InitPassData(ref RenderingData renderingData, ref PassData passData)
        {
            passData.cameraMaterial = m_CameraMaterial;
        }

        private void InitRendererLists(ref RenderingData renderingData, ref PassData passData, ScriptableRenderContext context, RenderGraph renderGraph, bool useRenderGraph)
        {
            var camera = renderingData.cameraData.camera;
            var drawingSettings = GetDrawingSettings(ref renderingData);
            var filteringSettings = new FilteringSettings(RenderQueueRange.opaque, camera.cullingMask);
            var renderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);
            if(useRenderGraph)
                RenderingUtils.CreateRendererListWithRenderStateBlock(renderGraph, renderingData, drawingSettings, filteringSettings, renderStateBlock, ref passData.rendererListHdl);
            else
                RenderingUtils.CreateRendererListWithRenderStateBlock(context, renderingData, drawingSettings, filteringSettings, renderStateBlock, ref passData.rendererList);
        }

        internal void Render(RenderGraph renderGraph, TextureHandle cameraDepthTexture, TextureHandle motionVectorColor, TextureHandle motionVectorDepth, ref RenderingData renderingData)
        {
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Motion Vector Pass", out var passData, base.profilingSampler))
            {
                //  TODO RENDERGRAPH: culling? force culling off for testing
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);

                passData.motionVectorColor = builder.UseTextureFragment(motionVectorColor, 0, IBaseRenderGraphBuilder.AccessFlags.Write);
                passData.motionVectorDepth = builder.UseTextureFragmentDepth(motionVectorDepth, IBaseRenderGraphBuilder.AccessFlags.Write);
                InitPassData(ref renderingData, ref passData);
                passData.cameraDepth = builder.UseTexture(cameraDepthTexture, IBaseRenderGraphBuilder.AccessFlags.Read);
                passData.renderingData = renderingData;

                InitRendererLists(ref renderingData, ref passData, default(ScriptableRenderContext), renderGraph, true);
                builder.UseRendererList(passData.rendererListHdl);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    ExecutePass(context.cmd, data, data.rendererListHdl, ref data.renderingData);
                });
            }

            RenderGraphUtils.SetGlobalTexture(renderGraph,k_MotionVectorTextureName, motionVectorColor, "Set Motion Vector Color Texture");
            RenderGraphUtils.SetGlobalTexture(renderGraph,k_MotionVectorDepthTextureName, motionVectorDepth, "Set Motion Vector Depth Texture");
        }
    }
}
