// XRSystem is where information about XR views and passes are read from 2 exclusive sources:
// - XRDisplaySubsystem from the XR SDK
// - the test automated test framework

#if ENABLE_VR && ENABLE_XR_MODULE

using System;
using System.Collections.Generic;
using UnityEngine.XR;

namespace UnityEngine.Rendering.Universal
{
    internal partial class XRSystem
    {
        // Valid empty pass when a camera is not using XR
        internal readonly XRPass emptyPass = new XRPass();

        // Store active passes and avoid allocating memory every frames
        List<XRPass> framePasses = new List<XRPass>();

        // XR SDK display interface
        static List<XRDisplaySubsystem> displayList = new List<XRDisplaySubsystem>();
        XRDisplaySubsystem              display = null;
        // XRSDK does not support msaa per XR display. All displays share the same msaa level.
        static  int                     msaaLevel = 1;

        // Internal resources used by XR rendering
        Material                        occlusionMeshMaterial = null;
        Material                        mirrorViewMaterial = null;
        MaterialPropertyBlock           mirrorViewMaterialProperty = new MaterialPropertyBlock();

        RenderTexture testRenderTexture = null;

        const string k_XRMirrorTag = "XR Mirror View";
        static ProfilingSampler _XRMirrorProfilingSampler = new ProfilingSampler(k_XRMirrorTag);

        internal XRSystem()
        {
            RefreshXrSdk();

            TextureXR.maxViews = Math.Max(TextureXR.slices, GetMaxViews());
        }

        internal void InitializeXRSystemData(XRSystemData data)
        {
            if (data)
            {
                if (occlusionMeshMaterial != null)
                    CoreUtils.Destroy(occlusionMeshMaterial);

                if (mirrorViewMaterial != null)
                    CoreUtils.Destroy(mirrorViewMaterial);

                occlusionMeshMaterial = CoreUtils.CreateEngineMaterial(data.shaders.xrOcclusionMeshPS);
                mirrorViewMaterial = CoreUtils.CreateEngineMaterial(data.shaders.xrMirrorViewPS);
            }
        }

        static void GetDisplaySubsystem()
        {
#if UNITY_2020_2_OR_NEWER
            //SubsystemManager.GetSubsystems(displayList);
            SubsystemManager.GetInstances(displayList);
#else
            SubsystemManager.GetInstances(displayList);
#endif
        }

        // With XR SDK: disable legacy VR system before rendering first frame
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        internal static void XRSystemInit()
        {
            if (GraphicsSettings.currentRenderPipeline == null)
                return;

            GetDisplaySubsystem();

            // XRTODO: refactor with RefreshXrSdk()
            for (int i = 0; i < displayList.Count; i++)
            {
                displayList[i].disableLegacyRenderer = true;
                displayList[i].textureLayout = XRDisplaySubsystem.TextureLayout.Texture2DArray;
                displayList[i].sRGB = QualitySettings.activeColorSpace == ColorSpace.Linear;
            }
        }

        internal static void UpdateMSAALevel(int level)
        {
            if (msaaLevel == level)
                return;

            level = Mathf.NextPowerOfTwo(level);
            level = Mathf.Clamp(level, (int)MsaaQuality.Disabled, (int)MsaaQuality._8x);

            GetDisplaySubsystem();

#if UNITY_2020_2_OR_NEWER
            for (int i = 0; i < displayList.Count; i++)
                displayList[i].SetMSAALevel(level);
#endif
            msaaLevel = level;
        }

        internal static int GetMSAALevel()
        {
            return msaaLevel;
        }

        internal static void UpdateRenderScale(float renderScale)
        {
            GetDisplaySubsystem();

            for (int i = 0; i < displayList.Count; i++)
                displayList[i].scaleOfAllRenderTargets = renderScale;
        }

        // Compute the maximum number of views (slices) to allocate for texture arrays
        internal int GetMaxViews()
        {
            int maxViews = 1;

            if (display != null)
            {
                // XRTODO : replace by API from XR SDK, assume we have 2 slices until then
                maxViews = 2;
            }
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            else if (XRGraphicsAutomatedTests.enabled)
            {
                maxViews = Math.Max(maxViews, 2);
            }
#endif

            return maxViews;
        }

        internal void BeginLateLatching(Camera camera, XRPass xrPass)
        {
            //Only support late latching for multiview use case
            if (display != null && xrPass.singlePassEnabled && xrPass.viewCount == 2)
            {
                display.BeginRecordingIfLateLatched(camera);
                xrPass.isLateLatchEnabled = true;
            }
        }

