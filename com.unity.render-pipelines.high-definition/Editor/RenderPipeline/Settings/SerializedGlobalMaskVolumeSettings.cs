using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    class SerializedGlobalMaskVolumeSettings
    {
        internal SerializedProperty root;

        internal SerializedProperty atlasResolution;

        internal SerializedGlobalMaskVolumeSettings(SerializedProperty root)
        {
            this.root = root;

            atlasResolution = root.Find((GlobalMaskVolumeSettings s) => s.atlasResolution);
        }
    }
}
