using UnityEditor.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    class SerializedLowResTransparencySettings
    {
        public SerializedProperty root;

        public SerializedProperty enabled;
        public SerializedProperty checkerboardDepthBuffer;
        public SerializedProperty upsampleType;

        public SerializedLowResTransparencySettings(SerializedProperty root)
        {
            this.root = root;

            enabled                     = root.Find((GlobalLowResolutionTransparencySettings s) => s.enabled);
            checkerboardDepthBuffer     = root.Find((GlobalLowResolutionTransparencySettings s) => s.checkerboardDepthBuffer);
            upsampleType                = root.Find((GlobalLowResolutionTransparencySettings s) => s.upsampleType);
        }
    }
}
