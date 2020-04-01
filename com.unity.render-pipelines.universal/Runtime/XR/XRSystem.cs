// XRSystem is where information about XR views and passes are read from 2 exclusive sources:
// - XRDisplaySubsystem from the XR SDK
// - the test automated test framework

using System;
using System.Collections.Generic;

#if ENABLE_VR && ENABLE_XR_MODULE
using UnityEngine.XR;
#endif

namespace UnityEngine.Rendering.Universal
{
    internal partial class XRSystem
    {
        // Valid empty pass when a camera is not using XR
        internal readonly XRPass emptyPass = new XRPass();

        // Store active passes and avoid allocating memory every frames
        List<XRPass> framePasses = new List<XRPass>();

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

        RenderTexture testRenderTexture = null;

        const string k_XRMirrorTag = "XR Mirror View";
        static ProfilingSampler _XRMirrorProfilingSampler = new ProfilingSampler(k_XRMirrorTag);

        internal XRSystem()
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            RefreshXrSdk();
#endif

            // XRTODO: replace by dynamic render graph
            TextureXR.maxViews = Math.Max(TextureXR.slices, GetMaxViews());
        }

        internal void InitializeXRSystemData(XRSystemData data)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            if (data)
            {
                occlusionMeshMaterial = CoreUtils.CreateEngineMaterial(data.shaders.xrOcclusionMeshPS);
                mirrorViewMaterial = CoreUtils.CreateEngineMaterial(data.shaders.xrMirrorViewPS);
            }
#endif
        }

#if ENABLE_VR && ENABLE_XR_MODULE
        // With XR SDK: disable legacy VR system before rendering first frame
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        internal static void XRSystemInit()
        {
            SubsystemManager.GetInstances(displayList);

            // XRTODO: refactor with RefreshXrSdk()
            for (int i = 0; i < displayList.Count; i++)
            {
                displayList[i].disableLegacyRenderer = true;
                displayList[i].textureLayout = XRDisplaySubsystem.TextureLayout.Texture2DArray;
                displayList[i].sRGB = QualitySettings.activeColorSpace == ColorSpace.Linear;
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
            else
#endif
            {
                if (XRGraphics.stereoRenderingMode == XRGraphics.StereoRenderingMode.SinglePassInstanced)
                    maxViews = 2;
            }

            if (testModeEnabled)
                maxViews = Math.Max(maxViews, 2);

            return maxViews;
        }

        // XRTODO: Remove MountShimLayer/UnmountShimLayer. (Workaround URP Skybox using legacy skybox shader, XRSDK MSAA not exposed to SRP)
        internal bool MountShimLayer()
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            if (display != null)
            {
                display.disableLegacyRenderer = false;
                return true;
            }
#endif
            return false;
        }

        internal bool UnmountShimLayer()
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            if (display != null)
            {
                display.disableLegacyRenderer = true;
                return true;
            }
#endif
            return false;
        }

        internal List<XRPass> SetupFrame(ref CameraData cameraData, bool singlePassAllowed, bool singlePassTestModeActive)
        {
            Camera camera = cameraData.camera;
            bool xrEnabled = RefreshXrSdk();

#if ENABLE_VR && ENABLE_XR_MODULE
            if (display != null)
            {
                // XRTODO: Handle stereo mode selection in URP pipeline asset UI
                display.textureLayout = XRDisplaySubsystem.TextureLayout.Texture2DArray;
                display.zNear = cameraData.camera.nearClipPlane;
                display.zFar  = cameraData.camera.farClipPlane;
                display.sRGB  = QualitySettings.activeColorSpace == ColorSpace.Linear;
            }
#endif

            if (framePasses.Count > 0)
            {
                Debug.LogWarning("XRSystem.ReleaseFrame() was not called!");
                ReleaseFrame();
            }

            if (camera == null)
                return framePasses;

            // Enable XR layout only for gameview camera
            bool xrSupported = camera.cameraType == CameraType.Game && camera.targetTexture == null;

            if (testModeEnabled && automatedTestRunning && LayoutSinglePassTestMode(ref cameraData, new XRLayout() { camera = camera, xrSystem = this }))
            {
                // test layout in used
            }
            else if (xrEnabled && xrSupported)
            {
                CreateLayoutFromXrSdk(camera, singlePassAllowed);
            }
            else
            {
                AddPassToFrame(emptyPass);
            }

            return framePasses;
        }

        internal void ReleaseFrame()
        {
            foreach (XRPass xrPass in framePasses)
            {
                if (xrPass != emptyPass)
                    XRPass.Release(xrPass);
            }

            framePasses.Clear();

            if (testRenderTexture)
                RenderTexture.ReleaseTemporary(testRenderTexture);
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

                // Refresh max views
                TextureXR.maxViews = Math.Max(TextureXR.slices, GetMaxViews());

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

                    AddPassToFrame(xrPass);
                }
                else
                {
                    for (int renderParamIndex = 0; renderParamIndex < renderPass.GetRenderParameterCount(); ++renderParamIndex)
                    {
                        renderPass.GetRenderParameter(camera, renderParamIndex, out var renderParam);

                        var xrPass = XRPass.Create(renderPass, multipassId: framePasses.Count, cullingParams, occlusionMeshMaterial);
                        xrPass.AddView(renderPass, renderParam);

                        AddPassToFrame(xrPass);
                    }
                }
            }
