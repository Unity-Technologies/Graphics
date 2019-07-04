// This file contain the two main data structures controlled by the XRSystem.
// XRView contains the parameters required to render (proj and view matrices, viewport, etc)
// XRPass holds the render target information and a list of XRView.
// When a pass has 2+ views, hardware instancing will be active.
// To avoid allocating every frame, XRView is a struct and XRPass is pooled.

#if UNITY_2019_3_OR_NEWER && ENABLE_VR
#define USE_XR_SDK
#endif

using System;
using System.Collections.Generic;
using UnityEngine.Rendering;
#if USE_XR_SDK
using UnityEngine.Experimental.XR;
#endif

namespace UnityEngine.Experimental.Rendering.HDPipeline
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

#if USE_XR_SDK
        internal XRView(XRDisplaySubsystem.XRRenderParameter renderParameter)
        {
            projMatrix = renderParameter.projection;
            viewMatrix = renderParameter.view;
            viewport = renderParameter.viewport;
            occlusionMesh = renderParameter.occlusionMesh;
            legacyStereoEye = (Camera.StereoscopicEye)(-1);
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

        // Ability to specify where to render the pass
        internal RenderTargetIdentifier  renderTarget     { get; private set; }
        internal RenderTextureDescriptor renderTargetDesc { get; private set; }
        static RenderTargetIdentifier    invalidRT = -1;
        internal bool                    renderTargetValid { get => renderTarget != invalidRT; }

        // Access to view information
        internal Matrix4x4 GetProjMatrix(int viewIndex = 0) { return views[viewIndex].projMatrix; }
        internal Matrix4x4 GetViewMatrix(int viewIndex = 0) { return views[viewIndex].viewMatrix; }
        internal Rect GetViewport(int viewIndex = 0)        { return views[viewIndex].viewport; }

        // Instanced views support (instanced draw calls or multiview extension)
        internal int viewCount { get => views.Count; }
        internal bool instancingEnabled { get => viewCount > 1; }

        // Occlusion mesh rendering
        Material occlusionMeshMaterial = null;

        // XRTODO(2019.3) : remove once XRE-445 is done
        // We need an intermediate target to render the mirror view
        internal RenderTexture tempRenderTexture { get; private set; } = null;
#if USE_XR_SDK
        RenderTextureDescriptor tempRenderTextureDesc;
#endif

        // Legacy multipass support
        internal int  legacyMultipassEye      { get => (int)views[0].legacyStereoEye; }
        internal bool legacyMultipassEnabled  { get => enabled && !instancingEnabled && legacyMultipassEye >= 0; }

        internal static XRPass Create(int multipassId, RenderTexture rt = null)
        {
            XRPass passInfo = GenericPool<XRPass>.Get();

            passInfo.multipassId = multipassId;
            passInfo.cullingPassId = multipassId;
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
            passInfo.tempRenderTexture = null;

#if USE_XR_SDK
            passInfo.tempRenderTextureDesc = default;
#endif

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

#if USE_XR_SDK
        internal static XRPass Create(XRDisplaySubsystem.XRRenderPass xrRenderPass, int multipassId, Material occlusionMeshMaterial)
        {
            XRPass passInfo = GenericPool<XRPass>.Get();

            passInfo.multipassId = multipassId;
            passInfo.cullingPassId = xrRenderPass.cullingPassIndex;
            passInfo.views.Clear();
            passInfo.renderTarget = xrRenderPass.renderTarget;
            passInfo.renderTargetDesc = xrRenderPass.renderTargetDesc;
            passInfo.occlusionMeshMaterial = occlusionMeshMaterial;
            passInfo.xrSdkEnabled = true;

            Debug.Assert(passInfo.renderTargetValid, "Invalid render target from XRDisplaySubsystem!");

            // XRTODO(2019.3) : remove once XRE-445 is done
            {
                // Avoid allocating every frame by reusing the same RT unless the configuration changed
                if (passInfo.tempRenderTexture == null || !Equals(passInfo.tempRenderTextureDesc, xrRenderPass.renderTargetDesc))
                {
                    if (passInfo.tempRenderTexture != null)
                        passInfo.tempRenderTexture.Release();

                    passInfo.tempRenderTexture = new RenderTexture(xrRenderPass.renderTargetDesc);
                    passInfo.tempRenderTexture.Create();

                    // Store the original descriptor because the one from the RT has the flag 'CreatedFromScript' and would fail the Equals()
                    passInfo.tempRenderTextureDesc = xrRenderPass.renderTargetDesc;
                }
            }

            return passInfo;
        }

        // XRTODO(2019.3) : remove once XRE-445 is done
        ~XRPass()
        {
            if (tempRenderTexture != null)
                tempRenderTexture.Release();
        }

        internal void AddView(XRDisplaySubsystem.XRRenderParameter xrSdkRenderParameter)
        {
            AddViewInternal(new XRView(xrSdkRenderParameter));
        }
#endif
        internal static void Release(XRPass xrPass)
        {
            GenericPool<XRPass>.Release(xrPass);
        }

        internal void AddViewInternal(XRView xrView)
        {
            views.Add(xrView);

            // Validate memory limitations
            Debug.Assert(views.Count <= TextureXR.slices);
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
                else if (instancingEnabled)
                {
                    if (viewCount == 2)
                    {
                        cmd.EnableShaderKeyword("STEREO_INSTANCING_ON");
#if UNITY_2019_3_OR_NEWER
                        //cmd.SetInstanceMultiplier(2);
#endif
                    }
                    else
                    {
                        throw new NotImplementedException();
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
#if UNITY_2019_3_OR_NEWER
                    //cmd.SetInstanceMultiplier(1);
#endif
                }
            }
        }

        internal void EndCamera(CommandBuffer cmd, HDCamera hdCamera, ScriptableRenderContext renderContext)
        {
            if (!enabled)
                return;

            if (xrSdkEnabled)
            {
                // XRTODO(2019.3) : remove once XRE-445 is done
                if (tempRenderTexture && hdCamera.camera.targetTexture == null)
                {
                    // Blit to device
                    cmd.SetRenderTarget(renderTarget, 0, CubemapFace.Unknown, -1);
                    cmd.SetViewport(hdCamera.finalViewport);
                    HDUtils.BlitQuad(cmd, tempRenderTexture, new Vector4(1, 1, 0, 0), new Vector4(1, 1, 0, 0), 0, false);

                    // Mirror view (only works with stereo for now)
                    if (viewCount == 1)
                    {
                        if (multipassId < 2)
                        {
                            cmd.SetRenderTarget(new RenderTargetIdentifier(BuiltinRenderTextureType.CameraTarget));
                            cmd.SetViewport(hdCamera.camera.pixelRect);

                            Vector4 scaleBiasRT = new Vector4(0.5f, 1, multipassId * 0.5f, 0);
                            HDUtils.BlitQuad(cmd, tempRenderTexture, new Vector4(1, 1, 0, 0), scaleBiasRT, 0, true);
                        }
                        else
                        {
                            throw new NotImplementedException();
                        }
                    }
                    else
                    {
                        // Only one eye for single-pass instancing for now until XRE-445 is done
                        cmd.SetRenderTarget(new RenderTargetIdentifier(BuiltinRenderTextureType.CameraTarget));
                        cmd.SetViewport(hdCamera.camera.pixelRect);
                        HDUtils.BlitQuad(cmd, tempRenderTexture, new Vector4(1, 1, 0, 0), new Vector4(1, 1, 0, 0), 0, true);
                    }
                }

                StopSinglePass(cmd, hdCamera.camera, renderContext);
            }
            else
            {
                StopSinglePass(cmd, hdCamera.camera, renderContext);

                // Pushes to XR headset and/or display mirror
                if (legacyMultipassEnabled)
                    renderContext.StereoEndRender(hdCamera.camera, legacyMultipassEye, legacyMultipassEye == 1);
                else
                    renderContext.StereoEndRender(hdCamera.camera);
            }
        }

        internal void RenderOcclusionMeshes(CommandBuffer cmd, RTHandleSystem.RTHandle depthBuffer)
        {
            if (enabled && xrSdkEnabled && occlusionMeshMaterial != null)
            {
                using (new ProfilingSample(cmd, "XR Occlusion Meshes"))
                {
                    Matrix4x4 m = Matrix4x4.Ortho(0.0f, 1.0f, 0.0f, 1.0f, -1.0f, 1.0f);

                    for (int viewId = 0; viewId < viewCount; ++viewId)
                    {
                        if (views[viewId].occlusionMesh != null)
                        {
                            HDUtils.SetRenderTarget(cmd, depthBuffer, ClearFlag.None, 0, CubemapFace.Unknown, viewId);
                            cmd.DrawMesh(views[viewId].occlusionMesh, m, occlusionMeshMaterial);
                        }
                    }
                }
            }
        }
    }
}
