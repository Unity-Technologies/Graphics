using System;
using System.Collections.Generic;
using Unity.Scripting.LifecycleManagement;
using UnityEngine.Rendering;

#if ENABLE_VR && ENABLE_XR_MODULE
using UnityEngine.XR;
#endif

namespace UnityEngine.Experimental.Rendering
{

    /// <summary>
    /// Identifies the XR rendering layout for the current frame.
    /// The layout is inferred from the XR display subsystem configuration
    /// (number of render passes and views per pass) and determines how
    /// the render pipeline structures its XR rendering passes.
    /// </summary>
    public enum XRLayoutType
    {
        /// <summary>
        /// The layout type could not be determined. This value is used as
        /// the default before inference runs and indicates an unsupported
        /// or unrecognized XR display configuration.
        /// </summary>
        Unknown,

        /// <summary>
        /// Single-pass instanced stereo rendering. One render pass with two
        /// views (left and right eye) rendered simultaneously via instancing.
        /// </summary>
        SinglePassStereo,

        /// <summary>
        /// Multi-pass stereo rendering. Two render passes, each with one
        /// view (one eye per pass).
        /// </summary>
        TwoPassStereo,

        /// <summary>
        /// Two-pass quad views rendering. Two render passes, each with two
        /// views. The first pass renders peripheral (outer) views and the
        /// second pass renders foveal (inner) views.
        /// </summary>
        TwoPassQuadViews,

        /// <summary>
        /// One-pass rendering. One render pass with one view.
        /// </summary>
        OnePassOneView
    }

    /// <summary>
    /// Used by render pipelines to control the active XR shader variant.
    /// </summary>
    public static partial class SinglepassKeywords
    {
        /// <summary> XR shader keyword used by multiview rendering </summary>
        [AutoStaticsCleanup]
        public static GlobalKeyword STEREO_MULTIVIEW_ON;
        /// <summary> XR shader keywordused by single pass instanced rendering </summary>
        [AutoStaticsCleanup]
        public static GlobalKeyword STEREO_INSTANCING_ON;
    }

    /// <summary>
    /// Used by render pipelines to communicate with XR SDK.
    /// </summary>
    public static partial class XRSystem
    {
        [OnCodeInitializing]
        static void ResetStaticsOnLoad()
        {
            // Drain any remaining layouts to return pooled XRPass/XRLayout resources
            while (s_Layout != null && s_Layout.hasLayout)
                s_Layout.Release();
            s_Layout = new XRLayoutStack();

            // Dispose materials before nulling — requires custom disposal logic
            CoreUtils.Destroy(s_OcclusionMeshMaterial);
            s_OcclusionMeshMaterial = null;
            CoreUtils.Destroy(s_MirrorViewMaterial);
            s_MirrorViewMaterial = null;

#if ENABLE_VR && ENABLE_XR_MODULE
            s_OcclusionMeshScaling = 1.0f;
#endif
            s_RenderTargetToCamera.Clear();
        }

        // Keep track of only one XR layout — manually drained in ResetStaticsOnLoad()
        [NoAutoStaticsCleanup]
        static XRLayoutStack s_Layout = new ();

        // Delegate allocations of XRPass to the render pipeline
        [AutoStaticsCleanup]
        static Func<XRPassCreateInfo, XRPass> s_PassAllocator = null;

#if ENABLE_VR && ENABLE_XR_MODULE
        [AutoStaticsCleanup]
        static List<XRDisplaySubsystem> s_DisplayList = new List<XRDisplaySubsystem>();
        [AutoStaticsCleanup]
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
        [AutoStaticsCleanup]
        static MSAASamples s_MSAASamples = MSAASamples.None;

#if ENABLE_VR && ENABLE_XR_MODULE
        // Occlusion Mesh scaling factor — manually reset; captured into XRPassCreateInfo
        [NoAutoStaticsCleanup]
        static float s_OcclusionMeshScaling = 1.0f;

        // Return true if wants to enable visibility mesh passes
        [AutoStaticsCleanup]
        static bool s_UseVisibilityMesh = true;
#endif

