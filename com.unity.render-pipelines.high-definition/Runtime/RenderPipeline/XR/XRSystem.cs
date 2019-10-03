// XRSystem is where information about XR views and passes are read from 2 exclusive sources:
// - XRDisplaySubsystem from the XR SDK
// - or the 'legacy' C++ stereo rendering path and XRSettings (will be removed in 2020.1)

using System;
using System.Collections.Generic;
using UnityEngine.XR;

namespace UnityEngine.Rendering.HighDefinition
{
    // XRTODO: custom user defined views + culling
    internal enum XRLayoutOverride
    {
        None,                       // default layout
        TestComposite,              // split the  into tiles to simulate multi-pass
        TestSinglePassOneEye,       // render only eye with single-pass path
    }

    internal class XRSystem
    {
        // Valid empty pass when a camera is not using XR
        internal readonly XRPass emptyPass = new XRPass();

        // Display layout override property
        internal static XRLayoutOverride layoutOverride { get; set; } = XRLayoutOverride.None;

        // Used by test framework
        internal static bool testModeEnabled { get => Array.Exists(Environment.GetCommandLineArgs(), arg => arg == "-xr-tests"); }

        // Store active passes and avoid allocating memory every frames
        List<(Camera, XRPass)> framePasses = new List<(Camera, XRPass)>();

#if ENABLE_XR_MODULE
        // XR SDK display interface
        static List<XRDisplaySubsystem> displayList = new List<XRDisplaySubsystem>();
        XRDisplaySubsystem display = null;

        // Internal resources used by XR rendering
        Material occlusionMeshMaterial = null;
        Material mirrorViewMaterial = null;
        MaterialPropertyBlock mirrorViewMaterialProperty = new MaterialPropertyBlock();
#endif

        internal XRSystem(RenderPipelineResources.ShaderResources shaders)
        {
#if ENABLE_XR_MODULE
            RefreshXrSdk();

            if (shaders != null)
            {
                occlusionMeshMaterial = CoreUtils.CreateEngineMaterial(shaders.xrOcclusionMeshPS);
                mirrorViewMaterial = CoreUtils.CreateEngineMaterial(shaders.xrMirrorViewPS);
            }
#endif
            // XRTODO: replace by dynamic render graph
            TextureXR.maxViews = GetMaxViews();
        }

#if ENABLE_XR_MODULE
        // With XR SDK: disable legacy VR system before rendering first frame
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSplashScreen)]
        internal static void XRSystemInit()
        {
            SubsystemManager.GetInstances(displayList);

            for (int i = 0; i < displayList.Count; i++)
                displayList[i].disableLegacyRenderer = true;
        }
#endif

        // Compute the maximum number of views (slices) to allocate for texture arrays
        internal int GetMaxViews()
        {
            int maxViews = 1;

#if ENABLE_XR_MODULE
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

                if (testModeEnabled)
                    maxViews = 2;
            }

            return maxViews;
        }

        internal List<(Camera, XRPass)> SetupFrame(Camera[] cameras)
        {
            bool xrSdkActive = RefreshXrSdk();

            if (framePasses.Count > 0)
            {
                Debug.LogWarning("XRSystem.ReleaseFrame() was not called!");
                ReleaseFrame();
            }

            foreach (var camera in cameras)
            {
                if (camera == null)
                    continue;

#if ENABLE_VR && ENABLE_VR_MODULE
                // Read XR SDK or legacy settings
                bool xrEnabled = xrSdkActive || (camera.stereoEnabled && XRGraphics.enabled);

                // Enable XR layout only for gameview camera
                bool xrSupported = camera.cameraType == CameraType.Game && camera.targetTexture == null;

                // Debug modes can override the entire layout
                if (ProcessDebugMode(xrEnabled, camera))
                    continue;

                if (xrEnabled && xrSupported)
                {
                    if (XRGraphics.renderViewportScale != 1.0f)
                    {
                        Debug.LogWarning("RenderViewportScale has no effect with this render pipeline. Use dynamic resolution instead.");
                    }

                    if (xrSdkActive)
                    {
                        CreateLayoutFromXrSdk(camera);
                    }
                    else
                    {
                        CreateLayoutLegacyStereo(camera);
                    }
                }
                else
#endif
                {
                    AddPassToFrame(camera, emptyPass);
                }
            }

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
#if ENABLE_XR_MODULE
            SubsystemManager.GetInstances(displayList);

            if (displayList.Count > 0)
            {
                if (displayList.Count > 1)
                    throw new NotImplementedException("Only 1 XR display is supported.");

                display = displayList[0];
                display.disableLegacyRenderer = true;

                return display.running;
            }
            else
            {
                display = null;
            }
#endif

            return false;
        }

        void CreateLayoutLegacyStereo(Camera camera)
        {
            if (!camera.TryGetCullingParameters(true, out var cullingParams))
            {
                Debug.LogError("Unable to get Culling Parameters from camera!");
                return;
            }

            if (XRGraphics.stereoRenderingMode == XRGraphics.StereoRenderingMode.MultiPass)
            {
                for (int passIndex = 0; passIndex < 2; ++passIndex)
                {
                    var xrPass = XRPass.Create(multipassId: passIndex, cullingPassId: 0, cullingParams);
                    xrPass.AddView(camera, (Camera.StereoscopicEye)passIndex);

                    AddPassToFrame(camera, xrPass);
                }
            }
            else
            {
                var xrPass = XRPass.Create(multipassId: 0, cullingPassId: 0, cullingParams);

                for (int viewIndex = 0; viewIndex < 2; ++viewIndex)
                {
                    xrPass.AddView(camera, (Camera.StereoscopicEye)viewIndex);
                }

               AddPassToFrame(camera, xrPass);
            }
        }

