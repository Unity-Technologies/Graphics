using System;
using UnityEditor.Build.Reporting;

namespace UnityEditor.ShaderAnalysis
{
    /// <summary>A build report job helper.</summary>
    public abstract class AsyncBuildReportJob : AsyncJob
    {
        /// <summary>The target of this job.</summary>
        public BuildTarget target { get; }

        /// <summary>Get the built report</summary>
        /// <returns>The built report.</returns>
        public abstract ShaderBuildReport builtReport { get; }

        /// <summary>Wether the <see cref="builtReport"/> is available.</summary>
        public abstract bool hasReport { get; }

        /// <summary> Throw an exception when an error occurs. Useful when running in batch mode to interrupt a process.</summary>
        public bool throwOnError { get; set; } = false;

        public bool logCommandLine { get; set; } = false;

        public ShaderProgramFilter filter { get; }
        protected BuildReportFeature features { get; }

        protected AsyncBuildReportJob(ShaderAnalysisReport args)
        {
            target = args.targetPlatform;
            filter = args.filter ?? new ShaderProgramFilter();
            features = args.features;
            logCommandLine = args.logCommandLines;
        }
    }
}
