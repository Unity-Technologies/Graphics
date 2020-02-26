using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEditor.ShaderAnalysis.Internal;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityEditor.ShaderAnalysis
{
    /// <summary>
    /// API to generate performance report on shaders.
    /// </summary>
    public static class EditorShaderTools
    {
        static Internal.ShaderAnalysisReport s_Instance = new Internal.ShaderAnalysisReport();

        public static IEnumerable<BuildTarget> SupportedBuildTargets => s_Instance.SupportedBuildTargets;

        /// <summary>
        /// Generate a performance report for <paramref name="args"/>.asset for the platform <paramref name="args"/>.targetPlatform.
        /// </summary>
        /// <returns>An async job that builds the report.</returns>
        /// <exception cref="ArgumentNullException">for <paramref name="args"/>.asset</exception>
        /// <exception cref="InvalidOperationException">if <see cref="PlatformJob.BuildShaderPerfReport"/> is not supported for <paramref name="targetPlatform"/></exception>
        public static IAsyncJob GenerateBuildReportAsync<TAsset>(ShaderAnalysisReport<TAsset> args)
        {
            if (args.asset == null || args.asset.Equals(null))
                throw new ArgumentNullException(nameof(args.asset));

            if (typeof(TAsset) == typeof(Shader))
            {
                if(!DoesPlatformSupport(args.common.targetPlatform, PlatformJob.BuildShaderPerfReport))
                    throw new InvalidOperationException(
                        $"Job {PlatformJob.BuildShaderPerfReport} is not supported for {args.common.targetPlatform}."
                    );
            }
            else if (typeof(TAsset) == typeof(ComputeShader))
            {
                if (!DoesPlatformSupport(args.common.targetPlatform, PlatformJob.BuildComputeShaderPerfReport))
                    throw new InvalidOperationException(
                        $"Job {PlatformJob.BuildComputeShaderPerfReport} is not supported for {args.common.targetPlatform}."
                    );
            }
            else if (typeof(TAsset) == typeof(Material))
            {
                if (!DoesPlatformSupport(args.common.targetPlatform, PlatformJob.BuildMaterialPerfReport))
                    throw new InvalidOperationException(
                        $"Job {PlatformJob.BuildMaterialPerfReport} is not supported for {args.common.targetPlatform}."
                    );
            }
            else
                throw new ArgumentOutOfRangeException(nameof(TAsset));


            return s_Instance.BuildReportAsync(args);
        }

        /// <summary>
        /// Generate a performance report for <paramref name="compute"/> for the platform <paramref name="targetPlatform"/>.
        /// </summary>
        /// <returns>An async job that builds the report.</returns>
        /// <exception cref="ArgumentNullException">for <paramref name="compute"/></exception>
        /// <exception cref="InvalidOperationException">if <see cref="PlatformJob.BuildComputeShaderPerfReport"/> is not supported for <paramref name="targetPlatform"/></exception>
        public static IAsyncJob GenerateBuildReportAsyncGeneric(ShaderAnalysisReport<Object> args)
        {
            if (args.asset == null || args.asset.Equals(null))
                throw new ArgumentNullException(nameof(args.asset));

            switch (args.asset)
            {
                case ComputeShader _:
                    return GenerateBuildReportAsync(args.Into<ComputeShader>());
                case Shader _:
                    return GenerateBuildReportAsync(args.Into<Shader>());
                case Material _:
                    return GenerateBuildReportAsync(args.Into<Material>());
                default: throw new ArgumentException($"Invalid asset: {args.asset}");
            }
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
