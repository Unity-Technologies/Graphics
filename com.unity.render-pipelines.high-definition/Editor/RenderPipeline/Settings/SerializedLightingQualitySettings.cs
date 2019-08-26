using UnityEditor.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    class SerializedLightingQualitySettings
    {
        public SerializedProperty root;

        public SerializedLightingQualitySettings(SerializedProperty root)
        {
            this.root = root;
        }
    }
}