        internal void EndLateLatching(Camera camera, XRPass xrPass)
        {
            if (display != null && xrPass.isLateLatchEnabled)
            {
                display.EndRecordingIfLateLatched(camera);
                xrPass.isLateLatchEnabled = false;
            }
        }

        internal List<XRPass> SetupFrame(CameraData cameraData)
        {
            Camera camera = cameraData.camera;
            bool xrEnabled = RefreshXrSdk();

            if (display != null)
            {
                // XRTODO: Handle stereo mode selection in URP pipeline asset UI
                display.textureLayout = XRDisplaySubsystem.TextureLayout.Texture2DArray;
                display.zNear = camera.nearClipPlane;
                display.zFar  = camera.farClipPlane;
                display.sRGB  = QualitySettings.activeColorSpace == ColorSpace.Linear;
            }

            if (framePasses.Count > 0)
            {
                Debug.LogWarning("XRSystem.ReleaseFrame() was not called!");
                ReleaseFrame();
            }

            if (camera == null)
                return framePasses;

            // Enable XR layout only for game camera
            bool isGameCamera = (camera.cameraType == CameraType.Game || camera.cameraType == CameraType.VR);
            bool xrSupported = isGameCamera && camera.targetTexture == null && cameraData.xrRendering;

#if DEVELOPMENT_BUILD || UNITY_EDITOR
            if (XRGraphicsAutomatedTests.enabled && XRGraphicsAutomatedTests.running && isGameCamera && LayoutSinglePassTestMode(cameraData, new XRLayout() { camera = camera, xrSystem = this }))
            {
                // test layout in used
            }
            else
#endif
            if (xrEnabled && xrSupported)
            {
                // Disable vsync on the main display when rendering to a XR device
                // XRTODO: Quest provider has a bug where vSyncCount must be 1, otherwise app is locked to 30fps
                if (Application.platform == RuntimePlatform.Android)
                    QualitySettings.vSyncCount = 1;
                else
                    QualitySettings.vSyncCount = 0;

                CreateLayoutFromXrSdk(camera, singlePassAllowed: true);
            }
            else
            {
                AddPassToFrame(emptyPass);
            }

            return framePasses;
        }

        internal void ReleaseFrame()
        {
            for (int i = 0; i < framePasses.Count; i++)
            {
                // Pop from the back to keep initial ordering (see implementation of ObjectPool)
                var xrPass = framePasses[framePasses.Count - i - 1];

                if (xrPass != emptyPass)
                    XRPass.Release(xrPass);
            }

            framePasses.Clear();

            if (testRenderTexture)
                RenderTexture.ReleaseTemporary(testRenderTexture);
        }

        internal bool RefreshXrSdk()
        {
            GetDisplaySubsystem();

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

            return false;
        }

        // Used for updating URP cameraData data struct with XRPass data. 
        internal void UpdateCameraData(ref CameraData baseCameraData, in XRPass xr)
        {
            // Update cameraData viewport for XR
            Rect cameraRect = baseCameraData.camera.rect;
            Rect xrViewport = xr.GetViewport();
            baseCameraData.pixelRect = new Rect(cameraRect.x * xrViewport.width + xrViewport.x,
                                                cameraRect.y * xrViewport.height + xrViewport.y,
                                                cameraRect.width * xrViewport.width,
                                                cameraRect.height * xrViewport.height);
            Rect camPixelRect = baseCameraData.pixelRect;
            baseCameraData.pixelWidth  = (int)System.Math.Round(camPixelRect.width + camPixelRect.x) - (int)System.Math.Round(camPixelRect.x);
            baseCameraData.pixelHeight = (int)System.Math.Round(camPixelRect.height + camPixelRect.y) - (int)System.Math.Round(camPixelRect.y);
            baseCameraData.aspectRatio = (float)baseCameraData.pixelWidth / (float)baseCameraData.pixelHeight;

            bool isDefaultXRViewport = (!(Math.Abs(xrViewport.x) > 0.0f || Math.Abs(xrViewport.y) > 0.0f ||
                                            Math.Abs(xrViewport.width) < xr.renderTargetDesc.width ||
                                            Math.Abs(xrViewport.height) < xr.renderTargetDesc.height));
            baseCameraData.isDefaultViewport = baseCameraData.isDefaultViewport && isDefaultXRViewport;

            // Update cameraData cameraTargetDescriptor for XR. This descriptor is mainly used for configuring intermediate screen space textures
            var originalTargetDesc = baseCameraData.cameraTargetDescriptor;
            baseCameraData.cameraTargetDescriptor = xr.renderTargetDesc;
            if (baseCameraData.isHdrEnabled)
            {
                baseCameraData.cameraTargetDescriptor.graphicsFormat = originalTargetDesc.graphicsFormat;
            }
            baseCameraData.cameraTargetDescriptor.msaaSamples = originalTargetDesc.msaaSamples;
            baseCameraData.cameraTargetDescriptor.width = baseCameraData.pixelWidth;
            baseCameraData.cameraTargetDescriptor.height = baseCameraData.pixelHeight;
        }

