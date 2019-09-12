// This file contain the two main data structures controlled by the XRSystem.
// XRView contains the parameters required to render (proj and view matrices, viewport, etc)
// XRPass holds the render target information and a list of XRView.
// When a pass has 2+ views, single-pass will be active.
// To avoid allocating every frame, XRView is a struct and XRPass is pooled.

using System;
using System.Collections.Generic;
using UnityEngine.XR;

namespace UnityEngine.Rendering.HighDefinition
{
    internal struct XRView
    {
        internal readonly Matrix4x4 projMatrix;
        internal readonly Matrix4x4 viewMatrix;
        internal readonly Rect viewport;
        internal readonly Mesh occlusionMesh;
        internal readonly Camera.StereoscopicEye legacyStereoEye;

        internal XRView(Camera camera, Camera.StereoscopicEye eye)
        {
            projMatrix = camera.GetStereoProjectionMatrix(eye);
            viewMatrix = camera.GetStereoViewMatrix(eye);
            viewport = camera.pixelRect;
            occlusionMesh = null;
            legacyStereoEye = eye;
        }

        internal XRView(Matrix4x4 proj, Matrix4x4 view, Rect vp)
        {
            projMatrix = proj;
            viewMatrix = view;
            viewport = vp;
            occlusionMesh = null;
            legacyStereoEye = (Camera.StereoscopicEye)(-1);
        }

#if ENABLE_XR_MODULE
        internal XRView(XRDisplaySubsystem.XRRenderPass renderPass, XRDisplaySubsystem.XRRenderParameter renderParameter)
        {
            projMatrix = renderParameter.projection;
            viewMatrix = renderParameter.view;
            viewport = renderParameter.viewport;
            occlusionMesh = renderParameter.occlusionMesh;
            legacyStereoEye = (Camera.StereoscopicEye)(-1);

            // Convert viewport from normalized to screen space
            viewport.x      *= renderPass.renderTargetDesc.width;
            viewport.width  *= renderPass.renderTargetDesc.width;
            viewport.y      *= renderPass.renderTargetDesc.height;
            viewport.height *= renderPass.renderTargetDesc.height;
        }
#endif
    }

    public class XRPass
    {
        readonly List<XRView> views = new List<XRView>(2);

        internal bool enabled      { get => views.Count > 0; }
        internal bool xrSdkEnabled { get; private set; }

        internal int multipassId    { get; private set; }
        internal int cullingPassId  { get; private set; }
        internal int dstSliceIndex  { get; private set; }

        // Ability to specify where to render the pass
        internal RenderTargetIdentifier  renderTarget     { get; private set; }
        internal RenderTextureDescriptor renderTargetDesc { get; private set; }
        static RenderTargetIdentifier    invalidRT = -1;
        internal bool                    renderTargetValid { get => renderTarget != invalidRT; }

        // Access to view information
        internal Matrix4x4 GetProjMatrix(int viewIndex = 0) { return views[viewIndex].projMatrix; }
        internal Matrix4x4 GetViewMatrix(int viewIndex = 0) { return views[viewIndex].viewMatrix; }
        internal Rect GetViewport(int viewIndex = 0)        { return views[viewIndex].viewport; }

        // Combined projection and view matrices for culling
        internal ScriptableCullingParameters cullingParams { get; private set; }

        // Instanced views support (instanced draw calls or multiview extension)
        internal int viewCount { get => views.Count; }
        internal bool singlePassEnabled { get => viewCount > 1; }

        // Occlusion mesh rendering
        Material occlusionMeshMaterial = null;

        // Legacy multipass support
        internal int  legacyMultipassEye      { get => (int)views[0].legacyStereoEye; }
        internal bool legacyMultipassEnabled  { get => enabled && !singlePassEnabled && legacyMultipassEye >= 0; }

        internal static XRPass Create(int multipassId, int cullingPassId, ScriptableCullingParameters cullingParameters, RenderTexture rt = null)
        {
            XRPass passInfo = GenericPool<XRPass>.Get();

            passInfo.multipassId = multipassId;
            passInfo.cullingPassId = cullingPassId;
            passInfo.cullingParams = cullingParameters;
            passInfo.dstSliceIndex = -1;
            passInfo.views.Clear();

            if (rt != null)
            {
                passInfo.renderTarget = new RenderTargetIdentifier(rt);
                passInfo.renderTargetDesc = rt.descriptor;
            }
            else
            {
                passInfo.renderTarget = invalidRT;
                passInfo.renderTargetDesc = default;
            }

            passInfo.occlusionMeshMaterial = null;
            passInfo.xrSdkEnabled = false;

            return passInfo;
        }

        internal void AddView(Camera camera, Camera.StereoscopicEye eye)
        {
            AddViewInternal(new XRView(camera, eye));
        }

        internal void AddView(Matrix4x4 proj, Matrix4x4 view, Rect vp)
        {
            AddViewInternal(new XRView(proj, view, vp));
        }

#if ENABLE_XR_MODULE
        internal static XRPass Create(XRDisplaySubsystem.XRRenderPass xrRenderPass, int multipassId, int textureArraySlice, ScriptableCullingParameters cullingParameters, Material occlusionMeshMaterial)
        {
            XRPass passInfo = GenericPool<XRPass>.Get();

            passInfo.multipassId = multipassId;
            passInfo.cullingPassId = xrRenderPass.cullingPassIndex;
            passInfo.cullingParams = cullingParameters;
            passInfo.dstSliceIndex = textureArraySlice;
            passInfo.views.Clear();
            passInfo.renderTarget = xrRenderPass.renderTarget;
            passInfo.renderTargetDesc = xrRenderPass.renderTargetDesc;
            passInfo.occlusionMeshMaterial = occlusionMeshMaterial;
            passInfo.xrSdkEnabled = true;

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
                throw new NotImplementedException($"Invalid XR setup for single-pass, trying to add too many views! Max supported: {maxSupportedViews}");
            }
        }

        internal void StartSinglePass(CommandBuffer cmd, Camera camera, ScriptableRenderContext renderContext)
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

        internal void StopSinglePass(CommandBuffer cmd, Camera camera, ScriptableRenderContext renderContext)
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
        }

        internal void RenderOcclusionMeshes(CommandBuffer cmd, RTHandle depthBuffer)
        {
            if (enabled && xrSdkEnabled && occlusionMeshMaterial != null)
            {
                using (new ProfilingSample(cmd, "XR Occlusion Mesh"))
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
