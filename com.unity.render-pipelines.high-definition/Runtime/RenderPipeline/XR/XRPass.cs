// This file contain the two main data structures controlled by the XRSystem.
// XRView contains the parameters required to render (proj and view matrices, viewport, etc)
// XRPass holds the render target information and a list of XRView.
// When a pass has 2+ views, single-pass will be active.
// To avoid allocating every frame, XRView is a struct and XRPass is pooled.

using System;
using System.Collections.Generic;

#if ENABLE_VR && ENABLE_XR_MODULE
using UnityEngine.XR;
#endif

namespace UnityEngine.Rendering.HighDefinition
{
    internal struct XRPassCreateInfo
    {
        public int multipassId;
        public int cullingPassId;
        public RenderTexture renderTarget;
        public ScriptableCullingParameters cullingParameters;
        public XRPass.CustomMirrorView customMirrorView;
    }

    internal struct XRViewCreateInfo
    {
        public Matrix4x4 projMatrix;
        public Matrix4x4 viewMatrix;
        public Rect viewport;
        public int textureArraySlice;
    }

    internal struct XRView
    {
        internal readonly Matrix4x4 projMatrix;
        internal readonly Matrix4x4 viewMatrix;
        internal readonly Rect viewport;
        internal readonly Mesh occlusionMesh;
        internal readonly int textureArraySlice;
        internal readonly Camera.StereoscopicEye legacyStereoEye;

        internal XRView(Camera camera, Camera.StereoscopicEye eye, int dstSlice)
        {
            projMatrix = camera.GetStereoProjectionMatrix(eye);
            viewMatrix = camera.GetStereoViewMatrix(eye);
            viewport = camera.pixelRect;
            occlusionMesh = null;
            textureArraySlice = dstSlice;
            legacyStereoEye = eye;
        }

        internal XRView(Matrix4x4 proj, Matrix4x4 view, Rect vp, int dstSlice)
        {
            projMatrix = proj;
            viewMatrix = view;
            viewport = vp;
            occlusionMesh = null;
            textureArraySlice = dstSlice;
            legacyStereoEye = (Camera.StereoscopicEye)(-1);
        }

#if ENABLE_VR && ENABLE_XR_MODULE
        internal XRView(XRDisplaySubsystem.XRRenderPass renderPass, XRDisplaySubsystem.XRRenderParameter renderParameter)
        {
            projMatrix = renderParameter.projection;
            viewMatrix = renderParameter.view;
            viewport = renderParameter.viewport;
            occlusionMesh = renderParameter.occlusionMesh;
            textureArraySlice = renderParameter.textureArraySlice;
            legacyStereoEye = (Camera.StereoscopicEye)(-1);

            // Convert viewport from normalized to screen space
            viewport.x      *= renderPass.renderTargetDesc.width;
            viewport.width  *= renderPass.renderTargetDesc.width;
            viewport.y      *= renderPass.renderTargetDesc.height;
            viewport.height *= renderPass.renderTargetDesc.height;
        }
#endif
    }

    class XRPass
    {
        readonly List<XRView> views = new List<XRView>(2);

        internal bool enabled      { get => views.Count > 0; }
        internal bool xrSdkEnabled { get; private set; }
        internal bool copyDepth    { get; private set; }

        internal int multipassId    { get; private set; }
        internal int cullingPassId  { get; private set; }

        // Ability to specify where to render the pass
        internal RenderTargetIdentifier  renderTarget     { get; private set; }
        internal RenderTextureDescriptor renderTargetDesc { get; private set; }
        static RenderTargetIdentifier    invalidRT = -1;
        internal bool                    renderTargetValid { get => renderTarget != invalidRT; }

        // Access to view information
        internal Matrix4x4 GetProjMatrix(int viewIndex = 0)  { return views[viewIndex].projMatrix; }
        internal Matrix4x4 GetViewMatrix(int viewIndex = 0)  { return views[viewIndex].viewMatrix; }
        internal int GetTextureArraySlice(int viewIndex = 0) { return views[viewIndex].textureArraySlice; }
        internal Rect GetViewport(int viewIndex = 0)         { return views[viewIndex].viewport; }