        // Used for camera stacking where we need to update the parameters per camera
        internal void UpdateFromCamera(ref XRPass xrPass, CameraData cameraData)
        {
            bool isGameCamera = (cameraData.camera.cameraType == CameraType.Game || cameraData.camera.cameraType == CameraType.VR);
            if (XRGraphicsAutomatedTests.enabled && XRGraphicsAutomatedTests.running && isGameCamera)
            {
                // XR test framework code path. Update 2nd view with camera's view projection data
                Matrix4x4 projMatrix = cameraData.camera.projectionMatrix;
                Matrix4x4 viewMatrix = cameraData.camera.worldToCameraMatrix;
                Rect      viewport = new Rect(0, 0, testRenderTexture.width, testRenderTexture.height);
                int       textureArraySlice = -1;
                xrPass.UpdateView(1, projMatrix, viewMatrix, viewport, textureArraySlice);

                // Update culling params for this xr pass using camera's culling params
                cameraData.camera.TryGetCullingParameters(false, out var cullingParams);
                cullingParams.stereoProjectionMatrix = cameraData.camera.projectionMatrix;
                cullingParams.stereoViewMatrix = cameraData.camera.worldToCameraMatrix;

                //// Disable legacy stereo culling path
                cullingParams.cullingOptions &= ~CullingOptions.Stereo;
                xrPass.UpdateCullingParams(0, cullingParams);
            }
            else if (xrPass.enabled && display != null)
            {
                display.GetRenderPass(xrPass.multipassId, out var renderPass);
                display.GetCullingParameters(cameraData.camera, renderPass.cullingPassIndex, out var cullingParams);
                // Disable legacy stereo culling path
                cullingParams.cullingOptions &= ~CullingOptions.Stereo;

                xrPass.UpdateCullingParams(cullingPassId: renderPass.cullingPassIndex, cullingParams);
                if (xrPass.singlePassEnabled)
                {

                    for (int renderParamIndex = 0; renderParamIndex < renderPass.GetRenderParameterCount(); ++renderParamIndex)
                    {
                        renderPass.GetRenderParameter(cameraData.camera, renderParamIndex, out var renderParam);
                        xrPass.UpdateView(renderParamIndex, renderPass, renderParam);
                    }
                }
                else
                {
                    renderPass.GetRenderParameter(cameraData.camera, 0, out var renderParam);
                    xrPass.UpdateView(0, renderPass, renderParam);
                }
            }
        }

        void CreateLayoutFromXrSdk(Camera camera, bool singlePassAllowed)
        {
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
        }

        internal void Dispose()
        {
            CoreUtils.Destroy(occlusionMeshMaterial);
            CoreUtils.Destroy(mirrorViewMaterial);
        }

        internal void AddPassToFrame(XRPass xrPass)
        {
            xrPass.UpdateOcclusionMesh();
            framePasses.Add(xrPass);
        }

        internal static class XRShaderIDs
        {
            public static readonly int _SourceTexArraySlice = Shader.PropertyToID("_SourceTexArraySlice");
            public static readonly int _SRGBRead            = Shader.PropertyToID("_SRGBRead");
            public static readonly int _SRGBWrite           = Shader.PropertyToID("_SRGBWrite");
        }

