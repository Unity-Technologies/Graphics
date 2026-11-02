using System;
using UnityEngine.Rendering.RenderGraphModule;
using System.Runtime.CompilerServices; // AggressiveInlining

namespace UnityEngine.Rendering.Universal
{
    internal sealed class UpscalerPostProcessPass : PostProcessPass
    {
        public const string k_UpscaledColorTargetName = "CameraColorUpscaled";
        Texture2D[] m_BlueNoise16LTex;
        bool m_IsValid;

#if ENABLE_UPSCALER_FRAMEWORK
        bool m_WarnedHardwareDrsTemporalUnsupported;
        bool m_WarnedMissingMotionData;
#endif

        public UpscalerPostProcessPass(Texture2D[] blueNoise16LTex)
        {
            this.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing - 1;
            this.profilingSampler = null;   // Use default name
            m_BlueNoise16LTex = blueNoise16LTex;

            m_IsValid = m_BlueNoise16LTex != null && m_BlueNoise16LTex.Length > 0;
        }

        public override void Dispose()
        {
            m_IsValid = false;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
#if ENABLE_UPSCALER_FRAMEWORK
            if (!m_IsValid)
                return;

            UniversalPostProcessingData postProcessingData = frameData.Get<UniversalPostProcessingData>();
            if (postProcessingData.activeUpscaler == null)
                return;

            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            if (cameraData.imageScalingMode != ImageScalingMode.Upscaling)
                return;

            // Skip temporal upscaling when the camera uses hardware DRS (ScalableBufferManager). A temporal upscaler
            // reconstructs to full resolution mid-frame, but ScalableBufferManager is a single global scale with no per-stage
            // render->display transition, so the post-upscale chain (UberPost, final blit) keeps writing into the
            // ScalableBufferManager-scaled sub-rect and only that sub-rect of the screen updates.
            // Gate on camera.allowDynamicResolution (the stable opt-in), not the live ScalableBufferManager factor, 
            // which would flip per-frame as the app crosses factor 1.0. 
            if (cameraData.camera.allowDynamicResolution && postProcessingData.activeUpscaler.isTemporal)
            {
                if (Debug.isDebugBuild && !m_WarnedHardwareDrsTemporalUnsupported)
                {
                    m_WarnedHardwareDrsTemporalUnsupported = true;
                    Debug.LogWarning(
                        "Hardware Dynamic Resolution (Allow Dynamic Resolution / ScalableBufferManager) is not supported " +
                        $"with the temporal upscaler '{postProcessingData.activeUpscaler.name}' in URP yet (in any " +
                        "Resolution Mode); the upscaler is skipped and the camera falls back to hardware DRS without " +
                        "temporal upscaling. To use the temporal upscaler, disable Allow Dynamic Resolution on the camera" +
                        " and control the resolution via Render Scale or the upscaler's quality mode if available.");
                }
                return;
            }
            // Left the skip path: reset so re-entering it warns again.
            if (Debug.isDebugBuild)
                m_WarnedHardwareDrsTemporalUnsupported = false;

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            var sourceTexture = resourceData.cameraColor;
            var srcDesc = sourceTexture.GetDescriptor(renderGraph);

            // Create a context item containing upscaling inputs
            UpscalingIO io = frameData.Create<UpscalingIO>();
            io.cameraColor = sourceTexture;
            io.cameraDepth = resourceData.cameraDepth;
            io.motionVectorColor = resourceData.motionVectorColor;
            io.motionVectorDomain = UpscalingIO.MotionVectorDomain.NDC;
            io.motionVectorDirection = UpscalingIO.MotionVectorDirection.PreviousFrameToCurrentFrame;
            io.jitteredMotionVectors = false; // URP has no jittering in MVs
            // io.exposureTexture; // TODO: set exposure texture when available
            io.preExposureValue = 1.0f; // TODO: set if exposure value is pre-multiplied
            io.hdrDisplayInformation = cameraData.isHDROutputActive ? cameraData.hdrDisplayInformation : new HDROutputUtils.HDRDisplayInformation(-1, -1, -1, 160.0f);
            io.postUpscaleResolution = new Vector2Int(cameraData.pixelWidth, cameraData.pixelHeight);

            // Report DRS as active whenever the camera is aliased by hardware DRS / ScalableBufferManager (SBM)
            // (descriptor.useDynamicScale), not only when the
            // current factor is < 1.0: the upscaler bakes its dynamic-resolution state at context-creation time (e.g.
            // FSR2's EnableDynamicResolution flag) and isn't recreated when the factor changes, so a context created at
            // factor 1.0 would otherwise run with DRS off while later receiving a varying sub-rect. Use the SBM scale
            // captured on cameraData at setup, not the live global, which another pass could mutate before this point.
            Vector2 hwDrsScale = cameraData.hardwareDynamicResolutionScale;
            bool cameraUsesHardwareDrs = cameraData.cameraTargetDescriptor.useDynamicScale;
            io.dynamicResolution = cameraUsesHardwareDrs ? DynamicResolutionType.Hardware : (DynamicResolutionType?)null;
            // preUpscaleResolution is the actually-rendered region (the descriptor scaled by the SBM factor), not the
            // full descriptor allocation, so the upscaler reconstructs from what was rendered.
            io.preUpscaleResolution = cameraUsesHardwareDrs
                ? new Vector2Int(
                    Mathf.CeilToInt(hwDrsScale.x * cameraData.cameraTargetDescriptor.width),
                    Mathf.CeilToInt(hwDrsScale.y * cameraData.cameraTargetDescriptor.height))
                : new Vector2Int(cameraData.cameraTargetDescriptor.width, cameraData.cameraTargetDescriptor.height);

            // The max render size = the full camera-target allocation (the descriptor). Under SBM the rendered region
            // (preUpscaleResolution) is a sub-rect of it; without DRS they're equal. Upscalers allocate history at this.
            io.maxPreUpscaleResolution = new Vector2Int(cameraData.cameraTargetDescriptor.width, cameraData.cameraTargetDescriptor.height);

            cameraData.camera.TryGetComponent<UniversalAdditionalCameraData>(out var additionalCameraData);
            MotionVectorsPersistentData motionData = additionalCameraData != null ? additionalCameraData.motionVectorsPersistentData : null;
            if (motionData == null) // Per-camera motion data is required for upscaling (camera matrices, positions, previous-frame sizes).
            {
                if (Debug.isDebugBuild && !m_WarnedMissingMotionData)
                {
                    m_WarnedMissingMotionData = true;
                    Debug.LogWarning("UpscalerPostProcessPass: camera has no UniversalAdditionalCameraData/" +
                        "MotionVectorsPersistentData, which the temporal upscaler requires; skipping upscaling for this camera.");
                }
                return;
            }
            
            if (Debug.isDebugBuild) // Saw valid motion data: reset so a later missing-data camera warns again.
                m_WarnedMissingMotionData = false;

            // Track the previous frame's render resolution for temporal upscalers via the per-camera persistent state.
            io.previousPreUpscaleResolution = motionData.previousPreUpscaleResolution;
            motionData.previousPreUpscaleResolution = io.preUpscaleResolution;

            io.motionVectorTextureSize = io.preUpscaleResolution;
            io.enableTexArray = cameraData.xr.enabled && cameraData.xr.singlePassEnabled;

            io.cameraInstanceID = EntityId.ToULong(cameraData.camera.GetEntityId());
            io.nearClipPlane = cameraData.camera.nearClipPlane;
            io.farClipPlane = cameraData.camera.farClipPlane;
            io.fieldOfViewDegrees = cameraData.camera.fieldOfView;
            io.invertedDepth = SystemInfo.usesReversedZBuffer;
            io.flippedY = SystemInfo.graphicsUVStartsAtTop;
            io.flippedX = false;
            io.hdrInput = Experimental.Rendering.GraphicsFormatUtility.IsHDRFormat(srcDesc.format);
            io.numActiveViews = cameraData.xr.enabled ? cameraData.xr.viewCount : 1;

            io.projectionMatrices = motionData.projectionStereo;
            io.previousProjectionMatrices = motionData.previousProjectionStereo;
            io.previousPreviousProjectionMatrices = motionData.previousPreviousProjectionStereo;
            io.viewMatrices = motionData.viewStereo;
            io.previousViewMatrices = motionData.previousViewStereo;
            io.previousPreviousViewMatrices = motionData.previousPreviousViewStereo;

            // Per-view world-space camera positions into persistent per-camera buffers (resized only on view-count
            // change) to avoid a per-frame allocation. Mono reuses the stored camera position; each XR eye is at a
            // distinct position, derived from its view matrix via CoreMatrixUtils.GetWorldPositionFromOrthonormalViewMatrix
            // instead of a full inverse.
            var camPosViews = motionData.GetWorldSpaceCameraPosViews(io.numActiveViews);
            var prevCamPosViews = motionData.GetPreviousWorldSpaceCameraPosViews(io.numActiveViews);
            var prevPrevCamPosViews = motionData.GetPreviousPreviousWorldSpaceCameraPosViews(io.numActiveViews);
            if (io.numActiveViews == 1)
            {
                camPosViews[0] = motionData.worldSpaceCameraPos;
                prevCamPosViews[0] = motionData.previousWorldSpaceCameraPos;
                prevPrevCamPosViews[0] = motionData.previousPreviousWorldSpaceCameraPos;
            }
            else
            {
                for (int i = 0; i < io.numActiveViews; i++)
                {
                    camPosViews[i] = CoreMatrixUtils.GetWorldPositionFromOrthonormalViewMatrix(motionData.viewStereo[i]);
                    prevCamPosViews[i] = CoreMatrixUtils.GetWorldPositionFromOrthonormalViewMatrix(motionData.previousViewStereo[i]);
                    prevPrevCamPosViews[i] = CoreMatrixUtils.GetWorldPositionFromOrthonormalViewMatrix(motionData.previousPreviousViewStereo[i]);
                }
            }
            io.worldSpaceCameraPositions = camPosViews;
            io.previousWorldSpaceCameraPositions = prevCamPosViews;
            io.previousPreviousWorldSpaceCameraPositions = prevPrevCamPosViews;
            io.resetHistory = cameraData.resetHistory;
            io.frameIndex = TemporalAA.CalculateTaaFrameIndex(ref cameraData.taaSettings);
            io.deltaTime = motionData.deltaTime;
            io.previousDeltaTime = motionData.lastDeltaTime;
            io.blueNoiseTextureSet = m_BlueNoise16LTex;

            // The motion scaling feature is only active outside of test environments. If we allowed it to run
            // during automated graphics tests, the results of each test run would be dependent on system
            // performance.
#if LWRP_DEBUG_STATIC_POSTFX
            io.enableMotionScaling = false;
#else
            io.enableMotionScaling = true;
#endif

            // Acquire the per-camera context for the active upscaler
            // In XR multi-pass rendering, encode eye information into the camera ID to ensure separate contexts per eye
            var upscaler = postProcessingData.activeUpscaler;
            ulong viewId = io.cameraInstanceID;
            if (cameraData.xr.enabled && !cameraData.xr.singlePassEnabled)
                viewId = (ulong)HashCode.Combine(io.cameraInstanceID, cameraData.xr.multipassId);

            // Fetch the framework-owned options for this upscaler (per-camera overrides are future work).
            UpscalerOptions upscalerOptions = UniversalRenderPipeline.upscaling.GetGlobalOptions(upscaler);

            io.context = UniversalRenderPipeline.upscaling.AcquireContext(
                viewId,
                upscaler,
                upscalerOptions,
                io.postUpscaleResolution
            );

            // Use jitter already computed during camera setup
            io.subpixelJitter = cameraData.subpixelJitter;

            // Per-frame settings (sharpness, etc.); upscalers read these from io.options, not from the context.
            io.options = upscalerOptions;

            // Insert the active upscaler's render graph passes
            upscaler.RecordRenderGraph(renderGraph, frameData);

            // Update the camera resolution to reflect the upscaled size
            var dstDesc = io.cameraColor.GetDescriptor(renderGraph);
            UpdateCameraResolution(renderGraph, cameraData, new Vector2Int(dstDesc.width, dstDesc.height));

            // Use the output texture of upscaling
            resourceData.cameraColor = io.cameraColor;
#endif
        }

