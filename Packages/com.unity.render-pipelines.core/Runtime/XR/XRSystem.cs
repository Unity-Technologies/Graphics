using System;
using System.Collections.Generic;
using UnityEngine.Rendering;

#if ENABLE_VR && ENABLE_XR_MODULE
using UnityEngine.XR;
#endif

namespace UnityEngine.Experimental.Rendering
{
    /// <summary>
    /// Used by render pipelines to communicate with XR SDK.
    /// </summary>
    public static class XRSystem
    {
        // Keep track of only one XR layout
        static XRLayout s_Layout = new XRLayout();

        // Delegate allocations of XRPass to the render pipeline
        static Func<XRPassCreateInfo, XRPass> s_PassAllocator = null;

#if ENABLE_VR && ENABLE_XR_MODULE
        static List<XRDisplaySubsystem> s_DisplayList = new List<XRDisplaySubsystem>();
        static XRDisplaySubsystem s_Display;

        /// <summary>
        /// Returns the active XR display.
        /// </summary>
        static public XRDisplaySubsystem GetActiveDisplay()
        {
            return s_Display;
        }
#endif

        // MSAA level (number of samples per pixel) shared by all XR displays
        static MSAASamples s_MSAASamples = MSAASamples.None;

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
        /// Returns if the XR display is running in HDR mode.
        /// </summary>
        static public bool isHDRDisplayOutputActive
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            get => s_Display?.hdrOutputSettings?.active ?? false;
#else
            get => false;
#endif
        }

        /// <summary>
        /// Valid empty pass when a camera is not using XR.
        /// </summary>
        public static readonly XRPass emptyPass = new XRPass();

        /// <summary>
        /// If true, the system will try to create a layout compatible with single-pass rendering.
        /// </summary>
        static public bool singlePassAllowed { get; set; } = true;

        /// <summary>
        /// Cached value of SystemInfo.foveatedRenderingCaps.
        /// </summary>
        static public FoveatedRenderingCaps foveatedRenderingCaps { get; set; }

        /// <summary>
        /// If true, the system will log some information about the layout to the console.
        /// </summary>
        static public bool dumpDebugInfo { get; set; } = false;

        /// <summary>
        /// Use this method to assign the shaders that will be used to render occlusion mesh for each XRPass and the final mirror view.
        /// </summary>
        /// <param name="passAllocator"></param>
        /// <param name="occlusionMeshPS"></param>
        /// <param name="mirrorViewPS"></param>
        public static void Initialize(Func<XRPassCreateInfo, XRPass> passAllocator, Shader occlusionMeshPS, Shader mirrorViewPS)
        {
            if (passAllocator == null)
                throw new ArgumentNullException("passCreator");

            s_PassAllocator = passAllocator;

            RefreshDeviceInfo();

            foveatedRenderingCaps = SystemInfo.foveatedRenderingCaps;

            if (occlusionMeshPS != null && s_OcclusionMeshMaterial == null)
                s_OcclusionMeshMaterial = CoreUtils.CreateEngineMaterial(occlusionMeshPS);

            if (mirrorViewPS != null && s_MirrorViewMaterial == null)
                s_MirrorViewMaterial = CoreUtils.CreateEngineMaterial(mirrorViewPS);

            if (XRGraphicsAutomatedTests.enabled)
                SetLayoutOverride(XRGraphicsAutomatedTests.OverrideLayout);
        }

        /// <summary>
        /// Used by the render pipeline to communicate to the XR device how many samples are used by MSAA.
        /// </summary>
        /// <param name="msaaSamples"></param>
        public static void SetDisplayMSAASamples(MSAASamples msaaSamples)
        {
            if (s_MSAASamples == msaaSamples)
                return;

            s_MSAASamples = msaaSamples;

#if ENABLE_VR && ENABLE_XR_MODULE
            SubsystemManager.GetInstances(s_DisplayList);

            foreach (var display in s_DisplayList)
                display.SetMSAALevel((int)s_MSAASamples);
#endif
        }

        /// <summary>
        /// Returns the number of samples (MSAA) currently configured on the XR device.
        /// </summary>
        /// <returns></returns>
        public static MSAASamples GetDisplayMSAASamples()
        {
            return s_MSAASamples;
        }

        /// <summary>
        /// Used by the render pipeline to scale the render target on the XR device.
        /// </summary>
        /// <param name="renderScale">A value of 1.0f represents 100% of the original resolution.</param>
        public static void SetRenderScale(float renderScale)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            SubsystemManager.GetInstances(s_DisplayList);

            foreach (var display in s_DisplayList)
                display.scaleOfAllRenderTargets = renderScale;
#endif
        }

        /// <summary>
        /// Used by the render pipeline to initiate a new rendering frame through a XR layout.
        /// </summary>
        /// <returns></returns>
        public static XRLayout NewLayout()
        {
            RefreshDeviceInfo();

            if (s_Layout.GetActivePasses().Count > 0)
            {
                Debug.LogWarning("Render Pipeline error : the XR layout still contains active passes. Executing XRSystem.EndLayout() right now.");
                EndLayout();
            }

            return s_Layout;
        }

        /// <summary>
        /// Used by the render pipeline to complete the XR layout at the end of the frame.
        /// </summary>
        public static void EndLayout()
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
            if (s_OcclusionMeshMaterial != null)
            {
                CoreUtils.Destroy(s_OcclusionMeshMaterial);
                s_OcclusionMeshMaterial = null;
            }

            if (s_MirrorViewMaterial != null)
            {
                CoreUtils.Destroy(s_MirrorViewMaterial);
                s_MirrorViewMaterial = null;
            }
        }

        // Used by the render pipeline to communicate to the XR device the range of the depth buffer.
        internal static void SetDisplayZRange(float zNear, float zFar)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            if (s_Display != null)
            {
                s_Display.zNear = zNear;
                s_Display.zFar = zFar;
            }
#endif
        }

        // XRTODO : expose as public API
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

        // Setup the layout to use multi-pass or single-pass based on the runtime caps
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
                    var xrPass = s_PassAllocator(BuildPass(renderPass, cullingParams));

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

                        var xrPass = s_PassAllocator(BuildPass(renderPass, cullingParams));
                        xrPass.AddView(BuildView(renderPass, renderParam));

                        s_Layout.AddPass(camera, xrPass);
                    }
                }
            }

            if (s_LayoutOverride != null)
                s_LayoutOverride.Invoke(s_Layout, camera);
#endif
        }

        // Update the parameters of one pass with a different camera
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

            // XRTODO : remove this line and use XRSettings.useOcclusionMesh instead when it's fixed
            Mesh occlusionMesh = XRGraphicsAutomatedTests.running ? null : renderParameter.occlusionMesh;

            return new XRView(renderParameter.projection, renderParameter.view, viewport, occlusionMesh, renderParameter.textureArraySlice);
        }

        static XRPassCreateInfo BuildPass(XRDisplaySubsystem.XRRenderPass xrRenderPass, ScriptableCullingParameters cullingParameters)
        {
            // We can't use descriptor directly because y-flip is forced
            // XRTODO : fix root problem
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
                foveatedRenderingInfo   = xrRenderPass.foveatedRenderingInfo,
                multipassId             = s_Layout.GetActivePasses().Count,
                cullingPassId           = xrRenderPass.cullingPassIndex,
                copyDepth               = xrRenderPass.shouldFillOutDepth,
                xrSdkRenderPass         = xrRenderPass
            };

            return passInfo;
        }

#endif
    }
}
