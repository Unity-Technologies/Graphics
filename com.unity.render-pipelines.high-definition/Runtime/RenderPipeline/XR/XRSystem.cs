// XRSystem is where information about XR views and passes are read from 2 exclusive sources:
// - XRDisplaySubsystem from the XR SDK
// - or the 'legacy' C++ stereo rendering path and XRSettings (will be removed in 2020.1)

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
    public partial class XRSystem
    {
        // Valid empty pass when a camera is not using XR
        public readonly XRPass emptyPass = new XRPass();

        // Store active passes and avoid allocating memory every frames
        List<(Camera, XRPass)> framePasses = new List<(Camera, XRPass)>();

#if USE_XR_SDK
        List<XRDisplaySubsystem> displayList = new List<XRDisplaySubsystem>();
        XRDisplaySubsystem display = null;
#endif

        internal XRSystem()
        {
            RefreshXrSdk();
        }

        internal List<(Camera, XRPass)> SetupFrame(Camera[] cameras)
        {
            bool xrSdkActive = RefreshXrSdk();

            // Validate state
            {
                Debug.Assert(framePasses.Count == 0, "XRSystem.ReleaseFrame() was not called!");

                if (XRGraphics.enabled)
                {
                    Debug.Assert(XRGraphics.stereoRenderingMode != XRGraphics.StereoRenderingMode.SinglePass, "single-pass (double-wide) is not compatible with HDRP.");
                    Debug.Assert(!xrSdkActive, "The legacy C++ stereo rendering path must be disabled with XR SDK! Go to Project Settings --> Player --> XR Settings");
                }
            }
            
            foreach (var camera in cameras)
            {
                if (camera == null)
                    continue;

                // Read XR SDK or legacy settings
                bool xrEnabled = xrSdkActive || (camera.stereoEnabled && XRGraphics.enabled);

                // Enable XR layout only for gameview camera
                // XRTODO: support render to texture
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
            TextureXR.maxViews = (XRGraphics.stereoRenderingMode == XRGraphics.StereoRenderingMode.SinglePassInstanced) ? 2 : 1;

#if USE_XR_SDK
            SubsystemManager.GetInstances(displayList);

            // XRTODO: bind cameras to XR displays (only display 0 is used for now)
            if (displayList.Count > 0)
            {
                display = displayList[0];
                display.disableLegacyRenderer = true;

                // XRTODO: handle more than 2 instanced views
                TextureXR.maxViews = 2;

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
                    // XRTODO: instanced views support with XR SDK
                }
                else
                {
                    for (int renderParamIndex = 0; renderParamIndex < renderPass.GetRenderParameterCount(); ++renderParamIndex)
                    {
                        renderPass.GetRenderParameter(camera, renderParamIndex, out var renderParam);

                        var xrPass = XRPass.Create(renderPass);
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
            DestroyDebugVolume();

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
            // XRTODO: instanced views support with XR SDK
            return false;

            // check viewCount > 1, valid texture array format and valid slice index
            // limit to 2 for now (until code fully fixed)
        }
#endif
    }
}