        private class UpdateCameraResolutionPassData
        {
            internal Vector2Int newCameraTargetSize;
        }

        // Updates render target descriptors and shader constants to reflect a new render size
        // This should be called immediately after the resolution changes mid-frame (typically after an upscaling operation).
        static internal void UpdateCameraResolution(RenderGraph renderGraph, UniversalCameraData cameraData, Vector2Int newCameraTargetSize)
        {
            // Update the camera data descriptor to reflect post-upscaled sizes
            cameraData.cameraTargetDescriptor.width = newCameraTargetSize.x;
            cameraData.cameraTargetDescriptor.height = newCameraTargetSize.y;

            // Update the shader constants to reflect the new camera resolution
            using (var builder = renderGraph.AddUnsafePass<UpdateCameraResolutionPassData>("Update Camera Resolution", out var passData))
            {
                passData.newCameraTargetSize = newCameraTargetSize;

                // This pass only modifies shader constants
                builder.AllowGlobalStateModification(true);

                // Wrap constant modification into a pass to force graph execution timeline.
                builder.SetRenderFunc(static (UpdateCameraResolutionPassData data, UnsafeGraphContext ctx) =>
                {
                    ctx.cmd.SetGlobalVector(
                        ShaderPropertyId.screenSize,
                        new Vector4(
                            data.newCameraTargetSize.x,
                            data.newCameraTargetSize.y,
                            1.0f / data.newCameraTargetSize.x,
                            1.0f / data.newCameraTargetSize.y
                        )
                    );
                });
            }
        }

        // Precomputed shader ids to same some CPU cycles (mostly affects mobile)
        public static class ShaderConstants
        {
        }
    }
}
