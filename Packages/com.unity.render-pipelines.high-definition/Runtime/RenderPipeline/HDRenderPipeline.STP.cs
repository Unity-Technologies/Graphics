using System;
using UnityEngine.Assertions;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        void PopulateStpConfig(HDCamera hdCamera, TextureHandle inputColor, TextureHandle inputDepth, TextureHandle inputMotion, TextureHandle inputStencil, int debugViewIndex, TextureHandle debugView, TextureHandle destination, out STP.Config config)
        {
            hdCamera.RequestStpHistory(out var useHwDrs, out var hasValidHistory, out var history);
            
            config.historyContext = history;

            config.enableHwDrs = useHwDrs;
            config.enableTexArray = TextureXR.useTexArray;

            // The motion scaling feature is only active outside of test environments.
            // If we allowed it to run during automated graphics tests, the results of each test run would be dependent on system performance.
#if HDRP_DEBUG_STATIC_POSTFX
            config.enableMotionScaling = false;
#else
            config.enableMotionScaling = true;
#endif

            Assert.IsTrue(Mathf.IsPowerOfTwo(m_BlueNoise.textures16L.Length));
            config.noiseTexture = m_BlueNoise.textures16L[hdCamera.taaFrameIndex & (m_BlueNoise.textures16L.Length - 1)];

            config.inputColor = inputColor;
            config.inputDepth = inputDepth;
            config.inputMotion = inputMotion;
            config.inputStencil = inputStencil;

            config.debugView = debugView;

            config.destination = destination;

            // Ensure that the shader knows when it has valid history data
            // We can have invalid history data if the camera post processing history is reset manually due to a camera cut or because
            // this is the first frame and we haven't actually generated history data yet.
            hasValidHistory &= !hdCamera.resetPostProcessingHistory;

            config.nearPlane = hdCamera.projectionParams.y;
            config.farPlane = hdCamera.projectionParams.z;
            config.frameIndex = hdCamera.taaFrameIndex;
            config.hasValidHistory = hasValidHistory;
            config.stencilMask = (int)StencilUsage.ExcludeFromTUAndAA;
            config.debugViewIndex = debugViewIndex;

            config.deltaTime = hdCamera.currentRenderDeltaTime;
            config.lastDeltaTime = hdCamera.lastRenderDeltaTime;

            config.currentImageSize = hdCamera.historyRTHandleProperties.currentViewportSize;
            config.priorImageSize = hdCamera.historyRTHandleProperties.previousViewportSize;
            config.outputImageSize = new Vector2Int((int)hdCamera.finalViewport.width, (int)hdCamera.finalViewport.height);

            // The number of active views may vary over time, but it must never be more than we expected during initialization.
            int numActiveViews = hdCamera.m_XRViewConstants.Length;
            Debug.Assert(numActiveViews <= STP.perViewConfigs.Length);

            for (int viewIndex = 0; viewIndex < numActiveViews; ++viewIndex)
            {
                STP.PerViewConfig perViewConfig;

                // STP requires non-jittered versions of the current, previous, and "previous previous" projection matrix.
                // NOTE: The "prevProjMatrix" and "prevPrevProjMatrix" values are always unjittered because they are sourced from the nonJitteredProjMatrix value.
                perViewConfig.currentProj = hdCamera.m_XRViewConstants[viewIndex].nonJitteredProjMatrix;
                perViewConfig.lastProj = hdCamera.m_XRViewConstants[viewIndex].prevProjMatrix;
                perViewConfig.lastLastProj = hdCamera.m_XRViewConstants[viewIndex].prevPrevProjMatrix;

                perViewConfig.currentView = hdCamera.m_XRViewConstants[viewIndex].viewMatrix;
                perViewConfig.lastView = hdCamera.m_XRViewConstants[viewIndex].prevViewMatrix;
                perViewConfig.lastLastView = hdCamera.m_XRViewConstants[viewIndex].prevPrevViewMatrix;

                // NOTE: STP assumes the view matrices also contain the camera position. However, HDRP may be configured to perform camera relative rendering which
                //       removes the camera translation from the view matrices. We inject the camera position directly into the view matrix here to make sure we don't
                //       run into issues when camera relative rendering is enabled.
                //
                //       Also, the previous world space camera position variable is specified as a value relative to the current world space camera position.
                //       We must add both values together in order to produce the last camera position as an absolute world space value.
                Vector3 currentPosition = hdCamera.m_XRViewConstants[viewIndex].worldSpaceCameraPos;
                Vector3 lastPosition = hdCamera.m_XRViewConstants[viewIndex].prevWorldSpaceCameraPos + hdCamera.m_XRViewConstants[viewIndex].worldSpaceCameraPos;
                Vector3 lastLastPosition = hdCamera.m_XRViewConstants[viewIndex].prevPrevWorldSpaceCameraPos + lastPosition;

                perViewConfig.currentView.SetColumn(3, new Vector4(-currentPosition.x, -currentPosition.y, -currentPosition.z, 1.0f));
                perViewConfig.lastView.SetColumn(3, new Vector4(-lastPosition.x, -lastPosition.y, -lastPosition.z, 1.0f));
                perViewConfig.lastLastView.SetColumn(3, new Vector4(-lastLastPosition.x, -lastLastPosition.y, -lastLastPosition.z, 1.0f));

                STP.perViewConfigs[viewIndex] = perViewConfig;
            }

            config.numActiveViews = numActiveViews;
            config.perViewConfigs = STP.perViewConfigs;
        }

        TextureHandle DoStpPasses(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle inputColor, TextureHandle inputDepth, TextureHandle inputMotion, TextureHandle inputStencil)
        {
            bool debugEnabled = (m_CurrentDebugDisplaySettings.data.fullScreenDebugMode == FullScreenDebugMode.STP);
            int debugViewIndex = debugDisplaySettings.data.stpDebugViewIndex;

            TextureHandle destination = GetPostprocessUpsampledOutputHandle(hdCamera, renderGraph, "STP Output Color");

            TextureHandle debugView = TextureHandle.nullHandle;

            if (debugEnabled)
            {
                debugView = renderGraph.CreateTexture(new TextureDesc(m_AfterDynamicResUpscaleRes.x, m_AfterDynamicResUpscaleRes.y, false, true)
                {
                    name = "STP Debug View",
                    format = GraphicsFormat.R8G8B8A8_UNorm,
                    clearBuffer = true,
                    enableRandomWrite = true
                });
            }

            PopulateStpConfig(hdCamera, inputColor, inputDepth, inputMotion, inputStencil, debugViewIndex, debugView, destination, out var config);
            STP.Execute(renderGraph, ref config);

            if (debugEnabled)
            {
                // TODO: Investigate 2x2 pixel size issue in debug view
                //       Issue is still present regardless of fixed size or 1.0 scale debug texture create info
                PushFullScreenDebugTexture(renderGraph, debugView, true, Vector2.one, rtFormat: GraphicsFormat.R8G8B8A8_UNorm);
            }

            return destination;
        }
    }
}
