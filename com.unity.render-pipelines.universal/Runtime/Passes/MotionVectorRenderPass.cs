using System;
using Unity.Collections;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal
{
    sealed class MotionVectorRenderPass : ScriptableRenderPass
    {
        #region Fields
        const string k_PreviousViewProjectionNoJitter = "_PrevViewProjMatrix";
        const string k_ViewProjectionNoJitter = "_NonJitteredViewProjMatrix";
#if ENABLE_VR && ENABLE_XR_MODULE
        const string k_PreviousViewProjectionNoJitterStereo = "_PrevViewProjMatrixStereo";
        const string k_ViewProjectionNoJitterStereo = "_NonJitteredViewProjMatrixStereo";
#endif

        internal const string k_MotionVectorTextureName = "_MotionVectorTexture";
        internal const string k_MotionVectorDepthTextureName = "_MotionVectorDepthTexture";

        internal const GraphicsFormat k_TargetFormat = GraphicsFormat.R16G16_SFloat;

        static readonly string[] s_ShaderTags = new string[] { "MotionVectors" };

        RTHandle m_Color;
        RTHandle m_Depth;
        readonly Material m_CameraMaterial;
        readonly Material m_ObjectMaterial;

        private PassData m_PassData;
        #endregion

        #region Constructors
        internal MotionVectorRenderPass(Material cameraMaterial, Material objectMaterial)
        {
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
            m_CameraMaterial = cameraMaterial;
            m_ObjectMaterial = objectMaterial;
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
        private static void ExecutePass(ScriptableRenderContext context, PassData passData, ref RenderingData renderingData)
        {
            var cameraMaterial = passData.cameraMaterial;
            var objectMaterial = passData.objectMaterial;

            if (cameraMaterial == null || objectMaterial == null)
                return;

            // Get data
            ref var cameraData = ref renderingData.cameraData;
            Camera camera = cameraData.camera;
            MotionVectorsPersistentData motionData = null;

            if(camera.TryGetComponent<UniversalAdditionalCameraData>(out var additionalCameraData))
                motionData = additionalCameraData.motionVectorsPersistentData;

            if (motionData == null)
                return;

            // Never draw in Preview
            if (camera.cameraType == CameraType.Preview)
                return;

            // Profiling command
            var cmd = renderingData.commandBuffer;
            using (new ProfilingScope(cmd, ProfilingSampler.Get(URPProfileId.MotionVectors)))
            {
                int passID = motionData.GetXRMultiPassId(ref cameraData);

                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
#if ENABLE_VR && ENABLE_XR_MODULE
                if (cameraData.xr.enabled && cameraData.xr.singlePassEnabled)
                {
                    cmd.SetGlobalMatrixArray(k_PreviousViewProjectionNoJitterStereo, motionData.previousViewProjectionStereo);
                    cmd.SetGlobalMatrixArray(k_ViewProjectionNoJitterStereo, motionData.viewProjectionStereo);
                }
                else
#endif
                {
                    // TODO: These should be part of URP main matrix set. For now, we set them here for motion vector rendering.
                    cmd.SetGlobalMatrix(k_PreviousViewProjectionNoJitter, motionData.previousViewProjectionStereo[passID]);
                    cmd.SetGlobalMatrix(k_ViewProjectionNoJitter, motionData.viewProjectionStereo[passID]);
                }

                // These flags are still required in SRP or the engine won't compute previous model matrices...
                // If the flag hasn't been set yet on this camera, motion vectors will skip a frame.
                camera.depthTextureMode |= DepthTextureMode.MotionVectors | DepthTextureMode.Depth;

                // TODO: add option to only draw either one?
                DrawCameraMotionVectors(context, cmd, camera, cameraMaterial);
                DrawObjectMotionVectors(context, ref renderingData, camera, objectMaterial, cmd);
            }
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            m_PassData.cameraMaterial = m_CameraMaterial;
            m_PassData.objectMaterial = m_ObjectMaterial;

            ExecutePass(context, m_PassData, ref renderingData);
        }

        private static DrawingSettings GetDrawingSettings(ref RenderingData renderingData, Material objectMaterial)
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

            // Material that will be used if shader tags cannot be found
            drawingSettings.fallbackMaterial = objectMaterial;

            return drawingSettings;
        }

        // NOTE: depends on camera depth to reconstruct static geometry positions
        private static void DrawCameraMotionVectors(ScriptableRenderContext context, CommandBuffer cmd, Camera camera, Material cameraMaterial)
        {
            // Draw fullscreen quad
            cmd.DrawProcedural(Matrix4x4.identity, cameraMaterial, 0, MeshTopology.Triangles, 3, 1);
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
        }

        private static void DrawObjectMotionVectors(ScriptableRenderContext context, ref RenderingData renderingData, Camera camera, Material objectMaterial, CommandBuffer cmd)
        {
            var drawingSettings = GetDrawingSettings(ref renderingData, objectMaterial);
            var filteringSettings = new FilteringSettings(RenderQueueRange.opaque, camera.cullingMask);
            var renderStateBlock = new RenderStateBlock(RenderStateMask.Nothing);

            RenderingUtils.DrawRendererListWithRenderStateBlock(context, cmd, renderingData, drawingSettings, filteringSettings, renderStateBlock);
        }
        #endregion

        private class PassData
        {
            internal TextureHandle motionVectorColor;
            internal TextureHandle motionVectorDepth;
            internal TextureHandle cameraDepth;
            internal RenderingData renderingData;
            internal Material cameraMaterial;
            internal Material objectMaterial;
        }

        internal void Render(RenderGraph renderGraph, ref TextureHandle cameraDepthTexture, in TextureHandle motionVectorColor, in TextureHandle motionVectorDepth, ref RenderingData renderingData)
        {
            using (var builder = renderGraph.AddRenderPass<PassData>("Motion Vector Pass", out var passData, base.profilingSampler))
            {
                //  TODO RENDERGRAPH: culling? force culling off for testing
                builder.AllowPassCulling(false);

                passData.motionVectorColor = builder.UseColorBuffer(motionVectorColor, 0);
                passData.motionVectorDepth = builder.UseDepthBuffer(motionVectorDepth, DepthAccess.Write);
                passData.cameraDepth       = builder.ReadTexture(cameraDepthTexture);
                passData.renderingData = renderingData;
                passData.cameraMaterial = m_CameraMaterial;
                passData.objectMaterial = m_ObjectMaterial;

                builder.SetRenderFunc((PassData data, RenderGraphContext context) =>
                {
                    ExecutePass(context.renderContext, data, ref data.renderingData);
                });
            }

            RenderGraphUtils.SetGlobalTexture(renderGraph,k_MotionVectorTextureName, motionVectorColor, "Set Motion Vector Color Texture");
            RenderGraphUtils.SetGlobalTexture(renderGraph,k_MotionVectorDepthTextureName, motionVectorDepth, "Set Motion Vector Depth Texture");
        }
    }
}
