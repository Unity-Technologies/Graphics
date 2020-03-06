using System;
using UnityEditor;

#if ENABLE_VR && ENABLE_VR_MODULE
using UnityEngine.XR;
#endif

namespace UnityEngine.Rendering
{
    /// <summary>
    /// XRGraphics insulates SRP from API changes across platforms, Editor versions, and as XR transitions into XR SDK
    /// </summary>
    [Serializable]
    public class XRGraphics
    {
        /// <summary>
        /// Stereo Rendering Modes.
        /// </summary>
        public enum StereoRenderingMode
        {
            /// <summary>Multi Pass.</summary>
            MultiPass = 0,
            /// <summary>Single Pass.</summary>
            SinglePass,
            /// <summary>Single Pass Instanced.</summary>
            SinglePassInstanced,
            /// <summary>Single Pass Multi View.</summary>
            SinglePassMultiView
        };

        /// <summary>
        /// Eye texture resolution scale.
        /// </summary>
        public static float eyeTextureResolutionScale
        {
            get
            {
#if ENABLE_VR && ENABLE_VR_MODULE
                if (enabled)
                    return XRSettings.eyeTextureResolutionScale;
#endif
                return 1.0f;
            }
        }

        /// <summary>
        /// Render viewport scale.
        /// </summary>
        public static float renderViewportScale
        {
            get
            {
#if ENABLE_VR && ENABLE_VR_MODULE
                if (enabled)
                    return XRSettings.renderViewportScale;
#endif
                return 1.0f;
            }
        }

        /// <summary>
        /// Try enable.
        /// </summary>
#if UNITY_EDITOR
        // TryEnable gets updated before "play" is pressed- we use this for updating GUI only.
        public static bool tryEnable
        {
            get
            {
            #if UNITY_2020_1_OR_NEWER
                return false;
            #else
                return UnityEditorInternal.VR.VREditor.GetVREnabledOnTargetGroup(BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget));
            #endif
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
        /// Stereo rendering mode.
        /// </summary>
        public static StereoRenderingMode stereoRenderingMode
        {
            get
            {
#if ENABLE_VR && ENABLE_VR_MODULE
                if (enabled)
                    return (StereoRenderingMode)XRSettings.stereoRenderingMode;
#endif

                return StereoRenderingMode.SinglePass;
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
    }
}
