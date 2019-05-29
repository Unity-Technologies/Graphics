using System;
using UnityEditor;

#if ENABLE_VR

#if UNITY_2017_2_OR_NEWER
using UnityEngine.XR;
using XRSettings = UnityEngine.XR.XRSettings;
#elif UNITY_5_6_OR_NEWER
using UnityEngine.VR;
using XRSettings = UnityEngine.VR.VRSettings;
#endif

#endif // ENABLE_VR

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
#if ENABLE_VR
                if (enabled)
                    return XRSettings.eyeTextureResolutionScale;
#endif
                return 1.0f;
            }
        }

        public static float renderViewportScale
        {
            get
            {
#if ENABLE_VR
                if (enabled)
                    return XRSettings.renderViewportScale;
#endif
                return 1.0f;    
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
#if ENABLE_VR
                if (enabled)
                    return XRSettings.isDeviceActive;
#endif
                return false;
            }
        }

        public static string loadedDeviceName
        {
            get
            {
#if ENABLE_VR
                if (enabled)
                    return XRSettings.loadedDeviceName;
#endif
                return "No XR device loaded";
            }
        }

        public static string[] supportedDevices
        {
            get
            {
#if ENABLE_VR
                if (enabled)
                    return XRSettings.supportedDevices;
#endif
                return new string[1];
            }
        }

        public static StereoRenderingMode stereoRenderingMode
        {
            get
            {
#if ENABLE_VR
                if (enabled)
                {
    #if UNITY_2018_3_OR_NEWER
                    return (StereoRenderingMode)XRSettings.stereoRenderingMode;
    #else // Reverse engineer it
                    if (eyeTextureDesc.vrUsage == VRTextureUsage.TwoEyes)
                    {
                        if (eyeTextureDesc.dimension == UnityEngine.Rendering.TextureDimension.Tex2DArray)
                            return StereoRenderingMode.SinglePassInstanced;
                        return StereoRenderingMode.SinglePassDoubleWide;
                    }
                    else
                        return StereoRenderingMode.MultiPass;
    #endif // UNITY_2018_3_OR_NEWER
                }
#endif // ENABLE_VR

                return StereoRenderingMode.SinglePass;
            }
        }

        public static RenderTextureDescriptor eyeTextureDesc
        {
            get
            {
#if ENABLE_VR
                if (enabled)
                    return XRSettings.eyeTextureDesc;
#endif
                return new RenderTextureDescriptor(0, 0);
            }
        }

        public static int eyeTextureWidth
        {
            get
            {
#if ENABLE_VR
                if (enabled)
                    return XRSettings.eyeTextureWidth;
#endif
                return 0;
            }
        }
        public static int eyeTextureHeight
        {
            get
            {
#if ENABLE_VR
                if (enabled)
                    return XRSettings.eyeTextureHeight;
#endif
                return 0;          
            }
        }
    }
}
