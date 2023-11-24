using System;
using UnityEditor;
using UnityEngine.Experimental.Rendering;

#if ENABLE_VR && ENABLE_VR_MODULE
using UnityEngine.XR;
#endif

namespace UnityEngine.Rendering
{
    /// <summary>
    /// XRGraphics insulates SRP from API changes across platforms, Editor versions, and as XR transitions into XR SDK
    /// </summary>
    [Serializable]
    public class XRSRPSettings
    {
        /// <summary>
        /// Try enable.
        /// </summary>
#if UNITY_EDITOR
        // TryEnable gets updated before "play" is pressed- we use this for updating GUI only.
        public static bool tryEnable
        {
            get
            {
                return false;
            }
        }
#endif

        /// <summary>
        /// SRP should use this to safely determine whether XR is enabled at runtime.
        /// </summary>
        public static bool enabled
        {
            get
            {
#if ENABLE_VR && ENABLE_VR_MODULE
                return XRSettings.enabled;
#else
                return false;
#endif
            }
        }

        /// <summary>
        /// Returns true if the XR device is active.
        /// </summary>
        public static bool isDeviceActive
        {
            get
            {
#if ENABLE_VR && ENABLE_VR_MODULE
                if (enabled)
                    return XRSettings.isDeviceActive;
#endif
                return false;
            }
        }

        /// <summary>
        /// Name of the loaded XR device.
        /// </summary>
        public static string loadedDeviceName
        {
            get
            {
#if ENABLE_VR && ENABLE_VR_MODULE
                if (enabled)
                    return XRSettings.loadedDeviceName;
#endif
                return "No XR device loaded";
            }
        }

        /// <summary>
        /// List of supported XR devices.
        /// </summary>
        public static string[] supportedDevices
        {
            get
            {
#if ENABLE_VR && ENABLE_VR_MODULE
                if (enabled)
                    return XRSettings.supportedDevices;
#endif
                return new string[1];
            }
        }

        /// <summary>
        /// Eye texture descriptor.
        /// </summary>
        public static RenderTextureDescriptor eyeTextureDesc
        {
            get
            {
#if ENABLE_VR && ENABLE_VR_MODULE
                if (enabled)
                    return XRSettings.eyeTextureDesc;
#endif
                return new RenderTextureDescriptor(0, 0);
            }
        }

        /// <summary>
        /// Eye texture width.
        /// </summary>
        public static int eyeTextureWidth
        {
            get
            {
#if ENABLE_VR && ENABLE_VR_MODULE
                if (enabled)
                    return XRSettings.eyeTextureWidth;
#endif
                return 0;
            }
        }

        /// <summary>
        /// Eye texture height.
        /// </summary>
        public static int eyeTextureHeight
        {
            get
            {
#if ENABLE_VR && ENABLE_VR_MODULE
                if (enabled)
                    return XRSettings.eyeTextureHeight;
#endif
                return 0;
            }
        }

        /// <summary>
        /// Occlusion mesh's scaling factor. 
        /// </summary>
        public static float occlusionMeshScale
        {
            get
            {
#if ENABLE_VR && ENABLE_VR_MODULE
                if (enabled)
                    return XRSystem.GetOcclusionMeshScale();
#endif
                return 0;
            }
            set
            {
#if ENABLE_VR && ENABLE_VR_MODULE
                if (enabled)
                    XRSystem.SetOcclusionMeshScale(value);
#endif
            }
        }

        /// <summary>
        /// Controls XR mirror view blit operation
        /// </summary>
        public static int mirrorViewMode
        {
            get
            {
#if ENABLE_VR && ENABLE_VR_MODULE
                if (enabled)
                    return XRSystem.GetMirrorViewMode();
#endif
                return 0;
            }
            set
            {
#if ENABLE_VR && ENABLE_VR_MODULE
                if (enabled)
                    XRSystem.SetMirrorViewMode(value);
#endif
            }
        }
    }
}