        // Internal resources used by XR rendering — manually cleaned up via CoreUtils.Destroy()
        [NoAutoStaticsCleanup]
        static Material s_OcclusionMeshMaterial;
        [NoAutoStaticsCleanup]
        static Material s_MirrorViewMaterial;

        // Ability to override the default XR layout
        [AutoStaticsCleanup]
        static Action<XRLayout, Camera> s_LayoutOverride = null;

        const ulong k_DefaultRenderTargetId = 0;

        // Manually cleared in ResetStaticsOnLoad()
        [NoAutoStaticsCleanup]
        static readonly Dictionary<ulong, Camera> s_RenderTargetToCamera = new Dictionary<ulong, Camera>();

        /// <summary>
        /// Associates a camera with a specific XR render target.
        /// Called by the spatial container system at container creation.
        /// </summary>
        /// <param name="renderTargetId">Stable identity for the logical render target. Must not be 0, which is reserved for the default single-target path.</param>
        /// <param name="camera">The camera to render this target.</param>
        /// <exception cref="ArgumentNullException"><paramref name="camera"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="renderTargetId"/> is 0.</exception>
        /// <exception cref="InvalidOperationException">The render target is already associated with a different camera, or the camera is already associated with a different render target.</exception>
        public static void SetCameraRenderTarget(ulong renderTargetId, Camera camera)
        {
            if (camera == null)
                throw new ArgumentNullException(nameof(camera));

            if (renderTargetId == k_DefaultRenderTargetId)
                throw new ArgumentException("Render target ID 0 is reserved for the default single-target path.", nameof(renderTargetId));

            if (s_RenderTargetToCamera.TryGetValue(renderTargetId, out var existingCamera))
            {
                if (existingCamera == camera)
                    return;

                if (existingCamera != null)
                    throw new InvalidOperationException($"Render target {renderTargetId} is already associated with camera '{existingCamera.name}'.");
            }

            if (TryGetRenderTarget(camera, out var existingTarget))
                throw new InvalidOperationException($"Camera '{camera.name}' is already associated with render target {existingTarget}.");

            s_RenderTargetToCamera[renderTargetId] = camera;
        }

        /// <summary>
        /// Clears all camera-to-render-target associations.
        /// </summary>
        public static void ClearCameraRenderTargets()
            => s_RenderTargetToCamera.Clear();

        /// <summary>
        /// Removes a single camera-to-render-target association.
        /// </summary>
        /// <param name="renderTargetId">The render target identity to remove.</param>
        public static void ClearCameraRenderTarget(ulong renderTargetId)
            => s_RenderTargetToCamera.Remove(renderTargetId);

        internal static bool TryGetCameraForTarget(ulong renderTargetId, out Camera camera)
            => s_RenderTargetToCamera.TryGetValue(renderTargetId, out camera);

        internal static bool TryGetRenderTarget(Camera camera, out ulong renderTargetId)
        {
            foreach (var kvp in s_RenderTargetToCamera)
            {
                if (kvp.Value == camera)
                {
                    renderTargetId = kvp.Key;
                    return true;
                }
            }
            renderTargetId = k_DefaultRenderTargetId;
            return false;
        }

        /// <summary>Per-frame Temporal Pixel Synthesis configuration override.</summary>
        public struct TemporalPixelSynthesisConfig
        {
            /// <summary>TPS active this frame (gates URP TAA/STP/upscaler).</summary>
            public bool active;
            /// <summary>Runtime requests a stencil aspect on the motion-vector target.</summary>
            public bool requestStencil;
            /// <summary>Stencil reference value: (stencil &amp; mask) == value ⇒ excluded from TPS.</summary>
            public uint stencilValue;
            /// <summary>Stencil bitmask tested by TPS (non-zero; must not overlap pipeline-private bits).</summary>
            public uint stencilMask;
            /// <summary>Compositor performs upscaling (eye target may be rendered below display res).</summary>
            public bool upscalingEnabled;
        }

        /// <summary>
        /// Overrides the Temporal Pixel Synthesis contract reported by <see cref="XRPass"/>, for exercising
        /// the feature without an XR provider that implements <c>XR_META_temporal_pixel_synthesis</c>.
        /// Takes precedence over provider-supplied values while <see cref="TemporalPixelSynthesisConfig.active"/> is true.
        /// </summary>
        [AutoStaticsCleanup]
        public static TemporalPixelSynthesisConfig temporalPixelSynthesisConfig { get; set; }

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
        [NoAutoStaticsCleanup]
        public static readonly XRPass emptyPass = new XRPass();

