using UnityEditor.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
#if ENABLE_VIRTUALTEXTURES
    public sealed class SerializedVirtualTexturingSettings
    {
        public SerializedProperty root;

        public SerializedProperty cpuCacheSizeInMegaBytes;
        public SerializedProperty gpuCacheSizeInMegaBytes;
        public SerializedProperty gpuCacheSizeOverridesStreaming;

        public SerializedVirtualTexturingSettings(SerializedProperty root)
        {
            this.root = root;

            cpuCacheSizeInMegaBytes = root.Find((VirtualTexturingSettings s) => s.cpuCacheSizeInMegaBytes);
            gpuCacheSizeInMegaBytes = root.Find((VirtualTexturingSettings s) => s.gpuCacheSizeInMegaBytes);
            gpuCacheSizeOverridesStreaming = root.Find((VirtualTexturingSettings s) => s.gpuCacheSizeOverridesStreaming);
        }
    }
#endif
}
