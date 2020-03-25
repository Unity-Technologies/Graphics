using System;

namespace UnityEditor.ShaderAnalysis
{
    /// <summary>Base implementation of <see cref="IAsyncJob"/></summary>
    public abstract class AsyncJob : IAsyncJob
    {
        int m_TaskId;
        bool m_OnCompleteLaunched;
        bool m_IsCancelled = false;
        Action<IAsyncJob> m_OnComplete;

        public abstract string name { get; }
        /// <inheritdoc cref="IAsyncJob"/>
        public float progress { get; private set; }
        /// <inheritdoc cref="IAsyncJob"/>
        public string message { get; private set; }

        /// <inheritdoc cref="IAsyncJob"/>
        public bool Tick()
        {
#if UNITY_2020_1_OR_NEWER
            if (m_TaskId == 0)
            {
                m_TaskId = Progress.Start(name, message);
                Progress.RegisterCancelCallback(m_TaskId, CancelCallback);
            }
#endif
            return Internal_Tick();
        }
        protected abstract bool Internal_Tick();

        /// <inheritdoc cref="IAsyncJob"/>
        public void Cancel()
        {
#if UNITY_2020_1_OR_NEWER
            m_IsCancelled = true;
            if (m_TaskId != 0)
                Progress.Cancel(m_TaskId);
            else
#endif
                Internal_Cancel();
        }
        protected abstract void Internal_Cancel();

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

#if UNITY_2020_1_OR_NEWER
            if (m_TaskId != 0)
                Progress.Report(m_TaskId, progress, message);
#endif

            if (progressArg >= 1 && !m_OnCompleteLaunched)
            {
                m_OnCompleteLaunched = true;
                m_OnComplete?.Invoke(this);

#if UNITY_2020_1_OR_NEWER
                // Don't remove task when cancelling
                if (m_TaskId != 0 && !m_IsCancelled)
                {
                    Progress.Remove(m_TaskId);
                    m_TaskId = 0;
                }
#endif
            }
        }

        bool CancelCallback()
        {
            m_IsCancelled = true;
            Internal_Cancel();
            return true;
        }
    }
}
