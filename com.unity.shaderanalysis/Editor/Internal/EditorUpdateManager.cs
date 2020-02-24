using System;

namespace UnityEditor.ShaderAnalysis.Internal
{
    // Call update at regular interval, even when using executeMethod on the command line
    public static class EditorUpdateManager
    {
        public static Action ToUpdate;

        [InitializeOnLoadMethod]
        static void Register()
        {
            EditorApplication.update += Tick;
        }

        // Call this to tick
        public static void Tick() => ToUpdate?.Invoke();
    }
}
