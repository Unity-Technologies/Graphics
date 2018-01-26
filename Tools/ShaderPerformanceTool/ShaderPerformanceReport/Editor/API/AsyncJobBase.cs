using System;
using UnityEngine.Assertions;

namespace UnityEditor.Experimental.ShaderTools
{
    public abstract class AsyncJobBase : IAsyncJob
    {
        bool m_OnCompleteLaunched = false;
        Action<IAsyncJob> m_OnComplete = null;

        public float progress { get; private set; }
        public string message { get; private set; }
        public abstract bool Tick();

        public abstract void Cancel();
        public void OnComplete(Action<IAsyncJob> action)
        {
            Assert.IsNotNull(action);

            if (m_OnCompleteLaunched)
                action(this);
            else
                m_OnComplete += action;
        }

        protected void SetProgress(float progress, string message)
        {
            this.progress = progress;
            this.message = message;

            if (progress >= 1 && !m_OnCompleteLaunched)
            {
                m_OnCompleteLaunched = true;
                m_OnComplete(this);
            }
        }
    }
}
