using UnityEditor.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    class SerializedGlobalProbeVolumeSettings
    {
        public SerializedProperty root;

        public SerializedProperty atlasWidth;
        public SerializedProperty atlasHeight;

        public SerializedGlobalProbeVolumeSettings(SerializedProperty root)
        {
            this.root = root;

            atlasWidth = root.Find((GlobalProbeVolumeSettings s) => s.atlasWidth);
            atlasHeight = root.Find((GlobalProbeVolumeSettings s) => s.atlasHeight);
        }
    }
}
