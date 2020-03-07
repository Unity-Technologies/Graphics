using UnityEditor.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    class SerializedGlobalProbeVolumeSettings
    {
        public SerializedProperty root;

        public SerializedProperty atlasWidth;
        public SerializedProperty atlasHeight;
        public SerializedProperty atlasDepth;
        public SerializedProperty atlasOctahedralDepthWidth;
        public SerializedProperty atlasOctahedralDepthHeight;

        public SerializedGlobalProbeVolumeSettings(SerializedProperty root)
        {
            this.root = root;

            atlasWidth = root.Find((GlobalProbeVolumeSettings s) => s.atlasWidth);
            atlasHeight = root.Find((GlobalProbeVolumeSettings s) => s.atlasHeight);
            atlasDepth = root.Find((GlobalProbeVolumeSettings s) => s.atlasDepth);
            atlasOctahedralDepthWidth = root.Find((GlobalProbeVolumeSettings s) => s.atlasOctahedralDepthWidth);
            atlasOctahedralDepthHeight = root.Find((GlobalProbeVolumeSettings s) => s.atlasOctahedralDepthHeight);
        }
    }
}
