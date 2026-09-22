#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.Rendering;
#endif

namespace UnityEngine.Rendering
{
    public partial class GPUResidentDrawer
    {
#if !UNITY_WEBGL_RENDERER_ONLY
        static class Strings
        {
            public static readonly string drawerModeDisabled = "The GPU Resident Drawer is disabled. Enable it on your current Render Pipeline Asset.";
            public static readonly string allowInEditModeDisabled = "The current mode does not allow the GPU Resident Drawer. Check the Allow In Edit Mode setting.";
            public static readonly string notGPUResidentRenderPipeline = "The GPU Resident Drawer is disabled because the current render pipeline does not support it.";
            public static readonly string rawBufferNotSupportedByPlatform = "The current platform does not support the raw buffer path that the GPU Resident Drawer requires.";
            public static readonly string kernelNotPresent = "The GPU Resident Drawer kernel is not present. Ensure the player settings include a supported graphics API.";
            public static readonly string batchRendererGroupShaderStrippingModeInvalid = "The GPU Resident Drawer requires the \"BatchRendererGroup Variants\" setting to be \"Keep All\"." +
                " The current setting will cause errors when building a player because all DOTS instancing shaders will be stripped." +
                " To fix, modify Graphics settings and set \"BatchRendererGroup Variants\" to \"Keep All\".";
            public static readonly string visionOSNotSupported = "The GPU Resident Drawer is disabled on VisionOS as it is not applicable. This platform uses a custom rendering path and doesn't go through the resident drawer.";
        }

        internal static bool IsProjectSupported()
        {
            return IsProjectSupported(out string _, out LogType __);
        }

        internal static bool IsProjectSupported(out string message, out LogType severity)
        {
            message = string.Empty;
            severity = LogType.Log;

            if (Application.platform == RuntimePlatform.VisionOS)
            {
                message = Strings.visionOSNotSupported;
                severity = LogType.Log;
                return false;
            }

            // The GPUResidentDrawer only has support when the RawBuffer path of providing data
            // ConstantBuffer path and any other unsupported platforms early out here
            if (BatchRendererGroup.BufferTarget != BatchBufferTarget.RawBuffer)
            {
                severity = LogType.Warning;
                message  = Strings.rawBufferNotSupportedByPlatform;
                return false;
            }

#if UNITY_EDITOR
            // Check the build target is supported by checking the depth downscale kernel (which has an only_renderers pragma) is present
            var resources = GraphicsSettings.GetRenderPipelineSettings<GPUResidentDrawerResources>();
            if (!(resources.occluderDepthPyramidKernels && resources.occluderDepthPyramidKernels.HasKernel("OccluderDepthDownscale")))
            {
                severity = LogType.Warning;
                message  = Strings.kernelNotPresent;
                return false;
            }

            if (EditorGraphicsSettings.batchRendererGroupShaderStrippingMode != BatchRendererGroupStrippingMode.KeepAll)
            {
                severity = LogType.Warning;
                message = Strings.batchRendererGroupShaderStrippingModeInvalid;
                return false;
            }
#endif

            return true;
        }

        internal static bool IsGPUResidentDrawerSupportedBySRP(GPUResidentDrawerSettings settings, out string message, out LogType severity)
        {
            message = string.Empty;
            severity = LogType.Log;

            // nothing to create
            if (settings.mode == GPUResidentDrawerMode.Disabled)
            {
                message = Strings.drawerModeDisabled;
                return false;
            }

#if UNITY_EDITOR
            // In play mode, the GPU Resident Drawer is always allowed.
            // In edit mode, the GPU Resident Drawer is only allowed if the user explicitly requests it with a setting.
            bool isAllowedInCurrentMode = EditorApplication.isPlayingOrWillChangePlaymode || settings.allowInEditMode;
            if (!isAllowedInCurrentMode)
            {
                message = Strings.allowInEditModeDisabled;
                return false;
            }
			
			// Disable GRD in any external AssetImporter child process. GRD isn't made for AssetImporter workflow/lifetime
			// Avoid memory leak warning messages and also some future issues (UUM-90039)
            if (AssetDatabase.IsAssetImportWorkerProcess())
                return false;
#endif
            // If we are forcing the system, no need to perform further checks
            if (IsForcedOnViaCommandLine() || MaintainContext)
                return true;

            if (GraphicsSettings.currentRenderPipeline is not IGPUResidentRenderPipeline asset)
            {
                message = Strings.notGPUResidentRenderPipeline;
                severity = LogType.Warning;
                return false;
            }

            return asset.IsGPUResidentDrawerSupportedBySRP(out message, out severity) && IsProjectSupported(out message, out severity);
        }

        internal static void LogMessage(string message, LogType severity)
        {
            switch (severity)
            {
                case LogType.Error:
                case LogType.Exception:
                    Debug.LogError(message);
                    break;
                case LogType.Warning:
                    Debug.LogWarning(message);
                    break;
            }
        }
#else
        internal static bool IsProjectSupported()
        {
            return false;
        }

        internal static bool IsProjectSupported(out string message, out LogType severity)
        {
            message = string.Empty;
            severity = LogType.Log;
            return false;
        }

        internal static bool IsGPUResidentDrawerSupportedBySRP(GPUResidentDrawerSettings settings, out string message, out LogType severity)
        {
            message = string.Empty;
            severity = LogType.Log;
            return false;
        }

        internal static void LogMessage(string message, LogType severity) {}
#endif
    }
}
