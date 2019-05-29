using UnityEditor.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
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
