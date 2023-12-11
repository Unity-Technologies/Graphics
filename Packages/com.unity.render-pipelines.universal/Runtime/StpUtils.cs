using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal
{
    internal static class StpUtils
    {
        static void CalculateJitter(int frameIndex, out Vector2 jitter, out bool allowScaling)
        {
            // NOTE: STP's jitter must be negated in URP due to y-flip
            jitter = -STP.Jit16(frameIndex);
            allowScaling = false;
        }

        // Static allocation of JitterFunc delegate to avoid GC
        internal static TemporalAA.JitterFunc s_JitterFunc = CalculateJitter;

        static void PopulateStpConfig(UniversalCameraData cameraData, TextureHandle inputColor, TextureHandle inputDepth, TextureHandle inputMotion, int debugViewIndex, TextureHandle debugView, TextureHandle destination, Texture2D noiseTexture, out STP.Config config)
        {
            cameraData.camera.TryGetComponent<UniversalAdditionalCameraData>(out var additionalCameraData);
            Debug.Assert(additionalCameraData != null);

            var motionData = additionalCameraData.motionVectorsPersistentData;
            Debug.Assert(motionData != null);

            // Dynamic scaling isn't supported
            config.enableHwDrs = false;

            // URP only uses texture arrays when XR single pass mode is enabled
            config.enableTexArray = cameraData.xr.enabled && cameraData.xr.singlePassEnabled;

            // The motion scaling feature is only active outside of test environments.
            // If we allowed it to run during automated graphics tests, the results of each test run would be dependent on system performance.
#if LWRP_DEBUG_STATIC_POSTFX
            config.enableMotionScaling = false;
#else
            config.enableMotionScaling = true;
#endif

            int frameIndex = Time.frameCount;

            config.noiseTexture = noiseTexture;

            config.inputColor = inputColor;
            config.inputDepth = inputDepth;
            config.inputMotion = inputMotion;

            // TODO: Support STP Stencil Responsive Feature in URP (JIRA: GFXRT-366)
            //
            // STP is capable of treating certain pixels in the image differently if they're known to cause
            // problems when run through TAA. Normally this is handled by using a single bit in the stencil
            // buffer to mask these pixels. This feature is typically used for transparent objects like sparks
            // and it can significantly improve visual quality on some scenes when enabled.
            config.inputStencil = TextureHandle.nullHandle;
            config.stencilMask = 0;

            config.debugView = debugView;

            config.destination = destination;

            var stpHistory = cameraData.stpHistory;
            Debug.Assert(stpHistory != null);

            int eyeIndex = (cameraData.xr.enabled && !cameraData.xr.singlePassEnabled) ? cameraData.xr.multipassId : 0;

            config.historyContext = stpHistory.GetHistoryContext(eyeIndex);

            config.nearPlane = cameraData.camera.nearClipPlane;
            config.farPlane = cameraData.camera.farClipPlane;
            config.frameIndex = frameIndex;
            config.hasValidHistory = !cameraData.resetHistory;
            config.debugViewIndex = debugViewIndex;

            config.deltaTime = motionData.deltaTime;
            config.lastDeltaTime = motionData.lastDeltaTime;

            // Note: The current and prior image sizes are always identical since DRS is not currently supported.
            config.currentImageSize = new Vector2Int(cameraData.cameraTargetDescriptor.width, cameraData.cameraTargetDescriptor.height);
            config.priorImageSize = config.currentImageSize;
            config.outputImageSize = new Vector2Int(cameraData.pixelWidth, cameraData.pixelHeight);

            // The number of active views may vary over time, but it must never be more than the number of supported view configs.
            int numActiveViews = cameraData.xr.enabled ? cameraData.xr.viewCount : 1;
            Debug.Assert(numActiveViews <= STP.perViewConfigs.Length);

            for (int viewIndex = 0; viewIndex < numActiveViews; ++viewIndex)
            {
                int targetIndex = viewIndex + eyeIndex;

                STP.PerViewConfig perViewConfig;

                // STP requires non-jittered versions of the current, previous, and "previous previous" projection matrix.

                perViewConfig.currentProj = motionData.projectionStereo[targetIndex];
                perViewConfig.lastProj = motionData.previousProjectionStereo[targetIndex];
                perViewConfig.lastLastProj = motionData.previousPreviousProjectionStereo[targetIndex];

                perViewConfig.currentView = motionData.viewStereo[targetIndex];
                perViewConfig.lastView = motionData.previousViewStereo[targetIndex];
                perViewConfig.lastLastView = motionData.previousPreviousViewStereo[targetIndex];

                // NOTE: STP assumes the view matrices also contain the world space camera position so we inject the camera position directly here.
                Vector3 currentPosition = motionData.worldSpaceCameraPos;
                Vector3 lastPosition = motionData.previousWorldSpaceCameraPos;
                Vector3 lastLastPosition = motionData.previousPreviousWorldSpaceCameraPos;

                perViewConfig.currentView.SetColumn(3, new Vector4(-currentPosition.x, -currentPosition.y, -currentPosition.z, 1.0f));
                perViewConfig.lastView.SetColumn(3, new Vector4(-lastPosition.x, -lastPosition.y, -lastPosition.z, 1.0f));
                perViewConfig.lastLastView.SetColumn(3, new Vector4(-lastLastPosition.x, -lastLastPosition.y, -lastLastPosition.z, 1.0f));

                STP.perViewConfigs[viewIndex] = perViewConfig;
            }

            config.numActiveViews = numActiveViews;
            config.perViewConfigs = STP.perViewConfigs;
        }

        static internal void Execute(RenderGraph renderGraph, UniversalResourceData resourceData, UniversalCameraData cameraData, TextureHandle inputColor, TextureHandle inputDepth, TextureHandle inputMotion, TextureHandle destination, Texture2D noiseTexture)
        {
            var debugView = TextureHandle.nullHandle;
            int debugViewIndex = 0;

            DebugHandler debugHandler = ScriptableRenderPass.GetActiveDebugHandler(cameraData);
            if ((debugHandler != null) && debugHandler.TryGetFullscreenDebugMode(out var fullscreenDebugMode))
            {
                if (fullscreenDebugMode == DebugFullScreenMode.STP)
                {
                    debugView = renderGraph.CreateTexture(new TextureDesc(cameraData.pixelWidth, cameraData.pixelHeight, false, (cameraData.xr.enabled && cameraData.xr.singlePassEnabled))
                    {
                        name = "STP Debug View",
                        colorFormat = GraphicsFormat.R8G8B8A8_UNorm,
                        clearBuffer = true,
                        enableRandomWrite = true
                    });

                    debugViewIndex = debugHandler.stpDebugViewIndex;

                    // Save the debug view texture so it can be displayed later
                    resourceData.stpDebugView = debugView;
                }
            }

            PopulateStpConfig(cameraData, inputColor, inputDepth, inputMotion, debugViewIndex, debugView, destination, noiseTexture, out var config);
            STP.Execute(renderGraph, ref config);
        }
    }
}
