// XRSystem is where information about XR views and passes are read from 2 exclusive sources:
// - XRDisplaySubsystem from the XR SDK
// - or the 'legacy' C++ stereo rendering path and XRSettings (will be removed in 2020.1)

#if UNITY_2019_3_OR_NEWER && ENABLE_VR
#define USE_XR_SDK
#endif

using System;
using System.Collections.Generic;
#if USE_XR_SDK
using UnityEngine.Experimental.XR;
#endif

namespace UnityEngine.Rendering.HighDefinition
{
    internal enum XRLayoutOverride
    {
        None,                       // default layout
        TestComposite,              // split the  into tiles to simulate multi-pass
        TestSinglePassOneEye,       // render only eye with single-pass instancing path
        //Custom,                   // user defined
    }

    internal partial class XRSystem
    {
        // Valid empty pass when a camera is not using XR
        internal readonly XRPass emptyPass = new XRPass();

        // Store active passes and avoid allocating memory every frames
        List<(Camera, XRPass)> framePasses = new List<(Camera, XRPass)>();

        // Internal resources used by XR rendering
        Material occlusionMeshMaterial = null;
        Material mirrorViewMaterial = null;
        MaterialPropertyBlock mirrorViewMaterialProperty = new MaterialPropertyBlock();

        // Display layout override property
        internal XRLayoutOverride layoutOverride { get; set; } = XRLayoutOverride.None;

#if USE_XR_SDK
        List<XRDisplaySubsystem> displayList = new List<XRDisplaySubsystem>();
        XRDisplaySubsystem display = null;
#endif

        internal XRSystem(RenderPipelineResources.ShaderResources shaders)
        {
            RefreshXrSdk();
            TextureXR.maxViews = GetMaxViews();

            if (shaders != null)
            {
                occlusionMeshMaterial = CoreUtils.CreateEngineMaterial(shaders.xrOcclusionMeshPS);
                mirrorViewMaterial = CoreUtils.CreateEngineMaterial(shaders.xrMirrorViewPS);
            }
        }

        // Compute the maximum number of views (slices) to allocate for texture arrays
        internal int GetMaxViews()
        {
            int maxViews = 1;

#if USE_XR_SDK
            if (display != null)
            {
                // XRTODO(2019.3) : replace by API from XR SDK, assume we have 2 slices until then
                maxViews = 2;
            }
            else
#endif
            {
                if (XRGraphics.stereoRenderingMode == XRGraphics.StereoRenderingMode.SinglePassInstanced)
                    maxViews = 2;

#if UNITY_EDITOR
                // Apply XR layout override if required
                if (System.Array.Exists(System.Environment.GetCommandLineArgs(), arg => arg == "-xr-tests"))
                {
                    layoutOverride = XRLayoutOverride.TestSinglePassOneEye;
                    maxViews = 2;
                }
#endif
            }

            return maxViews;
        }

