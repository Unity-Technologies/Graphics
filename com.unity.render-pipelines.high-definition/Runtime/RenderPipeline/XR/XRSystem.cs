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
        internal readonly XRPass emptyPass = new XRPass();

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

        // Set by test framework
        internal static bool automatedTestRunning = false;

        // Used by test framework and to enable debug features
        static bool testModeEnabledInitialization { get => Array.Exists(Environment.GetCommandLineArgs(), arg => arg == "-xr-tests"); }
        internal static bool testModeEnabled = testModeEnabledInitialization;

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        internal static bool dumpDebugInfo = false;
        internal static List<string> passDebugInfos = new List<string>(8);
        internal static string ReadPassDebugInfo(int i) => passDebugInfos[i];
#endif

        internal XRSystem(RenderPipelineResources.ShaderResources shaders)
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
        	if (GraphicsSettings.currentRenderPipeline == null)
                return;

            SubsystemManager.GetInstances(displayList);

            for (int i = 0; i < displayList.Count; i++)
            {
                displayList[i].disableLegacyRenderer = true;
                displayList[i].sRGB = true;
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

            if (testModeEnabled)
                maxViews = Math.Max(maxViews, 2);

            return maxViews;
        }

        internal List<(Camera, XRPass)> SetupFrame(Camera[] cameras, bool singlePassAllowed, bool singlePassTestModeActive)
        {
            bool xrActive = RefreshXrSdk();

            if (framePasses.Count > 0)
            {
                Debug.LogWarning("XRSystem.ReleaseFrame() was not called!");
                ReleaseFrame();
            }

            if ((singlePassTestModeActive || automatedTestRunning) && testModeEnabled)
                SetCustomLayout(LayoutSinglePassTestMode);
            else
                SetCustomLayout(null);

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
                else if (xrActive && xrSupported)
                {
                    // Disable vsync on the main display when rendering to a XR device
                    QualitySettings.vSyncCount = 0;

                    CreateLayoutFromXrSdk(camera, singlePassAllowed);
                }
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
            foreach ((Camera _, XRPass xrPass) in framePasses)
            {
                if (xrPass != emptyPass)
                    XRPass.Release(xrPass);
            }

            framePasses.Clear();
        }

        bool RefreshXrSdk()
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            SubsystemManager.GetInstances(displayList);

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

        bool LayoutSinglePassTestMode(XRLayout frameLayout)
        {
            Camera camera = frameLayout.camera;

            if (camera != null && camera.cameraType == CameraType.Game && camera.TryGetCullingParameters(false, out var cullingParams))
            {
                cullingParams.stereoProjectionMatrix = camera.projectionMatrix;
                cullingParams.stereoViewMatrix = camera.worldToCameraMatrix;

                var passInfo = new XRPassCreateInfo
                {
                    multipassId = 0,
                    cullingPassId = 0,
                    cullingParameters = cullingParams,
                    renderTarget = camera.targetTexture,
                    customMirrorView = null
                };

                var viewInfo2 = new XRViewCreateInfo
                {
                    projMatrix = camera.projectionMatrix,
                    viewMatrix = camera.worldToCameraMatrix,
                    viewport = new Rect(camera.pixelRect.x, camera.pixelRect.y, camera.pixelWidth, camera.pixelHeight),
                    textureArraySlice = -1
                };

                // Change the first view so that it's a different viewpoint and projection to detect more issues
                var viewInfo1 = viewInfo2;
                var planes = viewInfo1.projMatrix.decomposeProjection;
                planes.left *= 0.44f;
                planes.right *= 0.88f;
                planes.top *= 0.11f;
                planes.bottom *= 0.33f;
                viewInfo1.projMatrix = Matrix4x4.Frustum(planes);
                viewInfo1.viewMatrix *= Matrix4x4.Translate(new Vector3(.34f, 0.25f, -0.08f));

                // single-pass 2x rendering
                {
                    XRPass pass = frameLayout.CreatePass(passInfo);

                    frameLayout.AddViewToPass(viewInfo1, pass);
                    frameLayout.AddViewToPass(viewInfo2, pass);
                }

                // valid layout
                return true;
            }

            return false;
        }
    }
}
