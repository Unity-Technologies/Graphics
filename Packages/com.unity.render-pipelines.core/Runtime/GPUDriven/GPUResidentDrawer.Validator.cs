#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.Rendering;
#endif

namespace UnityEngine.Rendering
{
    public partial class GPUResidentDrawer
    {
        static class Strings
        {
            public static readonly string drawerModeDisabled = $"{nameof(GPUResidentDrawer)} Drawer mode is disabled. Enable it on your current {nameof(RenderPipelineAsset)}";
            public static readonly string allowInEditModeDisabled = $"{nameof(GPUResidentDrawer)} The current mode does not allow the resident drawer. Check setting Allow In Edit Mode";
            public static readonly string notGPUResidentRenderPipeline = $"{nameof(GPUResidentDrawer)} Disabled due to current render pipeline not being of type {nameof(IGPUResidentRenderPipeline)}";
            public static readonly string rawBufferNotSupportedByPlatform = $"{nameof(GPUResidentDrawer)} The current platform does not support {BatchBufferTarget.RawBuffer.GetType()}";
            public static readonly string kernelNotPresent = $"{nameof(GPUResidentDrawer)} Kernel not present, please ensure the player settings includes a supported graphics API.";
            public static readonly string batchRendererGroupShaderStrippingModeInvalid = $"{nameof(GPUResidentDrawer)} \"BatchRendererGroup Variants\" setting must be \"Keep All\". " +
                " The current setting will cause errors when building a player because all DOTS instancing shaders will be stripped" +
                " To fix, modify Graphics settings and set \"BatchRendererGroup Variants\" to \"Keep All\".";
        }

        internal static bool IsProjectSupported()
        {
            return IsProjectSupported(out string _, out LogType __);
        }

        internal static bool IsProjectSupported(out string message, out LogType severity)
        {
            message = string.Empty;
            severity = LogType.Log;

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
#endif
            // If we are forcing the system, no need to perform further checks
            if (IsForcedOnViaCommandLine())
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
    }
}