#endif
        }

        internal void Cleanup()
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            CoreUtils.Destroy(occlusionMeshMaterial);
            CoreUtils.Destroy(mirrorViewMaterial);
#endif
        }

        internal void AddPassToFrame(XRPass xrPass)
        {
            framePasses.Add(xrPass);
        }

        internal static class XRShaderIDs
        {
            public static readonly int _BlitTexture       = Shader.PropertyToID("_BlitTexture");
            public static readonly int _BlitScaleBias     = Shader.PropertyToID("_BlitScaleBias");
            public static readonly int _BlitScaleBiasRt   = Shader.PropertyToID("_BlitScaleBiasRt");
            public static readonly int _BlitTexArraySlice = Shader.PropertyToID("_BlitTexArraySlice");
            public static readonly int _SRGBRead          = Shader.PropertyToID("_SRGBRead");
        }

        internal void RenderMirrorView(CommandBuffer cmd, ref CameraData cameraData)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            if (display == null || !display.running || !mirrorViewMaterial)
                return;

            using (new ProfilingScope(cmd, _XRMirrorProfilingSampler))
            {
                cmd.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);
                bool yflip = cameraData.targetTexture != null || cameraData.camera.cameraType == CameraType.SceneView || cameraData.camera.cameraType == CameraType.Preview;
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

                            Vector4 scaleBias = yflip ? new Vector4(blitParam.srcRect.width, -blitParam.srcRect.height, blitParam.srcRect.x, blitParam.srcRect.height + blitParam.srcRect.y) :
                                                        new Vector4(blitParam.srcRect.width, blitParam.srcRect.height, blitParam.srcRect.x, blitParam.srcRect.y);
                            Vector4 scaleBiasRT = new Vector4(blitParam.destRect.width, blitParam.destRect.height, blitParam.destRect.x, blitParam.destRect.y);

                            mirrorViewMaterialProperty.SetInt(XRShaderIDs._SRGBRead, blitParam.srcTex.sRGB ? 0 : 1);
                            mirrorViewMaterialProperty.SetTexture(XRShaderIDs._BlitTexture, blitParam.srcTex);
                            mirrorViewMaterialProperty.SetVector(XRShaderIDs._BlitScaleBias, scaleBias);
                            mirrorViewMaterialProperty.SetVector(XRShaderIDs._BlitScaleBiasRt, scaleBiasRT);
                            mirrorViewMaterialProperty.SetInt(XRShaderIDs._BlitTexArraySlice, blitParam.srcTexArraySlice);

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

        bool LayoutSinglePassTestMode(ref CameraData cameraData, XRLayout frameLayout)
        {
            Camera camera = frameLayout.camera;

            if (camera == null || camera.targetTexture == null || camera.cameraType != CameraType.Game)
                return false;

            if (camera.TryGetCullingParameters(false, out var cullingParams))
            {
                cullingParams.stereoProjectionMatrix = camera.projectionMatrix;
                cullingParams.stereoViewMatrix = camera.worldToCameraMatrix;

                // Allocate temp target to render test scene with single-pass
                // And copy the last view to the actual render texture used to compare image in test framework
                {
                    RenderTextureDescriptor rtDesc = cameraData.cameraTargetDescriptor;
                    rtDesc.dimension = TextureDimension.Tex2DArray;
                    rtDesc.volumeDepth = 2;

                    testRenderTexture = RenderTexture.GetTemporary(rtDesc);
                }

                var passInfo = new XRPassCreateInfo
                {
                    multipassId = 0,
                    cullingPassId = 0,
                    cullingParameters = cullingParams,
                    renderTarget = testRenderTexture,
                    renderTargetIsRenderTexture = true,
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
