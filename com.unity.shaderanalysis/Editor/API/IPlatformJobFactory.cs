using System;
using UnityEngine;

namespace UnityEditor.ShaderAnalysis
{
    /// <summary>
    /// Implement this interface to provide job capabitilies for a specific platform.
    ///
    /// <see cref="EditorShaderTools.SetPlatformJobs"/>
    /// </summary>
    public interface IPlatformJobFactory
    {
        /// <summary>The capabilities provided by this factory.</summary>
        PlatformJob capabilities { get; }

        /// <summary>Build a new job for <see cref="PlatformJob.BuildShaderPerfReport"/>.</summary>
        /// <param name="shader">the shader to analyze.</param>
        /// <returns>An async job performing the report.</returns>
        IAsyncJob CreateBuildReportJob(Shader shader, ShaderProgramFilter filter);
        /// <summary>Build a new job for <see cref="PlatformJob.BuildComputeShaderPerfReport"/>.</summary>
        /// <param name="compute">the compute shader to analyze.</param>
        /// <returns>An async job performing the report.</returns>
        IAsyncJob CreateBuildReportJob(ComputeShader compute, ShaderProgramFilter filter);
        /// <summary>Build a new job for <see cref="PlatformJob.BuildMaterialPerfReport"/>.</summary>
        /// <param name="material">the material to analyze.</param>
        /// <returns>An async job performing the report.</returns>
        IAsyncJob CreateBuildReportJob(Material material, ShaderProgramFilter filter);
    }

    public static class PlatformJobFactoryExtensions
    {
        /// <summary>Wether a factory has a specific capability.</summary>
        /// <param name="factory">The factory to check.</param>
        /// <param name="job">the job to check.</param>
        /// <returns><c>true</c> when the factory can provide a <paramref name="job"/>.</returns>
        public static bool HasCapability(this IPlatformJobFactory factory, PlatformJob job)
            => (factory.capabilities & job) == job;
    }
}
