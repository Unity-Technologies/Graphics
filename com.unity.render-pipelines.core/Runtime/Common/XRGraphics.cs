using System;
using UnityEditor;

#if ENABLE_VR && ENABLE_VR_MODULE
using UnityEngine.XR;
#endif

namespace UnityEngine.Rendering
{
    // XRGraphics insulates SRP from API changes across platforms, Editor versions, and as XR transitions into XR SDK
    [Serializable]
    public class XRGraphics
    {
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
#if ENABLE_VR && ENABLE_VR_MODULE
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
#if ENABLE_VR && ENABLE_VR_MODULE
                if (enabled)
                    return XRSettings.renderViewportScale;
#endif
                return 1.0f;    
            }
        }

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

        // SRP should use this to safely determine whether XR is enabled at runtime.
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
