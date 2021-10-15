// XRSystem is where information about XR views and passes are read from 2 exclusive sources:
// - XRDisplaySubsystem from the XR SDK
// - custom XR layout (only internal for now)

using System;
using System.Collections.Generic;

#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System.Diagnostics;
#endif

#if ENABLE_VR && ENABLE_XR_MODULE
using UnityEngine.XR;
#endif

namespace UnityEngine.Rendering.HighDefinition
{
    internal partial class XRSystem
    {
        // Valid empty pass when a camera is not using XR
        internal static readonly XRPass emptyPass = new XRPass();

        // Store active passes and avoid allocating memory every frames
        List<(Camera, XRPass)> framePasses = new List<(Camera, XRPass)>();

        // XRTODO: expose and document public API for custom layout
        internal delegate bool CustomLayout(XRLayout layout);
        private static CustomLayout customLayout = null;
        static internal void SetCustomLayout(CustomLayout cb) => customLayout = cb;

#if ENABLE_VR && ENABLE_XR_MODULE
        // XR SDK display interface
        static List<XRDisplaySubsystem> displayList = new List<XRDisplaySubsystem>();
        XRDisplaySubsystem display = null;

        // Internal resources used by XR rendering
        Material occlusionMeshMaterial = null;
        Material mirrorViewMaterial = null;
        MaterialPropertyBlock mirrorViewMaterialProperty = new MaterialPropertyBlock();
#endif

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        internal static bool dumpDebugInfo = false;
        internal static List<string> passDebugInfos = new List<string>(8);
        internal static string ReadPassDebugInfo(int i) => passDebugInfos[i];
#endif

        internal XRSystem(HDRenderPipelineRuntimeResources.ShaderResources shaders)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            RefreshXrSdk();

            if (shaders != null)
            {
                occlusionMeshMaterial = CoreUtils.CreateEngineMaterial(shaders.xrOcclusionMeshPS);
                mirrorViewMaterial = CoreUtils.CreateEngineMaterial(shaders.xrMirrorViewPS);
            }
#endif

            // XRTODO: replace by dynamic render graph
            TextureXR.maxViews = Math.Max(TextureXR.slices, GetMaxViews());
        }

#if ENABLE_VR && ENABLE_XR_MODULE
        // With XR SDK: disable legacy VR system before rendering first frame
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        internal static void XRSystemInit()
        {
            if (GraphicsSettings.currentRenderPipeline is HDRenderPipelineAsset)
                return;

#if UNITY_2020_2_OR_NEWER
            SubsystemManager.GetSubsystems(displayList);
#else
            SubsystemManager.GetInstances(displayList);
#endif

            for (int i = 0; i < displayList.Count; i++)
            {
                displayList[i].disableLegacyRenderer = true;
                displayList[i].sRGB = true;
                displayList[i].textureLayout = XRDisplaySubsystem.TextureLayout.Texture2DArray;
            }
        }

#endif

        // Compute the maximum number of views (slices) to allocate for texture arrays
        internal int GetMaxViews()
        {
            int maxViews = 1;

#if ENABLE_VR && ENABLE_XR_MODULE
            if (display != null)
            {
                // XRTODO : replace by API from XR SDK, assume we have 2 slices until then
                maxViews = 2;
            }
#endif

            if (XRGraphicsAutomatedTests.enabled)
                maxViews = Math.Max(maxViews, 2);

            return maxViews;
        }

#if UNITY_2021_1_OR_NEWER
        internal List<(Camera, XRPass)> SetupFrame(List<Camera> cameras, bool singlePassAllowed, bool singlePassTestModeActive)
#else
        internal List<(Camera, XRPass)> SetupFrame(Camera[] cameras, bool singlePassAllowed, bool singlePassTestModeActive)
#endif
        {
            bool xrActive = RefreshXrSdk();

            if (framePasses.Count > 0)
            {
                Debug.LogWarning("XRSystem.ReleaseFrame() was not called!");
                ReleaseFrame();
            }

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            bool singlePassTestMode = (singlePassTestModeActive || XRGraphicsAutomatedTests.running) && XRGraphicsAutomatedTests.enabled;
#endif

            foreach (var camera in cameras)
            {
                if (camera == null)
                    continue;

                // Enable XR layout only for gameview camera
                bool xrSupported = camera.cameraType == CameraType.Game && camera.targetTexture == null && HDUtils.TryGetAdditionalCameraDataOrDefault(camera).xrRendering;

                if (customLayout != null && customLayout(new XRLayout() { camera = camera, xrSystem = this }))
                {
                    // custom layout in used
                }
#if ENABLE_VR && ENABLE_XR_MODULE
                else if (xrActive && xrSupported)
                {
                    // Disable vsync on the main display when rendering to a XR device
                    QualitySettings.vSyncCount = 0;

                    if (display != null)
                    {
                        display.zNear = camera.nearClipPlane;
                        display.zFar = camera.farClipPlane;
                    }

                    CreateLayoutFromXrSdk(camera, singlePassAllowed);

#if DEVELOPMENT_BUILD || UNITY_EDITOR
                    if (singlePassTestMode)
                        OverrideForAutomatedTests(camera);
#endif
                }
#endif
                else
                {
                    AddPassToFrame(camera, emptyPass);
                }
            }

            CaptureDebugInfo();

            return framePasses;
        }

