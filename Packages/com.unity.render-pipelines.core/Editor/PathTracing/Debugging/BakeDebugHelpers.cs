using System;
using UnityEditor;
using UnityEngine.Experimental.Rendering;

namespace UnityEditor.PathTracing.Debugging
{
    internal sealed class BakeProfilingScope : IDisposable
    {
        private readonly bool _shouldCapture;

        private static bool IsFullBakeCaptureEnabled()
        {
            if (!Unsupported.IsDeveloperMode())
                return false;

            var prefSetting = new SavedBool("LightingSettings.CaptureBakeForProfiling", false);
            return prefSetting.value;
        }

        public BakeProfilingScope(bool shouldCapture)
        {
            _shouldCapture = shouldCapture;

            if (_shouldCapture)
            {
                ExternalGPUProfiler.BeginGPUCapture();
            }
        }

        public BakeProfilingScope()
            : this(IsFullBakeCaptureEnabled())
        {
        }

        public void Dispose()
        {
            if (_shouldCapture)
            {
                ExternalGPUProfiler.EndGPUCapture();
            }
        }
    }
}
