using UnityEditor.Experimental.ShaderTools.Internal;
using UnityEngine;

namespace UnityEditor.Experimental.ShaderTools
{
    public static class EditorShaderTools
    {
        static EditorShaderPerformanceReport s_Instance = new EditorShaderPerformanceReport();

        public static IAsyncJob GenerateBuildReportAsync(Shader shader, BuildTarget targetPlatform)
        {
            return s_Instance.BuildReportAsync(shader, targetPlatform);
        }

        public static IAsyncJob GenerateBuildReportAsync(Material material, BuildTarget targetPlatform)
        {
            return s_Instance.BuildReportAsync(material, targetPlatform);
        }

        public static IAsyncJob GenerateBuildReportAsync(ComputeShader compute, BuildTarget targetPlatform)
        {
            return s_Instance.BuildReportAsync(compute, targetPlatform);
        }

        public static bool DoesPlatformSupport(BuildTarget targetPlatform, PlatformJob job)
        {
            return s_Instance.DoesPlatformSupport(targetPlatform, job);
        }

        public static void SetPlatformJobs(BuildTarget target, IPlatformJobFactory factory)
        {
            s_Instance.SetPlatformJobs(target, factory);
        }
    }
}
