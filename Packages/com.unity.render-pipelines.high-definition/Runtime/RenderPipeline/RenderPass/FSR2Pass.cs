using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    internal class FSR2Pass
    {
        public struct Parameters
        {
            public HDCamera hdCamera;
            public GlobalDynamicResolutionSettings drsSettings;
        }

        public static bool SetupFeature()
        {
#if ENABLE_AMD && ENABLE_AMD_MODULE
            if (!AMD.AMDUnityPlugin.IsLoaded())
                return false;

            if (s_ExpectedDeviceVersion != AMD.GraphicsDevice.version)
            {
                Debug.LogWarning("Cannot instantiate AMD device because the version HDRP expects does not match the backend version.");
                return false;
            }
            
            AMD.GraphicsDevice device = AMD.GraphicsDevice.CreateGraphicsDevice();
            return device != null;
#else
            return false;
#endif
        }

        public static FSR2Pass Create()
        {
            FSR2Pass fsrPass = null;
#if ENABLE_AMD && ENABLE_AMD_MODULE
            if (!SetupFeature())
                return null;

            fsrPass = new FSR2Pass(AMD.GraphicsDevice.device);
#endif
            return fsrPass;
        }

        public void BeginFrame(HDCamera hdCamera)
        {
#if ENABLE_AMD && ENABLE_AMD_MODULE
            InternalAMDBeginFrame(hdCamera);
#endif
        }

        public void SetupDRSScaling(bool enableAutomaticSettings, Camera camera, in HDAdditionalCameraData hdCam, XRPass xrPass, ref GlobalDynamicResolutionSettings dynamicResolutionSettings)
        {
#if ENABLE_AMD && ENABLE_AMD_MODULE
            InternalAMDSetupDRSScaling(enableAutomaticSettings, camera, hdCam, xrPass, ref dynamicResolutionSettings);
#endif
        }

        public void Render(
            FSR2Pass.Parameters parameters,
            UpscalerResources.CameraResources resources,
            CommandBuffer cmdBuffer)
        {
#if ENABLE_AMD && ENABLE_AMD_MODULE
            InternalAMDRender(parameters, resources, cmdBuffer);
#endif
        }

    #region private members, nvidia specific code
#if ENABLE_AMD && ENABLE_AMD_MODULE
        private UpscalerCameras m_CameraStates = new UpscalerCameras();

        private CommandBuffer m_CommandBuffer = new CommandBuffer();

        private AMD.GraphicsDevice m_Device = null;

        private FSR2Pass(AMD.GraphicsDevice device)
        {
            m_Device = device;
        }

        private static uint s_ExpectedDeviceVersion = 0x00;

        private struct Fsr2ViewData
        {
            public UpscalerResolution inputRes;
            public UpscalerResolution outputRes;
            public float sharpness;
            public float jitterX;
            public float jitterY;
            public bool enableSharpening;
            public bool reset;
            public float frameTimeDelta;
            public float cameraNear;
            public float cameraFar;
            public float cameraFovAngleVertical;
            public bool CanFitInput(in UpscalerResolution inputRect)
            {
                return inputRes.width >= inputRect.width && inputRes.height > inputRect.height;
            }
        }

        private class ViewState
        {
            private AMD.FSR2Context m_Fsr2Context = null;
            private AMD.GraphicsDevice m_Device;
            private Fsr2ViewData m_Data = new Fsr2ViewData();
            private UpscalerResolution m_BackbufferRes;

            public AMD.FSR2Context fsr2Context { get { return m_Fsr2Context; } }

            public ViewState()
            {
            }

            public void Init(AMD.GraphicsDevice device)
            {
                m_Device = device;
            }

            public void UpdateViewState(
                in Fsr2ViewData viewData,
                CommandBuffer cmdBuffer)
            {
                bool isNew = false;
                if (viewData.outputRes != m_Data.outputRes ||
                    (viewData.inputRes.width > m_BackbufferRes.width || viewData.inputRes.height > m_BackbufferRes.height) ||
                    m_Fsr2Context == null)
                {
                    isNew = true;
                    m_BackbufferRes = viewData.inputRes;

                    if (m_Fsr2Context != null)
                    {
                        m_Device.DestroyFeature(cmdBuffer, m_Fsr2Context);
                        m_Fsr2Context = null;
                    }

                    var settings = new AMD.FSR2CommandInitializationData();
                    settings.SetFlag(AMD.FfxFsr2InitializationFlags.EnableHighDynamicRange, true);
                    settings.SetFlag(AMD.FfxFsr2InitializationFlags.EnableDisplayResolutionMotionVectors, false);
                    settings.SetFlag(AMD.FfxFsr2InitializationFlags.EnableMotionVectorsJitterCancellation, false);
                    settings.SetFlag(AMD.FfxFsr2InitializationFlags.DepthInverted, true);
                    settings.maxRenderSizeWidth   = viewData.outputRes.width;
                    settings.maxRenderSizeHeight  = viewData.outputRes.height;
                    settings.displaySizeWidth     = viewData.outputRes.width;;
                    settings.displaySizeHeight    = viewData.outputRes.height;;
                    m_Fsr2Context = m_Device.CreateFeature(cmdBuffer, settings);
                }

                m_Data = viewData;
                m_Data.reset = isNew || viewData.reset;
            }

            public void SubmitFsr2Commands(
                Texture source,
                Texture depth,
                Texture motionVectors,
                Texture biasColorMask,
                Texture output,
                CommandBuffer cmdBuffer)
            {
                if (m_Fsr2Context == null)
                    return;

                m_Fsr2Context.executeData.enableSharpening = m_Data.enableSharpening ? 1 : 0;
                m_Fsr2Context.executeData.sharpness = m_Data.sharpness;
                m_Fsr2Context.executeData.MVScaleX = -((float)m_Data.inputRes.width);
                m_Fsr2Context.executeData.MVScaleY = -((float)m_Data.inputRes.height);
                m_Fsr2Context.executeData.renderSizeWidth = m_Data.inputRes.width;
                m_Fsr2Context.executeData.renderSizeHeight = m_Data.inputRes.height;
                m_Fsr2Context.executeData.jitterOffsetX = -m_Data.jitterX;
                m_Fsr2Context.executeData.jitterOffsetY = -m_Data.jitterY;
                m_Fsr2Context.executeData.preExposure = 1.0f;
                m_Fsr2Context.executeData.frameTimeDelta = m_Data.frameTimeDelta;
                m_Fsr2Context.executeData.cameraNear = m_Data.cameraNear;
                m_Fsr2Context.executeData.cameraFar = m_Data.cameraFar;
                m_Fsr2Context.executeData.cameraFovAngleVertical = m_Data.cameraFovAngleVertical;
                m_Fsr2Context.executeData.reset = m_Data.reset ? 1 : 0;

                var textureTable = new AMD.FSR2TextureTable()
                {
                    colorInput = source,
                    colorOutput = output,
                    depth = depth,
                    motionVectors = motionVectors,
                    biasColorMask = biasColorMask
                };

                m_Device.ExecuteFSR2(cmdBuffer, m_Fsr2Context, textureTable);
            }

            public void Cleanup(CommandBuffer cmdBuffer)
            {
                if (m_Fsr2Context != null)
                {
                    m_Device.DestroyFeature(cmdBuffer, m_Fsr2Context);
                    m_Fsr2Context = null;
                }

                m_Device = null;
                m_Data = new Fsr2ViewData();
                m_BackbufferRes = new UpscalerResolution();
            }
        }

        private class FSR2Camera
        {
            List<ViewState> m_Views = null;
            AMD.GraphicsDevice m_Device = null;
            PerformDynamicRes m_ScaleDelegate = null;
            private bool m_UseAutomaticSettings = false;
            private float m_AutomaticPercentage = 1.0f;
            public PerformDynamicRes ScaleDelegate { get { return m_ScaleDelegate; } }
            public List<ViewState> ViewStates { get { return m_Views; } }
            public bool useAutomaticSettings { get { return m_UseAutomaticSettings; } }
            public float automaticPercentage { get { return m_AutomaticPercentage; } }

            public FSR2Camera()
            {
                m_ScaleDelegate = ScaleFn;
            }

            public void Init(AMD.GraphicsDevice device)
            {
                m_Device = device;
            }

            private float ScaleFn()
            {
                if (!m_UseAutomaticSettings)
                    return 100.0f;

                return m_AutomaticPercentage;
            }

            public void UseAutomaticSettings(bool useAutomaticSettings, float percentage)
            {
                m_UseAutomaticSettings = useAutomaticSettings;
                m_AutomaticPercentage = percentage;
            }

            public void SubmitCommands(
                HDCamera camera,
                in Fsr2ViewData viewData,
                in UpscalerResources.CameraResources camResources,
                CommandBuffer cmdBuffer)
            {
                int cameraViewCount = 1;
                int activeViewId = 0;
                if (camera.xr.enabled)
                {
                    cameraViewCount = camera.xr.singlePassEnabled ? camera.xr.viewCount : 2;
                    activeViewId = camera.xr.multipassId;
                }

                if (m_Views == null || m_Views.Count != cameraViewCount)
                {
                    if (m_Views != null)
                        Cleanup(cmdBuffer);

                    m_Views = ListPool<ViewState>.Get();
                    for (int viewId = 0; viewId < cameraViewCount; ++viewId)
                    {
                        var newView = GenericPool<ViewState>.Get();
                        newView.Init(m_Device);
                        m_Views.Add(newView);
                    }
                }

                void RunPass(ViewState viewState, CommandBuffer cmdBuffer, in Fsr2ViewData viewData, in UpscalerResources.ViewResources viewResources)
                {
                    viewState.UpdateViewState(viewData, cmdBuffer);
                    viewState.SubmitFsr2Commands(
                        viewResources.source,
                        viewResources.depth,
                        viewResources.motionVectors,
                        viewResources.biasColorMask,
                        viewResources.output, cmdBuffer);
                }

                if (camResources.copyToViews)
                {
                    Assertions.Assert.IsTrue(camera.xr.enabled && camera.xr.singlePassEnabled, "XR must be enabled for tmp copying to views to occur");

                    //copy to tmp views first, to maximize pipelining
                    for (int viewId = 0; viewId < m_Views.Count; ++viewId)
                    {
                        ViewState viewState = m_Views[viewId];
                        UpscalerResources.ViewResources tmpResources = viewId == 0 ? camResources.tmpView0 : camResources.tmpView1;

                        cmdBuffer.CopyTexture(camResources.resources.source, viewId, tmpResources.source, 0);
                        cmdBuffer.CopyTexture(camResources.resources.depth, viewId, tmpResources.depth, 0);
                        cmdBuffer.CopyTexture(camResources.resources.motionVectors, viewId, tmpResources.motionVectors, 0);

                        if (camResources.resources.biasColorMask != null)
                            cmdBuffer.CopyTexture(camResources.resources.biasColorMask, viewId, tmpResources.biasColorMask, 0);
                    }

                    for (int viewId = 0; viewId < m_Views.Count; ++viewId)
                    {
                        ViewState viewState = m_Views[viewId];
                        UpscalerResources.ViewResources tmpResources = viewId == 0 ? camResources.tmpView0 : camResources.tmpView1;
                        RunPass(viewState, cmdBuffer, viewData, tmpResources);
                        cmdBuffer.CopyTexture(tmpResources.output, 0, camResources.resources.output, viewId);
                    }
                }
                else
                {
                    RunPass(m_Views[activeViewId], cmdBuffer, viewData, camResources.resources);
                }
            }

            public void Cleanup(CommandBuffer cmdBuffer)
            {
                if (m_Views == null)
                    return;

                foreach (var v in m_Views)
                {
                    v.Cleanup(cmdBuffer);
                    GenericPool<ViewState>.Release(v);
                }

                ListPool<ViewState>.Release(m_Views);
                m_Views = null;
                m_Device = null;
            }
        }

        private void InternalAMDBeginFrame(HDCamera hdCamera)
        {
            if (m_Device == null)
                return;

            m_CameraStates.ProcessExpiredCameras();

            UpscalerCameras.State cameraState = m_CameraStates.GetState(hdCamera.camera);
            var fsr2Camera = cameraState != null ? cameraState.data as FSR2Camera : (FSR2Camera)null;

            bool fsr2Active = hdCamera.IsFSR2Enabled();

            if (fsr2Camera == null && fsr2Active)
            {
                fsr2Camera = GenericPool<FSR2Camera>.Get();
                fsr2Camera.Init(m_Device);
                cameraState = m_CameraStates.CreateState(hdCamera.camera);
                cameraState.data = fsr2Camera;
            }
            else if (cameraState != null && !fsr2Active)
            {
                m_CameraStates.InvalidateState(cameraState);
            }

            if (cameraState != null)
                m_CameraStates.TagUsed(cameraState);

            CleanupCameraStates();
            m_CameraStates.NextFrame();
        }

        private void CleanupCameraStates()
        {
            Dictionary<int, UpscalerCameras.State> cameras = m_CameraStates.cameras;
            m_CommandBuffer.Clear();
            foreach (var kv in cameras)
            {
                var cameraState = kv.Value;
                if (!m_CameraStates.HasCameraStateExpired(cameraState) || cameraState.data == null)
                    continue;

                var fsr2Camera = cameraState.data as FSR2Camera;
                fsr2Camera.Cleanup(m_CommandBuffer);
                GenericPool<FSR2Camera>.Release(fsr2Camera);
                cameraState.data = null;
            }
            Graphics.ExecuteCommandBuffer(m_CommandBuffer);
            m_CameraStates.CleanupCameraStates();
        }

        private void InternalAMDSetupDRSScaling(bool enableAutomaticSettings, Camera camera, in HDAdditionalCameraData hdCam, XRPass xrPass, ref GlobalDynamicResolutionSettings dynamicResolutionSettings)
        {
            if (m_Device == null)
                return;

            UpscalerCameras.State cameraState = m_CameraStates.GetState(camera);
            if (cameraState == null)
                return;

            var fsr2Camera = cameraState.data as FSR2Camera;
            if (fsr2Camera.ViewStates == null || fsr2Camera.ViewStates.Count == 0)
                return;

            float percentage = 100.0f;
            if (enableAutomaticSettings)
            {
                var qualityMode = (AMD.FSR2Quality)(hdCam.fidelityFX2SuperResolutionUseCustomAttributes ? hdCam.fidelityFX2SuperResolutionQuality : dynamicResolutionSettings.FSR2QualitySetting);
                percentage = (1.0f / m_Device.GetUpscaleRatioFromQualityMode(qualityMode)) * 100.0f;
                DynamicResolutionHandler.SetSystemDynamicResScaler(fsr2Camera.ScaleDelegate, DynamicResScalePolicyType.ReturnsPercentage);
                DynamicResolutionHandler.SetActiveDynamicScalerSlot(DynamicResScalerSlot.System);
            }

            fsr2Camera.UseAutomaticSettings(enableAutomaticSettings, percentage);
        }

        private void InternalAMDRender(
            FSR2Pass.Parameters parameters,
            UpscalerResources.CameraResources resources,
            CommandBuffer cmdBuffer)
        {
            if (m_Device == null || m_CameraStates.cameras.Count == 0)
                return;

            UpscalerCameras.State cameraState = m_CameraStates.GetState(parameters.hdCamera.camera);
            if (cameraState == null)
                return;

            FSR2Camera fsr2Camera = cameraState.data as FSR2Camera;
            bool useCameraCustomAttributes = parameters.hdCamera.fidelityFX2SuperResolutionUseCustomAttributes;
            var fsr2ViewData = new Fsr2ViewData();
            fsr2ViewData.inputRes  = new UpscalerResolution() { width = (uint)parameters.hdCamera.actualWidth, height = (uint)parameters.hdCamera.actualHeight };
            fsr2ViewData.outputRes = new UpscalerResolution() { width = (uint)DynamicResolutionHandler.instance.finalViewport.x, height = (uint)DynamicResolutionHandler.instance.finalViewport.y };
            fsr2ViewData.jitterX = parameters.hdCamera.taaJitter.x;
            fsr2ViewData.jitterY = parameters.hdCamera.taaJitter.y;
            fsr2ViewData.reset = parameters.hdCamera.isFirstFrame;
            fsr2ViewData.enableSharpening = useCameraCustomAttributes ? parameters.hdCamera.fidelityFX2SuperResolutionEnableSharpening : parameters.drsSettings.FSR2EnableSharpness;
            fsr2ViewData.sharpness = useCameraCustomAttributes ? parameters.hdCamera.fidelityFX2SuperResolutionSharpening : parameters.drsSettings.FSR2Sharpness;
            fsr2ViewData.cameraNear = parameters.hdCamera.camera.nearClipPlane;
            fsr2ViewData.cameraFar = parameters.hdCamera.camera.farClipPlane;
            fsr2ViewData.frameTimeDelta = Time.deltaTime * 1000.0f;
            fsr2ViewData.cameraFovAngleVertical = parameters.hdCamera.camera.fieldOfView * 2.0f * (float)Math.PI * (1 / 360.0f);
            fsr2Camera.SubmitCommands(parameters.hdCamera, fsr2ViewData, resources, cmdBuffer);
        }

#endif
    #endregion
    }
}