        internal void ReleaseFrame()
        {
            for (int i = 0; i < framePasses.Count; i++)
            {
                // Pop from the back to keep initial ordering (see implementation of ObjectPool)
                (Camera _, XRPass xrPass) = framePasses[framePasses.Count - i - 1];

                if (xrPass != emptyPass)
                    XRPass.Release(xrPass);
            }

            framePasses.Clear();
        }

        bool RefreshXrSdk()
        {
#if ENABLE_VR && ENABLE_XR_MODULE

#if UNITY_2020_2_OR_NEWER
            SubsystemManager.GetSubsystems(displayList);
#else
            SubsystemManager.GetInstances(displayList);
#endif

            if (displayList.Count > 0)
            {
                if (displayList.Count > 1)
                    throw new NotImplementedException("Only 1 XR display is supported.");

                display = displayList[0];
                display.disableLegacyRenderer = true;
                display.textureLayout = XRDisplaySubsystem.TextureLayout.Texture2DArray;

                return display.running;
            }
            else
            {
                display = null;
            }
#endif

            return false;
        }

        void CreateLayoutFromXrSdk(Camera camera, bool singlePassAllowed)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            bool CanUseSinglePass(XRDisplaySubsystem.XRRenderPass renderPass)
            {
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

            for (int renderPassIndex = 0; renderPassIndex < display.GetRenderPassCount(); ++renderPassIndex)
            {
                display.GetRenderPass(renderPassIndex, out var renderPass);
                display.GetCullingParameters(camera, renderPass.cullingPassIndex, out var cullingParams);

                // Disable legacy stereo culling path
                cullingParams.cullingOptions &= ~CullingOptions.Stereo;

                if (singlePassAllowed && CanUseSinglePass(renderPass))
                {
                    var xrPass = XRPass.Create(renderPass, multipassId: framePasses.Count, cullingParams, occlusionMeshMaterial);

                    for (int renderParamIndex = 0; renderParamIndex < renderPass.GetRenderParameterCount(); ++renderParamIndex)
                    {
                        renderPass.GetRenderParameter(camera, renderParamIndex, out var renderParam);
                        xrPass.AddView(renderPass, renderParam);
                    }

                    AddPassToFrame(camera, xrPass);
                }
                else
                {
                    for (int renderParamIndex = 0; renderParamIndex < renderPass.GetRenderParameterCount(); ++renderParamIndex)
                    {
                        renderPass.GetRenderParameter(camera, renderParamIndex, out var renderParam);

                        var xrPass = XRPass.Create(renderPass, multipassId: framePasses.Count, cullingParams, occlusionMeshMaterial);
                        xrPass.AddView(renderPass, renderParam);

                        AddPassToFrame(camera, xrPass);
                    }
                }
            }
#endif
        }

        internal void Cleanup()
        {
            customLayout = null;

#if ENABLE_VR && ENABLE_XR_MODULE
            CoreUtils.Destroy(occlusionMeshMaterial);
            CoreUtils.Destroy(mirrorViewMaterial);
#endif
        }

        internal void AddPassToFrame(Camera camera, XRPass xrPass)
        {
            xrPass.UpdateOcclusionMesh();
            framePasses.Add((camera, xrPass));
        }

