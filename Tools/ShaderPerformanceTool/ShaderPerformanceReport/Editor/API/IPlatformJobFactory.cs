using UnityEngine;

namespace UnityEditor.Experimental.ShaderTools
{
    public interface IPlatformJobFactory
    {
        PlatformJob capabilities { get; }

        IAsyncJob CreateBuildReportJob(Shader shader);
        IAsyncJob CreateBuildReportJob(ComputeShader compute);
        IAsyncJob CreateBuildReportJob(Material material);
    }

    public static class IPlatformJobFactoryExtensions
    {
        public static bool HasCapability(this IPlatformJobFactory factory, PlatformJob job)
        {
            return (factory.capabilities & job) == job;
        }
    }
}
