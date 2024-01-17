using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    internal class DLSSPass
    {

        #region public members, general engine code
        public struct Parameters
        {
            public bool resetHistory;
            public float preExposure;
            public HDCamera hdCamera;
            public GlobalDynamicResolutionSettings drsSettings;
        }

        public static bool SetupFeature()
        {
#if ENABLE_NVIDIA && ENABLE_NVIDIA_MODULE
            if (!NVIDIA.NVUnityPlugin.IsLoaded())
                return false;

            if (s_ExpectedDeviceVersion != NVIDIA.GraphicsDevice.version)
            {
                Debug.LogWarning("Cannot instantiate NVIDIA device because the version HDRP expects does not match the backend version.");
                return false;
            }

            if (!SystemInfo.graphicsDeviceVendor.ToLowerInvariant().Contains("nvidia"))
                return false;

            var device = NVIDIA.GraphicsDevice.CreateGraphicsDevice();
            return device != null && device.IsFeatureAvailable(NVIDIA.GraphicsDeviceFeature.DLSS);
#else
            return false;
#endif
        }

        public static DLSSPass Create()
        {
            DLSSPass dlssPass = null;

#if ENABLE_NVIDIA && ENABLE_NVIDIA_MODULE
            if (!SetupFeature())
                return null;

            dlssPass = new DLSSPass(NVIDIA.GraphicsDevice.device);
#endif
            return dlssPass;
        }

        public void BeginFrame(HDCamera hdCamera)
        {
#if ENABLE_NVIDIA && ENABLE_NVIDIA_MODULE
            InternalNVIDIABeginFrame(hdCamera);
#endif
        }

        public void SetupDRSScaling(bool enableAutomaticSettings, Camera camera, in HDAdditionalCameraData hdCam,  XRPass xrPass, ref GlobalDynamicResolutionSettings dynamicResolutionSettings)
        {
#if ENABLE_NVIDIA && ENABLE_NVIDIA_MODULE
            InternalNVIDIASetupDRSScaling(enableAutomaticSettings, camera, hdCam, xrPass, ref dynamicResolutionSettings);
#endif
        }

        public void Render(
            DLSSPass.Parameters parameters,
            UpscalerResources.CameraResources resources,
            CommandBuffer cmdBuffer)
        {
#if ENABLE_NVIDIA && ENABLE_NVIDIA_MODULE
            InternalNVIDIARender(parameters, resources, cmdBuffer);
#endif
        }

        #endregion

        #region private members, nvidia specific code
#if ENABLE_NVIDIA && ENABLE_NVIDIA_MODULE
        private static uint s_ExpectedDeviceVersion = 0x04;

        private UpscalerCameras m_CameraStates = new UpscalerCameras();

        private CommandBuffer m_CommandBuffer = new CommandBuffer();

        private NVIDIA.GraphicsDevice m_Device = null;

        private DLSSPass(NVIDIA.GraphicsDevice device)
        {
            m_Device = device;
        }

        private struct DlssViewData
        {
            public NVIDIA.DLSSQuality  perfQuality;
            public UpscalerResolution inputRes;
            public UpscalerResolution outputRes;
            public float sharpness;
            public float jitterX;
            public float jitterY;
            public bool reset;
            public bool CanFitInput(in UpscalerResolution inputRect)
            {
                return inputRes.width >= inputRect.width && inputRes.height > inputRect.height;
            }
        }

        private struct OptimalSettingsRequest
        {
            public NVIDIA.DLSSQuality quality;
            public Rect viewport;
            public NVIDIA.OptimalDLSSSettingsData optimalSettings;
            public bool CanFit(UpscalerResolution rect)
            {
                return rect.width >= optimalSettings.minWidth && rect.height >= optimalSettings.minHeight
                    && rect.width <= optimalSettings.maxWidth && rect.height <= optimalSettings.maxHeight;
            }
        }

        private static bool IsOptimalSettingsValid(in NVIDIA.OptimalDLSSSettingsData optimalSettings)
        {
            return optimalSettings.maxHeight >= optimalSettings.minHeight
                && optimalSettings.maxWidth  >= optimalSettings.minWidth
                && optimalSettings.maxWidth  != 0
                && optimalSettings.maxHeight != 0
                && optimalSettings.minWidth  != 0
                && optimalSettings.minHeight != 0;
        }

        private class ViewState
        {
            private NVIDIA.DLSSContext m_DlssContext = null;
            private NVIDIA.GraphicsDevice m_Device;
            private DlssViewData m_Data = new DlssViewData();
            private bool m_UsingOptimalSettings = false;
            private bool m_UseAutomaticSettings = false;
            private UpscalerResolution m_BackbufferRes;
            private OptimalSettingsRequest m_OptimalSettingsRequest = new OptimalSettingsRequest();

            public NVIDIA.DLSSContext DLSSContext { get { return m_DlssContext; } }
            public bool useAutomaticSettings { get { return m_UseAutomaticSettings; } }
            public OptimalSettingsRequest OptimalSettingsRequestData { get { return m_OptimalSettingsRequest; } }

            public ViewState()
            {
            }

            public void Init(NVIDIA.GraphicsDevice device)
            {
                m_Device = device;
                m_DlssContext = null;
            }

            public void RequestUseAutomaticSettings(bool useAutomaticSettings, NVIDIA.DLSSQuality quality, Rect viewport, in NVIDIA.OptimalDLSSSettingsData optimalSettings)
            {
                m_UseAutomaticSettings = useAutomaticSettings;
                m_OptimalSettingsRequest.quality = quality;
                m_OptimalSettingsRequest.viewport = viewport;
                m_OptimalSettingsRequest.optimalSettings = optimalSettings;
            }

            public void ClearAutomaticSettings()
            {
                m_UseAutomaticSettings = false;
            }

            private bool ShouldUseAutomaticSettings()
            {
                if (!m_UseAutomaticSettings || m_DlssContext == null)
                    return false;

                return m_DlssContext.initData.quality == m_OptimalSettingsRequest.quality
                    && m_DlssContext.initData.outputRTHeight == (uint)m_OptimalSettingsRequest.viewport.height
                    && m_DlssContext.initData.outputRTWidth  == (uint)m_OptimalSettingsRequest.viewport.width
                    && IsOptimalSettingsValid(m_OptimalSettingsRequest.optimalSettings);
            }

            public void UpdateViewState(
                in DlssViewData viewData,
                CommandBuffer cmdBuffer)
            {
                if(m_Device == null)
                    return;

                bool shouldUseOptimalSettings = ShouldUseAutomaticSettings();
                bool isNew = false;
                if (viewData.outputRes != m_Data.outputRes ||
                    (viewData.inputRes.width > m_BackbufferRes.width || viewData.inputRes.height > m_BackbufferRes.height) ||
                    (viewData.inputRes != m_BackbufferRes && !m_OptimalSettingsRequest.CanFit(viewData.inputRes)) ||
                    viewData.perfQuality != m_Data.perfQuality ||
                    m_DlssContext == null ||
                    shouldUseOptimalSettings != m_UsingOptimalSettings)
                {
                    isNew = true;
                    m_BackbufferRes = viewData.inputRes;

                    if (m_DlssContext != null)
                    {
                        m_Device.DestroyFeature(cmdBuffer, m_DlssContext);
                        m_DlssContext = null;
                    }

                    var settings = new NVIDIA.DLSSCommandInitializationData();
                    settings.SetFlag(NVIDIA.DLSSFeatureFlags.IsHDR, true);
                    settings.SetFlag(NVIDIA.DLSSFeatureFlags.MVLowRes, true);
                    settings.SetFlag(NVIDIA.DLSSFeatureFlags.DepthInverted, true);
                    settings.SetFlag(NVIDIA.DLSSFeatureFlags.DoSharpening, true);
                    settings.inputRTWidth   = m_BackbufferRes.width;
                    settings.inputRTHeight  = m_BackbufferRes.height;
                    settings.outputRTWidth  = viewData.outputRes.width;
                    settings.outputRTHeight = viewData.outputRes.height;
                    settings.quality = viewData.perfQuality;
                    m_UsingOptimalSettings = shouldUseOptimalSettings;
                    m_DlssContext = m_Device.CreateFeature(cmdBuffer, settings);
                }

                m_Data = viewData;
                m_Data.reset = isNew || viewData.reset;
            }

            public void SubmitDlssCommands(
                Texture source,
                Texture depth,
                Texture motionVectors,
                Texture biasColorMask,
                Texture output,
                float preExposure,
                CommandBuffer cmdBuffer)
            {
                if (m_DlssContext == null)
                    return;

                m_DlssContext.executeData.sharpness = m_UsingOptimalSettings ? m_OptimalSettingsRequest.optimalSettings.sharpness : m_Data.sharpness;
                m_DlssContext.executeData.mvScaleX = -((float)m_Data.inputRes.width);
                m_DlssContext.executeData.mvScaleY = -((float)m_Data.inputRes.height);
                m_DlssContext.executeData.subrectOffsetX = 0;
                m_DlssContext.executeData.subrectOffsetY = 0;
                m_DlssContext.executeData.subrectWidth = m_Data.inputRes.width;
                m_DlssContext.executeData.subrectHeight = m_Data.inputRes.height;
                m_DlssContext.executeData.jitterOffsetX = m_Data.jitterX;
                m_DlssContext.executeData.jitterOffsetY = m_Data.jitterY;
                m_DlssContext.executeData.preExposure = preExposure;
                m_DlssContext.executeData.invertYAxis = 1u;
                m_DlssContext.executeData.invertXAxis = 0u;
                m_DlssContext.executeData.reset = m_Data.reset ? 1 : 0;

                var textureTable = new NVIDIA.DLSSTextureTable()
                {
                    colorInput = source,
                    colorOutput = output,
                    depth = depth,
                    motionVectors = motionVectors,
                    biasColorMask = biasColorMask
                };

                m_Device.ExecuteDLSS(cmdBuffer, m_DlssContext, textureTable);
            }

            public void Cleanup(CommandBuffer cmdBuffer)
            {
                if (m_DlssContext != null)
                {
                    m_Device.DestroyFeature(cmdBuffer, m_DlssContext);
                    m_DlssContext = null;
                }

                m_Device = null;
                m_Data = new DlssViewData();
                m_UsingOptimalSettings = false;
                m_UseAutomaticSettings = false;
                m_BackbufferRes = new UpscalerResolution();
                m_OptimalSettingsRequest = new OptimalSettingsRequest();
            }
        }

        private class DLSSCamera
        {
            List<ViewState> m_Views = null;
            NVIDIA.GraphicsDevice m_Device = null;
            PerformDynamicRes m_ScaleDelegate = null;
            public PerformDynamicRes ScaleDelegate { get { return m_ScaleDelegate; } }
            public List<ViewState> ViewStates { get { return m_Views; } }

            public DLSSCamera()
            {
                m_ScaleDelegate = ScaleFn;
            }

            public void Init(NVIDIA.GraphicsDevice device)
            {
                m_Device = device;
            }

            public void ClearAutomaticSettings()
            {
                if (m_Views == null)
                    return;
                foreach (var v in m_Views)
                    v.ClearAutomaticSettings();
            }

            private float ScaleFn()
            {
                if (m_Views == null  || m_Views.Count == 0)
                    return 100.0f;

                var viewState = m_Views[0];
                if (!viewState.useAutomaticSettings)
                    return 100.0f;

                var optimalSettings = viewState.OptimalSettingsRequestData.optimalSettings;
                var targetViewport = viewState.OptimalSettingsRequestData.viewport;
                float suggestedPercentageX = (float)optimalSettings.outRenderWidth / targetViewport.width;
                float suggestedPercentageY = (float)optimalSettings.outRenderHeight / targetViewport.height;
                return Mathf.Min(suggestedPercentageX, suggestedPercentageY) * 100.0f;
            }

            public void SubmitCommands(
                HDCamera camera,
                float preExposure,
                in DlssViewData viewData,
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

                void RunPass(ViewState viewState, CommandBuffer cmdBuffer, in DlssViewData viewData, in UpscalerResources.ViewResources viewResources)
                {
                    viewState.UpdateViewState(viewData, cmdBuffer);
                    viewState.SubmitDlssCommands(
                        viewResources.source,
                        viewResources.depth,
                        viewResources.motionVectors,
                        viewResources.biasColorMask,
                        viewResources.output, preExposure, cmdBuffer);
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

        private void CleanupCameraStates()
        {
            Dictionary<int, UpscalerCameras.State> cameras = m_CameraStates.cameras;
            m_CommandBuffer.Clear();
            foreach (var kv in cameras)
            {
                var cameraState = kv.Value;
                if (!m_CameraStates.HasCameraStateExpired(cameraState) || cameraState.data == null)
                    continue;

                var dlssCamera = cameraState.data as DLSSCamera;
                dlssCamera.Cleanup(m_CommandBuffer);
                GenericPool<DLSSCamera>.Release(dlssCamera);
                cameraState.data = null;
            }
            Graphics.ExecuteCommandBuffer(m_CommandBuffer);
            m_CameraStates.CleanupCameraStates();
        }

        private void InternalNVIDIASetupDRSScaling(bool enableAutomaticSettings, Camera camera, in HDAdditionalCameraData hdCam, XRPass xrPass, ref GlobalDynamicResolutionSettings dynamicResolutionSettings)
        {
            if (m_Device == null)
                return;

            UpscalerCameras.State cameraState = m_CameraStates.GetState(camera);
            if (cameraState == null)
                return;

            var dlssCamera = cameraState.data as DLSSCamera;
            if (dlssCamera.ViewStates == null || dlssCamera.ViewStates.Count == 0)
                return;

            if (dlssCamera.ViewStates[0].DLSSContext == null)
                return;

            var usedQuality = dlssCamera.ViewStates[0].DLSSContext.initData.quality;
            Rect finalViewport = xrPass != null && xrPass.enabled ? xrPass.GetViewport() : new Rect(camera.pixelRect.x, camera.pixelRect.y, camera.pixelWidth, camera.pixelHeight);
            NVIDIA.OptimalDLSSSettingsData optimalSettings = new NVIDIA.OptimalDLSSSettingsData();
            m_Device.GetOptimalSettings((uint)finalViewport.width, (uint)finalViewport.height, usedQuality, out optimalSettings);

            foreach (var view in dlssCamera.ViewStates)
            {
                if (view == null)
                    continue;

                view.RequestUseAutomaticSettings(enableAutomaticSettings, usedQuality, finalViewport, optimalSettings);
            }

            if (enableAutomaticSettings)
            {
                if (IsOptimalSettingsValid(optimalSettings) && enableAutomaticSettings)
                {
                    dynamicResolutionSettings.maxPercentage = Mathf.Min((float)optimalSettings.maxWidth / finalViewport.width, (float)optimalSettings.maxHeight / finalViewport.height) * 100.0f;
                    dynamicResolutionSettings.minPercentage = Mathf.Max((float)optimalSettings.minWidth / finalViewport.width, (float)optimalSettings.minHeight / finalViewport.height) * 100.0f;
                    DynamicResolutionHandler.SetSystemDynamicResScaler(dlssCamera.ScaleDelegate, DynamicResScalePolicyType.ReturnsPercentage);
                    DynamicResolutionHandler.SetActiveDynamicScalerSlot(DynamicResScalerSlot.System);
                }
            }
            else
            {
                dlssCamera.ClearAutomaticSettings();
            }
        }

        private void InternalNVIDIABeginFrame(HDCamera hdCamera)
        {
            if (m_Device == null)
                return;

            m_CameraStates.ProcessExpiredCameras();

            UpscalerCameras.State cameraState = m_CameraStates.GetState(hdCamera.camera);
            var dlssCamera = cameraState != null ? cameraState.data as DLSSCamera : (DLSSCamera)null;

            bool dlssActive = hdCamera.IsDLSSEnabled();

            if (cameraState == null && dlssActive)
            {
                dlssCamera = GenericPool<DLSSCamera>.Get();
                dlssCamera.Init(m_Device);
                cameraState = m_CameraStates.CreateState(hdCamera.camera);
                cameraState.data = dlssCamera;
            }
            else if (cameraState != null && !dlssActive)
            {
                m_CameraStates.InvalidateState(cameraState);
            }

            if (cameraState != null)
                m_CameraStates.TagUsed(cameraState);

            CleanupCameraStates();
            m_CameraStates.NextFrame();
        }

        private void InternalNVIDIARender(in DLSSPass.Parameters parameters, UpscalerResources.CameraResources resources, CommandBuffer cmdBuffer)
        {
            if (m_Device == null || m_CameraStates.cameras.Count == 0)
                return;

            UpscalerCameras.State cameraState = m_CameraStates.GetState(parameters.hdCamera.camera);
            if (cameraState == null)
                return;

            DLSSCamera dlssCamera = cameraState.data as DLSSCamera;
            var dlssViewData = new DlssViewData();

            dlssViewData.perfQuality =
                (NVIDIA.DLSSQuality)(
                    parameters.hdCamera.deepLearningSuperSamplingUseCustomQualitySettings
                    ? parameters.hdCamera.deepLearningSuperSamplingQuality
                    : parameters.drsSettings.DLSSPerfQualitySetting);

            dlssViewData.sharpness = parameters.hdCamera.deepLearningSuperSamplingUseCustomAttributes
                ? parameters.hdCamera.deepLearningSuperSamplingSharpening
                : parameters.drsSettings.DLSSSharpness;

            dlssViewData.inputRes  = new UpscalerResolution() { width = (uint)parameters.hdCamera.actualWidth, height = (uint)parameters.hdCamera.actualHeight };
            dlssViewData.outputRes = new UpscalerResolution() { width = (uint)DynamicResolutionHandler.instance.finalViewport.x, height = (uint)DynamicResolutionHandler.instance.finalViewport.y };

            dlssViewData.jitterX = -parameters.hdCamera.taaJitter.x;
            dlssViewData.jitterY = -parameters.hdCamera.taaJitter.y;
            dlssViewData.reset = parameters.resetHistory;
            dlssCamera.SubmitCommands(parameters.hdCamera, parameters.preExposure, dlssViewData, resources, cmdBuffer);
        }

#endif
        #endregion
    }
}
