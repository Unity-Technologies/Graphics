using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    internal class DLSSPass
    {
        #region Render Graph Helper

        public struct ViewResourceHandles
        {
            public TextureHandle source;
            public TextureHandle output;
            public TextureHandle depth;
            public TextureHandle motionVectors;
            public TextureHandle biasColorMask;
            public void WriteResources(RenderGraphBuilder builder)
            {
                source = builder.WriteTexture(source);
                output = builder.WriteTexture(output);
                depth = builder.WriteTexture(depth);
                motionVectors = builder.WriteTexture(motionVectors);

                if (biasColorMask.IsValid())
                    biasColorMask = builder.WriteTexture(biasColorMask);
            }
        }

        public struct CameraResourcesHandles
        {
            internal ViewResourceHandles resources;
            internal bool copyToViews;
            internal ViewResourceHandles tmpView0;
            internal ViewResourceHandles tmpView1;
        }

        public static ViewResources GetViewResources(in ViewResourceHandles handles)
        {
            var resources = new ViewResources
            {
                source = (Texture)handles.source,
                output = (Texture)handles.output,
                depth = (Texture)handles.depth,
                motionVectors = (Texture)handles.motionVectors
            };

            resources.biasColorMask = (handles.biasColorMask.IsValid()) ? (Texture)handles.biasColorMask : (Texture)null;

            return resources;
        }

        public static CameraResourcesHandles CreateCameraResources(HDCamera camera, RenderGraph renderGraph, RenderGraphBuilder builder, in ViewResourceHandles resources)
        {
            var camResources = new CameraResourcesHandles();
            camResources.resources = resources;
            camResources.copyToViews = camera.xr.enabled && camera.xr.singlePassEnabled && camera.xr.viewCount > 1;

            if (camResources.copyToViews)
            {
                TextureHandle GetTmpViewXrTex(in TextureHandle handle)
                {
                    if (!handle.IsValid())
                        return TextureHandle.nullHandle;

                    var newTexDesc = renderGraph.GetTextureDesc(handle);
                    newTexDesc.slices = 1;
                    newTexDesc.dimension = TextureDimension.Tex2D;
                    return renderGraph.CreateTexture(newTexDesc);
                }

                void CreateCopyNoXR(in ViewResourceHandles input, out ViewResourceHandles newResources)
                {
                    newResources.source = GetTmpViewXrTex(input.source);
                    newResources.output = GetTmpViewXrTex(input.output);
                    newResources.depth = GetTmpViewXrTex(input.depth);
                    newResources.motionVectors = GetTmpViewXrTex(input.motionVectors);
                    newResources.biasColorMask = GetTmpViewXrTex(input.biasColorMask);
                    newResources.WriteResources(builder);
                }

                CreateCopyNoXR(resources, out camResources.tmpView0);
                CreateCopyNoXR(resources, out camResources.tmpView1);
            }

            return camResources;
        }

        public static CameraResources GetCameraResources(in CameraResourcesHandles handles)
        {
            var camResources = new CameraResources
            {
                resources = GetViewResources(handles.resources),
                copyToViews = handles.copyToViews
            };

            if (camResources.copyToViews)
            {
                camResources.tmpView0 = GetViewResources(handles.tmpView0);
                camResources.tmpView1 = GetViewResources(handles.tmpView1);
            }

            return camResources;
        }

        #endregion

        #region public members, general engine code
        public struct Parameters
        {
            public HDCamera hdCamera;
            public GlobalDynamicResolutionSettings drsSettings;
        }

        public struct ViewResources
        {
            public Texture source;
            public Texture output;
            public Texture depth;
            public Texture motionVectors;
            public Texture biasColorMask;
        }

        public struct CameraResources
        {
            internal ViewResources resources;
            internal bool copyToViews;
            internal ViewResources tmpView0;
            internal ViewResources tmpView1;
        }

        public static bool SetupFeature(HDRenderPipelineGlobalSettings pipelineSettings = null)
        {
#if ENABLE_NVIDIA && ENABLE_NVIDIA_MODULE
            if (!NVIDIA.NVUnityPlugin.IsLoaded())
                return false;

            if (s_ExpectedDeviceVersion != NVIDIA.GraphicsDevice.version)
            {
                Debug.LogWarning("Cannot instantiate NVIDIA device because the version HDRP expects does not match the backend version.");
                return false;
            }

            if (!SystemInfo.graphicsDeviceVendor.ToLower().Contains("nvidia"))
                return false;

            NVIDIA.GraphicsDevice device = null;
            if (pipelineSettings != null && pipelineSettings.useDLSSCustomProjectId)
            {
                device = NVIDIA.GraphicsDevice.CreateGraphicsDevice(pipelineSettings.DLSSProjectId);
            }
            else
            {
                device = NVIDIA.GraphicsDevice.CreateGraphicsDevice();
            }

            if (device == null)
                return false;

            return device.IsFeatureAvailable(NVIDIA.GraphicsDeviceFeature.DLSS);
#else
            return false;
#endif
        }

        public static DLSSPass Create(HDRenderPipelineGlobalSettings pipelineSettings = null)
        {
            DLSSPass dlssPass = null;

#if ENABLE_NVIDIA && ENABLE_NVIDIA_MODULE
            if (!SetupFeature(pipelineSettings))
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

        public void SetupDRSScaling(bool enableAutomaticSettings, Camera camera, XRPass xrPass, ref GlobalDynamicResolutionSettings dynamicResolutionSettings)
        {
#if ENABLE_NVIDIA && ENABLE_NVIDIA_MODULE
            InternalNVIDIASetupDRSScaling(enableAutomaticSettings, camera, xrPass, ref dynamicResolutionSettings);
#endif
        }

        public void Render(
            DLSSPass.Parameters parameters,
            DLSSPass.CameraResources resources,
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

        private Dictionary<CameraKey, DLSSPass.CameraState> m_CameraStates = new Dictionary<CameraKey, DLSSPass.CameraState>();
        private List<CameraKey> m_InvalidCameraKeys = new List<CameraKey>();

        private CommandBuffer m_CommandBuffer = new CommandBuffer();
        private UInt64 m_FrameId = 0;

        private NVIDIA.GraphicsDevice m_Device = null;

        private DLSSPass(NVIDIA.GraphicsDevice device)
        {
            m_Device = device;
        }

        //Amount of inactive frames dlss has rendered before we clean / destroy the plugin state.
        private static UInt64 sMaximumFrameExpiration = 400;

        private struct CameraKey
        {
            private int m_HashCode;
            public CameraKey(Camera camera)
            {
                m_HashCode = camera.GetInstanceID();
            }

            public override int GetHashCode()
            {
                return m_HashCode;
            }

            public override bool Equals(object obj)
            {
                if (obj.GetType() == typeof(CameraKey))
                    return GetHashCode() == ((CameraKey)obj).GetHashCode();

                return false;
            }
        }

        private struct Resolution
        {
            public uint width;
            public uint height;

            public static bool operator==(Resolution a, Resolution b) =>
                a.width == b.width && a.height == b.height;

            public static bool operator!=(Resolution a, Resolution b) =>
                !(a == b);
            public override bool Equals(object obj)
            {
                if (obj is Resolution)
                    return (Resolution)obj == this;
                return false;
            }

            public override int GetHashCode()
            {
                return (int)(width ^ height);
            }
        }

        private struct DlssViewData
        {
            public NVIDIA.DLSSQuality  perfQuality;
            public DLSSPass.Resolution inputRes;
            public DLSSPass.Resolution outputRes;
            public float sharpness;
            public float jitterX;
            public float jitterY;
            public bool reset;
            public bool CanFitInput(in DLSSPass.Resolution inputRect)
            {
                return inputRes.width >= inputRect.width && inputRes.height > inputRect.height;
            }
        }

        private struct OptimalSettingsRequest
        {
            public NVIDIA.DLSSQuality quality;
            public Rect viewport;
            public NVIDIA.OptimalDLSSSettingsData optimalSettings;
            public bool CanFit(DLSSPass.Resolution rect)
            {
                return rect.width >= optimalSettings.minWidth && rect.height >= optimalSettings.minHeight
                    && rect.width <= optimalSettings.maxWidth && rect.height <= optimalSettings.maxHeight;
            }
        }

        private static bool IsOptimalSettingsValid(in NVIDIA.OptimalDLSSSettingsData optimalSettings)
        {
            return optimalSettings.maxHeight > optimalSettings.minHeight
                && optimalSettings.maxWidth  > optimalSettings.minWidth
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
            private DLSSPass.Resolution m_BackbufferRes;
            private OptimalSettingsRequest m_OptimalSettingsRequest = new OptimalSettingsRequest();

            public NVIDIA.DLSSContext DLSSContext { get { return m_DlssContext; } }
            public bool useAutomaticSettings { get { return m_UseAutomaticSettings; } }
            public OptimalSettingsRequest OptimalSettingsRequestData { get { return m_OptimalSettingsRequest; } }

            public ViewState(NVIDIA.GraphicsDevice device)
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
                m_DlssContext.executeData.preExposure = 1.0f;
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
            }
        }

        private class CameraState
        {
            WeakReference<Camera> m_CamReference = null;
            ViewState[] m_Views = null;
            NVIDIA.GraphicsDevice m_Device = null;

            public ViewState[] ViewStates { get { return m_Views; } }
            public UInt64 LastFrameId { set; get; }

            public CameraState(NVIDIA.GraphicsDevice device, Camera camera)
            {
                m_CamReference = new WeakReference<Camera>(camera);
                m_Device = device;
            }

            public bool IsAlive()
            {
                return m_CamReference.TryGetTarget(out _);
            }

            public void ClearAutomaticSettings()
            {
                if (m_Views == null)
                    return;
                foreach (var v in m_Views)
                    v.ClearAutomaticSettings();
            }

            public void SubmitCommands(
                HDCamera camera,
                in DlssViewData viewData,
                in CameraResources camResources,
                CommandBuffer cmdBuffer)
            {
                int cameraViewCount = 1;
                int activeViewId = 0;
                if (camera.xr.enabled)
                {
                    cameraViewCount = camera.xr.singlePassEnabled ? camera.xr.viewCount : 2;
                    activeViewId = camera.xr.multipassId;
                }

                if (m_Views == null || m_Views.Length != cameraViewCount)
                {
                    if (m_Views != null)
                        Cleanup(cmdBuffer);

                    m_Views = new ViewState[cameraViewCount];
                    for (int viewId = 0; viewId < m_Views.Length; ++viewId)
                        m_Views[viewId] = new ViewState(m_Device);
                }

                void RunPass(ViewState viewState, CommandBuffer cmdBuffer, in DlssViewData viewData, in ViewResources viewResources)
                {
                    viewState.UpdateViewState(viewData, cmdBuffer);
                    viewState.SubmitDlssCommands(
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
                    for (int viewId = 0; viewId < m_Views.Length; ++viewId)
                    {
                        ViewState viewState = m_Views[viewId];
                        ViewResources tmpResources = viewId == 0 ? camResources.tmpView0 : camResources.tmpView1;

                        cmdBuffer.CopyTexture(camResources.resources.source, viewId, tmpResources.source, 0);
                        cmdBuffer.CopyTexture(camResources.resources.depth, viewId, tmpResources.depth, 0);
                        cmdBuffer.CopyTexture(camResources.resources.motionVectors, viewId, tmpResources.motionVectors, 0);

                        if (camResources.resources.biasColorMask != null)
                            cmdBuffer.CopyTexture(camResources.resources.biasColorMask, viewId, tmpResources.biasColorMask, 0);
                    }

                    for (int viewId = 0; viewId < m_Views.Length; ++viewId)
                    {
                        ViewState viewState = m_Views[viewId];
                        ViewResources tmpResources = viewId == 0 ? camResources.tmpView0 : camResources.tmpView1;
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
                    v.Cleanup(cmdBuffer);

                m_Views = null;
            }
        }

        private bool HasCameraStateExpired(CameraState cameraState)
        {
            return (m_FrameId - cameraState.LastFrameId) >= sMaximumFrameExpiration;
        }

        private void ProcessInvalidCameras()
        {
            foreach (KeyValuePair<CameraKey, CameraState> kv in m_CameraStates)
            {
                if (kv.Value.IsAlive() && !HasCameraStateExpired(kv.Value))
                    continue;

                m_InvalidCameraKeys.Add(kv.Key);
            }
        }

        private void CleanupCameraStates()
        {
            if (m_InvalidCameraKeys.Count == 0)
                return;

            m_CommandBuffer.Clear();
            foreach (var invalidKey in m_InvalidCameraKeys)
            {
                if (!m_CameraStates.TryGetValue(invalidKey, out var cameraState))
                    continue;

                cameraState.Cleanup(m_CommandBuffer);
                m_CameraStates.Remove(invalidKey);
            }
            Graphics.ExecuteCommandBuffer(m_CommandBuffer);
            m_InvalidCameraKeys.Clear();
        }

        private static CameraKey m_SelectedCameraKey;
        private float ScaleFn()
        {
            CameraState cameraState = null;
            if (!m_CameraStates.TryGetValue(m_SelectedCameraKey, out cameraState))
                return 100.0f;

            if (cameraState.ViewStates == null || cameraState.ViewStates.Length == 0)
                return 100.0f;

            var viewState = cameraState.ViewStates[0];
            if (!viewState.useAutomaticSettings)
                return 100.0f;

            var optimalSettings = viewState.OptimalSettingsRequestData.optimalSettings;
            var targetViewport = viewState.OptimalSettingsRequestData.viewport;
            float suggestedPercentageX = (float)optimalSettings.outRenderWidth / targetViewport.width;
            float suggestedPercentageY = (float)optimalSettings.outRenderHeight / targetViewport.height;
            return Mathf.Min(suggestedPercentageX, suggestedPercentageY) * 100.0f;
        }

        private void InternalNVIDIASetupDRSScaling(bool enableAutomaticSettings, Camera camera, XRPass xrPass, ref GlobalDynamicResolutionSettings dynamicResolutionSettings)
        {
            if (m_Device == null)
                return;

            var cameraKey = new CameraKey(camera);
            CameraState cameraState = null;
            if (!m_CameraStates.TryGetValue(cameraKey, out cameraState))
                return;

            if (cameraState.ViewStates == null || cameraState.ViewStates.Length == 0)
                return;

            if (cameraState.ViewStates[0].DLSSContext == null)
                return;

            var usedQuality = cameraState.ViewStates[0].DLSSContext.initData.quality;
            Rect finalViewport = xrPass != null && xrPass.enabled ? xrPass.GetViewport() : new Rect(camera.pixelRect.x, camera.pixelRect.y, camera.pixelWidth, camera.pixelHeight);
            NVIDIA.OptimalDLSSSettingsData optimalSettings = new NVIDIA.OptimalDLSSSettingsData();
            m_Device.GetOptimalSettings((uint)finalViewport.width, (uint)finalViewport.height, usedQuality, out optimalSettings);

            foreach (var view in cameraState.ViewStates)
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
                    m_SelectedCameraKey = cameraKey;
                    DynamicResolutionHandler.SetSystemDynamicResScaler(ScaleFn, DynamicResScalePolicyType.ReturnsPercentage);
                    DynamicResolutionHandler.SetActiveDynamicScalerSlot(DynamicResScalerSlot.System);
                }
            }
            else
            {
                cameraState.ClearAutomaticSettings();
            }
        }

        private void InternalNVIDIABeginFrame(HDCamera hdCamera)
        {
            if (m_Device == null)
                return;

            ProcessInvalidCameras();

            var cameraKey = new CameraKey(hdCamera.camera);
            CameraState cameraState = null;
            m_CameraStates.TryGetValue(cameraKey, out cameraState);
            bool dlssActive = hdCamera.IsDLSSEnabled();

            if (cameraState == null && dlssActive)
            {
                cameraState = new DLSSPass.CameraState(m_Device, hdCamera.camera);
                m_CameraStates.Add(cameraKey, cameraState);
            }
            else if (cameraState != null && !dlssActive)
            {
                m_InvalidCameraKeys.Add(cameraKey);
            }

            if (cameraState != null)
                cameraState.LastFrameId = m_FrameId;

            CleanupCameraStates();
            ++m_FrameId;
        }

        private void InternalNVIDIARender(in DLSSPass.Parameters parameters, DLSSPass.CameraResources resources, CommandBuffer cmdBuffer)
        {
            if (m_Device == null || m_CameraStates.Count == 0)
                return;

            if (!m_CameraStates.TryGetValue(new CameraKey(parameters.hdCamera.camera), out var cameraState))
                return;

            var dlssViewData = new DlssViewData();

            dlssViewData.perfQuality =
                (NVIDIA.DLSSQuality)(
                    parameters.hdCamera.deepLearningSuperSamplingUseCustomQualitySettings
                    ? parameters.hdCamera.deepLearningSuperSamplingQuality
                    : parameters.drsSettings.DLSSPerfQualitySetting);

            dlssViewData.sharpness = parameters.hdCamera.deepLearningSuperSamplingUseCustomAttributes
                ? parameters.hdCamera.deepLearningSuperSamplingSharpening
                : parameters.drsSettings.DLSSSharpness;

            dlssViewData.inputRes  = new Resolution() { width = (uint)parameters.hdCamera.actualWidth, height = (uint)parameters.hdCamera.actualHeight };
            dlssViewData.outputRes = new Resolution() { width = (uint)DynamicResolutionHandler.instance.finalViewport.x, height = (uint)DynamicResolutionHandler.instance.finalViewport.y };

            dlssViewData.jitterX = -parameters.hdCamera.taaJitter.x;
            dlssViewData.jitterY = -parameters.hdCamera.taaJitter.y;
            dlssViewData.reset = parameters.hdCamera.isFirstFrame;
            cameraState.SubmitCommands(parameters.hdCamera, dlssViewData, resources, cmdBuffer);
        }

#endif
        #endregion
    }
}
