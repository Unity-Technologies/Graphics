using System;

namespace UnityEditor.ShaderAnalysis
{
    /// <summary>
    /// Interface for async jobs
    /// </summary>
    public interface IAsyncJob
    {
        /// <summary>
        /// Progress of the job, in range [0-1].
        /// Equals to 1.0 when job is complete.
        /// </summary>
        float progress { get; }
        /// <summary>A descriptive message for the current step of the job.</summary>
        string message { get; }
        string name { get; }

        /// <summary>Process an increment of the job.</summary>
        /// <returns><c>true</c> when the job has finished ticking, <c>false</c> when there is still some work to process.</returns>
        bool Tick();
        /// <summary>Cancels this job.</summary>
        void Cancel();

        /// <summary>Sets a callback to call when the job has completed.</summary>
        /// <param name="action">The callback to call.</param>
        void OnComplete(Action<IAsyncJob> action);
    }

    public static class AsyncJobExtensions
    {
        /// <summary>Whether this job has completed.</summary>
        /// <param name="job">The job to check.</param>
        /// <returns><c>true</c> when the job has completed, <c>false</c> otherwise.</returns>
        public static bool IsComplete(this IAsyncJob job) => job.progress >= 1;
    }
}
