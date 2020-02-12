using System;

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

        protected AsyncBuildReportJob(BuildTarget target) => this.target = target;
    }
}