        internal List<(Camera, XRPass)> SetupFrame(Camera[] cameras)
        {
            bool xrSdkActive = RefreshXrSdk();

            // Validate state
            Debug.Assert(framePasses.Count == 0, "XRSystem.ReleaseFrame() was not called!");

            foreach (var camera in cameras)
            {
                if (camera == null)
                    continue;

                // Read XR SDK or legacy settings
                bool xrEnabled = xrSdkActive || (camera.stereoEnabled && XRGraphics.enabled);

                // Enable XR layout only for gameview camera
                bool xrSupported = camera.cameraType == CameraType.Game && camera.targetTexture == null;

                // Debug modes can override the entire layout
                if (ProcessDebugMode(xrEnabled, camera))
                    continue;

                if (xrEnabled && xrSupported)
                {
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
                {
                    AddPassToFrame(camera, emptyPass);
                }
            }

            return framePasses;
        }

        bool RefreshXrSdk()
        {
#if USE_XR_SDK
            SubsystemManager.GetInstances(displayList);

            if (displayList.Count > 0)
            {
                if (displayList.Count > 1)
                    throw new NotImplementedException("Only 1 XR display is supported.");

                display = displayList[0];
                display.disableLegacyRenderer = true;

                return true;
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
            if (XRGraphics.stereoRenderingMode == XRGraphics.StereoRenderingMode.MultiPass)
            {
                for (int passIndex = 0; passIndex < 2; ++passIndex)
                {
                    var xrPass = XRPass.Create(passIndex);
                    xrPass.AddView(camera, (Camera.StereoscopicEye)passIndex);

                    AddPassToFrame(camera, xrPass);
                }
            }
            else
            {
                var xrPass = XRPass.Create(multipassId: 0);

                for (int viewIndex = 0; viewIndex < 2; ++viewIndex)
                {
                    xrPass.AddView(camera, (Camera.StereoscopicEye)viewIndex);
                }

               AddPassToFrame(camera, xrPass);
            }
        }

        void CreateLayoutFromXrSdk(Camera camera)
        {
#if USE_XR_SDK
            for (int renderPassIndex = 0; renderPassIndex < display.GetRenderPassCount(); ++renderPassIndex)
            {
                display.GetRenderPass(renderPassIndex, out var renderPass);

                if (CanUseInstancing(camera, renderPass))
                {
                    var xrPass = XRPass.Create(renderPass, multipassId: framePasses.Count, occlusionMeshMaterial);

                    for (int renderParamIndex = 0; renderParamIndex < renderPass.GetRenderParameterCount(); ++renderParamIndex)
                    {
                        renderPass.GetRenderParameter(camera, renderParamIndex, out var renderParam);
                        xrPass.AddView(renderParam);
                    }

                    AddPassToFrame(camera, xrPass);
                }
                else
                {
                    for (int renderParamIndex = 0; renderParamIndex < renderPass.GetRenderParameterCount(); ++renderParamIndex)
                    {
                        renderPass.GetRenderParameter(camera, renderParamIndex, out var renderParam);

                        var xrPass = XRPass.Create(renderPass, multipassId: framePasses.Count, occlusionMeshMaterial);
                        xrPass.AddView(renderParam);

                        AddPassToFrame(camera, xrPass);
                    }
                }
            }
#endif
        }

        internal bool GetCullingParameters(Camera camera, XRPass xrPass, out ScriptableCullingParameters cullingParams)
        {
#if USE_XR_SDK
            if (display != null)
            {
                display.GetCullingParameters(camera, xrPass.cullingPassId, out cullingParams);
            }
            else
#endif
            {
                if (!camera.TryGetCullingParameters(camera.stereoEnabled, out cullingParams))
                    return false;
            }

            return true;
        }

        internal void ClearAll()
        {
            CoreUtils.Destroy(occlusionMeshMaterial);
            occlusionMeshMaterial = null;

            framePasses = null;

#if USE_XR_SDK
            displayList = null;
            display = null;
#endif
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

        internal void AddPassToFrame(Camera camera, XRPass xrPass)
        {
            framePasses.Add((camera, xrPass));
        }

#if USE_XR_SDK
        bool CanUseInstancing(Camera camera, XRDisplaySubsystem.XRRenderPass renderPass)
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
#endif

        internal void RenderMirrorView(CommandBuffer cmd)
        {
#if USE_XR_SDK
            if (display == null)
                return;

            using (new ProfilingSample(cmd, "XR Mirror View"))
            {
                cmd.SetRenderTarget(BuiltinRenderTextureType.CameraTarget);

                if (display.GetMirrorViewBlitDesc(null, out var blitDesc))
                {
                    if (blitDesc.nativeBlitAvailable)
                    {
                        display.AddGraphicsThreadMirrorViewBlit(cmd, blitDesc.nativeBlitInvalidStates);
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
                            cmd.DrawProcedural(Matrix4x4.identity, mirrorViewMaterial, shaderPass, MeshTopology.Triangles, 3, 1, mirrorViewMaterialProperty);
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
    }
}
