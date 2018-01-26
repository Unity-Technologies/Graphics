using System;

namespace UnityEditor.Experimental.ShaderTools
{
    public interface IAsyncJob
    {
        float progress { get; }
        string message { get; }

        bool Tick();
        void Cancel();

        void OnComplete(Action<IAsyncJob> action);
    }

    public static class IAsyncJobExtensions
    {
        public static bool IsComplete(this IAsyncJob job)
        {
            return job.progress >= 1;
        }
    }
}
