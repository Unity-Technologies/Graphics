using UnityEditor.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    class SerializedGlobalProbeVolumeSettings
    {
        internal SerializedProperty root;

        internal SerializedProperty atlasWidth;
        internal SerializedProperty atlasHeight;
        internal SerializedProperty atlasDepth;
        internal SerializedProperty atlasOctahedralDepthWidth;
        internal SerializedProperty atlasOctahedralDepthHeight;

        internal SerializedGlobalProbeVolumeSettings(SerializedProperty root)
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