        /// <summary>
        /// If true, the system will try to create a layout compatible with single-pass rendering.
        /// </summary>
        [AutoStaticsCleanup]
        static public bool singlePassAllowed { get; set; } = true;

        /// <summary>
        /// Cached value of SystemInfo.foveatedRenderingCaps.
        /// </summary>
        [AutoStaticsCleanup]
        static public FoveatedRenderingCaps foveatedRenderingCaps { get; set; }

        /// <summary>
        /// If true, the system will log some information about the layout to the console.
        /// </summary>
        [AutoStaticsCleanup]
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
        /// Used by the render pipeline to retrieve the applied renderViewportScale value from the XR display.
        /// One use case for retriving this value is that render pipeline can properly sync some SRP owned textures to scale accordingly
        /// </summary>
        /// <returns> Returns current appliedViewportScale value from the XRDisplaySubsystem. </returns>
        public static float GetRenderViewportScale()
        {
#if ENABLE_VR && ENABLE_XR_MODULE

            return s_Display.appliedViewportScale;
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
        /// Returns the current active XR layout, or null if none is active.
        /// Used by render passes to access cross-camera XR state like Quad View parameters.
        /// </summary>
        public static XRLayout currentLayout => s_Layout.hasLayout ? s_Layout.top : null;

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

        // Setup the layout to use multi-pass or single-pass based on the runtime caps.
        // Resolves the camera's render target (if any) via the render-target registry,
        // filters passes by renderTargetId, and classifies XRLayoutType within that set.
        internal static void CreateDefaultLayout(Camera camera, XRLayout layout)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            if (s_Display == null)
                throw new NullReferenceException(nameof(s_Display));

            int displayPassCount = s_Display.GetRenderPassCount();
            if (displayPassCount == 0)
                return;

            // Select the display render passes this camera renders.
            // passIndices[i] is the display render pass index of this camera's i-th pass.
            // Unregistered cameras get the default render target (0), so they only render passes not claimed by a render target.
            // Matching passes are not required to be contiguous.
            Span<int> passIndices = stackalloc int[displayPassCount];
            bool hasRenderTarget = TryGetRenderTarget(camera, out var renderTargetId);
            Span<ulong> passTargetIds = stackalloc ulong[displayPassCount];
            for (int i = 0; i < displayPassCount; ++i)
            {
                s_Display.GetRenderPass(i, out var renderPass);
                passTargetIds[i] = renderPass.renderTargetId;
            }

            int renderPassCount = CollectTargetPasses(passTargetIds, renderTargetId, passIndices);
            if (renderPassCount == 0)
                return;

            // Classify XRLayoutType on the matching passes, not the global pass count.
            Span<int> viewCountsPerPass = stackalloc int[renderPassCount];
            bool canUseSinglePass = true;
            for (int i = 0; i < renderPassCount; ++i)
            {
                s_Display.GetRenderPass(passIndices[i], out var renderPass);
                viewCountsPerPass[i] = renderPass.GetRenderParameterCount();
                canUseSinglePass &= CanUseSinglePass(camera, renderPass);
            }
            XRLayoutType layoutType = ClassifyLayout(viewCountsPerPass, canUseSinglePass, hasRenderTarget);
            if (layoutType == XRLayoutType.Unknown)
            {
                if (hasRenderTarget && ClassifyLayout(viewCountsPerPass, canUseSinglePass, false) == XRLayoutType.TwoPassQuadViews)
                    throw new NotImplementedException($"Quad views are not supported for cameras with an XR render target (render target {renderTargetId}).");
                throw new NotImplementedException($"Unsupported XR layout: {renderPassCount} render passes");
            }

            void AddViewToPass(XRPass xrPass, XRDisplaySubsystem.XRRenderPass renderPass, int renderParamIndex)
            {
                renderPass.GetRenderParameter(camera, renderParamIndex, out var renderParam);
                xrPass.AddView(BuildView(renderPass, renderParam));
            }

            // Helper: extract frustum extents from projection matrix.
            // Returns Vector4(width, height, left, top) at unit depth.
            static Vector4 ExtractFrustumBoundsFromProjection(Matrix4x4 proj)
            {
                float width = 2.0f / proj[0, 0];
                float height = 2.0f / proj[1, 1];
                float left = (proj[0, 2] - 1.0f) / proj[0, 0];
                float top = (1 - proj[1, 2]) / proj[1, 1];

                return new Vector4(width, height, left, top);
            }

            Vector4 ExtractViewBounds(XRDisplaySubsystem.XRRenderPass renderPass, int renderParamIndex)
            {
                renderPass.GetRenderParameter(camera, renderParamIndex, out var renderParam);
                var proj = renderParam.projection;
                return ExtractFrustumBoundsFromProjection(proj);
            }

            // Pre-calculate view bounds for quad view (2 passes × 2 views) using fixed-size stack variables
            // This avoids List allocations that would cause GC pressure every frame
            Vector4 pass0View0Bounds = default, pass0View1Bounds = default;
            Vector4 pass1View0Bounds = default, pass1View1Bounds = default;
            if (layoutType == XRLayoutType.TwoPassQuadViews)
            {
                s_Display.GetRenderPass(passIndices[0], out var pass0);
                s_Display.GetRenderPass(passIndices[1], out var pass1);
                pass0View0Bounds = ExtractViewBounds(pass0, 0);
                pass0View1Bounds = ExtractViewBounds(pass0, 1);
                pass1View0Bounds = ExtractViewBounds(pass1, 0);
                pass1View1Bounds = ExtractViewBounds(pass1, 1);
            }

            for (int renderPassIndex = 0; renderPassIndex < renderPassCount; ++renderPassIndex)
            {
                s_Display.GetRenderPass(passIndices[renderPassIndex], out var renderPass);

                int renderParameterCount = renderPass.GetRenderParameterCount();
                s_Display.GetCullingParameters(camera, renderPass.cullingPassIndex, out var cullingParams);

                bool isLastPass = renderPassIndex == renderPassCount - 1;
                // This parameter makes sure we are in 2 pass quad view's second pass, which is the only case we need to apply special UV scale and offset.
                bool isQuadViewLastPass = isLastPass && layoutType == XRLayoutType.TwoPassQuadViews;
                bool isQuadViewFirstPass = renderPassIndex == 0 && layoutType == XRLayoutType.TwoPassQuadViews;
                Vector4 uvScales = Vector4.one;
                Vector4 uvOffsets = Vector4.zero;
                if (isQuadViewFirstPass)
                {
                    s_Display.GetRenderPass(passIndices[renderPassIndex + 1], out var innerRenderPass);
                    SetSplitCullingPlanes(camera, innerRenderPass.cullingPassIndex, ref cullingParams);
                }

                if (isQuadViewLastPass)
                {
                    // Calculate UV scales and offsets from pre-computed view bounds
                    uvScales.x = pass1View0Bounds.x / pass0View0Bounds.x;
                    uvScales.y = pass1View0Bounds.y / pass0View0Bounds.y;
                    uvScales.z = pass1View1Bounds.x / pass0View1Bounds.x;
                    uvScales.w = pass1View1Bounds.y / pass0View1Bounds.y;

                    uvOffsets.x = (pass1View0Bounds.z - pass0View0Bounds.z) / pass0View0Bounds.x;
                    uvOffsets.y = -(pass1View0Bounds.w - pass0View0Bounds.w) / pass0View0Bounds.y;
                    uvOffsets.z = (pass1View1Bounds.z - pass0View1Bounds.z) / pass0View1Bounds.x;
                    uvOffsets.w = -(pass1View1Bounds.w - pass0View1Bounds.w) / pass0View1Bounds.y;

                    // Cache the inset rect as a bottom-left origin fraction of the full periphery frustum
                    // uvScales and uvOffsets are already ratios of inset-to-peripheral spans and bottom-left corner positions, respectively
                    float insetW = uvScales.x;
                    float insetH = uvScales.y;
                    float insetX = uvOffsets.x;
                    float insetY = uvOffsets.y;

                    // Asymmetric per-eye inner frustums are uncommon (Quest 3 uses conjugate gaze with symmetric frustums),
                    // but intersect both eye rects when they differ to avoid rejecting pixels the compositor still shows on either eye
                    float featherBoundsX = pass0View0Bounds.x;
                    float featherBoundsY = pass0View0Bounds.y;
                    if (!Mathf.Approximately(insetW, uvScales.z)  || !Mathf.Approximately(insetH, uvScales.w)  ||
                        !Mathf.Approximately(insetX, uvOffsets.z) || !Mathf.Approximately(insetY, uvOffsets.w))
                    {
                        float right = Mathf.Min(insetX + insetW, uvOffsets.z + uvScales.z);
                        float top   = Mathf.Min(insetY + insetH, uvOffsets.w + uvScales.w);
                        insetX = Mathf.Max(insetX, uvOffsets.z);
                        insetY = Mathf.Max(insetY, uvOffsets.w);
                        insetW = right - insetX;
                        insetH = top   - insetY;

                        // Use the smaller peripheral extent per axis so the feather fraction is larger (more conservative).
                        featherBoundsX = Mathf.Min(pass0View0Bounds.x, pass0View1Bounds.x);
                        featherBoundsY = Mathf.Min(pass0View0Bounds.y, pass0View1Bounds.y);
                    }

                    layout.quadView.peripheryInsetViewport = new Vector4(insetX, insetY, insetW, insetH);

                    // Cache a safety margin as a fraction of the full periphery frustum to avoid compositor blending issues
                    const float insetMarginRadians = 0.05f;
                    layout.quadView.peripheryInsetFeather = new Vector2(insetMarginRadians / featherBoundsX, insetMarginRadians / featherBoundsY);
                }

                if (CanUseSinglePass(camera, renderPass))
                {
                    var createInfo = BuildPass(renderPass, cullingParams, layout, layoutType, uvScales, uvOffsets);
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
                        var createInfo = BuildPass(renderPass, cullingParams, layout, layoutType, uvScales, uvOffsets);
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
                // Use the render pass stored at layout-build time rather than looking up by
                // multipassId. multipassId is a per-camera local index and does not match the
                // global display render pass index when multiple spatial container cameras
                // each build their own layout.
                var renderPass = xrPass.xrSdkRenderPass;
                Debug.Assert(xrPass.singlePassEnabled || renderPass.GetRenderParameterCount() == 1);

                s_Display.GetCullingParameters(camera, renderPass.cullingPassIndex, out var cullingParams);

                // For the outer QuadView pass, re-derive split planes from the inner pass every frame.
                // This must happen after BeginCameraRendering, which may have moved the camera.
                if (xrPass.xrLayoutType == XRLayoutType.TwoPassQuadViews && !xrPass.isQuadViewInnerPass)
                {
                    s_Display.GetRenderPass(xrPass.xrSdkRenderPass.renderPassIndex + 1, out var innerRenderPass);
                    SetSplitCullingPlanes(camera, innerRenderPass.cullingPassIndex, ref cullingParams);
                }

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

        // Matching passes are not required to be contiguous.
        internal static int CollectTargetPasses(ReadOnlySpan<ulong> passTargetIds, ulong target, Span<int> matchedPasses)
        {
            int matchCount = 0;
            for (int i = 0; i < passTargetIds.Length; ++i)
            {
                if (passTargetIds[i] == target)
                    matchedPasses[matchCount++] = i;
            }
            return matchCount;
        }

        internal static XRLayoutType ClassifyLayout(ReadOnlySpan<int> viewCountsPerPass, bool canUseSinglePass, bool hasRenderTarget)
        {
            switch (viewCountsPerPass.Length)
            {
                case 1:
                    // Without single-pass instancing the two views are split into separate passes
                    if (viewCountsPerPass[0] == 2)
                        return canUseSinglePass ? XRLayoutType.SinglePassStereo : XRLayoutType.TwoPassStereo;
                    else if (viewCountsPerPass[0] == 1)
                        return XRLayoutType.OnePassOneView;
                    break;

                case 2:
                    if (viewCountsPerPass[0] == 1 && viewCountsPerPass[1] == 1)
                        return XRLayoutType.TwoPassStereo;

                    // Without single-pass instancing each 2-view pass would be split, giving 4 passes, which is unsupported
                    // Quad views are not supported with XR render targets yet
                    if (viewCountsPerPass[0] == 2 && viewCountsPerPass[1] == 2 && canUseSinglePass && !hasRenderTarget)
                        return XRLayoutType.TwoPassQuadViews;
                    break;
            }
            return XRLayoutType.Unknown;
        }

#if ENABLE_VR && ENABLE_XR_MODULE
        static void SetSplitCullingPlanes(Camera camera, int innerCullingPassIndex, ref ScriptableCullingParameters cullingParams)
        {
            Debug.Assert(s_Display != null);
            s_Display.GetCullingParameters(camera, innerCullingPassIndex, out var innerParams);
            cullingParams.splitPlaneCount = innerParams.cullingPlaneCount;
            for (int i = 0; i < innerParams.cullingPlaneCount; ++i)
                cullingParams.SetSplitCullingPlane(i, innerParams.GetCullingPlane(i));
        }

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
            rtDesc.memoryless   = xrDesc.memoryless;
            return rtDesc;
        }

        // Frame-keyed rather than a bool reset on "TPS inactive": in multi-pass the provider may describe
        // pass 0 but not pass 1, and a per-pass reset would put the message back to once per frame.
        [AutoStaticsCleanup]
        static int s_LastTemporalPixelSynthesisStencilFormatErrorFrame = -1;

        static XRPassCreateInfo BuildPass(XRDisplaySubsystem.XRRenderPass xrRenderPass, ScriptableCullingParameters cullingParameters, XRLayout layout, XRLayoutType layoutType, Vector4 uvScales, Vector4 uvOffsets)
        {
            bool temporalPixelSynthesisActive;
            uint temporalPixelSynthesisStencilValue;
            uint temporalPixelSynthesisStencilMask;
            bool temporalPixelSynthesisStencilRequested;

            if (temporalPixelSynthesisConfig.active)
            {
                temporalPixelSynthesisActive = true;
                temporalPixelSynthesisStencilValue = temporalPixelSynthesisConfig.stencilValue;
                temporalPixelSynthesisStencilMask = temporalPixelSynthesisConfig.stencilMask;
                temporalPixelSynthesisStencilRequested = temporalPixelSynthesisConfig.requestStencil;
            }
            else
            {
                temporalPixelSynthesisActive = xrRenderPass.isTemporalPixelSynthesisActive;
                temporalPixelSynthesisStencilValue = (uint)xrRenderPass.motionVectorStencilValue;
                temporalPixelSynthesisStencilMask = (uint)xrRenderPass.motionVectorStencilMask;
                temporalPixelSynthesisStencilRequested = temporalPixelSynthesisActive;
            }

            // The 0xFF bound matters because the mask reaches StencilState.writeMask as a byte downstream,
            // so e.g. 0x100 would truncate to 0 and silently write no stencil at all.
            bool hasMotionVectorStencil = temporalPixelSynthesisActive
                && temporalPixelSynthesisStencilRequested
                && xrRenderPass.hasMotionVectorPass
                && temporalPixelSynthesisStencilMask != 0
                && temporalPixelSynthesisStencilMask <= 0xFF;

            // Don't publish a contract that isn't in force.
            if (!hasMotionVectorStencil)
            {
                temporalPixelSynthesisStencilValue = 0;
                temporalPixelSynthesisStencilMask = 0;
            }

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
                hasMotionVectorStencil   = hasMotionVectorStencil,
                motionVectorStencilValue = temporalPixelSynthesisStencilValue,
                motionVectorStencilMask  = temporalPixelSynthesisStencilMask,
                isTemporalPixelSynthesisActive   = temporalPixelSynthesisActive,
                skipFDMForFinalPasses   = xrRenderPass.skipFDMForFinalPasses,
                xrSdkRenderPass         = xrRenderPass,
                xrLayoutType            = layoutType,
                uvScales                 = uvScales,
                uvOffsets                = uvOffsets
            };

            // The pipeline-private bit-reservation check lives in URP (XRDepthMotionPass) instead, since
            // which stencil bits the producer reserves is a URP shader convention.
            if (passInfo.hasMotionVectorStencil)
            {
                Debug.Assert((passInfo.motionVectorStencilValue & passInfo.motionVectorStencilMask) == passInfo.motionVectorStencilValue,
                    "TPS: motionVectorStencilValue must be a subset of motionVectorStencilMask.");

                if (!GraphicsFormatUtility.IsStencilFormat(passInfo.motionVectorRenderTargetDesc.depthStencilFormat)
                    && s_LastTemporalPixelSynthesisStencilFormatErrorFrame != Time.frameCount)
                {
                    s_LastTemporalPixelSynthesisStencilFormatErrorFrame = Time.frameCount;
                    Debug.LogError($"Temporal Pixel Synthesis requested a motion-vector stencil aspect, but the motion-vector depth target format ({passInfo.motionVectorRenderTargetDesc.depthStencilFormat}) has no stencil bits. A stencil-capable format such as D24_UNorm_S8_UInt or D32_SFloat_S8_UInt is required; TPS pixel exclusion will not work.");
                }
            }

            DebugTraceTemporalPixelSynthesis(in passInfo, xrRenderPass);

            return passInfo;
        }

        // Traces what the provider delivers at the boundary where TPS data crosses into SRP.
        const bool k_DebugTemporalPixelSynthesis = false;

        // Indexed by multipassId, so the steady-state cost is a struct compare rather than a string per
        // pass per frame.
        [NoAutoStaticsCleanup]
        static readonly (bool active, bool stencil, uint value, uint mask, bool hasMVPass, GraphicsFormat mvDepthFmt)[] s_LastTemporalPixelSynthesisTrace
            = new (bool, bool, uint, uint, bool, GraphicsFormat)[kMaxTracedPasses];
        [NoAutoStaticsCleanup]
        static readonly bool[] s_TemporalPixelSynthesisTraceLogged = new bool[kMaxTracedPasses];
        const int kMaxTracedPasses = 8;

        static void DebugTraceTemporalPixelSynthesis(in XRPassCreateInfo passInfo, XRDisplaySubsystem.XRRenderPass xrRenderPass)
        {
#pragma warning disable 162 // unreachable code when the flag is false
            if (!k_DebugTemporalPixelSynthesis)
                return;

            int passIndex = passInfo.multipassId;
            if (passIndex < 0 || passIndex >= kMaxTracedPasses)
                return;

            var state = (passInfo.isTemporalPixelSynthesisActive,
                         passInfo.hasMotionVectorStencil,
                         passInfo.motionVectorStencilValue,
                         passInfo.motionVectorStencilMask,
                         passInfo.hasMotionVectorPass,
                         passInfo.motionVectorRenderTargetDesc.depthStencilFormat);

            if (s_TemporalPixelSynthesisTraceLogged[passIndex] && s_LastTemporalPixelSynthesisTrace[passIndex].Equals(state))
                return;

            s_TemporalPixelSynthesisTraceLogged[passIndex] = true;
            s_LastTemporalPixelSynthesisTrace[passIndex] = state;

            // raw(...) is pre-override, pre-validation: populated raw with an empty resolved value means
            // the contract was dropped here rather than never delivered.
            Debug.Log($"[TPS] pass={passIndex} active={passInfo.isTemporalPixelSynthesisActive} " +
                      $"stencilRequested={passInfo.hasMotionVectorStencil} " +
                      $"value=0x{passInfo.motionVectorStencilValue:X} mask=0x{passInfo.motionVectorStencilMask:X} " +
                      $"| hasMVPass={passInfo.hasMotionVectorPass} " +
                      $"mvDepthFmt={passInfo.motionVectorRenderTargetDesc.depthStencilFormat} " +
                      $"stencilCapable={GraphicsFormatUtility.IsStencilFormat(passInfo.motionVectorRenderTargetDesc.depthStencilFormat)} " +
                      $"| raw(active={xrRenderPass.isTemporalPixelSynthesisActive} value={xrRenderPass.motionVectorStencilValue} mask={xrRenderPass.motionVectorStencilMask}) " +
                      $"configOverride={temporalPixelSynthesisConfig.active}");
#pragma warning restore 162
        }

#endif
    }
}
