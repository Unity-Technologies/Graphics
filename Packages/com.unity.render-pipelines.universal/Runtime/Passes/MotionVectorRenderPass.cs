using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal
{
    sealed class MotionVectorRenderPass : ScriptableRenderPass
    {
        internal const string k_MotionVectorTextureName = "_MotionVectorTexture";
        internal const string k_MotionVectorDepthTextureName = "_MotionVectorDepthTexture";

        internal const GraphicsFormat k_TargetFormat = GraphicsFormat.R16G16_SFloat;

        public const string k_MotionVectorsLightModeTag = "MotionVectors";
        static readonly string[] s_ShaderTags = new string[] { k_MotionVectorsLightModeTag };

        static readonly int s_CameraDepthTextureID = Shader.PropertyToID("_CameraDepthTexture");
        static readonly ProfilingSampler s_SetMotionMatrixProfilingSampler = new ProfilingSampler("Set Motion Vector Global Matrices");

        readonly Material m_CameraMaterial;
        readonly FilteringSettings m_FilteringSettings;

        internal MotionVectorRenderPass(RenderPassEvent evt, Material cameraMaterial, LayerMask opaqueLayerMask)

        {
            profilingSampler = ProfilingSampler.Get(URPProfileId.DrawMotionVectors);
            renderPassEvent = evt;
            m_CameraMaterial = cameraMaterial;
            m_FilteringSettings = new FilteringSettings(RenderQueueRange.opaque,opaqueLayerMask);

            ConfigureInput(ScriptableRenderPassInput.Depth);
        }
        
        private static void ExecutePass(RasterCommandBuffer cmd, PassData passData, RendererList rendererList)
        {
            var cameraMaterial = passData.cameraMaterial;

            if (cameraMaterial == null)
                return;

            // Get data
            Camera camera = passData.camera;

            // Never draw in Preview
            if (camera.cameraType == CameraType.Preview)
                return;

            // These flags are still required in SRP or the engine won't compute previous model matrices...
            // If the flag hasn't been set yet on this camera, motion vectors will skip a frame.
            camera.depthTextureMode |= DepthTextureMode.MotionVectors | DepthTextureMode.Depth;

            // TODO: add option to only draw either one?
            DrawCameraMotionVectors(cmd, passData.xr, cameraMaterial);
            DrawObjectMotionVectors(cmd, passData.xr, ref rendererList);
        }

        private static DrawingSettings GetDrawingSettings(Camera camera, bool supportsDynamicBatching)
        {
            var sortingSettings = new SortingSettings(camera) { criteria = SortingCriteria.CommonOpaque };
            var drawingSettings = new DrawingSettings(ShaderTagId.none, sortingSettings)
            {
                perObjectData = PerObjectData.MotionVectors,
                enableDynamicBatching = supportsDynamicBatching,
                enableInstancing = true,
                lodCrossFadeStencilMask = 0, // Disable stencil-based lod because depth copy before motion vector pass doesn't copy stencils.
            };

            for (int i = 0; i < s_ShaderTags.Length; ++i)
            {
                drawingSettings.SetShaderPassName(i, new ShaderTagId(s_ShaderTags[i]));
            }

            return drawingSettings;
        }

        // NOTE: depends on camera depth to reconstruct static geometry positions
        private static void DrawCameraMotionVectors(RasterCommandBuffer cmd, XRPass xr, Material cameraMaterial)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            bool foveatedRendering = xr.supportsFoveatedRendering;
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

        private static void DrawObjectMotionVectors(RasterCommandBuffer cmd, XRPass xr, ref RendererList rendererList)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            bool foveatedRendering = xr.supportsFoveatedRendering;
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

        /// <summary>
        /// Shared pass data
        /// </summary>
        private class PassData
        {
            internal Camera camera;
            internal XRPass xr;

            internal TextureHandle cameraDepth;
            internal Material cameraMaterial;
            internal RendererListHandle rendererListHdl;

            // Required for code sharing purpose between RG and non-RG.
            internal RendererList rendererList;
        }
        /// <summary>
        /// Initialize the shared pass data.
        /// </summary>
        /// <param name="passData"></param>
        private void InitPassData(ref PassData passData, UniversalCameraData cameraData)
        {
            passData.camera = cameraData.camera;
            passData.xr = cameraData.xr;

            passData.cameraMaterial = m_CameraMaterial;
        }

        private void InitRendererLists(ref PassData passData, ref CullingResults cullResults, bool supportsDynamicBatching, RenderGraph renderGraph)
        {
            var drawingSettings = GetDrawingSettings(passData.camera, supportsDynamicBatching);
            var renderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);
            RenderingUtils.CreateRendererListWithRenderStateBlock(renderGraph, ref cullResults, drawingSettings, m_FilteringSettings, renderStateBlock, ref passData.rendererListHdl);
        }

        internal void Render(RenderGraph renderGraph, ContextContainer frameData, TextureHandle cameraDepthTexture, TextureHandle motionVectorColor, TextureHandle motionVectorDepth)
        {
            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            using (var builder = renderGraph.AddRasterRenderPass<PassData>(passName, out var passData, profilingSampler))
            {
                builder.UseAllGlobalTextures(true);

                builder.AllowGlobalStateModification(true);
                if (cameraData.xr.enabled)
                {
                    builder.EnableFoveatedRasterization(cameraData.xr.supportsFoveatedRendering && cameraData.xrUniversal.canFoveateIntermediatePasses);
                    builder.SetExtendedFeatureFlags(ExtendedFeatureFlags.MultiviewRenderRegionsCompatible);
                }

                builder.SetRenderAttachment(motionVectorColor, 0, AccessFlags.Write);
                builder.SetRenderAttachmentDepth(motionVectorDepth, AccessFlags.Write);
                InitPassData(ref passData, cameraData);
                passData.cameraDepth = cameraDepthTexture;
                builder.UseTexture(cameraDepthTexture, AccessFlags.Read);

                InitRendererLists(ref passData, ref renderingData.cullResults, renderingData.supportsDynamicBatching, renderGraph);
                builder.UseRendererList(passData.rendererListHdl);

                if (motionVectorColor.IsValid())
                    builder.SetGlobalTextureAfterPass(motionVectorColor, Shader.PropertyToID(k_MotionVectorTextureName));
                if (motionVectorDepth.IsValid())
                    builder.SetGlobalTextureAfterPass(motionVectorDepth, Shader.PropertyToID(k_MotionVectorDepthTextureName));

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    if (data.cameraMaterial != null)
                        data.cameraMaterial.SetTexture(s_CameraDepthTextureID, data.cameraDepth);

                    ExecutePass(context.cmd, data, data.rendererListHdl);
                });
            }
        }

        // Global motion vector matrix setup pass.
        // Used for MotionVector passes and also read in VFX early compute shader
        public class MotionMatrixPassData
        {
            public MotionVectorsPersistentData motionData;
            public XRPass xr;
        }

        internal static void SetRenderGraphMotionVectorGlobalMatrices(RenderGraph renderGraph, UniversalCameraData cameraData)
        {
            if (cameraData.camera.TryGetComponent<UniversalAdditionalCameraData>(out var additionalCameraData))
            {
                using (var builder = renderGraph.AddRasterRenderPass<MotionMatrixPassData>(s_SetMotionMatrixProfilingSampler.name, out var passData, s_SetMotionMatrixProfilingSampler))
                {
                    passData.motionData = additionalCameraData.motionVectorsPersistentData;
                    passData.xr = cameraData.xr;

                    builder.AllowGlobalStateModification(true);
                    builder.SetRenderFunc(static (MotionMatrixPassData data, RasterGraphContext context) =>
                    {
                        data.motionData.SetGlobalMotionMatrices(context.cmd, data.xr);
                    });
                }
            }
        }
    }
}
