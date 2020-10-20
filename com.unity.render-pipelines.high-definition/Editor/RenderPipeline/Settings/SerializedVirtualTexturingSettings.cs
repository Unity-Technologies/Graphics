using UnityEditor.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    internal sealed class SerializedVirtualTexturingSettings
    {
        public SerializedProperty root;

        public SerializedProperty streamingCpuCacheSizeInMegaBytes;
        public SerializedProperty streamingGpuCacheSettings;
        public SerializedVirtualTexturingSettings(SerializedProperty root)
        {
            this.root = root;

            streamingCpuCacheSizeInMegaBytes = root.Find((VirtualTexturingSettingsSRP s) => s.streamingCpuCacheSizeInMegaBytes);
            streamingGpuCacheSettings = root.Find((VirtualTexturingSettingsSRP s) => s.streamingGpuCacheSettings);
        }
    }
}
