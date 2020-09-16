using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Rendering.HighDefinition
{
    public class DeviceInfo
    {
        public static int optimalThreadGroupSize;
        public static int log2NumClusters; // / MSB of optimalThreadGroupSize, NumClusters is 1<<g_iLog2NumClusters
        public static string kernelVariantSuffix;
        public static bool preferComputeKernels;
        public static bool requiresExplicitMSAAResolve;

        static DeviceInfo()
        {
            ProbeDeviceInfo();
        }

        public static void ProbeDeviceInfo()
        {
            // Reset to defaults first
            optimalThreadGroupSize = 64;
            log2NumClusters = 6;
            kernelVariantSuffix = "";
            preferComputeKernels = true;
            requiresExplicitMSAAResolve = true;

            bool threadExecutionWidth32 = IsMobileBuildTarget;

            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Metal)
            {
                // TODO: Could conditionally enable threadExecutionWidth32/disable preferComputeKernels based on GPU
                // SystemInfo.hasHiddenSurfaceRemovalOnGPU == true on Apple GPUs
                requiresExplicitMSAAResolve = false;
            }
            else if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Switch)
            {
                threadExecutionWidth32 = true;
                preferComputeKernels = false;
            }

            if (threadExecutionWidth32 == true)
            {
                optimalThreadGroupSize = 32;
                log2NumClusters = 5;
                kernelVariantSuffix = "_LE";
            }

            // Debug.LogWarning("DeviceInfo: optimalThreadGroupSize: " + optimalThreadGroupSize + " preferComputeKernels: " + preferComputeKernels);
        }

        public static int FindKernel(ComputeShader cs, string name)
        {
            if (kernelVariantSuffix.Length != 0)
            {
                cs.EnableKeyword("PLATFORM_LANE_COUNT_32");
                string nameVariant = String.Concat(name, kernelVariantSuffix);
                if (cs.HasKernel(nameVariant))
                    return cs.FindKernel(nameVariant);
            }
            return cs.FindKernel(name);
        }

        public static bool IsMobileBuildTarget
        {
            get
            {
#if UNITY_EDITOR
                switch (EditorUserBuildSettings.activeBuildTarget)
                {
                    case BuildTarget.iOS:
                    case BuildTarget.Android:
                        return true;
                    default:
                        return false;
                }
#else
                return Application.isMobilePlatform;
#endif
            }
        }
    }
}