        internal void RenderMirrorView(CommandBuffer cmd, Camera camera)
        {
            // XRTODO : remove this check when the Quest plugin is fixed
            if (Application.platform == RuntimePlatform.Android)
                return;

            if (display == null || !display.running || !mirrorViewMaterial)
                return;

            using (new ProfilingScope(cmd, _XRMirrorProfilingSampler))
            {
                cmd.SetRenderTarget(camera.targetTexture != null  ? camera.targetTexture : new RenderTargetIdentifier(BuiltinRenderTextureType.CameraTarget));
                bool yflip = camera.targetTexture != null || camera.cameraType == CameraType.SceneView || camera.cameraType == CameraType.Preview;
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
                            Vector4 scaleBiasRt = new Vector4(blitParam.destRect.width, blitParam.destRect.height, blitParam.destRect.x, blitParam.destRect.y);

                            // Eye texture is always gamma corrected, use explicit sRGB read in shader if srcTex formats is not sRGB format. sRGB format will have implicit sRGB read so it is already handled.
                            mirrorViewMaterialProperty.SetInt(XRShaderIDs._SRGBRead, (blitParam.srcTex.sRGB) ? 0 : 1);
                            // Perform explicit sRGB write in shader if color space is gamma
                            mirrorViewMaterialProperty.SetInt(XRShaderIDs._SRGBWrite, (QualitySettings.activeColorSpace == ColorSpace.Linear) ? 0 : 1);
                            mirrorViewMaterialProperty.SetTexture(ShaderPropertyId.sourceTex, blitParam.srcTex);
                            mirrorViewMaterialProperty.SetVector(ShaderPropertyId.scaleBias, scaleBias);
                            mirrorViewMaterialProperty.SetVector(ShaderPropertyId.scaleBiasRt, scaleBiasRt);
                            mirrorViewMaterialProperty.SetInt(XRShaderIDs._SourceTexArraySlice, blitParam.srcTexArraySlice);

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
        }

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        static MaterialPropertyBlock testMirrorViewMaterialProperty = new MaterialPropertyBlock();
        static Material testMirrorViewMaterial = null;

        static void copyToTestRenderTexture(XRPass pass, CommandBuffer cmd, RenderTexture rt, Rect viewport)
        {
            cmd.SetViewport(viewport);
            cmd.SetRenderTarget(rt == null ? new RenderTargetIdentifier(BuiltinRenderTextureType.CameraTarget) : rt);

            Vector4 scaleBias = new Vector4(1.0f, 1.0f, 0.0f, 0.0f);
            Vector4 scaleBiasRT = new Vector4(1.0f, 1.0f, 0.0f, 0.0f);

            if (rt == null)
            {
                scaleBias.y = -1.0f;
                scaleBias.w = 1.0f;
            }

            testMirrorViewMaterialProperty.SetVector(ShaderPropertyId.scaleBias, scaleBias);
            testMirrorViewMaterialProperty.SetVector(ShaderPropertyId.scaleBiasRt, scaleBiasRT);

            // Copy result from the second slice
            testMirrorViewMaterialProperty.SetInt(XRShaderIDs._SourceTexArraySlice, 1);

            cmd.DrawProcedural(Matrix4x4.identity, testMirrorViewMaterial, 1, MeshTopology.Quads, 4, 1, testMirrorViewMaterialProperty);
        }

        static XRPass.CustomMirrorView testMirrorView = copyToTestRenderTexture;

        bool LayoutSinglePassTestMode(CameraData cameraData, XRLayout frameLayout)
        {
            Camera camera = frameLayout.camera;

            if (camera == null)
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
                    // If camera renders to subrect, we adjust size to match back buffer/target texture
                    if (!cameraData.isDefaultViewport)
                    {
                        if (cameraData.targetTexture == null)
                        {
                            rtDesc.width = (int)(rtDesc.width / cameraData.camera.rect.width);
                            rtDesc.height = (int)(rtDesc.height / cameraData.camera.rect.height);
                        }
                        else
                        {
                            rtDesc.width = (int)(cameraData.targetTexture.width);
                            rtDesc.height = (int)(cameraData.targetTexture.height);
                        }
                    }
                    testRenderTexture = RenderTexture.GetTemporary(rtDesc);

                    testMirrorViewMaterial = mirrorViewMaterial;
                    testMirrorViewMaterialProperty.SetInt(XRShaderIDs._SRGBRead, (testRenderTexture.sRGB) ? 0 : 1);
                    testMirrorViewMaterialProperty.SetInt(XRShaderIDs._SRGBWrite, (QualitySettings.activeColorSpace == ColorSpace.Linear) ? 0 : 1);
                    testMirrorViewMaterialProperty.SetTexture(ShaderPropertyId.sourceTex, testRenderTexture);
                }

                var passInfo = new XRPassCreateInfo
                {
                    multipassId = 0,
                    cullingPassId = 0,
                    cullingParameters = cullingParams,
                    renderTarget = testRenderTexture,
                    renderTargetIsRenderTexture = true,
                    customMirrorView = testMirrorView
                };

                var viewInfo2 = new XRViewCreateInfo
                {
                    projMatrix = camera.projectionMatrix,
                    viewMatrix = camera.worldToCameraMatrix,
                    viewport = new Rect(0, 0, testRenderTexture.width, testRenderTexture.height),
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
#endif
    }
}

#endif