        internal void RenderMirrorView(CommandBuffer cmd)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            if (display == null || !display.running)
                return;

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.XRMirrorView)))
            {
                cmd.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);

                int mirrorBlitMode = display.GetPreferredMirrorBlitMode();
                if (display.GetMirrorViewBlitDesc(null, out var blitDesc, mirrorBlitMode))
                {
                    if (blitDesc.nativeBlitAvailable)
                    {
                        display.AddGraphicsThreadMirrorViewBlit(cmd, blitDesc.nativeBlitInvalidStates, mirrorBlitMode);
                    }
                    else
                    {
                        for (int i = 0; i < blitDesc.blitParamsCount; ++i)
                        {
                            blitDesc.GetBlitParameter(i, out var blitParam);

                            Vector4 scaleBias = new Vector4(blitParam.srcRect.width, blitParam.srcRect.height, blitParam.srcRect.x, blitParam.srcRect.y);
                            Vector4 scaleBiasRT = new Vector4(blitParam.destRect.width, blitParam.destRect.height, blitParam.destRect.x, blitParam.destRect.y);

                            mirrorViewMaterialProperty.SetTexture(HDShaderIDs._BlitTexture, blitParam.srcTex);
                            mirrorViewMaterialProperty.SetVector(HDShaderIDs._BlitScaleBias, scaleBias);
                            mirrorViewMaterialProperty.SetVector(HDShaderIDs._BlitScaleBiasRt, scaleBiasRT);
                            mirrorViewMaterialProperty.SetInt(HDShaderIDs._BlitTexArraySlice, blitParam.srcTexArraySlice);

                            int shaderPass = (blitParam.srcTex.dimension == TextureDimension.Tex2DArray) ? 1 : 0;
                            cmd.DrawProcedural(Matrix4x4.identity, mirrorViewMaterial, shaderPass, MeshTopology.Quads, 4, 1, mirrorViewMaterialProperty);
                        }
                    }
                }
                else
                {
                    cmd.ClearRenderTarget(true, true, Color.black);
                }
            }
#endif
        }

        void CaptureDebugInfo()
        {
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (dumpDebugInfo)
            {
                passDebugInfos.Clear();

                for (int passIndex = 0; passIndex < framePasses.Count; passIndex++)
                {
                    var pass = framePasses[passIndex].Item2;
                    for (int viewIndex = 0; viewIndex < pass.viewCount; viewIndex++)
                    {
                        var viewport = pass.GetViewport(viewIndex);
                        passDebugInfos.Add(string.Format("    Pass {0} Cull {1} View {2} Slice {3} : {4} x {5}",
                            pass.multipassId,
                            pass.cullingPassId,
                            viewIndex,
                            pass.GetTextureArraySlice(viewIndex),
                            viewport.width,
                            viewport.height));
                    }
                }
            }

            while (passDebugInfos.Count < passDebugInfos.Capacity)
                passDebugInfos.Add("inactive");
#endif
        }

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        private void OverrideForAutomatedTests(Camera camera)
        {
            var camProjMatrix = camera.projectionMatrix;
            var camViewMatrix = camera.worldToCameraMatrix;

            if (camera.TryGetCullingParameters(false, out var cullingParams))
            {
                cullingParams.stereoProjectionMatrix = camProjMatrix;
                cullingParams.stereoViewMatrix = camViewMatrix;
                cullingParams.stereoSeparationDistance = 0.0f;
                cullingParams.cullingOptions &= ~CullingOptions.Stereo;

                for (int passId = 0; passId < framePasses.Count; passId++)
                {
                    var xrPass = framePasses[passId].Item2;
                    xrPass.UpdateCullingParams(xrPass.cullingPassId, cullingParams);

                    for (int viewId = 0; viewId < framePasses[passId].Item2.viewCount; viewId++)
                    {
                        var projMatrix = camProjMatrix;
                        var viewMatrix = camViewMatrix;

                        // Alter the first view in order to detect more issues
                        bool isFirstViewMultiPass = framePasses.Count == 2 && passId == 0;
                        bool isFirstViewSinglePass = framePasses.Count == 1 && viewId == 0;

                        if (isFirstViewMultiPass || isFirstViewSinglePass)
                        {
                            var planes = projMatrix.decomposeProjection;
                            planes.left *= 0.44f;
                            planes.right *= 0.88f;
                            planes.top *= 0.11f;
                            planes.bottom *= 0.33f;
                            projMatrix = Matrix4x4.Frustum(planes);
                            viewMatrix *= Matrix4x4.Translate(new Vector3(.34f, 0.25f, -0.08f));
                        }

                        xrPass.UpdateView(viewId, projMatrix, viewMatrix, xrPass.GetViewport(viewId), viewId);
                    }
                }
            }
        }

#endif
    }
}
