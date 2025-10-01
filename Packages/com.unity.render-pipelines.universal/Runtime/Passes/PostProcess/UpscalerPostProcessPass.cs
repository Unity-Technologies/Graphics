using System;
using UnityEngine.Rendering.RenderGraphModule;
using System.Runtime.CompilerServices; // AggressiveInlining

namespace UnityEngine.Rendering.Universal
{
    internal sealed class UpscalerPostProcessPass : ScriptableRenderPass, IDisposable
    {
        public const string k_UpscaledColorTargetName = "_CameraColorUpscaled";
        Texture2D[] m_BlueNoise16LTex;
        bool m_IsValid;

        // Settings

        // Input
        public TextureHandle sourceTexture { get; set; }

        // Output
        public TextureHandle destinationTexture { get; private set; }

        public UpscalerPostProcessPass(Texture2D[] blueNoise16LTex)
        {
            this.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing - 1;
            this.profilingSampler = null;   // Use default name
            m_BlueNoise16LTex = blueNoise16LTex;

            m_IsValid = m_BlueNoise16LTex != null && m_BlueNoise16LTex.Length > 0;
        }

        public void Dispose()
        {
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsValid()
        {
            return m_IsValid;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
#if ENABLE_UPSCALER_FRAMEWORK
            Assertions.Assert.IsTrue(sourceTexture.IsValid(), $"Source texture must be set for StpPostProcessPass.");

            // TODO: User should be able to set the destination texture externally. This allows the user to decide where the upscaled texture should be stored.
            //Assertions.Assert.IsTrue(destinationTexture.IsValid(), $"Destination texture must be set for StpPostProcessPass.");

            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalPostProcessingData postProcessingData = frameData.Get<UniversalPostProcessingData>();

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
            io.preUpscaleResolution = new Vector2Int(
                cameraData.cameraTargetDescriptor.width,
                cameraData.cameraTargetDescriptor.height
            );
            io.previousPreUpscaleResolution = io.preUpscaleResolution; // URP doesn't support Dynamic Resolution Scaling (DRS).
            io.postUpscaleResolution = new Vector2Int(cameraData.pixelWidth, cameraData.pixelHeight);
            io.motionVectorTextureSize = io.preUpscaleResolution;
            io.enableTexArray = cameraData.xr.enabled && cameraData.xr.singlePassEnabled;

            MotionVectorsPersistentData motionData = null;
            {
                cameraData.camera.TryGetComponent<UniversalAdditionalCameraData>(out var additionalCameraData);
                Debug.Assert(additionalCameraData != null);
                motionData = additionalCameraData.motionVectorsPersistentData;
                Debug.Assert(motionData != null);
            }
            io.cameraInstanceID = cameraData.camera.GetInstanceID();
            io.nearClipPlane = cameraData.camera.nearClipPlane;
            io.farClipPlane = cameraData.camera.farClipPlane;
            io.fieldOfViewDegrees = cameraData.camera.fieldOfView;
            io.invertedDepth = SystemInfo.usesReversedZBuffer;
            io.flippedY = SystemInfo.graphicsUVStartsAtTop;
            io.flippedX = false;
            io.hdrInput = Experimental.Rendering.GraphicsFormatUtility.IsHDRFormat(srcDesc.format);
            io.numActiveViews = cameraData.xr.enabled ? cameraData.xr.viewCount : 1;
            io.eyeIndex = (cameraData.xr.enabled && !cameraData.xr.singlePassEnabled) ? cameraData.xr.multipassId : 0;
            io.worldSpaceCameraPositions = new Vector3[io.numActiveViews];
            io.previousWorldSpaceCameraPositions = new Vector3[io.numActiveViews];
            io.previousPreviousWorldSpaceCameraPositions = new Vector3[io.numActiveViews];
            for (int i = 0; i < io.numActiveViews; i++)
            {
                io.worldSpaceCameraPositions[i] = motionData.worldSpaceCameraPos;
                io.previousWorldSpaceCameraPositions[i] = motionData.previousWorldSpaceCameraPos;
                io.previousPreviousWorldSpaceCameraPositions[i] = motionData.previousPreviousWorldSpaceCameraPos;
            }
            io.projectionMatrices = motionData.projectionStereo;
            io.previousProjectionMatrices = motionData.previousProjectionStereo;
            io.previousPreviousProjectionMatrices = motionData.previousPreviousProjectionStereo;
            io.viewMatrices = motionData.viewStereo;
            io.previousViewMatrices = motionData.previousViewStereo;
            io.previousPreviousViewMatrices = motionData.previousPreviousViewStereo;
            io.resetHistory = cameraData.resetHistory;
            // TODO (Apoorva): Maybe we want to support this?
            // URP supports adding an offset value to the TAA frame index for testing determinism as follows:
            //     io.frameIndex = Time.frameCount + settings.jitterFrameCountOffset;
            io.frameIndex = Time.frameCount;
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
            io.enableHwDrs = false; // URP doesn't support hardware dynamic resolution scaling
            // Insert the active upscaler's render graph passes
            postProcessingData.activeUpscaler.RecordRenderGraph(renderGraph, frameData);

            // Update the camera resolution to reflect the upscaled size
            var dstDesc = io.cameraColor.GetDescriptor(renderGraph);
            UpdateCameraResolution(renderGraph, cameraData, new Vector2Int(dstDesc.width, dstDesc.height));

            // Use the output texture of upscaling
            destinationTexture = io.cameraColor;
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
