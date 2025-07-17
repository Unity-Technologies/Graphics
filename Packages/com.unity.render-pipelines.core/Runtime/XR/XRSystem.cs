using System;
using System.Collections.Generic;
using UnityEngine.Rendering;

#if ENABLE_VR && ENABLE_XR_MODULE
using UnityEngine.XR;
#endif

namespace UnityEngine.Experimental.Rendering
{
    /// <summary>
    /// Used by render pipelines to control the active XR shader variant.
    /// </summary>
    public static class SinglepassKeywords
    {
        /// <summary> XR shader keyword used by multiview rendering </summary>
        public static GlobalKeyword STEREO_MULTIVIEW_ON;
        /// <summary> XR shader keywordused by single pass instanced rendering </summary>
        public static GlobalKeyword STEREO_INSTANCING_ON;
    }

    /// <summary>
    /// Used by render pipelines to communicate with XR SDK.
    /// </summary>
    public static class XRSystem
    {
        // Keep track of only one XR layout
        static XRLayoutStack s_Layout = new ();

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

#if ENABLE_VR && ENABLE_XR_MODULE
        // Occlusion Mesh scaling factor
        static float s_OcclusionMeshScaling = 1.0f;

        // Return true if wants to enable visibility mesh passes
        static bool s_UseVisibilityMesh = true;
#endif

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
        /// <param name="passAllocator"> Delegate funcion used to allocate XRPasses. </param>
        /// <param name="occlusionMeshPS"> Fragement shader used for rendering occlusion mesh. </param>
        /// <param name="mirrorViewPS"> Fragement shader used for rendering mirror view. </param>
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

            SinglepassKeywords.STEREO_MULTIVIEW_ON = GlobalKeyword.Create("STEREO_MULTIVIEW_ON");
            SinglepassKeywords.STEREO_INSTANCING_ON = GlobalKeyword.Create("STEREO_INSTANCING_ON");
        }

        /// <summary>
        /// Used by the render pipeline to communicate to the XR device how many samples are used by MSAA.
        /// </summary>
        /// <param name="msaaSamples"> The active msaa samples the XRDisplay to set to. The eye texture surfaces are reallocated when necessary to work with the active msaa samples. </param>
        public static void SetDisplayMSAASamples(MSAASamples msaaSamples)
        {
            if (s_MSAASamples == msaaSamples)
                return;

            s_MSAASamples = msaaSamples;

#if ENABLE_VR && ENABLE_XR_MODULE
            SubsystemManager.GetSubsystems(s_DisplayList);

            foreach (var display in s_DisplayList)
                display.SetMSAALevel((int)s_MSAASamples);
#endif
        }

        /// <summary>
        /// Returns the number of samples (MSAA) currently configured on the XR device.
        /// </summary>
        /// <returns> Returns current active msaa samples. </returns>
        public static MSAASamples GetDisplayMSAASamples()
        {
            return s_MSAASamples;
        }

        /// <summary>
        /// Used by the render pipeline to scale all occlusion meshes used by all XRPasses.
        /// </summary>
        /// <param name="occlusionMeshScale">A value of 1.0f represents 100% of the original mesh size. A value less or equal to 0.0f disables occlusion mesh draw. </param>
        internal static void SetOcclusionMeshScale(float occlusionMeshScale)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            s_OcclusionMeshScaling = occlusionMeshScale;
#endif
        }

        /// <summary>
        /// Returned value used by the render pipeline to scale all occlusion meshes used by all XRPasses.
        /// </summary>
        internal static float GetOcclusionMeshScale()
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            return s_OcclusionMeshScaling;
#else
            return 1.0f;
#endif
        }

        /// <summary>
        /// Used by the render pipeline to enable all visibility meshes passes.
        /// </summary>
        /// <param name="useVisibilityMesh">True to enable visibility mesh passes, false to disable them. </param>
        internal static void SetUseVisibilityMesh(bool useVisibilityMesh)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            s_UseVisibilityMesh = useVisibilityMesh;
#endif
        }

        /// <summary>
        /// Returned value used by the render pipeline to use all visibility mesh passes.
        /// </summary>
        internal static bool GetUseVisibilityMesh()
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            return s_UseVisibilityMesh;
#else
            return false;
#endif
        }

        /// <summary>
        /// Used to communicate to the XR device how to render the XR MirrorView. Note: not all blit modes are supported by all providers. Blitmode set here serves as preference purpose.
        /// </summary>
        /// <param name="mirrorBlitMode"> Mirror view mode to be set as preferred. See `XRMirrorViewBlitMode` for the builtin blit modes. </param>
        internal static void SetMirrorViewMode(int mirrorBlitMode)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            if (s_Display == null)
                return;

            s_Display.SetPreferredMirrorBlitMode(mirrorBlitMode);