#if ENABLE_XR_MODULE
        void CreateLayoutFromXrSdk(Camera camera)
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

                if (CanUseSinglePass(renderPass))
                {
                    var xrPass = XRPass.Create(renderPass, multipassId: framePasses.Count, textureArraySlice: -1, cullingParams, occlusionMeshMaterial);

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

                        var xrPass = XRPass.Create(renderPass, multipassId: framePasses.Count, textureArraySlice: renderParam.textureArraySlice, cullingParams, occlusionMeshMaterial);
                        xrPass.AddView(renderPass, renderParam);

                        AddPassToFrame(camera, xrPass);
                    }
                }
            }
        }
#endif

        internal void Cleanup()
        {
#if ENABLE_XR_MODULE
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
#if ENABLE_XR_MODULE
            if (display == null || !display.running)
                return;

            using (new ProfilingSample(cmd, "XR Mirror View"))
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

        bool ProcessDebugMode(bool xrEnabled, Camera camera)
        {
            if (layoutOverride == XRLayoutOverride.None || camera.cameraType != CameraType.Game || xrEnabled)
                return false;

            if (camera.TryGetCullingParameters(false, out var cullingParams))
            {
                cullingParams.stereoProjectionMatrix = camera.projectionMatrix;
                cullingParams.stereoViewMatrix = camera.worldToCameraMatrix;
            }
            else
            {
                Debug.LogError("Unable to get Culling Parameters from camera!");
                return false;
            }

            if (layoutOverride == XRLayoutOverride.TestSinglePassOneEye)
            {
                var xrPass = XRPass.Create(multipassId: framePasses.Count, cullingPassId: 0, cullingParams, camera.targetTexture);

                // 2x single-pass
                for (int i = 0; i < 2; ++i)
                    xrPass.AddView(camera.projectionMatrix, camera.worldToCameraMatrix, camera.pixelRect);

                AddPassToFrame(camera, xrPass);
            }
            else if (layoutOverride == XRLayoutOverride.TestComposite)
            {
                Rect fullViewport = camera.pixelRect;

                // Split into 4 tiles covering the original viewport
                int tileCountX = 2;
                int tileCountY = 2;
                float splitRatio = 2.0f;

                // Use frustum planes to split the projection into 4 parts
                var frustumPlanes = camera.projectionMatrix.decomposeProjection;

                for (int tileY = 0; tileY < tileCountY; ++tileY)
                {
                    for (int tileX = 0; tileX < tileCountX; ++tileX)
                    {
                        var xrPass = XRPass.Create(multipassId: framePasses.Count, cullingPassId: 0, cullingParams, camera.targetTexture);

                        float spliRatioX1 = Mathf.Pow((tileX + 0.0f) / tileCountX, splitRatio);
                        float spliRatioX2 = Mathf.Pow((tileX + 1.0f) / tileCountX, splitRatio);
                        float spliRatioY1 = Mathf.Pow((tileY + 0.0f) / tileCountY, splitRatio);
                        float spliRatioY2 = Mathf.Pow((tileY + 1.0f) / tileCountY, splitRatio);

                        var planes = frustumPlanes;
                        planes.left = Mathf.Lerp(frustumPlanes.left, frustumPlanes.right, spliRatioX1);
                        planes.right = Mathf.Lerp(frustumPlanes.left, frustumPlanes.right, spliRatioX2);
                        planes.bottom = Mathf.Lerp(frustumPlanes.bottom, frustumPlanes.top, spliRatioY1);
                        planes.top = Mathf.Lerp(frustumPlanes.bottom, frustumPlanes.top, spliRatioY2);

                        float tileOffsetX = spliRatioX1 * fullViewport.width;
                        float tileOffsetY = spliRatioY1 * fullViewport.height;
                        float tileSizeX = spliRatioX2 * fullViewport.width - tileOffsetX;
                        float tileSizeY = spliRatioY2 * fullViewport.height - tileOffsetY;

                        Rect viewport = new Rect(fullViewport.x + tileOffsetX, fullViewport.y + tileOffsetY, tileSizeX, tileSizeY);
                        Matrix4x4 proj = camera.orthographic ? Matrix4x4.Ortho(planes.left, planes.right, planes.bottom, planes.top, planes.zNear, planes.zFar) : Matrix4x4.Frustum(planes);

                        xrPass.AddView(proj, camera.worldToCameraMatrix, viewport);
                        AddPassToFrame(camera, xrPass);
                    }
                }
            }

            return true;
        }
    }
}
