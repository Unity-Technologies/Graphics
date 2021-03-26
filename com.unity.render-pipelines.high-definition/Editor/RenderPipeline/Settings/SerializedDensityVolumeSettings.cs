using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    class SerializedDensityVolumeSettings
    {
        internal SerializedProperty root;

        internal SerializedProperty atlasResolution;

        internal SerializedDensityVolumeSettings(SerializedProperty root)
        {
            this.root = root;

            atlasResolution = root.Find((DensityVolumeSettings s) => s.atlasResolution);
        }
    }
}
