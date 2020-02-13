using System;

namespace UnityEditor.ShaderAnalysis
{
    /// <summary>Base implementation of <see cref="IAsyncJob"/></summary>
    public abstract class AsyncJob : IAsyncJob
    {
        bool m_OnCompleteLaunched;
        Action<IAsyncJob> m_OnComplete;

        /// <inheritdoc cref="IAsyncJob"/>
        public float progress { get; private set; }
        /// <inheritdoc cref="IAsyncJob"/>
        public string message { get; private set; }
        /// <inheritdoc cref="IAsyncJob"/>
        public abstract bool Tick();

        /// <inheritdoc cref="IAsyncJob"/>
        public abstract void Cancel();

        /// <inheritdoc cref="IAsyncJob"/>
        public void OnComplete(Action<IAsyncJob> action)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            if (m_OnCompleteLaunched)
                action(this);
            else
                m_OnComplete += action;
        }

        /// <summary>Set the progress of this job.</summary>
        /// <param name="progressArg">
        /// The progress of the job, it will be min maxed into range [0-1].
        /// If it is equal to <c>1.0f</c>, then job will be considered as completed.</param>
        /// <param name="messageArg">A descriptive message indicating the current job operation.</param>
        public void SetProgress(float progressArg, string messageArg)
        {
            progressArg = Math.Max(0, progressArg);
            progressArg = Math.Min(1, progressArg);

            progress = progressArg;
            message = messageArg;

            if (progressArg >= 1 && !m_OnCompleteLaunched)
            {
                m_OnCompleteLaunched = true;
                m_OnComplete?.Invoke(this);
            }
        }
    }
}