        // Combined projection and view matrices for culling
        internal ScriptableCullingParameters cullingParams { get; private set; }

        // Single-pass rendering support (instanced draw calls or multiview extension)
        internal int viewCount { get => views.Count; }
        internal bool singlePassEnabled { get => viewCount > 1; }

        // Occlusion mesh rendering
        Material occlusionMeshMaterial = null;

        // Ability to override mirror view behavior for each pass
        internal delegate void CustomMirrorView(XRPass pass, CommandBuffer cmd, RenderTexture rt, Rect viewport);
        CustomMirrorView customMirrorView = null;
        internal void SetCustomMirrorView(CustomMirrorView callback) => customMirrorView = callback;

        // Legacy multipass support
        internal int  legacyMultipassEye      { get => (int)views[0].legacyStereoEye; }
        internal bool legacyMultipassEnabled  { get => enabled && !singlePassEnabled && legacyMultipassEye >= 0; }

        internal static XRPass Create(XRPassCreateInfo createInfo)
        {
            XRPass passInfo = GenericPool<XRPass>.Get();

            passInfo.multipassId = createInfo.multipassId;
            passInfo.cullingPassId = createInfo.cullingPassId;
            passInfo.cullingParams = createInfo.cullingParameters;
            passInfo.customMirrorView = createInfo.customMirrorView;
            passInfo.views.Clear();

            if (createInfo.renderTarget != null)
            {
                passInfo.renderTarget = new RenderTargetIdentifier(createInfo.renderTarget);
                passInfo.renderTargetDesc = createInfo.renderTarget.descriptor;
            }
            else
            {
                passInfo.renderTarget = invalidRT;
                passInfo.renderTargetDesc = default;
            }

            passInfo.occlusionMeshMaterial = null;
            passInfo.xrSdkEnabled = false;
            passInfo.copyDepth = false;

            return passInfo;
        }

        internal void AddView(Camera camera, Camera.StereoscopicEye eye, int textureArraySlice = -1)
        {
            AddViewInternal(new XRView(camera, eye, textureArraySlice));
        }

        internal void AddView(Matrix4x4 proj, Matrix4x4 view, Rect vp, int textureArraySlice = -1)
        {
            AddViewInternal(new XRView(proj, view, vp, textureArraySlice));
        }

#if ENABLE_VR && ENABLE_XR_MODULE
        internal static XRPass Create(XRDisplaySubsystem.XRRenderPass xrRenderPass, int multipassId, ScriptableCullingParameters cullingParameters, Material occlusionMeshMaterial)
        {
            XRPass passInfo = GenericPool<XRPass>.Get();

            passInfo.multipassId = multipassId;
            passInfo.cullingPassId = xrRenderPass.cullingPassIndex;
            passInfo.cullingParams = cullingParameters;
            passInfo.views.Clear();
            passInfo.renderTarget = xrRenderPass.renderTarget;
            passInfo.renderTargetDesc = xrRenderPass.renderTargetDesc;
            passInfo.occlusionMeshMaterial = occlusionMeshMaterial;
            passInfo.xrSdkEnabled = true;
            passInfo.copyDepth = xrRenderPass.shouldFillOutDepth;
            passInfo.customMirrorView = null;

            Debug.Assert(passInfo.renderTargetValid, "Invalid render target from XRDisplaySubsystem!");

            return passInfo;
        }

        internal void AddView(XRDisplaySubsystem.XRRenderPass xrSdkRenderPass, XRDisplaySubsystem.XRRenderParameter xrSdkRenderParameter)
        {
            AddViewInternal(new XRView(xrSdkRenderPass, xrSdkRenderParameter));
        }
#endif

        internal static void Release(XRPass xrPass)
        {
            GenericPool<XRPass>.Release(xrPass);
        }

