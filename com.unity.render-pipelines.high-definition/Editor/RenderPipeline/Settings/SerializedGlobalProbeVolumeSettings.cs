using UnityEditor.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    class SerializedGlobalProbeVolumeSettings
    {
        internal SerializedProperty root;

        internal SerializedProperty atlasResolution;
        internal SerializedProperty atlasOctahedralDepthResolution;

        internal SerializedGlobalProbeVolumeSettings(SerializedProperty root)
        {
            this.root = root;

            atlasResolution = root.Find((GlobalProbeVolumeSettings s) => s.atlasResolution);
            atlasOctahedralDepthResolution = root.Find((GlobalProbeVolumeSettings s) => s.atlasOctahedralDepthResolution);
        }
    }
}
