using System;
using System.Collections.Generic;
using UnityEditor.ShaderAnalysis.Internal;
using UnityEngine;

namespace UnityEditor.ShaderAnalysis
{
    /// <summary>
    /// API to generate performance report on shaders.
    /// </summary>
    public static class EditorShaderTools
    {
        static ShaderAnalysisReport s_Instance = new ShaderAnalysisReport();

        public static IEnumerable<BuildTarget> SupportedBuildTargets => s_Instance.SupportedBuildTargets;

        /// <summary>
        /// Generate a performance report for <paramref name="shader"/> for the platform <paramref name="targetPlatform"/>.
        /// </summary>
        /// <param name="shader">Shader to build.</param>
        /// <param name="targetPlatform">Target to build.</param>
        /// <returns>An async job that builds the report.</returns>
        /// <exception cref="ArgumentNullException">for <paramref name="shader"/></exception>
        /// <exception cref="InvalidOperationException">if <see cref="PlatformJob.BuildShaderPerfReport"/> is not supported for <paramref name="targetPlatform"/></exception>
        public static IAsyncJob GenerateBuildReportAsync(Shader shader, BuildTarget targetPlatform)
        {
            if (shader == null || shader.Equals(null))
                throw new ArgumentNullException(nameof(shader));
            if(!DoesPlatformSupport(targetPlatform, PlatformJob.BuildShaderPerfReport))
                throw new InvalidOperationException(
                    $"Job {PlatformJob.BuildShaderPerfReport} is not supported for {targetPlatform}."
                );

            return s_Instance.BuildReportAsync(shader, targetPlatform);
        }

        /// <summary>
        /// Generate a performance report for the shader used by <paramref name="material"/> for the platform <paramref name="targetPlatform"/>.
        /// </summary>
        /// <param name="material">Material to build.</param>
        /// <param name="targetPlatform">Target to build.</param>
        /// <returns>An async job that builds the report.</returns>
        /// <exception cref="ArgumentNullException">for <paramref name="material"/></exception>
        /// <exception cref="InvalidOperationException">if <see cref="PlatformJob.BuildMaterialPerfReport"/> is not supported for <paramref name="targetPlatform"/></exception>
        public static IAsyncJob GenerateBuildReportAsync(Material material, BuildTarget targetPlatform)
        {
            if (material == null || material.Equals(null))
                throw new ArgumentNullException(nameof(material));
            if (!DoesPlatformSupport(targetPlatform, PlatformJob.BuildMaterialPerfReport))
                throw new InvalidOperationException(
                    $"Job {PlatformJob.BuildMaterialPerfReport} is not supported for {targetPlatform}."
                );

            return s_Instance.BuildReportAsync(material, targetPlatform);
        }

        /// <summary>
        /// Generate a performance report for <paramref name="compute"/> for the platform <paramref name="targetPlatform"/>.
        /// </summary>
        /// <param name="compute">Material to build.</param>
        /// <param name="targetPlatform">Target to build.</param>
        /// <returns>An async job that builds the report.</returns>
        /// <exception cref="ArgumentNullException">for <paramref name="compute"/></exception>
        /// <exception cref="InvalidOperationException">if <see cref="PlatformJob.BuildComputeShaderPerfReport"/> is not supported for <paramref name="targetPlatform"/></exception>
        public static IAsyncJob GenerateBuildReportAsync(ComputeShader compute, BuildTarget targetPlatform)
        {
            if (compute == null || compute.Equals(null))
                throw new ArgumentNullException(nameof(compute));
            if (!DoesPlatformSupport(targetPlatform, PlatformJob.BuildComputeShaderPerfReport))
                throw new InvalidOperationException(
                    $"Job {PlatformJob.BuildComputeShaderPerfReport} is not supported for {targetPlatform}."
                );

            return s_Instance.BuildReportAsync(compute, targetPlatform);
        }

        /// <summary>Check whether a specific job is supported.</summary>
        /// <param name="targetPlatform">Target platform to check.</param>
        /// <param name="job">The job to check.</param>
        /// <returns>True when the job is supported, false otherwise.</returns>
        public static bool DoesPlatformSupport(BuildTarget targetPlatform, PlatformJob job)
            => s_Instance.DoesPlatformSupport(targetPlatform, job);

        /// <summary>Set the job factory to use for a specific platform.</summary>
        /// <param name="target">The platform to set.</param>
        /// <param name="factory">The factory to use when building jobs for <paramref name="target"/></param>
        /// <exception cref="ArgumentNullException">for <paramref name="factory"/></exception>
        public static void SetPlatformJobs(BuildTarget target, IPlatformJobFactory factory)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            s_Instance.SetPlatformJobs(target, factory);
        }
    }
}