        internal void AddViewInternal(XRView xrView)
        {
            int maxSupportedViews = Math.Min(TextureXR.slices, ShaderConfig.s_XrMaxViews);

            if (views.Count < maxSupportedViews)
            {
                views.Add(xrView);
            }
            else
            {
                if (xrSdkEnabled)
                {
                    Debug.LogWarning("If you're trying to enable XR single-pass after the first frame, you need to set TextureXR.maxViews to 2 before the render pipeline is created (typically in a script with Awake()).");
                }
                
                throw new NotImplementedException($"Invalid XR setup for single-pass, trying to add too many views! Max supported: {maxSupportedViews}");
            }
        }

        /// <summary>
        /// Enable XR single-pass rendering.
        /// </summary>
        public void StartSinglePass(CommandBuffer cmd, Camera camera, ScriptableRenderContext renderContext)
        {
            if (enabled)
            {
                // Required for some legacy shaders (text for example)
                cmd.SetViewProjectionMatrices(GetViewMatrix(), GetProjMatrix());

                if (camera.stereoEnabled)
                {
                    // Reset scissor and viewport for C++ stereo code
                    cmd.DisableScissorRect();
                    cmd.SetViewport(camera.pixelRect);

                    renderContext.ExecuteCommandBuffer(cmd);
                    cmd.Clear();

                    if (legacyMultipassEnabled)
                        renderContext.StartMultiEye(camera, legacyMultipassEye);
                    else
                        renderContext.StartMultiEye(camera);
                }
                else if (singlePassEnabled)
                {
                    if (viewCount <= TextureXR.slices)
                    {
                        cmd.EnableShaderKeyword("STEREO_INSTANCING_ON");
                        cmd.SetInstanceMultiplier((uint)viewCount);
                    }
                    else
                    {
                        throw new NotImplementedException($"Invalid XR setup for single-pass, trying to render too many views! Max supported: {TextureXR.slices}");
                    }
                }
            }
        }

        /// <summary>
        /// Disable XR single-pass rendering.
        /// </summary>
        public void StopSinglePass(CommandBuffer cmd, Camera camera, ScriptableRenderContext renderContext)
        {
            if (enabled)
            {
                if (camera.stereoEnabled)
                {
                    renderContext.ExecuteCommandBuffer(cmd);
                    cmd.Clear();
                    renderContext.StopMultiEye(camera);
                }
                else
                {
                    cmd.DisableShaderKeyword("STEREO_INSTANCING_ON");
                    cmd.SetInstanceMultiplier(1);
                }
            }
        }

        internal void EndCamera(CommandBuffer cmd, HDCamera hdCamera, ScriptableRenderContext renderContext)
        {
            if (!enabled)
                return;

            StopSinglePass(cmd, hdCamera.camera, renderContext);

            // Legacy VR - push to XR headset and/or display mirror
            if (hdCamera.camera.stereoEnabled)
            {
                if (legacyMultipassEnabled)
                    renderContext.StereoEndRender(hdCamera.camera, legacyMultipassEye, legacyMultipassEye == 1);
                else
                    renderContext.StereoEndRender(hdCamera.camera);
            }

            // Callback for custom mirror view
            if (customMirrorView != null)
            {
                using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.XRCustomMirrorView)))
                {
                    customMirrorView(this, cmd, hdCamera.camera.targetTexture, hdCamera.camera.pixelRect);
                }
            }
        }

        internal void RenderOcclusionMeshes(CommandBuffer cmd, RTHandle depthBuffer)
        {
            if (enabled && xrSdkEnabled && occlusionMeshMaterial != null)
            {
                using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.XROcclusionMesh)))
                {
                    Matrix4x4 m = Matrix4x4.Ortho(0.0f, 1.0f, 0.0f, 1.0f, -1.0f, 1.0f);

                    for (int viewId = 0; viewId < viewCount; ++viewId)
                    {
                        if (views[viewId].occlusionMesh != null)
                        {
                            CoreUtils.SetRenderTarget(cmd, depthBuffer, ClearFlag.None, 0, CubemapFace.Unknown, viewId);
                            cmd.DrawMesh(views[viewId].occlusionMesh, m, occlusionMeshMaterial);
                        }
                    }
                }
            }
        }
    }
}
