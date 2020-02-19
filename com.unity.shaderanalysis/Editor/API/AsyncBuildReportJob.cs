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

        public ShaderProgramFilter filter { get; }
        protected BuildReportFeature features { get; }

        protected AsyncBuildReportJob(BuildTarget target, ShaderProgramFilter filter, BuildReportFeature features)
        {
            this.target = target;
            this.filter = filter ?? new ShaderProgramFilter();
            this.features = features;
        }
    }
}