#endif
        }

        /// <summary>
        /// Get current blit modes preferred by XRDisplay
        /// </summary>
        internal static int GetMirrorViewMode()
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            if (s_Display == null)
                return XRMirrorViewBlitMode.None;

            return s_Display.GetPreferredMirrorBlitMode();
#else
            return 0;
#endif
        }

        /// <summary>
        /// Used by the render pipeline to scale the render target on the XR device.
        /// </summary>
        /// <param name="renderScale">A value of 1.0f represents 100% of the original resolution.</param>
        public static void SetRenderScale(float renderScale)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            SubsystemManager.GetSubsystems(s_DisplayList);

            foreach (var display in s_DisplayList)
                display.scaleOfAllRenderTargets = renderScale;
#endif
        }


        /// <summary>
        /// Used by the render pipeline to retrieve the renderViewportScale value from the XR display.
        /// One use case for retriving this value is that render pipeline can properly sync some SRP owned textures to scale accordingly
        /// </summary>
        /// <returns> Returns current scaleOfAllViewports value from the XRDisplaySubsystem. </returns>
        public static float GetRenderViewportScale()
        {
#if ENABLE_VR && ENABLE_XR_MODULE

            return s_Display.scaleOfAllViewports;
#else
            return 1.0f;
#endif
        }

        /// <summary>
        /// Used by the render pipeline to retrieve the DynamicResolutionScale value from the XR display.
        /// One use case for retrieving this value is that render pipeline can properly sync some SRP owned textures to scale accordingly
        /// </summary>
        /// <returns> Returns current DynamicResolutionScale value from the XRDisplaySubsystem. </returns>
        public static float GetDynamicResolutionScale()
        {
#if ENABLE_VR && ENABLE_XR_MODULE

            return s_Display.globalDynamicScale;
#else
            return 1.0f;
#endif
        }

        /// <summary>
        /// Used by the render pipeline to calculate texture scaled width for XR display if it supports dynamic resolution
        /// </summary>
        /// <param name="texture">Input texture that supports dynamic resolution</param>
        /// <returns> Returns current scaled width of the input texture. </returns>
        public static int ScaleTextureWidthForXR(RenderTexture texture)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            return s_Display.ScaledTextureWidth(texture);
#else
            return 1;
#endif
        }

        /// <summary>
        /// Used by the render pipeline to calculate texture scaled height for XR display if it supports dynamic resolution
        /// </summary>
        /// <param name="texture">Input texture that supports dynamic resolution</param>
        /// <returns> Returns current scaled width of the input texture. </returns>
        public static int ScaleTextureHeightForXR(RenderTexture texture)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            return s_Display.ScaledTextureHeight(texture);
#else
            return 1;
