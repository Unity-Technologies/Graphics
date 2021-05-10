using System;
using System.Collections.Generic;

#if ENABLE_VR && ENABLE_XR_MODULE
using UnityEngine.XR;
#endif

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Used by render pipelines to communicate with XR SDK.
    /// </summary>
    public static class XRSystem
    {
        // Valid empty pass when a camera is not using XR
        public static readonly XRPass emptyPass = new XRPass();

        // Keep track of only one XR layout
        static XRLayout s_Layout = new XRLayout();

    #if ENABLE_VR && ENABLE_XR_MODULE
        static List<XRDisplaySubsystem> s_DisplayList = new List<XRDisplaySubsystem>();
        static XRDisplaySubsystem s_Display;
    #endif

        // MSAA level (number of samples per pixel) shared by all XR displays
        static int s_MsaaLevel = 1;

        // Internal resources used by XR rendering
        static Material s_OcclusionMeshMaterial;
        static Material s_MirrorViewMaterial;

        // Ability to override the default XR layout
        static Action<XRLayout, Camera> s_LayoutOverride = null;

        /// <summary>
        /// Returns true if a XR device is connected and running.
        /// </summary>
        static public bool displayActive
        {
        #if ENABLE_VR && ENABLE_XR_MODULE
            get => (s_Display != null) ? s_Display.running : false;
        #else
            get => false;
        #endif
        }

        /// <summary>
        /// If true, the system will try to create a layout compatible with single-pass rendering.
        /// </summary>
        static public bool singlePassAllowed { get; set; } = true;

        /// <summary>
        /// If true, the system will log some information about the layout to the console.
        /// </summary>
        static public bool dumpDebugInfo { get; set; } = false;

        /// <summary>
        /// Use this method to assign the shaders that will be used to render occlusion mesh for each XRPass and the final mirror view.
        /// </summary>
        /// <param name="occlusionMeshPS"></param>
        /// <param name="mirrorViewPS"></param>
        public static void Initialize(Shader occlusionMeshPS, Shader mirrorViewPS)
        {
            RefreshDeviceInfo();

            if (occlusionMeshPS != null)
                s_OcclusionMeshMaterial = CoreUtils.CreateEngineMaterial(occlusionMeshPS);

            if (mirrorViewPS != null)
                s_MirrorViewMaterial = CoreUtils.CreateEngineMaterial(mirrorViewPS);

            if (XRGraphicsAutomatedTests.enabled)
                SetLayoutOverride(XRGraphicsAutomatedTests.OverrideLayout);
        }

        /// <summary>
        /// Used by the render pipeline to communicate to the XR device how many samples are used by MSAA.
        /// </summary>
        /// <param name="msaaSampleCount"></param>
        public static void SetDisplayAntialiasing(int msaaSampleCount)
        {
            if (s_MsaaLevel == msaaSampleCount)
                return;

            s_MsaaLevel = Mathf.NextPowerOfTwo(msaaSampleCount);
            s_MsaaLevel = Mathf.Clamp(s_MsaaLevel, 1, 8); // hard-coded ??

        #if ENABLE_VR && ENABLE_XR_MODULE
            SubsystemManager.GetInstances(s_DisplayList);

            foreach (var display in s_DisplayList)
                display.SetMSAALevel(s_MsaaLevel);
        #endif
        }

        /// <summary>
        /// Returns the number of samples (MSAA) currently configured on the XR device.
        /// </summary>
        /// <returns></returns>
        public static int GetDisplayAntialiasing()
        {
            return s_MsaaLevel;
        }

        /// <summary>
        /// is this still useful ?
        /// </summary>
        /// <param name="renderScale"></param>
        public static void SetRenderScale(float renderScale)
        {
        #if ENABLE_VR && ENABLE_XR_MODULE
            SubsystemManager.GetInstances(s_DisplayList);

            foreach (var display in s_DisplayList)
                display.scaleOfAllRenderTargets = renderScale;
        #endif
        }

        /// <summary>
        /// Used by the render pipeline to communicate to the XR device the range of the depth buffer.
        /// </summary>
        /// <param name="zNear"></param>
        /// <param name="zFar"></param>
        public static void SetDisplayZRange(float zNear, float zFar)
        {
        #if ENABLE_VR && ENABLE_XR_MODULE
            if (s_Display != null)
            {
                s_Display.zNear = zNear;
                s_Display.zFar = zFar;
            }
        #endif
        }

        // Expose properties for finer control ?
        public static void SetDisplaySync()
        {
            // Disable vsync on the main display when rendering to a XR device.
            QualitySettings.vSyncCount = 0;

            // On Android and iOS, vSyncCount is ignored and all frame rate control is done using Application.targetFrameRate.
            if (Application.platform == RuntimePlatform.Android)
            {
                float frameRate = 300.0f;
                //frameRate = s_Display.TryGetDisplayRefreshRate(out float refreshRate) ? refreshRate : frameRate;

                // XRTODO : move out, causing issues with HDRP timing
                Application.targetFrameRate = Mathf.CeilToInt(frameRate);
            }
        }

        /// <summary>
        /// Used by the render pipeline to setup a new rendering frame trhough a XR layout.
        /// </summary>
        public static XRLayout NewFrame()
        {
            RefreshDeviceInfo();

            if (s_Layout.GetFramePasses().Count > 0)
            {
                Debug.LogWarning("Render Pipeline error : XRSystem.EndFrame() was not called!");
                EndFrame();
            }

            return s_Layout;
        }

        /// <summary>
        /// Used by the render pipeline to complete the XR layout at the end of the frame.
        /// </summary>
        public static void EndFrame()
        {
            if (dumpDebugInfo)
                s_Layout.LogDebugInfo();

            s_Layout.Clear();
        }

        /// <summary>
        /// Used by the render pipeline to render the mirror view to the gameview, as configured by the XR device.
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="camera"></param>
        public static void RenderMirrorView(CommandBuffer cmd, Camera camera)
        {
        #if ENABLE_VR && ENABLE_XR_MODULE
            XRMirrorView.RenderMirrorView(cmd, camera, s_MirrorViewMaterial, s_Display);
        #endif
        }

        /// <summary>
        /// Free up resources used by the system.
        /// </summary>
        public static void Dispose()
        {
            CoreUtils.Destroy(s_OcclusionMeshMaterial);
            CoreUtils.Destroy(s_MirrorViewMaterial);
        }

        // XRTODO: expose as public API
        static void SetLayoutOverride(Action<XRLayout, Camera> action)
        {
            s_LayoutOverride = action;
        }

        // Disable legacy VR system before rendering first frame
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        static void XRSystemInit()
        {
            if (GraphicsSettings.currentRenderPipeline != null)
            {
                RefreshDeviceInfo();
            }
        }

        static void RefreshDeviceInfo()
        {
        #if ENABLE_VR && ENABLE_XR_MODULE
            SubsystemManager.GetInstances(s_DisplayList);

            if (s_DisplayList.Count > 0)
            {
                if (s_DisplayList.Count > 1)
                    throw new NotImplementedException("Only one XR display is supported!");

                s_Display = s_DisplayList[0];
                s_Display.disableLegacyRenderer = true;
                s_Display.sRGB = QualitySettings.activeColorSpace == ColorSpace.Linear;

                // XRTODO : discuss this code and UI implications
                s_Display.textureLayout = XRDisplaySubsystem.TextureLayout.Texture2DArray;

                // XRTODO : replace by API from XR SDK, assume we have 2 views max for now
                TextureXR.maxViews = Math.Max(TextureXR.slices, 2);
            }
            else
            {
                s_Display = null;
            }
        #endif
        }

        internal static void CreateDefaultLayout(Camera camera)
        {
        #if ENABLE_VR && ENABLE_XR_MODULE
            if (s_Display == null)
                throw new NullReferenceException(nameof(s_Display));

            for (int renderPassIndex = 0; renderPassIndex < s_Display.GetRenderPassCount(); ++renderPassIndex)
            {
                s_Display.GetRenderPass(renderPassIndex, out var renderPass);
                s_Display.GetCullingParameters(camera, renderPass.cullingPassIndex, out var cullingParams);

                if (CanUseSinglePass(camera, renderPass))
                {
                    var xrPass = XRPass.Create(BuildPass(renderPass, cullingParams));

                    for (int renderParamIndex = 0; renderParamIndex < renderPass.GetRenderParameterCount(); ++renderParamIndex)
                    {
                        renderPass.GetRenderParameter(camera, renderParamIndex, out var renderParam);
                        xrPass.AddView(BuildView(renderPass, renderParam));
                    }

                    s_Layout.AddPass(camera, xrPass);
                }
                else
                {
                    for (int renderParamIndex = 0; renderParamIndex < renderPass.GetRenderParameterCount(); ++renderParamIndex)
                    {
                        renderPass.GetRenderParameter(camera, renderParamIndex, out var renderParam);

                        var xrPass = XRPass.Create(BuildPass(renderPass, cullingParams));
                        xrPass.AddView(BuildView(renderPass, renderParam));

                        s_Layout.AddPass(camera, xrPass);
                    }
                }
            }

            if (s_LayoutOverride != null)
                s_LayoutOverride.Invoke(s_Layout, camera);
        #endif
        }

        internal static void ReconfigurePass(XRPass xrPass, Camera camera)
        {
        #if ENABLE_VR && ENABLE_XR_MODULE
            if (xrPass.enabled && s_Display != null)
            {
                s_Display.GetRenderPass(xrPass.multipassId, out var renderPass);
                Debug.Assert(xrPass.singlePassEnabled || renderPass.GetRenderParameterCount() == 1);

                s_Display.GetCullingParameters(camera, renderPass.cullingPassIndex, out var cullingParams);
                xrPass.AssignCullingParams(renderPass.cullingPassIndex, cullingParams);

                for (int renderParamIndex = 0; renderParamIndex < renderPass.GetRenderParameterCount(); ++renderParamIndex)
                {
                    renderPass.GetRenderParameter(camera, renderParamIndex, out var renderParam);
                    xrPass.AssignView(renderParamIndex, BuildView(renderPass, renderParam));
                }

                if (s_LayoutOverride != null)
                    s_LayoutOverride.Invoke(s_Layout, camera);
            }
        #endif
        }

#if ENABLE_VR && ENABLE_XR_MODULE
        static bool CanUseSinglePass(Camera camera, XRDisplaySubsystem.XRRenderPass renderPass)
        {
            if (!singlePassAllowed)
                return false;

            if (renderPass.renderTargetDesc.dimension != TextureDimension.Tex2DArray)
                return false;

            if (renderPass.GetRenderParameterCount() != 2 || renderPass.renderTargetDesc.volumeDepth != 2)
                return false;

            renderPass.GetRenderParameter(camera, 0, out var renderParam0);
            renderPass.GetRenderParameter(camera, 1, out var renderParam1);

            if (renderParam0.textureArraySlice != 0 || renderParam1.textureArraySlice != 1)
                return false;

            if (renderParam0.viewport != renderParam1.viewport)
                return false;

            return true;
        }

        static XRView BuildView(XRDisplaySubsystem.XRRenderPass renderPass, XRDisplaySubsystem.XRRenderParameter renderParameter)
        {
            // Convert viewport from normalized to screen space
            Rect viewport = renderParameter.viewport;
            viewport.x      *= renderPass.renderTargetDesc.width;
            viewport.width  *= renderPass.renderTargetDesc.width;
            viewport.y      *= renderPass.renderTargetDesc.height;
            viewport.height *= renderPass.renderTargetDesc.height;

            // XRTODO: remove this line and use XRSettings.useOcclusionMesh instead when it's fixed
            Mesh occlusionMesh = XRGraphicsAutomatedTests.running ? null : renderParameter.occlusionMesh;

            return new XRView(renderParameter.projection, renderParameter.view, viewport, occlusionMesh, renderParameter.textureArraySlice);
        }

        static XRPassCreateInfo BuildPass(XRDisplaySubsystem.XRRenderPass xrRenderPass, ScriptableCullingParameters cullingParameters)
        {
            // We can't use descriptor directly because y-flip is forced
            // XRTODO: fix root problem
            RenderTextureDescriptor xrDesc = xrRenderPass.renderTargetDesc;
            RenderTextureDescriptor rtDesc = new RenderTextureDescriptor(xrDesc.width, xrDesc.height, xrDesc.colorFormat, xrDesc.depthBufferBits, xrDesc.mipCount);
            rtDesc.dimension    = xrRenderPass.renderTargetDesc.dimension;
            rtDesc.volumeDepth  = xrRenderPass.renderTargetDesc.volumeDepth;
            rtDesc.vrUsage      = xrRenderPass.renderTargetDesc.vrUsage;
            rtDesc.sRGB         = xrRenderPass.renderTargetDesc.sRGB;

            XRPassCreateInfo passInfo = new XRPassCreateInfo
            {
                renderTarget            = xrRenderPass.renderTarget,
                renderTargetDesc        = rtDesc,
                cullingParameters       = cullingParameters,
                occlusionMeshMaterial   = s_OcclusionMeshMaterial,
                multipassId             = s_Layout.GetFramePasses().Count,
                cullingPassId           = xrRenderPass.cullingPassIndex,
                copyDepth               = xrRenderPass.shouldFillOutDepth
            };

            return passInfo;
        }
#endif
    }
}
