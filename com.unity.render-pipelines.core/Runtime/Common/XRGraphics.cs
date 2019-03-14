using System;
using UnityEditor;
#if UNITY_2017_2_OR_NEWER
using UnityEngine.XR;
using XRSettings = UnityEngine.XR.XRSettings;
#elif UNITY_5_6_OR_NEWER
using UnityEngine.VR;
using XRSettings = UnityEngine.VR.VRSettings;
#endif

namespace UnityEngine.Rendering
{
    [Serializable]
    public class XRGraphics
    { // XRGraphics insulates SRP from API changes across platforms, Editor versions, and as XR transitions into XR SDK

        public enum StereoRenderingMode
        {
            MultiPass = 0,
            SinglePass,
            SinglePassInstanced,
            SinglePassMultiView
        };

        public static float eyeTextureResolutionScale
        {
            get
            {
                if (!enabled)
                    return 1.0f;
                else
                    return XRSettings.eyeTextureResolutionScale;
            }
        }

        public static float renderViewportScale
        {
            get
            {
                if (!enabled)
                    return 1.0f;
                else
                    return XRSettings.renderViewportScale;
            }
        }

#if UNITY_EDITOR
        public static bool tryEnable
        { // TryEnable gets updated before "play" is pressed- we use this for updating GUI only.
            get { return PlayerSettings.virtualRealitySupported; }
        }
#endif

        public static bool enabled
        { // SRP should use this to safely determine whether XR is enabled at runtime.
            get
            {
#if ENABLE_VR
                return XRSettings.enabled;
#else
                return false;
#endif
            }
        }

        public static bool isDeviceActive
        {
            get
            {
                if (!enabled)
                    return false;
                return XRSettings.isDeviceActive;
            }
        }

        public static string loadedDeviceName
        {
            get
            {
                if (!enabled)
                    return "No XR device loaded";
                return XRSettings.loadedDeviceName;
            }
        }

        public static string[] supportedDevices
        {
            get
            {
                if (!enabled)
                    return new string[1];
                return XRSettings.supportedDevices;
            }
        }

        public static StereoRenderingMode stereoRenderingMode
        {
            get
            {
                if (!enabled)
                    return StereoRenderingMode.SinglePass;
#if UNITY_2018_3_OR_NEWER
                return (StereoRenderingMode)XRSettings.stereoRenderingMode;
#else // Reverse engineer it
                if (!enabled)
                    return StereoRenderingMode.SinglePassMultiView;
                if (eyeTextureDesc.vrUsage == VRTextureUsage.TwoEyes)
                {
                    if (eyeTextureDesc.dimension == UnityEngine.Rendering.TextureDimension.Tex2DArray)
                        return StereoRenderingMode.SinglePassInstanced;
                    return StereoRenderingMode.SinglePassDoubleWide;
                }
                else
                    return StereoRenderingMode.MultiPass;
#endif
            }
        }

        // XRTODO: remove once SinglePassInstanced is working
        public static uint GetPixelOffset(uint eye)
        {
            if (!enabled || stereoRenderingMode != StereoRenderingMode.SinglePass)
                return 0;
            return (uint)(Mathf.CeilToInt((eye * XRSettings.eyeTextureWidth) / 2));
        }

        public static int eyeCount
        {
            get
            {
                return enabled ? 2 : 1;
            }
        }

        public static int computePassCount
        {
            get
            {
                // XRTODO: need to also check if stereo is enabled in camera!
                if (stereoRenderingMode == StereoRenderingMode.SinglePassInstanced)
                    return eyeCount;

                return 1;
            }
        }

        public static RenderTextureDescriptor eyeTextureDesc
        {
            get
            {
                if (!enabled)
                {
                    return new RenderTextureDescriptor(0, 0);
                }

                return XRSettings.eyeTextureDesc;
            }
        }

        public static int eyeTextureWidth
        {
            get
            {
                if (!enabled)
                {
                    return 0;
                }

                return XRSettings.eyeTextureWidth;
            }
        }
        public static int eyeTextureHeight
        {
            get
            {
                if (!enabled)
                {
                    return 0;
                }

                return XRSettings.eyeTextureHeight;
            }
        }

    }
}