#endif
        }

        /// <summary>
        /// Used by the render pipeline to initiate a new rendering frame through a XR layout.
        /// </summary>
        /// <returns> Returns a new default layout. </returns>
        public static XRLayout NewLayout()
        {
            RefreshDeviceInfo();
            return s_Layout.New();
        }

        /// <summary>
        /// Used by the render pipeline to complete the XR layout at the end of the frame.
        /// </summary>
        public static void EndLayout()
        {
            if (dumpDebugInfo)
                s_Layout.top.LogDebugInfo();

            s_Layout.Release();
        }

        /// <summary>
        /// Used by the render pipeline to render the mirror view to the gameview, as configured by the XR device.
        /// </summary>
        /// <param name="cmd"> CommandBuffer on which to perform the mirror view draw. </param>
        /// <param name="camera"> Camera that has XR device connected to. The connected XR device determines how to perform the mirror view draw. </param>
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
            SubsystemManager.GetSubsystems(s_DisplayList);

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
        internal static void CreateDefaultLayout(Camera camera, XRLayout layout)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            if (s_Display == null)
                throw new NullReferenceException(nameof(s_Display));

            void AddViewToPass(XRPass xrPass, XRDisplaySubsystem.XRRenderPass renderPass, int renderParamIndex)
            {
                renderPass.GetRenderParameter(camera, renderParamIndex, out var renderParam);
                xrPass.AddView(BuildView(renderPass, renderParam));
            }

            for (int renderPassIndex = 0; renderPassIndex < s_Display.GetRenderPassCount(); ++renderPassIndex)
            {
                s_Display.GetRenderPass(renderPassIndex, out var renderPass);
                s_Display.GetCullingParameters(camera, renderPass.cullingPassIndex, out var cullingParams);

                int renderParameterCount = renderPass.GetRenderParameterCount();
                if (CanUseSinglePass(camera, renderPass))
                {
                    var createInfo = BuildPass(renderPass, cullingParams, layout);
                    var xrPass = s_PassAllocator(createInfo);

                    for (int renderParamIndex = 0; renderParamIndex < renderParameterCount; ++renderParamIndex)
                    {
                        AddViewToPass(xrPass, renderPass, renderParamIndex);
                    }

                    layout.AddPass(camera, xrPass);
                }
                else
                {
                    for (int renderParamIndex = 0; renderParamIndex < renderParameterCount; ++renderParamIndex)
                    {
                        var createInfo = BuildPass(renderPass, cullingParams, layout);
                        var xrPass = s_PassAllocator(createInfo);
                        AddViewToPass(xrPass, renderPass, renderParamIndex);
                        layout.AddPass(camera, xrPass);
                    }
                }
            }

            s_LayoutOverride?.Invoke(layout, camera);
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

                s_LayoutOverride?.Invoke(s_Layout.top, camera);
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
            
            viewport.x      *= renderPass.renderTargetScaledWidth;
            viewport.width  *= renderPass.renderTargetScaledWidth;
            viewport.y      *= renderPass.renderTargetScaledHeight;
            viewport.height *= renderPass.renderTargetScaledHeight;

            // XRTODO : remove this line and use XRSettings.useOcclusionMesh instead when it's fixed
            Mesh occlusionMesh = XRGraphicsAutomatedTests.running ? null : renderParameter.occlusionMesh;
            Mesh visibleMesh = XRGraphicsAutomatedTests.running ? null : renderParameter.visibleMesh;

            return new XRView(renderParameter.projection, renderParameter.view, renderParameter.previousView, renderParameter.isPreviousViewValid, viewport, occlusionMesh, visibleMesh, renderParameter.textureArraySlice);
        }

        private static RenderTextureDescriptor XrRenderTextureDescToUnityRenderTextureDesc(RenderTextureDescriptor xrDesc)
        {
            // We can't use descriptor directly because y-flip is forced
            // XRTODO : fix root problem
            RenderTextureDescriptor rtDesc = new RenderTextureDescriptor(xrDesc.width, xrDesc.height, xrDesc.graphicsFormat, xrDesc.depthStencilFormat, xrDesc.mipCount);
            rtDesc.dimension    = xrDesc.dimension;
            rtDesc.msaaSamples  = xrDesc.msaaSamples;
            rtDesc.volumeDepth  = xrDesc.volumeDepth;
            rtDesc.vrUsage      = xrDesc.vrUsage;
            rtDesc.sRGB         = xrDesc.sRGB;
            rtDesc.shadowSamplingMode = xrDesc.shadowSamplingMode;
            return rtDesc;
        }

        static XRPassCreateInfo BuildPass(XRDisplaySubsystem.XRRenderPass xrRenderPass, ScriptableCullingParameters cullingParameters, XRLayout layout)
        {    
            XRPassCreateInfo passInfo = new XRPassCreateInfo
            {
                renderTarget            = xrRenderPass.renderTarget,
                renderTargetDesc        = XrRenderTextureDescToUnityRenderTextureDesc(xrRenderPass.renderTargetDesc),
                renderTargetScaledWidth = xrRenderPass.renderTargetScaledWidth,
                renderTargetScaledHeight = xrRenderPass.renderTargetScaledHeight,
                hasMotionVectorPass      = xrRenderPass.hasMotionVectorPass,
                motionVectorRenderTarget = xrRenderPass.motionVectorRenderTarget,
                motionVectorRenderTargetDesc = XrRenderTextureDescToUnityRenderTextureDesc(xrRenderPass.motionVectorRenderTargetDesc),
                cullingParameters       = cullingParameters,
                occlusionMeshMaterial   = s_OcclusionMeshMaterial,
                occlusionMeshScale      = GetOcclusionMeshScale(),
                foveatedRenderingInfo   = xrRenderPass.foveatedRenderingInfo,
                multipassId             = layout.GetActivePasses().Count,
                cullingPassId           = xrRenderPass.cullingPassIndex,
                copyDepth               = xrRenderPass.shouldFillOutDepth,
                spaceWarpRightHandedNDC = xrRenderPass.spaceWarpRightHandedNDC,
                xrSdkRenderPass         = xrRenderPass
            };

            return passInfo;
        }

#endif
    }
}
